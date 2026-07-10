using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EisSocketService.Handlers
{
    // 실제 회사 프로젝트의 MessageHandlerFactory 패턴을 축소 적용
    // Command + Direction 조합을 키로 핸들러를 미리 등록해두고, 수신 시 바로 찾아쓴다.
    public class MessageHandlerFactory
        
    {
        private readonly Dictionary<string, IMessageHandler> _messageHandlers;

        public MessageHandlerFactory(IEnumerable<IMessageHandler> messageHandlers)
        { 
            _messageHandlers = new Dictionary<string, IMessageHandler>();
            foreach (var handler in messageHandlers)
            {
                RegisterHandler(handler);
            }
        }

        private void RegisterHandler(IMessageHandler messageHandler)
        {
            var key = $"{messageHandler.Command}_{messageHandler.Direction}";
            _messageHandlers[key] = messageHandler;
        }

        public bool HasHandler(string command, string direction)
        {
            var key = $"{command}_{direction}";
            return _messageHandlers.ContainsKey(key);
        }

        public IMessageHandler GetMessageHandler(string command, string direction)
        { 
            var key = $"{command}_{ direction}";
            if (_messageHandlers.TryGetValue(key, out var message))
            {
                return message;
            }
            throw new NotSupportedException($"핸들러 없음 - command: {command}, Direction: {direction}");
        }
    }
}
