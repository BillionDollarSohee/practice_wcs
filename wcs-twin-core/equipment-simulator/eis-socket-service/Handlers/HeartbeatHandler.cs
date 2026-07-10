using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 00 처리 핸들러 - Heartbeat
    public class HeartbeatHandler : IMessageHandler
    {
        public string Command => "00";
        public string Direction => "RECEIVE";

        public byte[] Handle(byte[] requestFrame)
        {
            return ProtocolCodec.BuildFrame(
                "00",
                ProtocolCodec.PadField("00", 2)
            );
        }
    }
}