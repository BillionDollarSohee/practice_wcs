using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TaskPractice.Conveyor.Service
{
    // PLC 역할을 하는 Mock TCP 서버
    public class MockPlcServer
    {
        private readonly int _port;
        private TcpListener _listener = null;
        private bool _stopSwitch = false;

        // 트랙번호 → 현재 메모리 바이트 (PLC 메모리맵 흉내)
        private Dictionary<int, byte[]> _memoryMap = new Dictionary<int, byte[]>();

        // 트랙당 바이트 크기 (10 Word = 20 Byte)
        private const int TRACK_BYTE_SIZE = 20;

        public MockPlcServer(int port)
        {
            _port = port;
        }

        public void Start()
        {
            Task.Run(() =>
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();

                Console.WriteLine($"MockPlcServer 시작 - Port: {_port}");

                while (!_stopSwitch)
                {
                    try
                    {
                        // 클라이언트 연결 대기
                        var client = _listener.AcceptTcpClient();
                        Console.WriteLine($"MockPlcServer 클라이언트 연결 - Port: {_port}");

                        // 클라이언트별 스레드 처리
                        Task.Run(() => HandleClient(client));
                    }
                    catch
                    {
                        // 서버 종료 시 AcceptTcpClient 예외 무시
                    }
                }
            });
        }

        public void Stop()
        {
            _stopSwitch = true;
            _listener?.Stop();

            Console.WriteLine($"MockPlcServer 종료 - Port: {_port}");
        }

        private void HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            try
            {
                while (!_stopSwitch && client.Connected)
                {
                    string request = reader.ReadLine();
                    if (string.IsNullOrEmpty(request))
                        break;

                    Console.WriteLine($"MockPlcServer 수신 - Port: {_port} / {request}");

                    if (request.StartsWith("READ"))
                    {
                        int totalBytes = int.Parse(request.Split(' ')[1]);
                        byte[] response = GenerateReadResponse(totalBytes);
                        stream.Write(response, 0, response.Length);
                    }
                    else if (request.StartsWith("WRITE"))
                    {
                        var parts = request.Split(' ');
                        int trackNo = int.Parse(parts[1]);
                        string hexData = parts[2];

                        byte[] writeBytes = Convert.FromHexString(hexData);
                        _memoryMap[trackNo] = writeBytes;

                        Console.WriteLine($"MockPlcServer WRITE - TrackNo: {trackNo}, CmdNo: {BitConverter.ToInt16(writeBytes, 0)}, Dest: {BitConverter.ToInt16(writeBytes, 2)}");

                        writer.WriteLine("OK");
                    }
                }
            }
            catch
            {
                // 클라이언트 연결 종료 시 정상 처리
            }
        }

        private byte[] GenerateReadResponse(int totalBytes)
        {
            byte[] response = new byte[totalBytes];

            // 메모리맵에 쓰인 데이터가 있으면 반영
            int trackCount = totalBytes / TRACK_BYTE_SIZE;
            for (int i = 0; i < trackCount; i++)
            {
                if (_memoryMap.TryGetValue(i + 1, out byte[] trackBytes))
                {
                    // WCS가 쓴 명령을 읽기영역(D126~)에 완료 상태로 반영 (시뮬레이션)
                    Array.Copy(trackBytes, 0, response, i * TRACK_BYTE_SIZE, TRACK_BYTE_SIZE);

                    // 바이트 12~13 (알람코드) = 0 (정상)
                    response[i * TRACK_BYTE_SIZE + 12] = 0;
                    // 바이트 14~15 (센서상태) = 1 (완료)
                    response[i * TRACK_BYTE_SIZE + 14] = 1;
                }
            }

            return response;
        }
    }
}