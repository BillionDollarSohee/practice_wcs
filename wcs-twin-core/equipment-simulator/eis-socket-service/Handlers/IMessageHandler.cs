namespace EisSocketService.Handlers
{
    public interface IMessageHandler
    {
        string Command { get; }
        string Direction { get; }
        byte[] Handle(byte[] requestFrame);
    }
}