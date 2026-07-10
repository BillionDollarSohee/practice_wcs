namespace ModbusService.Model
{
    // Input Register 배열을 파싱한 결과를 담는 모델
    public class ParsedRfidStatus
    {
        public ushort ResponseCode { get; set; }
        public bool IsBusy { get; set; }
        public ushort Length { get; set; }
        public bool HasError { get; set; }
        public ushort ErrorCode { get; set; }
        public string ReadDataText { get; set; } = string.Empty;
    }
}