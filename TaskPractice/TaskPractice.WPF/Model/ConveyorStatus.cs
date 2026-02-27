using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPractice.Model
{
    // 컨베이어 현재 상태
    public class ConveyorStatus
    {
        // 설비 ID
        public string EqpId { get; set; }
        // 알람 코드
        public string AlarmCode { get; set; }
        // 센서 상태
        public string SensorStatus { get; set; }
        // 명령 가능 여부
        public string IsCmdReadyYn { get; set; }
        // 마지막 업데이트 시각
        public DateTime UpdateDttm { get; set; }
    }
}
