using System;
using System.Text;
using Database;
using Database.Models;
using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 11 처리 핸들러 - 검사완료
    public class VisionProductCompleteHandler : IMessageHandler
    {
        public string Command => "11";
        public string Direction => "RECEIVE";

        private readonly WcsTwinContext _context;

        public VisionProductCompleteHandler(WcsTwinContext context)
        {
            _context = context;
        }

        public byte[] Handle(byte[] requestFrame)
        {
            var (_, fields) = ProtocolCodec.ParseFrame(requestFrame);
            string cartId = fields[0].Trim();
            string undercarJudgment = fields.Length > 11 ? fields[11].Trim() : "0";
            string result = undercarJudgment == "1" ? "OK" : "NG";

            string resultId = "VR" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

            _context.VisionResults.Add(new VisionResult
            {
                ResultId = resultId,
                CartId = cartId,
                OverallResult = result,
                InspectDttm = DateTime.Now
            });

            _context.EquipmentStatusHists.Add(new EquipmentStatusHist
            {
                EqpId = "VISION01.EQP.01",
                StatusType = "VISION_PRODUCT_COMPLETE",
                CartId = cartId,
                Status = "COMPLETED",
                ResultJson = Encoding.ASCII.GetString(requestFrame),
                CreateDttm = DateTime.Now
            });

            _context.SaveChanges();

            Console.WriteLine($"검사결과 저장 완료 - CartId: {cartId}, Result: {result}");

            return null;
        }
    }
}