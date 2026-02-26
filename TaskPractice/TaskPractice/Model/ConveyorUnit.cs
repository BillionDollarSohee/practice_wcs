using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPractice.Model
{
    // 설비 정보
    public class ConveyorUnit
    {
        // 설비 ID (ex. CV.01.001)
        public string EqpId { get; set; }
        // 설비 그룹 (ex. CV.01)
        public string EqpGroup { get; set; }
        // 폴링 그룹 (ex. CV.01.PLC1)
        public string PollingGroup { get; set; }
        // PLC 트랙 번호 (메모리맵 인덱스)
        public int CvTrackNo { get; set; }
        // PLC 목적지 번호
        public int CvDestNo { get; set; }
    }
}
