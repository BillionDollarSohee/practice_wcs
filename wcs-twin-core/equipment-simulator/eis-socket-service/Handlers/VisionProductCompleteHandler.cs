using System;
using System.Text;
using System.Threading.Tasks;
using Database;
using Database.Models;
using ProtocolCore;
using RfidControllService;

namespace EisSocketService.Handlers
{
    // Command 11 처리 핸들러 - 검사완료
    //
    // 실제 흐름: 비전검사 종료 -> OK면 RFID 태그에 결과 기록 -> 출고
    //
    // 동시성 제어 게이트가 없다 - equipment-simulator가 라인 전용 워커로 대차를 순서대로 보내주므로
    // WCS는 지금 들어온 요청만 처리하면 된다 (VisionCartInfoRequestHandler와 동일한 이유).
    //
    // 중요: 반드시 ACK를 응답으로 돌려줘야 한다. RFID 쓰기는 재시도/알람 때문에 오래 걸릴 수 있는데,
    // 설비(시뮬레이터)가 이 ACK를 기다리지 않고 다음 대차로 넘어가버리면, 그 사이에 같은 라인의
    // 다음 대차가 진입해서 "두 대가 동시에 검사대에 있는" 것처럼 되어버린다(라인 전용 워커가 순차적으로
    // 도는 전제 자체가 깨짐). ACK를 기다리게 해야 라인 워커의 순차성이 Command 11 처리까지 보장된다.
    public class VisionProductCompleteHandler : IMessageHandler
    {
        public string Command => "11";
        public string Direction => "RECEIVE";

        private readonly WcsTwinContext _context;
        private readonly RFIDControllService _rfidControllService;

        public VisionProductCompleteHandler(
            WcsTwinContext context,
            RFIDControllService rfidControllService)
        {
            _context = context;
            _rfidControllService = rfidControllService;
        }

        public async Task<byte[]> Handle(byte[] requestFrame)
        {
            var (_, fields) = ProtocolCodec.ParseFrame(requestFrame);
            string cartId = fields[0].Trim();
            // Command 03 응답 때 실어보낸 lineType을 설비가 Command 11에도 그대로 실어보낸다 (fields[5])
            string lineType = fields.Length > 5 ? fields[5].Trim() : "";
            string undercarJudgment = fields.Length > 11 ? fields[11].Trim() : "0";
            string result = undercarJudgment == "1" ? "OK" : "NG";
            string eqpId = string.IsNullOrEmpty(lineType) ? "VISION.UNKNOWN" : $"VISION.{lineType}";

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
                EqpId = eqpId,
                StatusType = "VISION_PRODUCT_COMPLETE",
                CartId = cartId,
                Status = "COMPLETED",
                ResultJson = Encoding.ASCII.GetString(requestFrame),
                CreateDttm = DateTime.Now
            });

            _context.SaveChanges();

            Console.WriteLine($"검사결과 저장 완료 - CartId: {cartId}, Result: {result}");

            if (result == "OK")
            {
                Console.WriteLine($"[흐름] RFID 결과기록 시작 - CartId: {cartId}");
                bool writeOk = await _rfidControllService.RequestWriteTagAsync(cartId, lineType);

                _context.EquipmentStatusHists.Add(new EquipmentStatusHist
                {
                    EqpId = "PLC01.EQP.01",
                    StatusType = "DISCHARGED",
                    CartId = cartId,
                    Status = writeOk ? "COMPLETED" : "FAILED",
                    ResultJson = $"RFID 기록 {(writeOk ? "성공" : "실패")} 후 출고 처리",
                    CreateDttm = DateTime.Now
                });
                _context.SaveChanges();

                Console.WriteLine($"[흐름] 출고 처리 - CartId: {cartId}, RFID기록:{(writeOk ? "성공" : "실패")}");
            }
            else
            {
                Console.WriteLine($"[흐름] 검사 NG - CartId: {cartId}, RFID 기록/출고 생략");
            }

            // 검사완료 처리가 (재시도 포함) 다 끝난 뒤에야 ACK를 보낸다 - 설비는 이걸 받고서야 다음 대차로 넘어간다
            return ProtocolCodec.BuildFrame(
                "11",
                ProtocolCodec.PadField(cartId, 20),
                "1"
            );
        }
    }
}
