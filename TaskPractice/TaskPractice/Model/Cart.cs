namespace TaskPractice.Model
{
    public class Cart
    {
        public string CartId { get; set; }              // 대차 ID
        public StockType StockType { get; set; }        // 재고 유형 (공대차/실대차)
        public LocationType LocationType { get; set; }  // 위치 유형
        public string LocationCode { get; set; }        // 대차 위치 코드
        public CartStatus CartStatus { get; set; }      // 대차 상태 (정상/파손)
        public DateTime InDttm { get; set; }            // 입고 일시
        public DateTime? OutDttm { get; set; }          // 출고 일시 (null 허용)

        // 서열 매핑 정보 (wcs_work_cart_seq_mapping)
        public string MappingId { get; set; }           // 매핑 ID
        public string SeqOrdId { get; set; }            // 서열 오더 ID
        public string MappingType { get; set; }         // 매핑 유형 (자동 A / 수동 M)
        public DateTime? MappingDttm { get; set; }      // 매핑 일시
        public bool PrintYn { get; set; } = false;      // 서열지 출력 여부

        // 이동 오더용
        public string FromEqpId { get; set; }           // 출발 설비 ID
        public string ToEqpId { get; set; }             // 목적지 설비 ID
    }

    // 재고 유형
    public enum StockType
    { 
        EMPTY,   // 공대차
        PULL     // 실대차
    }

    // 위치 유형
    public enum LocationType
    {
        STORAGE,        // 보관
        OUT_BUFFER,     // 출고버퍼
        OUT_CV,         // 출고CV
        PICKING,        // 피킹중
        EXTERNAL        // 외부
    }


    // 대차 상태
    public enum CartStatus
    {
        NORMAL,  // 정상
        DAMAGED  // 파손
    }
}
