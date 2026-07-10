using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtocolCore;

namespace EisSocketService.Handlers
{
    public class HeartbeatHandler : IMessageHandler
    {
        public string Command => "00";
        public string Direction => "RECEIVE";

        public byte[] Handle(byte[] requestFrame)
        {
            // 응답 필드: STATUS(2) - 00 = 정상
            return ProtocolCore.ProtocolCodec.BuildFrame(
                "00",
                ProtocolCore.ProtocolCodec.PadField("00", 2)
            );
        }
    }
}
