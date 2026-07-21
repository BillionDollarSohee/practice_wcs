using System;
using System.Text;
using Database;
using Database.Models;
using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 41 처리 핸들러 - BCR(바코드리더) 스캔결과 수신
    public class BcrReadHandler : IMessageHandler
    {
        public string Command => "41";
        public string Direction => "RECEIVE";

        private readonly WcsTwinContext _context;

        public BcrReadHandler(WcsTwinContext context)
        {
            _context = context;
        }

        public Task<byte[]> Handle(byte[] requestFrame)
        {
            var (_, fields) = ProtocolCodec.ParseFrame(requestFrame);
            string cartId = fields[0].Trim();

            _context.EquipmentStatusHists.Add(new EquipmentStatusHist
            {
                EqpId = "BCR01.EQP.01",
                StatusType = "BCR_READ",
                CartId = cartId,
                Status = "SCANNED",
                ResultJson = Encoding.ASCII.GetString(requestFrame),
                CreateDttm = DateTime.Now
            });
            _context.SaveChanges();

            Console.WriteLine($"BCR 스캔 저장 완료 - CartId: {cartId}");

            return Task.FromResult<byte[]>(null);
        }
    }
}