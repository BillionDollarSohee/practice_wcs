using System.Data;
using System.Text.Json;
using TaskPractice.Rfid.Interface;

namespace TaskPractice.Rfid.Service
{
    public class RfidService : IRfidService
    {
        private readonly IRfidProvider _rfidProvider;

        // 명령 번호 채번용 카운터
        private int _instNoCounter = 0;

        public RfidService(IRfidProvider rfidProvider)
        {
            _rfidProvider = rfidProvider;
        }

        // 현재 장비 상태 조회
        public string GetStatus(string eqpId, string detailId)
        {
            try
            {
                var dt = _rfidProvider.SelectRfidEqpStatus(eqpId);

                if (dt != null && dt.Rows.Count > 0)
                {
                    // detailId에 해당하는 행 찾기
                    var row = dt.AsEnumerable()
                        .FirstOrDefault(r => r["EQP_DETAIL_ID"].ToString() == detailId);

                    return row?["RESULT_VALUE"]?.ToString() ?? "";
                }

                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetStatus: {ex.Message}");
                return "";
            }
        }

        // 명령 번호 생성
        private string GenerateInstNo(string eqpId)
        {
            _instNoCounter++;
            return $"{eqpId}_{DateTime.Now:yyyyMMddHHmmss}_{_instNoCounter:D4}";
        }

        // 읽기 명령 요청
        public bool RequestRead(string eqpId)
        {
            try
            {
                // 장비 현재 상태  확인
                if (!IsPossibleCommand(eqpId))
                {
                    Console.WriteLine($"[RFID] {eqpId} 장비 사용 불가 상태 -> READ 명령 취소 ");
                    return false;
                }

                // READ 명령 JSON 생성
                var commandJson = JsonSerializer.Serialize(new
                {
                    COMMAND_CODE = 2, // READ 명령
                    MEMORY_AREA = 1,  // EPC 영역
                    START_ADDRESS = 0,
                    WRITE_LENGTH = 4
                });

                string instNo = GenerateInstNo(eqpId);

                // DB에 명령 등록
                int result = _rfidProvider.InsertRfidCommand(eqpId, instNo, commandJson, "READ");

                if (result > 0)
                {
                    Console.WriteLine($"[RFID READ] {eqpId} 명령 등록 완료 - instNo : {instNo}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] RequestRead : {ex.Message}");
                return false;
            }
        }

        // Write 명령 요청
        public bool RequestWrite(string eqpId, string data)
        {
            try
            {
                // 장비 현재 상태 확인
                if (!IsPossibleCommand(eqpId))
                {
                    Console.WriteLine($"[RFID] {eqpId} 장비 사용 불가 상태 - Write 명령 취소");
                    return false;
                }

                // Write 명령 Json 생성
                var commandJson = JsonSerializer.Serialize(new
                {
                    COMMAND_CODE = 4, // WRITE 명령
                    MEMORY_AREA = 3, // USER 영역
                    START_ADDRESS = 0,
                    WRITE_LENGTH = data.Length,
                    WRITE_DATA = data
                });

                string instNo = GenerateInstNo(eqpId);

                // DB에 명령 등록
                int result = _rfidProvider.InsertRfidCommand(eqpId, instNo, commandJson, "WRITE");

                if (result > 0)
                {
                    Console.WriteLine($"[RFID WRITE] {eqpId} 명령 등록 완료 - InstNo: {instNo}, Data: {data}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] RequestWrite: {ex.Message}");
                return false;
            }
        }

        // 명령 가능 상태 확인 (WCS의 IsPossibleRfidCommand와 동일한 로직)
        private bool IsPossibleCommand(string eqpId)
        {
            try
            {
                string busyStatus = GetStatus(eqpId, "BUSY_STATUS");
                string commandCode = GetStatus(eqpId, "COMMAND_CODE");
                string responseCode = GetStatus(eqpId, "RESPONSE_CODE");

                int iBusyStatus = int.TryParse(busyStatus, out int b) ? b : 9999;
                int iCommandCode = int.TryParse(commandCode, out int c) ? c : 9999;
                int iResponseCode = int.TryParse(responseCode, out int r) ? r : 9999;

                // Busy = 0 (대기) 이고 이전 명령이 완료된 상태
                if (iBusyStatus == 0 && iCommandCode == iResponseCode)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] IsPossibleCommand: {ex.Message}");
                return false;
            }
        }
    }
}