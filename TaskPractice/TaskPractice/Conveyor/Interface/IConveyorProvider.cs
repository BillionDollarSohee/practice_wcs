using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskPractice.Model;

namespace TaskPractice.Conveyor.Interface
{
    // 컨베이어 DB 접근 인터페이스
    public interface IConveyorProvider
    {
        // 설비 그룹의 READY 오더 조회 후 CV_COMMAND 생성
        bool FindConveyorOrderAndCreateCommand(string eqpGroup);

        // CV_COMMAND WAIT 목록 조회
        List<ConveyorCommand> GetWaitCommandList();

        // CV_COMMAND 전송 상태 업데이트 (COMPLETE / FAIL)
        bool UpdateCommandSendStatus(string eqpId, string sendStatus);

        // CV_STATUS 업데이트 (PLC 읽기 결과)
        bool UpdateConveyorStatus(string eqpId, string alarmCode, string sensorStatus);

        // CV_STATUS IS_CMD_READY_YN 업데이트
        bool UpdateConveyorStatusIsCmdReady(string eqpId, string isCmdReadyYn);

        // CV_UNIT 전체 조회 (Repository 로드용)
        List<ConveyorUnit> GetConveyorUnitList();
    }
}
