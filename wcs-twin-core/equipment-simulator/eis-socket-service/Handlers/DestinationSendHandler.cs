using System;

namespace EisSocketService.Handlers
{
    // Command 51 핸들러 - 목적지 지시 (WCS -> 설비, SEND 방향)
    // 실제 코드에서도 이 핸들러는 요청을 받아 처리하는 게 아니라,
    // "송신이 완료됐다"는 사실만 로그로 남기는 역할이다 (Direction=SEND는 수신 라우팅 대상이 아님).
    //
    // 우리 프로젝트는 아직 서버가 능동적으로 목적지 명령을 만들어 보내는 기능이 없어서,
    // 지금은 구조(팩토리에 SEND 방향 핸들러도 등록 가능하다는 것)만 보여주는 자리표시자다.
    // 다음 단계에서 서버 -> 설비 능동 송신 기능을 추가하면 이 핸들러가 실제로 호출되게 만들 수 있다.
    public class DestinationSendHandler : IMessageHandler
    {
        public string Command => "51";
        public string Direction => "SEND";

        public byte[] Handle(byte[] requestFrame)
        {
            Console.WriteLine("목적지 지시 송신 완료 로그 (자리표시자 - 아직 실제 송신 트리거 없음)");
            return null;
        }
    }
}