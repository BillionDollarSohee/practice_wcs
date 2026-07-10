using System;

namespace Database.Models
{
    // EQUIPMENT_STATUS_HIST 테이블 매핑
    public class EquipmentStatusHist
    {
        public long HistId { get; set; }
        public string EqpId { get; set; }
        public string StatusType { get; set; }
        public string CartId { get; set; }
        public string Status { get; set; }
        public string ResultJson { get; set; }
        public DateTime CreateDttm { get; set; }
    }
}