using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPractice.Model
{
    // 컨베이어 명령
    public class ConveyorCommand
    {
        // 설비 ID
        public string EqpId { get; set; }
        // 오더 번호
        public string EqpCmdNo { get; set; }
        // 목적지 PLC 번호
        public int DestPlcEqpNo { get; set; }
        // 전송 상태 (WAIT / COMPLETE / FAIL)
        public string SendStatus { get; set; }
        // 생성 시각
        public DateTime SaveDttm { get; set; }
        // 생성 주체
        public string SaveBy { get; set; }
    }
}
