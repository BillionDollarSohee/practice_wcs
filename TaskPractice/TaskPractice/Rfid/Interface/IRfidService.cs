using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPractice.Rfid.Interface
{
    // 비즈니스 로직 담당
    internal interface IRfidService
    {
        // Read 명령 요청
        bool RequestRead(string eqpId);

        // Write 명령 요청
        bool RequestWrite(string eqpId, string data);

        // 현재 장비 상태 조회
        string GetStatus(string eqpId, string datailId);
    }
}
