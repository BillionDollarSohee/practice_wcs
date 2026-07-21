using System;
using System.Text;
using Database;
using Database.Models;
using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 61 처리 핸들러 - 배출완료
    public class DischargedHandler : IMessageHandler
    {
        public string Command => "61";
        public string Direction => "RECEIVE";

        private readonly WcsTwinContext _context;

        public DischargedHandler(WcsTwinContext context)
        {
            _context = context;
        }

        public Task<byte[]> Handle(byte[] requestFrame)
        {
            var (_, fields) = ProtocolCodec.ParseFrame(requestFrame);
            string cartId = fields[0].Trim();
            string destinationNo = fields.Length > 1 ? fields[1].Trim() : "";
            string completedCd = fields.Length > 2 ? fields[2].Trim() : "";

            _context.EquipmentStatusHists.Add(new EquipmentStatusHist
            {
                EqpId = "PLC01.EQP.01",
                StatusType = "DISCHARGED",
                CartId = cartId,
                Status = "COMPLETED",
                ResultJson = Encoding.ASCII.GetString(requestFrame),
                CreateDttm = DateTime.Now
            });
            _context.SaveChanges();

            Console.WriteLine($"배출완료 저장 - CartId: {cartId}, DestinationNo: {destinationNo}, CompletedCd: {completedCd}");

            return Task.FromResult<byte[]>(null);
        }
    }
}