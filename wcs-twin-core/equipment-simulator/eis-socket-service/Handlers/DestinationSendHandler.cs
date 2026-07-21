using System;

namespace EisSocketService.Handlers
{
    // Command 51 핸들러 - 목적지 지시 (WCS -> 설비, SEND 방향)
    // 아직 서버가 능동적으로 목적지 명령을 만들어 보내는 기능은 없어서,
    // 지금은 구조(팩토리에 SEND 방향 핸들러도 등록 가능하다는 것)만 보여주는 자리표시자다.
    public class DestinationSendHandler : IMessageHandler
    {
        public string Command => "51";
        public string Direction => "SEND";

        public Task<byte[]> Handle(byte[] requestFrame)
        {
            Console.WriteLine("목적지 지시 송신 완료 로그 (자리표시자 - 아직 실제 송신 트리거 없음)");
            return Task.FromResult<byte[]>(null);
        }
    }
}