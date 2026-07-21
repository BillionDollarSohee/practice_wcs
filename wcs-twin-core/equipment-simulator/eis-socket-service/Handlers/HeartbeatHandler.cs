using ProtocolCore;

namespace EisSocketService.Handlers
{
    // Command 00 처리 핸들러 - Heartbeat
    public class HeartbeatHandler : IMessageHandler
    {
        public string Command => "00";
        public string Direction => "RECEIVE";

        public Task<byte[]> Handle(byte[] requestFrame)
        {
            byte[] response = ProtocolCodec.BuildFrame(
                "00",
                ProtocolCodec.PadField("00", 2)
            );
            return Task.FromResult(response);
        }
    }
}