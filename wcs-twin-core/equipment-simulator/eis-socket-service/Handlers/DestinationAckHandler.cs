using System;
using System.Text;
using EisSocketService.Data;
using EisSocketService.Models;
using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 52 처리 핸들러 - 목적지 지시 ACK
    // 실제 코드는 ECS_ORDER 테이블과 연동해 오더 상태를 갱신하지만,
    // 우리 프로젝트엔 오더 테이블이 없어 CartId 기준 상태 이력만 남긴다.
    //
    // 필드: CarID(20), DestinationNo(4)
    public class DestinationAckHandler : IMessageHandler
    {
        public string Command => "52";
        public string Direction => "RECEIVE";

        private readonly WcsTwinContext _context;

        public DestinationAckHandler(WcsTwinContext context)
        {
            _context = context;
        }

        public byte[] Handle(byte[] requestFrame)
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

            return null;
        }
    }
}