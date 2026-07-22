// ============================================================
// Kata 3 (서버) : STX/ETX 프레이밍 TCP 서버
// ------------------------------------------------------------
// 실무 코드의 SocketVisionInterfaceService가 쓰던 프로토콜을 그대로 재현합니다:
//   메시지 = STX(0x02) + 내용(UTF8 바이트) + ETX(0x03)
//
// TCP는 "스트림"이라서, 상대방이 한 번에 보낸 메시지가 여러 조각으로 나뉘어
// 도착할 수도 있고, 여러 메시지가 한 번에 뭉쳐서 도착할 수도 있습니다.
// 그래서 "받은 바이트를 일단 누적버퍼에 쌓아두고, 그 안에서 완전한
// STX~ETX 구간을 찾아 하나씩 꺼내 쓰는" 방식이 필요합니다.
// (실무 코드의 AsynchronousClient_ReceiveDataEvent + _accumulatedBuffer 로직과 동일한 문제입니다.)
//
// 완성 조건 (Acceptance Criteria):
//   1) 여러 클라이언트가 동시에 접속해도 각자 독립적으로 처리된다 (한 클라이언트가
//      메시지를 안 보내고 있어도 다른 클라이언트가 막히지 않는다).
//   2) 클라이언트가 보낸 메시지를 대문자로 바꿔서 STX/ETX로 다시 감싸 그대로 echo한다.
//   3) 아주 긴 메시지(수천 바이트)가 여러 번의 Read로 나뉘어 와도 정상적으로
//      하나의 메시지로 재조립해서 처리한다.
//   4) 클라이언트가 "QUIT" 메시지를 보내면 그 연결을 정상 종료한다.
//   5) 클라이언트가 그냥 연결을 끊어도(Read가 0을 반환) 예외 없이 정리된다.
//
// 힌트: SocketVisionInterfaceService.cs 의 AsynchronousClient_ReceiveDataEvent 를
//       참고하되 복사/붙여넣기 하지 말고, "누적 → ETX 찾기 → 자르기 → 남은 건 보관" 이라는
//       구조만 가져오세요.
// ============================================================

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    private const byte STX = 0x02;
    private const byte ETX = 0x03;
    private const int PORT = 5060;

    static async Task Main()
    {
        // TODO 1: TcpListener 를 PORT로 만들고 Start() 하세요.
        TcpListener listener = new TcpListener(IPAddress.Any, PORT);
        listener.Start();
        // TODO 2: while(true) 로 AcceptTcpClientAsync() 를 반복 대기하다가
        //         클라이언트가 붙을 때마다 Task.Run(() => HandleClientAsync(client)) 으로
        //         "각 클라이언트를 독립된 Task에서" 처리하세요.
        //         (fire-and-forget 이 됩니다 - 이 kata에서는 그래도 괜찮습니다.
        //          단, 나중에 "이게 왜 실무에서는 위험할 수 있는지" 한번 생각해보세요.)

        Console.WriteLine($"서버 시작 - port {PORT}. Ctrl+C로 종료.");
        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            Task.Run(() => HandleClientAsync(client));
        }
    }

    /// <summary>
    /// 클라이언트 하나를 담당. 연결이 끊기거나 QUIT을 받을 때까지 계속 메시지를 처리한다.
    /// </summary>
    private static async Task HandleClientAsync(TcpClient client)
    {
        string clientAddr = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        Console.WriteLine($"[Server] 접속 - {clientAddr}");

        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            // TODO 3: 누적 버퍼를 준비하세요 (List<byte> 를 추천 - 실무 코드의 _accumulatedBuffer와 같은 역할)
            List<byte> accumulatedBuffer = new List<byte>(); // 누적 버퍼
            byte[] readBuffer = new byte[1024]; // 한번 읽을 때

            bool keepRunning = true;
            while (keepRunning)
            {
                // TODO 4: stream.ReadAsync(readBuffer) 로 바이트를 받으세요.
                //         반환값이 0이면 상대가 연결을 끊은 것이므로 루프를 빠져나가세요.
                //         받은 만큼(readBuffer의 앞부분)을 accumulatedBuffer에 추가하세요.
                int bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
                if (bytesRead == 0)
                {
                    keepRunning = false; // 연결 끊김
                    break;
                }
                accumulatedBuffer.AddRange(readBuffer.AsSpan(0, bytesRead).ToArray());

                // TODO 5: accumulatedBuffer 안에서 완전한 메시지(STX...ETX)를 찾을 수 있는 만큼
                //         반복해서 잘라내세요. (한 번의 Read로 메시지가 2개 이상 뭉쳐올 수도 있으니
                //         "찾을 수 있는 동안 계속" 처리해야 합니다 - while 루프 안에 while 루프)
                //   - ETX의 위치를 IndexOf로 찾고
                //   - 그 앞에서 가장 가까운 STX를 찾고 (STX 없이 ETX만 있으면 그 앞부분은 버림)
                //   - STX부터 ETX까지를 잘라내서 메시지 하나로 만들고
                //   - 그 부분을 누적버퍼에서 제거하고
                //   - 잘라낸 메시지를 아래 ProcessOneMessageAsync 로 넘겨서 처리하세요.
                //     반환값이 false 면(QUIT 받음) keepRunning = false; break;
                while (accumulatedBuffer.Count > 0)
                {
                    int etxIndex = accumulatedBuffer.IndexOf(ETX);
                    if (etxIndex == -1)
                    {
                        // ETX가 없으면 아직 완전한 메시지가 아님
                        break;
                    }
                    int stxIndex = accumulatedBuffer.LastIndexOf(STX, etxIndex);
                    if (stxIndex == -1)
                    {
                        // STX가 없으면 유효한 메시지가 아님
                        accumulatedBuffer.RemoveRange(0, etxIndex + 1);
                        break;
                    }
                    bool result = await ProcessOneMessageAsync(stream, accumulatedBuffer.Skip(stxIndex).Take(etxIndex - stxIndex + 1).ToArray());
                    accumulatedBuffer.RemoveRange(0, etxIndex + 1);

                    if (!result)
                    {
                        keepRunning = false;
                        break;   // 안쪽 while 탈출 → 바깥 while(keepRunning)도 조건이 false라 같이 끝남
                    }
                }

            }
        }

        Console.WriteLine($"[Server] 종료 - {clientAddr}");
    }

    /// <summary>
    /// 완전한 메시지(STX~ETX 포함 바이트) 하나를 처리.
    /// 반환값 false = 연결을 끊어야 함(QUIT 수신).
    /// </summary>
    private static async Task<bool> ProcessOneMessageAsync(NetworkStream stream, byte[] framedMessage)
    {
        // TODO 6: framedMessage 에서 STX(맨앞)와 ETX(맨뒤)를 벗겨내고 나머지를 UTF8 문자열로 변환하세요.
        if (framedMessage.Length < 2 || framedMessage[0] != STX || framedMessage[framedMessage.Length - 1] != ETX)
        {
            throw new ArgumentException("Invalid framed message");
        }
        string text = Encoding.UTF8.GetString(framedMessage, 1, framedMessage.Length - 2);
        // TODO 7: 콘솔에 받은 내용 출력 (예: "[Server] 수신: {text}", 너무 길면 앞 50자만 출력)
        if (framedMessage.Length > 50)
        {
            Console.WriteLine($"[Server] 수신: {text.Substring(0, 50)}");
        }
        else
        {
            Console.WriteLine($"[Server] 수신: {text}");
        }
        // TODO 8: text 가 "QUIT" 이면 false를 반환하세요 (더 이상 echo 하지 않음).
        if (text == "QUIT")
        {
            return false;
        }
        // TODO 9: 그 외엔 text.ToUpper() 로 바꾼 뒤, STX + UTF8바이트 + ETX 로 다시 프레이밍해서
        //         stream.WriteAsync로 그대로 돌려보내고 true를 반환하세요.
        text = text.ToUpper();
        await stream.WriteAsync(new byte[] { STX } .Concat(Encoding.UTF8.GetBytes(text)).Concat(new byte[] { ETX }).ToArray());
        return true;
    }
}
