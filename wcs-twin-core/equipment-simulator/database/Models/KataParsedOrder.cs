using System;

namespace Database.Models
{
    // Kata 4 (DB 폴링/트랜잭션 연습) 전용 테이블 매핑. 실제 트윈 시뮬레이터 로직과는 무관합니다.
    // KATA_PARSED_ORDER 테이블 매핑
    public class KataParsedOrder
    {
        public int ParsedId { get; set; }
        public int RawId { get; set; }
        public string PartCd { get; set; }
        public int Qty { get; set; }
        public string Location { get; set; }
        public DateTime CreateDttm { get; set; }
    }
}
