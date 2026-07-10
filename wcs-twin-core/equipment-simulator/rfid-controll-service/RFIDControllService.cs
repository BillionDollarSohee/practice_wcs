using System;
using System.Text.Json;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Microsoft.Extensions.Logging;
using ModbusService;
using ModbusService.Model;

namespace RfidControllService
{
    // RFID Read/Write 요청을 실제로 수행하는 비즈니스 로직
    // ModbusService(통신 계층)를 호출해서 결과를 받고, WcsTwinContext(database 프로젝트)로 결과를 기록한다
    public class RFIDControllService
    {
        private readonly ModbusPlcInterfaceService _modbusService;
        private readonly WcsTwinContext _context;
        private readonly ILogger<RFIDControllService> _logger;

        public RFIDControllService(ModbusPlcInterfaceService modbusService, WcsTwinContext context, ILogger<RFIDControllService> logger)
        {
            _modbusService = modbusService;
            _context = context;
            _logger = logger;
        }

        public async Task<bool> RequestReadTagAsync(string cartId)
        {
            await SaveStatusAsync(cartId, "RFID_READ_REQ", "REQUESTED", "");

            ParsedRfidStatus result = await _modbusService.ReadTagAsync();

            string resultJson = JsonSerializer.Serialize(result);
            string status = result.HasError ? "FAILED" : "COMPLETED";
            await SaveStatusAsync(cartId, "RFID_READ_REQ", status, resultJson);

            if (result.HasError)
            {
                _logger.LogWarning("RFID Read 실패 - CartId:{CartId}, ErrorCode:0x{ErrorCode:X4}", cartId, result.ErrorCode);
                return false;
            }

            _logger.LogInformation("RFID Read 성공 - CartId:{CartId}, TagData:{Data}", cartId, result.ReadDataText);
            return true;
        }

        public async Task<bool> RequestWriteTagAsync(string cartId)
        {
            await SaveStatusAsync(cartId, "RFID_WRITE_REQ", "REQUESTED", "");

            ParsedRfidStatus result = await _modbusService.WriteTagAsync();

            string resultJson = JsonSerializer.Serialize(result);
            string status = result.HasError ? "FAILED" : "COMPLETED";
            await SaveStatusAsync(cartId, "RFID_WRITE_REQ", status, resultJson);

            return !result.HasError;
        }

        private async Task SaveStatusAsync(string cartId, string statusType, string status, string resultJson)
        {
            _context.EquipmentStatusHists.Add(new EquipmentStatusHist
            {
                EqpId = "RFID01.EQP.01",
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