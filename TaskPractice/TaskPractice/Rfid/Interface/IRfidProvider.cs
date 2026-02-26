using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPractice.Rfid.Interface
{
    // DB접근 담당
    public interface IRfidProvider
    {
        // 장비 상태 조회
        DataTable SelectRfidEqpStatus(string eqpId);

        // 장비 상태 저장
        int UpdateRfidEqpStatus(string eqpId, string detailId, string resultValue);
        
        // 명령 조회
        DataTable SelectRfidCommand(string eqpId, string ifStatus);
        
        // 명령 상태 업데이트
        int UpdateRfidCommandStatus(string eqpId, string instNo, string ifStatus, string ifMessage);
       
        // 명령 등록
        int InsertRfidCommand(string eqpId, string instNo, string instValuesJsonStr, string commandType);

        // PROCESSINT 상대 오래된 명령 (= 비정상) 조회
        DataTable SelectRfidCommandTimeout(string eqpId, int timeoutSeconds);

        // IF_STATUS 복수 조건 조회 (WAIT + COMMAND_TYPE)
        DataTable SelectRfidCommandByType(string eqpId, string ifStatus, string commandType);
    }
}
