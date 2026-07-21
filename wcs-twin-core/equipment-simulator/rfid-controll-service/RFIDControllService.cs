using System;
using System.Text.Json;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModbusService;
using ModbusService.Model;

namespace RfidControllService
{
    // RFID Read/Write 요청을 실제로 수행하는 비즈니스 로직
    // ModbusServiceRegistry(통신 계층)에서 라인(TR/FL/DR)에 맞는 리더를 찾아 호출하고,
    // WcsTwinContext(database 프로젝트)로 결과를 기록한다
    //
    // 실물 대차는 실패했다고 자리를 비우고 사라지지 않으므로, 실패하면 곧바로 포기하지 않고
    // 몇 번 더 재시도하고, 그래도 안 되면 사람이 알람을 해제할 때까지 그 자리를 계속 점유한 채 대기한다.
    // (게이트 반납은 이 메서드가 true를 반환한 뒤에야 호출부에서 일어난다)
    public class RFIDControllService
    {
        private const int MaxAttempts = 4; // 최초 시도 1번 + 재시도 3번
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan AlarmPollInterval = TimeSpan.FromSeconds(2);

        private readonly ModbusServiceRegistry _modbusRegistry;
        private readonly WcsTwinContext _context;
        private readonly ILogger<RFIDControllService> _logger;

        public RFIDControllService(ModbusServiceRegistry modbusRegistry, WcsTwinContext context, ILogger<RFIDControllService> logger)
        {
            _modbusRegistry = modbusRegistry;
            _context = context;
            _logger = logger;
        }

        public async Task<bool> RequestReadTagAsync(string cartId, string lineType)
        {
            if (!_modbusRegistry.TryGetService(lineType, out var modbusService))
            {
                _logger.LogWarning("RFID Read 실패 - 라인타입 리더 없음 - CartId:{CartId}, LineType:{LineType}", cartId, lineType);
                return false;
            }

            return await ExecuteWithRetryAndAlarmAsync(cartId, lineType, "RFID_READ_REQ", () => modbusService.ReadTagAsync());
        }

        public async Task<bool> RequestWriteTagAsync(string cartId, string lineType)
        {
            if (!_modbusRegistry.TryGetService(lineType, out var modbusService))
            {
                _logger.LogWarning("RFID Write 실패 - 라인타입 리더 없음 - CartId:{CartId}, LineType:{LineType}", cartId, lineType);
                return false;
            }

            return await ExecuteWithRetryAndAlarmAsync(cartId, lineType, "RFID_WRITE_REQ", () => modbusService.WriteTagAsync());
        }

        // statusType별로 공통되는 "요청 -> 시도 -> 실패시 재시도 -> 그래도 안되면 알람 대기" 흐름
        private async Task<bool> ExecuteWithRetryAndAlarmAsync(string cartId, string lineType, string statusType, Func<Task<ParsedRfidStatus>> operation)
        {
            await SaveStatusAsync(cartId, lineType, statusType, "REQUESTED", "");

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                ParsedRfidStatus result = await operation();
                string resultJson = JsonSerializer.Serialize(result);

                if (!result.HasError)
                {
                    await SaveStatusAsync(cartId, lineType, statusType, "COMPLETED", resultJson);
                    _logger.LogInformation("{StatusType} 성공 - CartId:{CartId}, 시도:{Attempt}/{MaxAttempts}", statusType, cartId, attempt, MaxAttempts);
                    return true;
                }

                bool isLastAttempt = attempt == MaxAttempts;
                await SaveStatusAsync(cartId, lineType, statusType, isLastAttempt ? "FAILED" : "RETRYING", resultJson);
                _logger.LogWarning("{StatusType} 실패 - CartId:{CartId}, 시도:{Attempt}/{MaxAttempts}, ErrorCode:0x{ErrorCode:X4}",
                    statusType, cartId, attempt, MaxAttempts, result.ErrorCode);

                if (!isLastAttempt)
                {
                    await Task.Delay(RetryDelay);
                }
            }

            // 여기까지 왔으면 MaxAttempts번 다 실패한 것 - 알람을 올리고 사람이 해제할 때까지 자리를 계속 점유한 채 대기
            return await RaiseAlarmAndWaitForClearAsync(cartId, lineType, statusType);
        }

        private async Task<bool> RaiseAlarmAndWaitForClearAsync(string cartId, string lineType, string statusType)
        {
            DateTime alarmRaisedAt = DateTime.Now;
            await SaveStatusAsync(cartId, lineType, "RFID_ALARM", "ACTIVE", $"{statusType} {MaxAttempts}회 실패 - 알람 발생");
            _logger.LogWarning("RFID 알람 발생 - CartId:{CartId}, LineType:{LineType} - 알람 해제 대기중", cartId, lineType);

            while (true)
            {
                await Task.Delay(AlarmPollInterval);

                bool cleared = await _context.EquipmentStatusHists.AnyAsync(h =>
                    h.CartId == cartId &&
                    h.StatusType == "RFID_ALARM" &&
                    h.Status == "CLEARED" &&
                    h.CreateDttm >= alarmRaisedAt);

                if (cleared)
                {
                    break;
                }
            }

            // 알람 해제 = 관리자가 수동으로 통과시킨 것으로 간주 - 이후 흐름은 정상 성공과 동일하게 진행
            await SaveStatusAsync(cartId, lineType, statusType, "MANUAL_OVERRIDE", "관리자 알람 해제 - 수동 처리로 진행");
            _logger.LogInformation("RFID 알람 해제됨 - 관리자 수동 처리로 진행 - CartId:{CartId}", cartId);
            return true;
        }

        private async Task SaveStatusAsync(string cartId, string lineType, string statusType, string status, string resultJson)
        {
            _context.EquipmentStatusHists.Add(new EquipmentStatusHist
            {
                EqpId = $"RFID.{lineType}",
                StatusType = statusType,
                CartId = cartId,
                Status = status,
                ResultJson = resultJson ?? string.Empty,
                CreateDttm = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
    }
}
