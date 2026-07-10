using System;

namespace Database.Models
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