namespace TaskPractice.Model
{
    public enum DetailStatus
    {
        INIT,
        READY,
        WORKING,
        COMPLETE
    }

    public class OrderDetail
    {
        public string OrderId { get; set; }
        public int Seq { get; set; }            // 구간 순서
        public string FromEqpId { get; set; }   // 구간 출발 설비
        public string ToEqpId { get; set; }     // 구간 도착 설비
        public DetailStatus Status { get; set; } = DetailStatus.INIT;
    }
}