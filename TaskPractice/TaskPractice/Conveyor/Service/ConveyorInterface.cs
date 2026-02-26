using System.Net.Sockets;
using TaskPractice.Conveyor.Interface;
using TaskPractice.Model;

namespace TaskPractice.Conveyor.Service
{
    // PLC 소켓 통신 담당 (TCP 단순화 버전)
    public class ConveyorInterface
    {
        private readonly IConveyorProvider _conveyorProvider;

        private readonly string _pollingGroup;
        private readonly string _ipAddr;
        private readonly int _port;

        // 설비ID → 이전 바이트 (변경 감지용)
        private Dictionary<string, byte[]> _prevBytesDict = new Dictionary<string, byte[]>();

        // 명령 큐 (ServiceManager에서 Enqueue, 여기서 Dequeue)
        public Queue<ConveyorCommand> CommandQueue = new Queue<ConveyorCommand>();

        // 트랙당 바이트 크기 (10 Word = 20 Byte)
        private const int TRACK_BYTE_SIZE = 20;

        // 폴링 주기 (ms)
        private const int POLLING_INTERVAL = 100;

        private Timer _interfaceThread = null;
        private bool _threadStopSwitch = false;

        // 폴링그룹 소속 설비 목록
        private List<ConveyorUnit> _conveyorUnits = new List<ConveyorUnit>();

        public ConveyorInterface(
            IConveyorProvider conveyorProvider,
            string pollingGroup,
            string ipAddr,
            int port,
            List<ConveyorUnit> conveyorUnits)
        {
            _conveyorProvider = conveyorProvider;
            _pollingGroup = pollingGroup;
            _ipAddr = ipAddr;
            _port = port;
            _conveyorUnits = conveyorUnits;

            // 이전 바이트 딕셔너리 초기화
            foreach (var unit in _conveyorUnits)
            {
                _prevBytesDict[unit.EqpId] = new byte[TRACK_BYTE_SIZE];
            }
        }

        public void Start()
        {
            if (_interfaceThread == null)
            {
                _threadStopSwitch = false;
                _interfaceThread = new Timer(PlcLogic);
                _interfaceThread.Change(0, Timeout.Infinite);

                Console.WriteLine($"ConveyorInterface Start - PollingGroup: {_pollingGroup}");
            }
        }

        public void Stop()
        {
            _threadStopSwitch = true;
        }

        private void PlcLogic(object? state)
        {
            // 타이머 중지
            _interfaceThread.Change(Timeout.Infinite, Timeout.Infinite);

            try
            {
                // 1. PLC 읽기 → DB 업데이트
                ReadPlcStatusAndUpdateDb();

                // 2. 명령 큐 → PLC 쓰기
                WriteCvCommand();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (!_threadStopSwitch)
                {
                    _interfaceThread.Change(POLLING_INTERVAL, Timeout.Infinite);
                }
                else
                {
                    _interfaceThread.Dispose();
                    _interfaceThread = null;

                    Console.WriteLine($"ConveyorInterface Stop - PollingGroup: {_pollingGroup}");
                }
            }
        }

        private void ReadPlcStatusAndUpdateDb()
        {
            int totalBytes = _conveyorUnits.Count * TRACK_BYTE_SIZE;

            // 매번 새 연결로 READ (연결 유지 없이 단순화)
            using var client = new TcpClient();
            client.Connect(_ipAddr, _port);
            using var stream = client.GetStream();

            // 읽기 요청 전송
            byte[] request = System.Text.Encoding.UTF8.GetBytes($"READ {totalBytes}\n");
            stream.Write(request, 0, request.Length);

            // 응답 수신
            byte[] responseBuffer = new byte[totalBytes];
            int offset = 0;
            while (offset < totalBytes)
            {
                int read = stream.Read(responseBuffer, offset, totalBytes - offset);
                if (read == 0) break;
                offset += read;
            }

            if (offset != totalBytes)
                return;

            // 트랙별로 잘라서 처리
            for (int i = 0; i < _conveyorUnits.Count; i++)
            {
                var unit = _conveyorUnits[i];

                byte[] trackBytes = new byte[TRACK_BYTE_SIZE];
                Array.Copy(responseBuffer, i * TRACK_BYTE_SIZE, trackBytes, 0, TRACK_BYTE_SIZE);

                // 이전 바이트와 비교 - 변경된 경우만 DB 업데이트
                if (_prevBytesDict[unit.EqpId].SequenceEqual(trackBytes))
                    continue;

                // PLC 읽기영역 파싱 (바이트 12~13: 알람, 14~15: 센서)
                string alarmCode = BitConverter.ToInt16(trackBytes, 12).ToString();
                string sensorStatus = BitConverter.ToInt16(trackBytes, 14).ToString();

                _conveyorProvider.UpdateConveyorStatus(unit.EqpId, alarmCode, sensorStatus);

                _prevBytesDict[unit.EqpId] = trackBytes;

                Console.WriteLine($"상태 변경 감지 - EqpId: {unit.EqpId}, Alarm: {alarmCode}, Sensor: {sensorStatus}");
            }
        }

        private void WriteCvCommand()
        {
            if (CommandQueue.Count <= 0)
                return;

            var command = CommandQueue.Dequeue();

            try
            {
                var unit = _conveyorUnits.FirstOrDefault(w => w.EqpId == command.EqpId);
                if (unit == null)
                    return;

                // 쓰기 영역 바이트 구성
                byte[] writeBytes = new byte[TRACK_BYTE_SIZE];

                // 작업번호 (바이트 0~1)
                byte[] cmdNoBytes = BitConverter.GetBytes((short)(command.EqpCmdNo.GetHashCode() & 0x7FFF));
                Array.Copy(cmdNoBytes, 0, writeBytes, 0, 2);

                // 목적지 (바이트 2~3)
                byte[] destBytes = BitConverter.GetBytes((short)command.DestPlcEqpNo);
                Array.Copy(destBytes, 0, writeBytes, 2, 2);

                string hexData = Convert.ToHexString(writeBytes);

                // 매번 새 연결로 WRITE
                using var client = new TcpClient();
                client.Connect(_ipAddr, _port);
                using var stream = client.GetStream();
                using var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
                using var writer = new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

                // WRITE 요청
                writer.WriteLine($"WRITE {unit.CvTrackNo} {hexData}");

                // OK 응답 대기
                string response = reader.ReadLine()?.Trim();

                if (response == "OK")
                {
                    _conveyorProvider.UpdateCommandSendStatus(command.EqpId, "COMPLETE");
                    Console.WriteLine($"명령 전송 성공 - EqpId: {command.EqpId}, Dest: {command.DestPlcEqpNo}");
                }
                else
                {
                    _conveyorProvider.UpdateCommandSendStatus(command.EqpId, "FAIL");
                    Console.WriteLine($"명령 전송 실패 - EqpId: {command.EqpId}");
                }

                // 쓰기 바이트 초기화
                writer.WriteLine($"WRITE {unit.CvTrackNo} {new string('0', TRACK_BYTE_SIZE * 2)}");
            }
            catch (Exception ex)
            {
                _conveyorProvider.UpdateCommandSendStatus(command.EqpId, "FAIL");
                Console.WriteLine(ex.Message);
            }
        }
    }
}