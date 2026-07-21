using System;
using System.Text;
using Database;
using Database.Models;
using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 52 처리 핸들러 - 목적지 지시 ACK
    public class DestinationAckHandler : IMessageHandler
    {
        public string Command => "52";
        public string Direction => "RECEIVE";

        private readonly WcsTwinContext _context;

        public DestinationAckHandler(WcsTwinContext context)
        {
            _context = context;
        }

        public Task<byte[]> Handle(byte[] requestFrame)
        {
            var (_, fields) = ProtocolCodec.ParseFrame(requestFrame);
            string cartId = fields[0].Trim();
            string destinationNo = fields.Length > 1 ? fields[1].Trim() : "";

            _context.EquipmentStatusHists.Add(new EquipmentStatusHist
            {
                EqpId = "PLC01.EQP.01",
                StatusType = "DESTINATION_ACK",
                CartId = cartId,
                Status = "ACKED",
                ResultJson = Encoding.ASCII.GetString(requestFrame),
                CreateDttm = DateTime.Now
            });
            _context.SaveChanges();

            Console.WriteLine($"목적지 ACK 저장 완료 - CartId: {cartId}, DestinationNo: {destinationNo}");

            return Task.FromResult<byte[]>(null);
        }
    }
}