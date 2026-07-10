using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EisSocketService.Handlers
{
    // 실제 회사 프로젝트의 IMessageHandler 패턴을 축소 적용
    public interface IMessageHandler
    {
        string Command { get; }

        // 메시지 방향 (RECEIVE/SEND) - 실제 코드처럼 Factory 키 구성에 사용
        string Direction { get; }

        // 요청 프레임(STX~ETX 포함)을 받아 처리하고, 응답 프레임을 반환
        // 응답이 필요없는 Command는 null 반환
        byte[] Handle(byte[] requestFrame);
    }
}
