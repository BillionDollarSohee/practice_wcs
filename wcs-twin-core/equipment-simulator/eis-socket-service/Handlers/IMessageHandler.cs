using System.Threading.Tasks;

namespace EisSocketService.Handlers
{
    public interface IMessageHandler
    {
        string Command { get; }
        string Direction { get; }

        // Vision 핸들러 안에서 RFID(비동기) 호출을 기다려야 하므로 Task<byte[]>로 변경
        Task<byte[]> Handle(byte[] requestFrame);
    }
}