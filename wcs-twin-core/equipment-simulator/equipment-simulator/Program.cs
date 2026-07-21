using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using ProtocolCore;

namespace PlcSimulator
{
    //   Command 03 : 설비 -> 서버, 대차정보 요청
    //   Command 01 : 서버 -> 서버, 대차정보 응답 (설비는 이 응답을 기다림)
    //   Command 11 : 설비 -> 서버, 검사완료
    //
    // 실제 공장에서는 라인(트림/파이널/도어)마다 물리적으로 별도의 비전 카메라가 있고,
    // 카메라는 자기 라인에 배정된 대차만 한 번에 1대씩 순서대로 처리한다.
    // 그래서 이 시뮬레이터도 시작할 때 CART_MASTER를 조회해서 대차를 라인별로 나눠두고,
    // 라인당 전용 워커 1개가 자기 라인 대차만 순차적으로(병렬 없이) 처리하게 만든다.
    // 이러면 "같은 라인 대차 2대가 동시에 검사대에 들어가는" 상황이 구조적으로 생길 수 없어서,
    // WCS(eis-socket-service) 쪽에 별도의 동시성 제어 게이트가 필요 없어진다.
    //
    // 또한 트레일러 한 대는 트림+파이널+도어가 한 세트로 실려나가야 하므로, 출고된 대차는 곧바로
    // 타공장으로 보내지 않고 "출고 도크"에서 자기 라인 파트너들을 기다렸다가 세트가 완성되면
    // 셋이 동시에 타공장으로 출발한다(TrailerLoadCoordinator).
    class Program
    {
        private const string ServerIp = "127.0.0.1";
        private const int ServerPort = 9000;

        // eis-socket-service/appsettings.json의 ConnectionStrings:WcsTwin과 반드시 동일해야 한다.
        private const string DbConnectionString =
            "Server=127.0.0.1;Port=3307;Database=wcs_twin;User=root;Password=1234;CharSet=utf8mb4;SslMode=None;";

        private static readonly string[] LineTypes = { "TR", "FL", "DR" };
        private static readonly Random _random = new Random();

        // RFID 재시도(최대 4회, 3초 간격)까지 감안한 여유 시간 - 이보다 오래 걸리면(예: 알람 대기,
        // 서버쪽 예외로 응답 유실) 포기하고 다음 대차로 넘어간다. 한 라인이 영원히 멈추는 걸 막는 안전장치.
        private const int ResponseTimeoutMs = 30000;

        static async Task Main(string[] args)
        {
            Console.WriteLine("비전 설비 시뮬레이터 시작 (라인 전용 카메라 3대, STX/ETX 프로토콜)");

            Dictionary<string, List<string>> cartIdsByLine = await LoadCartIdsByLineAsync();

            // 라인 워커 3개가 공유하는 상태 - 그래서 전부 스레드 안전한 자료구조를 쓴다.
            var cooldownUntil = new ConcurrentDictionary<string, DateTime>();
            var waitingAtDock = new ConcurrentDictionary<string, byte>();

            var coordinator = new TrailerLoadCoordinator((departLineType, departCartId) =>
            {
                waitingAtDock.TryRemove(departCartId, out _);
                int cooldownSeconds = _random.Next(60, 181);
                cooldownUntil[departCartId] = DateTime.Now.AddSeconds(cooldownSeconds);
                Console.WriteLine($"[{departLineType}][타공장 출발] CartId:{departCartId}, {cooldownSeconds}초 후 재입고 가능");
                SaveOffsiteStatus(departLineType, departCartId);
            });

            var workers = new List<Task>();
            foreach (var lineType in LineTypes)
            {
                var cartIds = cartIdsByLine.TryGetValue(lineType, out var list) ? list : new List<string>();
                Console.WriteLine($"[{lineType}] 담당 대차 {cartIds.Count}대");
                workers.Add(Task.Run(() => RunLineWorkerLoop(lineType, cartIds, cooldownUntil, waitingAtDock, coordinator)));
            }

            await Task.WhenAll(workers);
        }

        // CART_MASTER를 라인타입 기준으로 조회해서 워커별 담당 대차 목록을 미리 나눈다.
        private static async Task<Dictionary<string, List<string>>> LoadCartIdsByLineAsync()
        {
            var options = new DbContextOptionsBuilder<WcsTwinContext>()
                .UseMySql(DbConnectionString, ServerVersion.AutoDetect(DbConnectionString))
                .Options;

            using var context = new WcsTwinContext(options);
            List<CartMaster> carts = await context.CartMasters.ToListAsync();

            return carts
                .GroupBy(c => c.LineType)
                .ToDictionary(g => g.Key, g => g.Select(c => c.CartId).ToList());
        }

        // 라인 하나를 전담하는 워커 - 자기 라인 대차만, 병렬 없이 한 번에 1대씩 순서대로 처리한다.
        private static void RunLineWorkerLoop(
            string lineType,
            List<string> cartIds,
            ConcurrentDictionary<string, DateTime> cooldownUntil,
            ConcurrentDictionary<string, byte> waitingAtDock,
            TrailerLoadCoordinator coordinator)
        {
            if (cartIds.Count == 0)
            {
                Console.WriteLine($"[{lineType}] 담당 대차가 없어서 워커를 시작하지 않음 (CART_MASTER.LINE_TYPE 확인 필요)");
                return;
            }

            int cursor = 0;

            while (true)
            {
                string cartId = PickNextAvailableCart(cartIds, cooldownUntil, waitingAtDock, ref cursor);
                if (cartId == null)
                {
                    // 담당 대차가 전부 쿨다운/도크 대기 중 - 잠깐 대기 후 재시도
                    Thread.Sleep(2000);
                    continue;
                }

                bool discharged = false;
                try
                {
                    using (TcpClient client = new TcpClient())
                    {
                        client.Connect(ServerIp, ServerPort);
                        using (NetworkStream stream = client.GetStream())
                        {
                            // 응답(Command 01 / Command 11 ACK)을 이 시간 안에 못 받으면 포기하고 다음 대차로 넘어간다.
                            // 재시도(최대 4회, 3초 간격)까지는 정상적으로 걸릴 수 있는 시간이라 여유 있게 잡는다 -
                            // 이게 없으면 서버 쪽에서 예외가 나서 응답을 못 보내는 경우 그 라인 워커가 영원히 멈춘다.
                            stream.ReadTimeout = ResponseTimeoutMs;
                            discharged = RunOneCartCycle(lineType, cartId, stream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{lineType}] 접속/처리 실패 - CartId:{cartId}, {ex.Message}");
                }

                if (discharged)
                {
                    // 출고 성공(OK) - 곧바로 타공장으로 보내지 않고 도크에서 트림/파이널/도어 세트가
                    // 다 찰 때까지 대기한다. 세트가 완성되면 coordinator가 셋을 동시에 타공장으로 보낸다.
                    waitingAtDock[cartId] = 0;
                    Console.WriteLine($"[{lineType}] 출고 도크 대기 - CartId:{cartId} (트레일러 세트 완성 대기중)");
                    coordinator.ArriveAtDock(lineType, cartId);
                }
                else
                {
                    // NG였거나 처리에 실패한 경우 - 대차가 실제로 어디 실려나간 게 아니므로 타공장에 보내지 않고
                    // 그냥 잠깐 쉬었다가 다시 시도한다.
                    int cooldownSeconds = _random.Next(60, 181);
                    cooldownUntil[cartId] = DateTime.Now.AddSeconds(cooldownSeconds);
                    Console.WriteLine($"[{lineType}][쿨다운] CartId:{cartId}, {cooldownSeconds}초 후 재시도 가능");
                }

                Thread.Sleep(500);
            }
        }

        // 대차가 타공장으로 출발했음을 기록한다.
        // 이 기록이 없으면 화면에서 "출고 직후(도크 대기중)"와 "재입고 대기중"을 구분할 수 없다.
        private static void SaveOffsiteStatus(string lineType, string cartId)
        {
            try
            {
                var options = new DbContextOptionsBuilder<WcsTwinContext>()
                    .UseMySql(DbConnectionString, ServerVersion.AutoDetect(DbConnectionString))
                    .Options;

                using var context = new WcsTwinContext(options);
                context.EquipmentStatusHists.Add(new EquipmentStatusHist
                {
                    EqpId = $"CART.{lineType}",
                    StatusType = "CART_OFFSITE",
                    CartId = cartId,
                    Status = "AWAY",
                    ResultJson = "타공장 이동중 - 재입고 대기",
                    CreateDttm = DateTime.Now
                });
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{lineType}] 타공장 상태 기록 실패 - CartId:{cartId}, {ex.Message}");
            }
        }

        // 담당 대차 목록을 순서대로 돌면서(라운드로빈) 쿨다운 중이거나 출고 도크에서 대기 중인 대차는 건너뛴다.
        private static string PickNextAvailableCart(
            List<string> cartIds,
            ConcurrentDictionary<string, DateTime> cooldownUntil,
            ConcurrentDictionary<string, byte> waitingAtDock,
            ref int cursor)
        {
            DateTime now = DateTime.Now;
            for (int i = 0; i < cartIds.Count; i++)
            {
                int idx = (cursor + i) % cartIds.Count;
                string candidate = cartIds[idx];

                if (waitingAtDock.ContainsKey(candidate)) continue;
                if (cooldownUntil.TryGetValue(candidate, out DateTime availableAt) && availableAt > now) continue;

                cursor = (idx + 1) % cartIds.Count;
                return candidate;
            }

            return null;
        }

        // 대차 1대에 대한 전체 검사 사이클 진행. 실제로 출고(OK 판정 + ACK 확인)까지 끝났으면 true.
        private static bool RunOneCartCycle(string lineType, string cartId, NetworkStream stream)
        {
            byte[] requestFrame = ProtocolCodec.BuildFrame("03", ProtocolCodec.PadField(cartId, 20));
            SendFrame(lineType, cartId, stream, requestFrame);

            byte[] responseFrame = ReceiveFrame(stream);
            if (responseFrame == null)
            {
                Console.WriteLine($"[{lineType}] 응답 수신 실패 - CartId:{cartId}");
                return false;
            }

            var (command, fields) = ProtocolCodec.ParseFrame(responseFrame);
            string responseLineType = fields.Length > 4 ? fields[4].Trim() : "";
            string result = fields.Length > 7 ? fields[7].Trim() : "";
            string errorCode = fields.Length > 8 ? fields[8].Trim() : "";
            Console.WriteLine($"[{lineType}] 응답수신 - CartId:{cartId}, Command:{command}, Result:{result}, ErrorCode:{errorCode}");

            if (result != "1")
            {
                Console.WriteLine($"[{lineType}] 대차정보 응답 비정상 - CartId:{cartId}, 검사완료 전송 생략");
                return false;
            }

            Thread.Sleep(1000); // 검사 진행 시뮬레이션

            string judgment = GenerateRandomResult() == "OK" ? "1" : "0";
            string dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            byte[] completeFrame = ProtocolCodec.BuildFrame(
                "11",
                ProtocolCodec.PadField(cartId, 20),
                ProtocolCodec.PadField("0000", 4),
                ProtocolCodec.PadField("", 12),
                ProtocolCodec.PadField("", 10),
                ProtocolCodec.PadField("", 4),
                ProtocolCodec.PadField(responseLineType, 2),
                ProtocolCodec.PadField(dateTime, 14),
                ProtocolCodec.PadField("0000", 4),
                "",
                "",
                "",
                judgment,
                "0"
            );
            SendFrame(lineType, cartId, stream, completeFrame);
            Console.WriteLine($"[{lineType}] 검사완료 전송 - CartId:{cartId}, Judgment:{(judgment == "1" ? "OK" : "NG")}");

            // WCS의 ACK를 받을 때까지 기다린다 - RFID 쓰기는 재시도/알람 때문에 오래 걸릴 수 있는데,
            // 이걸 안 기다리고 다음 대차로 넘어가면 같은 라인에 대차가 겹쳐 들어가게 된다.
            byte[] ackFrame = ReceiveFrame(stream);
            if (ackFrame == null)
            {
                Console.WriteLine($"[{lineType}] 검사완료 ACK 수신 실패 - CartId:{cartId}");
                return false;
            }

            Console.WriteLine($"[{lineType}] 검사완료 ACK 수신 - CartId:{cartId}");
            return judgment == "1";
        }

        private static string GenerateRandomResult()
        {
            return _random.Next(0, 10) < 8 ? "OK" : "NG";
        }

        private static void SendFrame(string lineType, string cartId, NetworkStream stream, byte[] frame)
        {
            stream.Write(frame, 0, frame.Length);
            Console.WriteLine($"[{lineType}] 전송 - CartId:{cartId} (bytes: {frame.Length})");
        }

        private static byte[] ReceiveFrame(NetworkStream stream)
        {
            List<byte> buffer = new List<byte>();
            byte[] readBuffer = new byte[1024];

            while (true)
            {
                int readCount = stream.Read(readBuffer, 0, readBuffer.Length);
                if (readCount == 0) return null;

                buffer.AddRange(new ArraySegment<byte>(readBuffer, 0, readCount));

                byte[] frame = ProtocolCodec.ExtractFrame(buffer);
                if (frame != null) return frame;
            }
        }
    }

    // 출고 도크에서 트림/파이널/도어 대차가 각각 1대 이상 모이면 그 순간 한 세트로 묶어서
    // 셋을 동시에 타공장으로 출발시킨다 (트레일러 한 대 = 트림+파이널+도어 세트).
    // 3개 라인 워커 스레드가 동시에 호출할 수 있으므로 내부적으로 락을 쓴다.
    class TrailerLoadCoordinator
    {
        private readonly Dictionary<string, Queue<string>> _dock = new()
        {
            ["TR"] = new Queue<string>(),
            ["FL"] = new Queue<string>(),
            ["DR"] = new Queue<string>()
        };
        private readonly object _lock = new object();
        private readonly Action<string, string> _onDeparture;

        public TrailerLoadCoordinator(Action<string, string> onDeparture)
        {
            _onDeparture = onDeparture;
        }

        public void ArriveAtDock(string lineType, string cartId)
        {
            lock (_lock)
            {
                _dock[lineType].Enqueue(cartId);
                Console.WriteLine($"[출고도크] TR:{_dock["TR"].Count} FL:{_dock["FL"].Count} DR:{_dock["DR"].Count}");

                while (_dock["TR"].Count > 0 && _dock["FL"].Count > 0 && _dock["DR"].Count > 0)
                {
                    string tr = _dock["TR"].Dequeue();
                    string fl = _dock["FL"].Dequeue();
                    string dr = _dock["DR"].Dequeue();
                    Console.WriteLine($"[트레일러 출발] 세트 완성 - TR:{tr}, FL:{fl}, DR:{dr}");
                    _onDeparture("TR", tr);
                    _onDeparture("FL", fl);
                    _onDeparture("DR", dr);
                }
            }
        }
    }
}
