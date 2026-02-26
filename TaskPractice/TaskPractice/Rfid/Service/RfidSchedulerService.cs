using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using TaskPractice.Rfid.Interface;

namespace TaskPractice.Rfid.Service
{
    public class RfidSchedulerService
    {
        private readonly IRfidProvider _rfidProvider;
        private readonly IRfidService _rfidService;

        private Timer? _timer;
        private bool _stopSwitch = false;

        // 상태 모니터링용도의 이전 값 메모리
        private Dictionary<string, string> _statusMemory = new Dictionary<string, string>();

        // PROCESSING 타임아웃(초)
        private const int PROCESSING_TIMEOUT_SEC = 10;

        // MockPLC 처리 지연 시간(ms)
        private const int MOCK_PLC_DELAY_MS = 500;

        public RfidSchedulerService(IRfidProvider rfidProvider, IRfidService rfidService)
        {
            _rfidProvider = rfidProvider;
            _rfidService = rfidService;
        }


        public void Start()
        {
            _stopSwitch = false;
            _timer = new Timer(SchedulerLogic);
            _timer.Change(20, Timeout.Infinite);
            Console.WriteLine("[RFID 스케줄러] 시작");
        }

        public void Stop()
        {
            _stopSwitch = true;

            // 타이머가 완전히 종료될 때까지 최대 3초 대기
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (_timer != null && sw.ElapsedMilliseconds < 3000)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine("[RFID 스케줄러] 종료");
        }

        private void SchedulerLogic(object? state)
        {
            // 타이머 중지(처리중 중복 실행 방지)
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            try
            {
                // Clear -> Read -> Write
                SchedulerClear();
                SchedulerRead();
                SchedulerWrite();
                SchedulerStatusMonitor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RFID 스케줄러 오류] {ex.Message}");
            }
            finally
            {
                if (!_stopSwitch)
                {
                    _timer.Change(20, Timeout.Infinite);
                }
                else 
                {
                    // 타이머 자원 해제
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        // rfid_eqp_status 변화 감지 및 콘솔 출력
        private void SchedulerStatusMonitor()
        {
            try
            {
                var dt = _rfidProvider.SelectRfidEqpStatus("RFID_IN1");
                if (dt == null || dt.Rows.Count == 0) return;

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    string eqpId = row["EQP_ID"].ToString() ?? "";
                    string detailId = row["EQP_DETAIL_ID"].ToString() ?? "";
                    string resultValue = row["RESULT_VALUE"].ToString() ?? "";

                    string memoryKey = $"{eqpId}_{detailId}";

                    // 이전 값과 비교하여 변화 감지
                    if (!_statusMemory.ContainsKey(memoryKey) || _statusMemory[memoryKey] != resultValue)
                    {
                        Console.WriteLine($"[RFID STATUS] {eqpId} - {detailId} : {resultValue}");
                        _statusMemory[memoryKey] = resultValue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RFID STATUS 모니터 오류] {ex.Message}");
            }
        }

        // WAIT 상태의 WRITE 명령을 처리
        private void SchedulerWrite()
        {
            try
            {
                var dt = _rfidProvider.SelectRfidCommandByType("", "WAIT", "WRITE");
                if (dt == null || dt.Rows.Count == 0) return;

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    string eqpId = row["EQP_ID"].ToString() ?? "";
                    string instNo = row["INST_NO"].ToString() ?? "";
                    string instValuesJson = row["INST_VALUES_JSON_STR"].ToString() ?? "";

                    // PROCESSING으로 변경
                    _rfidProvider.UpdateRfidCommandStatus(eqpId, instNo, "PROCESSING", "");
                    Console.WriteLine($"[RFID WRITE] {eqpId} - {instNo} 처리 시작");

                    try
                    {
                        // MockPLC: 가상 태그에 데이터 저장
                        Thread.Sleep(MOCK_PLC_DELAY_MS);

                        // rfid_eqp_status 갱신
                        _rfidProvider.UpdateRfidEqpStatus(eqpId, "RESPONSE_CODE", "4");  // 4 = Write 성공
                        _rfidProvider.UpdateRfidEqpStatus(eqpId, "BUSY_STATUS", "0");
                        _rfidProvider.UpdateRfidEqpStatus(eqpId, "COMMAND_CODE", "4");

                        // COMPLETE 처리
                        _rfidProvider.UpdateRfidCommandStatus(eqpId, instNo, "COMPLETE", "WRITE 성공");
                        Console.WriteLine($"[RFID WRITE] {eqpId} - {instNo} 완료");
                    }
                    catch (Exception ex)
                    {
                        _rfidProvider.UpdateRfidCommandStatus(eqpId, instNo, "CANCELED", ex.Message);
                        Console.WriteLine($"[RFID WRITE 실패] {eqpId} - {instNo} : {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RFID WRITE 스케줄러 오류] {ex.Message}");
            }
         }

        // WAIT 상태의 READ 명령을 처리
        private void SchedulerRead()
        {
            try
            {
                var dt = _rfidProvider.SelectRfidCommandByType("", "WAIT", "READ");
                if (dt == null || dt.Rows.Count == 0) return;

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    string eqpId = row["EQP_ID"].ToString() ?? "";
                    string instNo = row["INST_NO"].ToString() ?? "";

                    // PROCESSING으로 변경 (다른 폴링에서 중복 처리 방지)
                    _rfidProvider.UpdateRfidCommandStatus(eqpId, instNo, "PROCESSING", "");
                    Console.WriteLine($"[RFID READ] {eqpId} - {instNo} 처리 시작");

                    try
                    {
                        // MockPLC: 가상 태그 데이터 생성
                        Thread.Sleep(MOCK_PLC_DELAY_MS);
                        string mockTagData = $"TAG_{eqpId}_{DateTime.Now:HHmmss}";

                        // rfid_eqp_status 갱신
                        _rfidProvider.UpdateRfidEqpStatus(eqpId, "RESPONSE_CODE", "2");  // 2 = Read 성공
                        _rfidProvider.UpdateRfidEqpStatus(eqpId, "BUSY_STATUS", "0");
                        _rfidProvider.UpdateRfidEqpStatus(eqpId, "COMMAND_CODE", "2");

                        // COMPLETE 처리
                        _rfidProvider.UpdateRfidCommandStatus(eqpId, instNo, "COMPLETE", $"READ 성공: {mockTagData}");
                        Console.WriteLine($"[RFID READ] {eqpId} - {instNo} 완료 - 태그데이터: {mockTagData}");
                    }
                    catch (Exception ex)
                    {
                        // MockPLC 처리 실패 시 CANCELED
                        _rfidProvider.UpdateRfidCommandStatus(eqpId, instNo, "CANCELED", ex.Message);
                        Console.WriteLine($"[RFID READ 실패] {eqpId} - {instNo} : {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RFID READ 스케줄러 오류] {ex.Message}");
            }
        }

        // PROCESSING 상태에서 타임아웃 된 명령을 CANCELED로 정리
        private void SchedulerClear()
        {
            try
            {
                var dt = _rfidProvider.SelectRfidCommandTimeout("", PROCESSING_TIMEOUT_SEC);
                if (dt == null || dt.Rows.Count == 0) return;

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    string eqpId = row["EQP_ID"].ToString() ?? "";
                    string instNo = row["INST_No"].ToString() ?? "";

                    _rfidProvider.UpdateRfidCommandStatus(eqpId, instNo, "CANCELED", "PROCESSING 타임아웃으로 인한 자동 취소");
                    Console.WriteLine($"[RFID CLEAR] {eqpId} - {instNo} 타임아웃 CANCELED 처리");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RFID CLEAR 오류] {ex.Message}");
            }
        }
    }
}
