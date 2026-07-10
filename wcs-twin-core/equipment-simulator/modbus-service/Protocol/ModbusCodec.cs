using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModbusService.Protocol
{
    public static class ModbusCodec
    {
        public const byte FC_READ_HOLDING_REGISTERS = 0x03;
        public const byte FC_READ_INPUT_REGISTERS = 0x04;
        public const byte FC_WRITE_SINGLE_REGISTER = 0x06;
        public const byte FC_WRITE_MULTIPLE_REGISTERS = 0x10;

        private const byte EXCEPTION_FLAG = 0x80;

        private static void AppendUInt16BigEndian(List<byte> buffer, ushort value)
        {
            buffer.Add((byte)(value >> 8));
            buffer.Add((byte)(value & 0xFF));
        }

        private static ushort ReadUInt16BigEndian(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        public static byte[] BuildReadRegistersRequest(byte functionCode, ushort transactionId, byte unitId, ushort startAddress, ushort quantity)
        {
            List<byte> pdu = new List<byte> { functionCode };
            AppendUInt16BigEndian(pdu, startAddress);
            AppendUInt16BigEndian(pdu, quantity);
            return BuildFrame(transactionId, unitId, pdu.ToArray());
        }

        public static byte[] BuildWriteMultipleRegistersRequest(ushort transactionId, byte unitId, ushort startAddress, ushort[] values)
        {
            List<byte> pdu = new List<byte> { FC_WRITE_MULTIPLE_REGISTERS };
            AppendUInt16BigEndian(pdu, startAddress);
            AppendUInt16BigEndian(pdu, (ushort)values.Length);
            pdu.Add((byte)(values.Length * 2));
            foreach (var value in values)
            {
                AppendUInt16BigEndian(pdu, value);
            }
            return BuildFrame(transactionId, unitId, pdu.ToArray());
        }

        public static (byte FunctionCode, ushort[] Registers) ParseReadResponse(byte[] frame)
        {
            byte functionCode = frame[7];
            if ((functionCode & EXCEPTION_FLAG) != 0)
            {
                byte exceptionCode = frame[8];
                throw new ModbusException(functionCode, exceptionCode);
            }
            byte byteCount = frame[8];
            int registerCount = byteCount / 2;
            ushort[] registers = new ushort[registerCount];
            for (int i = 0; i < registerCount; i++)
            {
                registers[i] = ReadUInt16BigEndian(frame, 9 + i * 2);
            }
            return (functionCode, registers);
        }

        public static void ParseWriteResponse(byte[] frame)
        {
            byte functionCode = frame[7];
            if ((functionCode & EXCEPTION_FLAG) != 0)
            {
                byte exceptionCode = frame[8];
                throw new ModbusException(functionCode, exceptionCode);
            }
        }

        public static (ushort TransactionId, byte UnitId, byte FunctionCode, byte[] Pdu) ParseFrame(byte[] frame)
        {
            ushort transactionId = ReadUInt16BigEndian(frame, 0);
            byte unitId = frame[6];
            byte functionCode = frame[7];
            byte[] pdu = frame.Skip(7).ToArray();
            return (transactionId, unitId, functionCode, pdu);
        }

        public static (ushort StartAddress, ushort Quantity) ParseReadRequest(byte[] pdu)
        {
            ushort startAddress = ReadUInt16BigEndian(pdu, 1);
            ushort quantity = ReadUInt16BigEndian(pdu, 3);
            return (startAddress, quantity);
        }

        public static (ushort StartAddress, ushort[] Values) ParseWriteMultipleRequest(byte[] pdu)
        {
            ushort startAddress = ReadUInt16BigEndian(pdu, 1);
            ushort quantity = ReadUInt16BigEndian(pdu, 3);
            ushort[] values = new ushort[quantity];
            for (int i = 0; i < quantity; i++)
            {
                values[i] = ReadUInt16BigEndian(pdu, 6 + i * 2);
            }
            return (startAddress, values);
        }

        public static byte[] BuildReadRegistersResponse(ushort transactionId, byte unitId, byte functionCode, ushort[] registers)
        {
            List<byte> pdu = new List<byte> { functionCode, (byte)(registers.Length * 2) };
            foreach (var value in registers)
            {
                AppendUInt16BigEndian(pdu, value);
            }
            return BuildFrame(transactionId, unitId, pdu.ToArray());
        }

        public static byte[] BuildWriteMultipleRegistersResponse(ushort transactionId, byte unitId, ushort startAddress, ushort quantity)
        {
            List<byte> pdu = new List<byte> { FC_WRITE_MULTIPLE_REGISTERS };
            AppendUInt16BigEndian(pdu, startAddress);
            AppendUInt16BigEndian(pdu, quantity);
            return BuildFrame(transactionId, unitId, pdu.ToArray());
        }

        private static byte[] BuildFrame(ushort transactionId, byte unitId, byte[] pdu)
        {
            List<byte> frame = new List<byte>();
            AppendUInt16BigEndian(frame, transactionId);
            AppendUInt16BigEndian(frame, 0x0000);
            AppendUInt16BigEndian(frame, (ushort)(pdu.Length + 1));
            frame.Add(unitId);
            frame.AddRange(pdu);
            return frame.ToArray();
        }

        public static int? TryGetExpectedFrameLength(List<byte> buffer)
        {
            if (buffer.Count < 6) return null;
            ushort length = (ushort)((buffer[4] << 8) | buffer[5]);
            return 6 + length;
        }
    }

    public class ModbusException : Exception
    {
        public byte FunctionCode { get; }
        public byte ExceptionCode { get; }

        public ModbusException(byte functionCode, byte exceptionCode)
            : base($"Modbus 예외 응답 - FunctionCode: 0x{functionCode:X2}, ExceptionCode: 0x{exceptionCode:X2}")
        {
            FunctionCode = functionCode;
            ExceptionCode = exceptionCode;
        }
    }
}