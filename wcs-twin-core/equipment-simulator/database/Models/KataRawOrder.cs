using System;

namespace Database.Models
{
    // Kata 4 (DB 폴링/트랜잭션 연습) 전용 테이블 매핑. 실제 트윈 시뮬레이터 로직과는 무관합니다.
    // KATA_RAW_ORDER 테이블 매핑
    public class KataRawOrder
    {
        public int RawId { get; set; }
        public string RawData { get; set; }
        public string ProcessStatus { get; set; }
        public string ErrorMsg { get; set; }
        public DateTime CreateDttm { get; set; }
    }
}
