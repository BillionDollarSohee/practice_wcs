using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using ProtocolCore;

namespace PlcSimulator
{
    //   Command 03 : 설비 -> 서버, 대차정보 요청
    //   Command 01 : 서버 -> 설비, 대차정보 응답 (설비는 이 응답을 기다림)
    //   Command 11 : 설비 -> 서버, 검사완료
    //
    // 실제 스펙(STX/ETX + 고정길이 필드)을 그대로 따른다.
    // 미니 프로젝트라 BatchNo/BodyNo/CarType/Item 상세 데이터는 비워서 보내지만,
    // 자리(바이트 수)는 실제 스펙과 동일하게 유지한다.
    class Program
    {
        private const string ServerIp = "127.0.0.1";
        private const int ServerPort = 9000;

        private static readonly string[] CartIdList = { "CART_0001", "CART_0002", "CART_0003", "CART_0004", "CART_0005", "CART_0006" };

        static void Main(string[] args)
        {
            Console.WriteLine("비전 설비 시뮬레이터 시작 (STX/ETX 프로토콜)");
            Console.WriteLine($"접속 대상: {ServerIp}:{ServerPort}");

            while (true)
            {
                try
                {
                    using (TcpClient client = new TcpClient())
                    {
                        client.Connect(ServerIp, ServerPort);
                        Console.WriteLine("서버 접속 성공");

                        using (NetworkStream stream = client.GetStream())
                        {
                            while (client.Connected)
                            {
                                RunOneCartCycle(stream);
                                Thread.Sleep(5000);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"접속 실패: {ex.Message}");
                    Thread.Sleep(3000);
                }
            }
        }

        // 대차 1대에 대한 전체 검사 사이클 진행
        // 요청(03) -> 응답대기(01) -> 완료(11) 순서로 진행
        private static void RunOneCartCycle(NetworkStream stream)
        {
            string cartId = PickRandomCartId();

            // Command 03 - 대차정보요청 (CarID 20바이트 고정)
            byte[] requestFrame = ProtocolCodec.BuildFrame("03", ProtocolCodec.PadField(cartId, 20));
            SendFrame(stream, requestFrame);

            // Command 01 - 서버 응답 대기
            byte[] responseFrame = ReceiveFrame(stream);
            if (responseFrame == null)
            {
                Console.WriteLine("응답 수신 실패 - 이번 사이클 종료");
                return;
            }

            // 응답 필드 순서: CarID, BatchNo, BodyNo, CarType, LineType, ItemCount, Item, Result, ErrorCode
            var (command, fields) = ProtocolCodec.ParseFrame(responseFrame);
            string lineType = fields.Length > 4 ? fields[4].Trim() : "";
            string result = fields.Length > 7 ? fields[7].Trim() : "";
            string errorCode = fields.Length > 8 ? fields[8].Trim() : "";
            Console.WriteLine($"응답수신 - Command:{command}, LineType:{lineType}, Result:{result}, ErrorCode:{errorCode}");

            if (result != "1")
            {
                Console.WriteLine("대차정보 응답 비정상 - 검사완료 전송 생략");
                return;
            }

            // 검사 진행 시뮬레이션
            Thread.Sleep(1000);

            // Command 11 - 검사완료
            // 필드 순서: CarID, ItemLength, BatchNo, BodyNo, CarType, LineType,
            //           년월일시분초, Item개수, Item, ImgPath, 하부대차ImgFile,
            //           하부대차판정결과, 비전강제완료여부
            string judgment = GenerateRandomResult() == "OK" ? "1" : "0";
            string dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            byte[] completeFrame = ProtocolCodec.BuildFrame(
                "11",
                ProtocolCodec.PadField(cartId, 20),
                ProtocolCodec.PadField("0000", 4),
                ProtocolCodec.PadField("", 12),
                ProtocolCodec.PadField("", 10),
                ProtocolCodec.PadField("", 4),
                ProtocolCodec.PadField(lineType, 2),
                ProtocolCodec.PadField(dateTime, 14),
                ProtocolCodec.PadField("0000", 4),
                "",
                "",
                "",
                judgment,
                "0"
            );
            SendFrame(stream, completeFrame);
            Console.WriteLine($"검사완료 전송 - CartId:{cartId}, Judgment:{(judgment == "1" ? "OK" : "NG")}");
        }

        private static string PickRandomCartId()
        {
            Random random = new Random();
            int index = random.Next(0, CartIdList.Length);
            return CartIdList[index];
        }

        private static string GenerateRandomResult()
        {
            Random random = new Random();
            return random.Next(0, 10) < 8 ? "OK" : "NG";
        }

        private static void SendFrame(NetworkStream stream, byte[] frame)
        {
            stream.Write(frame, 0, frame.Length);
            Console.WriteLine($"전송 (bytes: {frame.Length})");
        }

        // STX~ETX 프레임 하나가 완성될 때까지 읽어서 반환
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
}