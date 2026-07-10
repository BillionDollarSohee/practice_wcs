using System;
using System.Text;
using EisSocketService.Data;
using EisSocketService.Models;
using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 41 처리 핸들러 - BCR(바코드리더) 스캔결과 수신
    // 실제 코드는 PId(4byte)+BcrNo(6byte)+Barcode(170byte) 바이너리 오프셋 파싱이지만,
    // 우리 프로토콜은 텍스트 기반이라 CarID(20) 필드 하나로 단순화했다.
    //
    // 응답은 없음 (실제 코드도 별도 ACK 없이 상태 저장만 수행)
    public class BcrReadHandler : IMessageHandler
    {
        public string Command => "41";
        public string Direction => "RECEIVE";

        private readonly WcsTwinContext _context;

        public BcrReadHandler(WcsTwinContext context)
        {
            _context = context;
        }

        public byte[] Handle(byte[] requestFrame)
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

            return null;
        }
    }
}