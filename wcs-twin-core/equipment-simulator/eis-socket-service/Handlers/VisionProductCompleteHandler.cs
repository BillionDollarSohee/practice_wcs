using System;
using System.Text;
using EisSocketService.Data;
using EisSocketService.Models;
using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 11 처리 핸들러 - 검사완료
    // 설비 -> 서버: 검사완료
    //
    // 실제 스펙 요청 필드 순서:
    // CarID(20), ItemLength(4), BatchNo(12), BodyNo(10), CarType(4), LineType(2),
    // 년월일시분초(14), Item개수(4), Item(가변), ImgPath(가변), 하부대차ImgFile(가변),
    // 하부대차판정결과(1), 비전강제완료여부(1)
    //
    // 미니 프로젝트에서는 Item/ImgPath 상세 파싱은 생략하고,
    // 하부대차판정결과(1=OK, 0=NG) 필드만 판정에 사용한다.
    //
    // 참고: 이번 단계에서도 응답은 보내지 않는다.
    // 시뮬레이터가 11 전송 후 응답을 기다리지 않는 구조라서,
    // 여기서 응답을 보내면 다음 사이클의 01 응답과 뒤섞여 파싱 오류가 날 수 있다.
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