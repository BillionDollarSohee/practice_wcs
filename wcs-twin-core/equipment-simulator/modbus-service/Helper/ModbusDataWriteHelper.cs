namespace ModbusService.Helper
{
    // Command 정보(CommandCode/MemoryArea/StartAddress/Length/Timeout)를
    // Holding Register 배열(FC 0x10 쓰기용)로 변환
    public static class ModbusDataWriteHelper
    {
        private const int OFFSET_COMMAND_CODE = 0;
        private const int OFFSET_MEMORY_AREA = 1;
        private const int OFFSET_START_ADDRESS = 2;
        private const int OFFSET_LENGTH = 3;
        private const int OFFSET_TIMEOUT = 4;
        private const int REGISTER_BLOCK_SIZE = 5;

        public static ushort[] BuildCommandRegisters(ushort commandCode, ushort memoryArea, ushort startAddress, ushort length, ushort timeoutMs)
        {
            ushort[] registers = new ushort[REGISTER_BLOCK_SIZE];
            registers[OFFSET_COMMAND_CODE] = commandCode;
            registers[OFFSET_MEMORY_AREA] = memoryArea;
            registers[OFFSET_START_ADDRESS] = startAddress;
            registers[OFFSET_LENGTH] = length;
            registers[OFFSET_TIMEOUT] = timeoutMs;
            return registers;
        }
    }
}