using ModbusCore;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RfidModbusSimulator
{
    // 가짜 UHF RFID 리더 - Modbus TCP 슬레이브(서버) 역할
    //
    // 트림/파이널/도어 검사대는 각자 물리적으로 독립된 RFID 리더/라이터를 쓰므로,
    // 포트별로 완전히 독립된 레지스터 상태를 갖는 MockRfidReader 인스턴스를 라인별로 하나씩 띄운다.
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("가짜 UHF RFID 리더 시뮬레이터 시작 (Modbus TCP 슬레이브, 라인별 3대)");

            var readers = new[]
            {
                new MockRfidReader("TR", 101),
                new MockRfidReader("FL", 51),
                new MockRfidReader("DR", 71)
            };

            await Task.WhenAll(Array.ConvertAll(readers, r => r.RunAsync()));
        }
    }

    // 리더 1대(=검사대 1개 라인)를 표현한다. 레지스터 상태를 자기 안에 들고 있어서
    // 다른 라인의 리더와 절대 register를 공유하지 않는다.
    class MockRfidReader
    {
        private const int CH_RESPONSE_CODE = 0;
        private const int CH_BUSY = 1;
        private const int CH_LENGTH = 2;
        private const int CH_ERROR_STATUS = 5;
        private const int CH_ERROR_CODE = 6;
        private const int CH_READ_DATA_START = 11;

        private readonly string _lineType;
        private readonly int _port;
        private readonly ushort[] _holdingRegisters = new ushort[1000];
        private readonly ushort[] _inputRegisters = new ushort[1000];
        private readonly object _registerLock = new object();

        public MockRfidReader(string lineType, int port)
        {
            _lineType = lineType;
            _port = port;
        }

        public async Task RunAsync()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            Console.WriteLine($"[{_lineType}] 리더 대기 포트: {_port}");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine($"[{_lineType}] WCS(마스터) 접속됨");
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                List<byte> buffer = new List<byte>();
                byte[] readBuffer = new byte[1024];

                while (client.Connected)
                {
                    int readCount;
                    try
                    {
                        readCount = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
                    }
                    catch
                    {
                        break;
                    }

                    if (readCount == 0) break;

                    buffer.AddRange(new ArraySegment<byte>(readBuffer, 0, readCount));

                    while (true)
                    {
                        int? expectedLength = ModbusCodec.TryGetExpectedFrameLength(buffer);
                        if (expectedLength == null || buffer.Count < expectedLength.Value) break;

                        byte[] frame = buffer.GetRange(0, expectedLength.Value).ToArray();
                        buffer.RemoveRange(0, expectedLength.Value);

                        byte[] response = ProcessRequest(frame);
                        if (response != null)
                        {
                            await stream.WriteAsync(response, 0, response.Length);
                        }
                    }
                }
                Console.WriteLine($"[{_lineType}] WCS 접속 종료");
            }
        }

        private byte[] ProcessRequest(byte[] frame)
        {
            var (transactionId, unitId, functionCode, pdu) = ModbusCodec.ParseFrame(frame);

            switch (functionCode)
            {
                case ModbusCodec.FC_READ_HOLDING_REGISTERS:
                    {
                        var (start, qty) = ModbusCodec.ParseReadRequest(pdu);
                        ushort[] values = ReadRegisters(_holdingRegisters, start, qty);
                        return ModbusCodec.BuildReadRegistersResponse(transactionId, unitId, functionCode, values);
                    }
                case ModbusCodec.FC_READ_INPUT_REGISTERS:
                    {
                        var (start, qty) = ModbusCodec.ParseReadRequest(pdu);
                        ushort[] values = ReadRegisters(_inputRegisters, start, qty);
                        return ModbusCodec.BuildReadRegistersResponse(transactionId, unitId, functionCode, values);
                    }
                case ModbusCodec.FC_WRITE_MULTIPLE_REGISTERS:
                    {
                        var (start, values) = ModbusCodec.ParseWriteMultipleRequest(pdu);
                        WriteRegisters(_holdingRegisters, start, values);
                        Console.WriteLine($"[{_lineType}][FC10] Holding Register 쓰기 - Start:{start}, CommandCode:{values[0]}");

                        if (start == 0)
                        {
                            _ = SimulateCommandExecutionAsync(values[0]);
                        }

                        return ModbusCodec.BuildWriteMultipleRegistersResponse(transactionId, unitId, start, (ushort)values.Length);
                    }
                default:
                    Console.WriteLine($"[{_lineType}] 지원하지 않는 FunctionCode: 0x{functionCode:X2}");
                    return null;
            }
        }

        private ushort[] ReadRegisters(ushort[] source, ushort start, ushort qty)
        {
            lock (_registerLock)
            {
                ushort[] result = new ushort[qty];
                Array.Copy(source, start, result, 0, qty);
                return result;
            }
        }

        private void WriteRegisters(ushort[] target, ushort start, ushort[] values)
        {
            lock (_registerLock)
            {
                Array.Copy(values, 0, target, start, values.Length);
            }
        }

        private async Task SimulateCommandExecutionAsync(ushort commandCode)
        {
            if (commandCode == 0)
            {
                lock (_registerLock)
                {
                    _inputRegisters[CH_RESPONSE_CODE] = 0;
                    _inputRegisters[CH_BUSY] = 0;
                    _inputRegisters[CH_ERROR_STATUS] = 0;
                }
                Console.WriteLine($"[{_lineType}][리더] 준비 명령 처리 - 버퍼 초기화");
                return;
            }

            lock (_registerLock)
            {
                _inputRegisters[CH_BUSY] = 1;
            }
            Console.WriteLine($"[{_lineType}][리더] Command {commandCode} 처리 시작 (Busy=1)");

            await Task.Delay(1000);

            Random random = new Random();
            bool isTagDetected = random.Next(0, 10) < 8;

            lock (_registerLock)
            {
                _inputRegisters[CH_BUSY] = 0;

                if (!isTagDetected)
                {
                    _inputRegisters[CH_ERROR_STATUS] = 1;
                    _inputRegisters[CH_ERROR_CODE] = 0x4006;
                    Console.WriteLine($"[{_lineType}][리더] 태그 감지 실패 (Error Code: 0x4006)");
                    return;
                }

                _inputRegisters[CH_ERROR_STATUS] = 0;

                if (commandCode == 2)
                {
                    _inputRegisters[CH_RESPONSE_CODE] = 2;
                    string dummyEpc = ("EPC" + random.Next(100000, 999999)).PadRight(24, ' ');
                    byte[] epcBytes = Encoding.ASCII.GetBytes(dummyEpc);
                    _inputRegisters[CH_LENGTH] = (ushort)epcBytes.Length;
                    for (int i = 0; i < epcBytes.Length / 2; i++)
                    {
                        ushort word = (ushort)((epcBytes[i * 2] << 8) | epcBytes[i * 2 + 1]);
                        _inputRegisters[CH_READ_DATA_START + i] = word;
                    }
                    Console.WriteLine($"[{_lineType}][리더] Read 완료 - EPC: {dummyEpc.Trim()}");
                }
                else if (commandCode == 4)
                {
                    _inputRegisters[CH_RESPONSE_CODE] = 4;
                    Console.WriteLine($"[{_lineType}][리더] Write 완료");
                }
            }
        }
    }
}
