using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;
using Database.Models;
using ProtocolCore;
using RfidControllService;

namespace EisSocketService.Handlers
{
    // Command 03 처리 핸들러 - 대차정보 요청
    //
    // 실제 흐름: 입고 -> 대기줄 진입(기록만) -> RFID 도착확인 -> 비전검사 시작 지시
    //
    // 라인(트림/파이널/도어)마다 물리적으로 별도의 비전 카메라가 있고, 카메라는 자기 라인
    // 대차를 한 번에 1대씩만 순서대로 보낸다(equipment-simulator가 라인 전용 워커로 그렇게 동작함).
    // 그래서 WCS는 "동시에 같은 라인 대차가 여러 대 들어올 수 있는지"를 방어할 필요가 없고,
    // 지금 들어온 요청을 그대로 처리하기만 하면 된다 - 별도의 동시성 제어 게이트가 없다.
    public class VisionCartInfoRequestHandler : IMessageHandler
    {
        public string Command => "03";
        public string Direction => "RECEIVE";

        private static readonly string[] LineTypes = { "TR", "FL", "DR" };

        private readonly WcsTwinContext _wcsTwinContext;
        private readonly RFIDControllService _rfidControllService;

        public VisionCartInfoRequestHandler(
            WcsTwinContext wcsTwinContext,
            RFIDControllService rfidControllService)
        {
            _wcsTwinContext = wcsTwinContext;
            _rfidControllService = rfidControllService;
        }

        public async Task<byte[]> Handle(byte[] requestFrame)
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

            // CART_MASTER.LINE_TYPE에 등록 안 된 값(오타/누락)이면 여기서 걸러진다
            if (!LineTypes.Contains(cart.LineType))
            {
                Console.WriteLine($"[흐름] 알 수 없는 라인타입 - CartId: {cartId}, LineType: {cart.LineType}");
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
                    "03"
                );
            }

            string eqpId = $"VISION.{cart.LineType}";

            // 대기줄 진입 기록 (화면 표시용 - 나중에 DPS가 순서를 정해서 넘겨주는 지점이 될 자리)
            _wcsTwinContext.EquipmentStatusHists.Add(new EquipmentStatusHist
            {
                EqpId = eqpId,
                StatusType = "INSPECTION_QUEUE",
                CartId = cartId,
                Status = "WAITING",
                ResultJson = "대기줄 진입",
                CreateDttm = DateTime.Now
            });
            _wcsTwinContext.SaveChanges();
            Console.WriteLine($"[흐름] 대기줄 진입 - CartId: {cartId}, LineType: {cart.LineType}");

            // RFID로 대차 도착확인 (실패하면 내부에서 재시도/알람 대기까지 처리하고 성공한 뒤에 리턴한다)
            await _rfidControllService.RequestReadTagAsync(cartId, cart.LineType);

            Console.WriteLine($"[흐름] RFID 도착확인 성공 - CartId: {cartId}, 비전검사 시작 지시");

            // 비전검사 진행중 - Command 11(검사완료)이 올 때까지의 구간을 화면에 명시적으로 보여주기 위한 기록
            _wcsTwinContext.EquipmentStatusHists.Add(new EquipmentStatusHist
            {
                EqpId = eqpId,
                StatusType = "VISION_INSPECTING",
                CartId = cartId,
                Status = "IN_PROGRESS",
                ResultJson = "비전검사 진행중",
                CreateDttm = DateTime.Now
            });
            _wcsTwinContext.SaveChanges();

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
