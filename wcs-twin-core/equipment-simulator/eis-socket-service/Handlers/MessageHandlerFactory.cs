using System;
using System.Collections.Generic;

namespace EisSocketService.Handlers
{
    public class MessageHandlerFactory
    {
        private readonly Dictionary<string, IMessageHandler> _handlers;

        public MessageHandlerFactory(IEnumerable<IMessageHandler> handlers)
        {
            _handlers = new Dictionary<string, IMessageHandler>();
            foreach (var handler in handlers)
            {
                RegisterHandler(handler);
            }
        }

        private void RegisterHandler(IMessageHandler handler)
        {
            var key = $"{handler.Command}_{handler.Direction}";
            _handlers[key] = handler;
        }

        public bool HasHandler(string command, string direction)
        {
            var key = $"{command}_{direction}";
            return _handlers.ContainsKey(key);
        }

        public IMessageHandler GetHandler(string command, string direction)
        {
            var key = $"{command}_{direction}";
            if (_handlers.TryGetValue(key, out var handler))
            {
                return handler;
            }
            throw new NotSupportedException($"핸들러 없음 - Command: {command}, Direction: {direction}");
        }
    }
}