using System.Text;
using ModbusService.Model;

namespace ModbusService.Helper
{
    // Input Register 배열을 사람이 읽을 수 있는 ParsedRfidStatus로 변환
    public static class ModbusDataParsingHelper
    {
        private const int OFFSET_RESPONSE_CODE = 0;
        private const int OFFSET_BUSY = 1;
        private const int OFFSET_LENGTH = 2;
        private const int OFFSET_ERROR_STATUS = 5;
        private const int OFFSET_ERROR_CODE = 6;
        private const int OFFSET_READ_DATA_START = 11;

        public static ParsedRfidStatus Parse(ushort[] inputRegisters)
        {
            var status = new ParsedRfidStatus
            {
                ResponseCode = inputRegisters[OFFSET_RESPONSE_CODE],
                IsBusy = inputRegisters[OFFSET_BUSY] == 1,
                Length = inputRegisters[OFFSET_LENGTH],
                HasError = inputRegisters[OFFSET_ERROR_STATUS] == 1,
                ErrorCode = inputRegisters[OFFSET_ERROR_CODE]
            };

            if (!status.HasError && status.Length > 0)
            {
                int wordCount = (status.Length + 1) / 2;
                StringBuilder textBuilder = new StringBuilder();

                for (int i = 0; i < wordCount && OFFSET_READ_DATA_START + i < inputRegisters.Length; i++)
                {
                    ushort word = inputRegisters[OFFSET_READ_DATA_START + i];
                    textBuilder.Append((char)(word >> 8));
                    textBuilder.Append((char)(word & 0xFF));
                }

                status.ReadDataText = textBuilder.ToString().Trim();
            }

            return status;
        }
    }
}