using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EisSocketService.Models
{
    // VISION_RESULT 테이블 매핑
    public class VisionResult
    {
        public string ResultId { get; set; }
        public string CartId { get; set; }
        public string OverallResult { get; set; }
        public DateTime InspectDttm { get; set; }
    }
}
