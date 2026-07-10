using System;
using System.Linq;
using System.Text;
using EisSocketService.Data;
using EisSocketService.Models;
using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 03 처리 핸들러 - 대차정보 요청
    // 설비 -> 서버: 대차정보 요청 (CarID 20바이트)
    // 서버 -> 설비: Command 01로 대차정보 응답
    //
    // 실제 스펙 응답 필드 순서:
    // CarID(20), BatchNo(12), BodyNo(10), CarType(4), LineType(2),
    // ItemCount(4), Item(가변), Result(1), ErrorCode(2)
    //
    // 미니 프로젝트라 BatchNo/BodyNo/CarType/Item은 빈 값으로 보내되,
    // 자리(바이트 수)는 실제 스펙과 동일하게 맞춘다.
    public class VisionCartInfoRequestHandler : IMessageHandler
    {
        public string Command => "03";
        public string Direction => "RECEIVE";

        private readonly WcsTwinContext _wcsTwinContext;

        public VisionCartInfoRequestHandler(WcsTwinContext wcsTwinContext)
        {
            _wcsTwinContext = wcsTwinContext;
        }

        public byte[] Handle(byte[] requestFrame)
        {
            var (_, fields) = ProtocolCodec.ParseFrame(requestFrame);
            string cartId = fields[0].Trim();

            CartMaster cart = _wcsTwinContext.CartMasters.FirstOrDefault(c => c.CartId == cartId);

            _wcsTwinContext.EquipmentStatusHists.Add(new EquipmentStatusHist
            {
                EqpId = "VISION01.EQP.01",
                StatusType = "VISION_CART_INFO_REQ",
                CartId = cartId,
                Status = "REQUESTED",
                ResultJson = Encoding.ASCII.GetString(requestFrame),
                CreateDttm = DateTime.Now
            });
            _wcsTwinContext.SaveChanges();

            if (cart == null)
            {
                // 미등록대차 - 에러코드 01
                return ProtocolCodec.BuildFrame(
                    "01",
                    ProtocolCodec.PadField(cartId, 20),
                    ProtocolCodec.PadField("", 12),
                    ProtocolCodec.PadField("", 10),
                    ProtocolCodec.PadField("", 4),
                    ProtocolCodec.PadField("", 2),
                    ProtocolCodec.PadField("0000", 4),
                    "",
                    "0",
                    "01"
                );
            }

            return ProtocolCodec.BuildFrame(
                "01",
                ProtocolCodec.PadField(cartId, 20),
                ProtocolCodec.PadField("", 12),
                ProtocolCodec.PadField("", 10),
                ProtocolCodec.PadField("", 4),
                ProtocolCodec.PadField(cart.LineType, 2),
                ProtocolCodec.PadField("0000", 4),
                "",
                "1",
                "00"
            );
        }
    }
}