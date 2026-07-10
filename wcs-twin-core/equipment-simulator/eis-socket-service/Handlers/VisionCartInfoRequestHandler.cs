using System;
using System.Linq;
using System.Text;
using Database;
using Database.Models;
using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 03 처리 핸들러 - 대차정보 요청
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