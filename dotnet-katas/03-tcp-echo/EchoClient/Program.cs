// ============================================================
// Kata 3 (클라이언트) : STX/ETX 프레이밍 TCP 클라이언트
// ------------------------------------------------------------
// EchoServer 에 접속해서 메시지 3개를 순서대로 보내고, 각각의 echo 응답을 받아 출력합니다.
//
// 완성 조건 (Acceptance Criteria):
//   1) "hello" 를 보내면 서버가 대문자로 바꾼 "HELLO"를 echo 해서 그게 그대로 출력된다.
//   2) 5000자짜리 긴 문자열을 보내도 깨지지 않고 전체가 정확히 echo 되어 돌아온다
//      (이게 되면 서버의 누적버퍼 로직이 제대로 동작한다는 뜻입니다).
//   3) "QUIT"을 보내면 서버가 연결을 종료하고, 클라이언트도 정상 종료된다.
//
// 힌트: 서버(EchoServer/Program.cs)와 프레이밍 규칙(STX/ETX)이 똑같아야 통신이 됩니다.
// ============================================================

using System;
using System.Collections.Generic;
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
        Console.WriteLine("EchoServer 를 먼저 실행해두세요 (dotnet run --project ../EchoServer).");
        Console.WriteLine("연결 시도 중...");

        // TODO 1: TcpClient 로 "127.0.0.1", PORT 에 연결하세요.
        // TODO 2: NetworkStream 을 얻으세요.
        throw new NotImplementedException("TODO 1, 2: 서버 연결");

        // 아래 세 메시지를 순서대로 하나씩 보내고, 각각 응답을 받은 뒤 다음 메시지를 보내세요.
        string[] messages =
        {
            "hello",
            new string('A', 5000), // 일부러 아주 긴 메시지 - TCP가 여러 조각으로 나눠 보낼 확률이 높음
            "QUIT"
        };

        // foreach (string msg in messages)
        // {
        //     TODO 3: msg 를 STX + UTF8바이트 + ETX 로 프레이밍해서 stream에 WriteAsync
        //     TODO 4: 응답을 받으세요. 서버와 마찬가지로 "누적버퍼에 모으다가 ETX 나오면 끝"
        //             방식으로 완전한 메시지 하나를 다 받을 때까지 반복 Read 하세요.
        //             (한 번의 ReadAsync로 5000자짜리 응답이 다 안 들어올 수 있습니다!)
        //     TODO 5: 받은 응답에서 STX/ETX를 벗기고 문자열로 변환해서 콘솔에 출력
        //             (예: "[Client] 보낸 것: hello (5자) → 받은 것: HELLO (5자)")
        // }
    }
}
