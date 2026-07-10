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

        private static void RunOneCartCycle(NetworkStream stream)
        {
            string cartId = PickRandomCartId();

            byte[] requestFrame = ProtocolCodec.BuildFrame("03", ProtocolCodec.PadField(cartId, 20));
            SendFrame(stream, requestFrame);

            byte[] responseFrame = ReceiveFrame(stream);
            if (responseFrame == null)
            {
                Console.WriteLine("응답 수신 실패 - 이번 사이클 종료");
                return;
            }

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

            Thread.Sleep(1000);

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