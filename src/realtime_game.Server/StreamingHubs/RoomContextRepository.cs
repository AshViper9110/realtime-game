using Cysharp.Runtime.Multicast;
using System.Collections.Concurrent;
using MagicOnion.Server.Hubs;
using realtime_game.Server.Models.Contexts;
using realtime_game.Shared.Interfaces.StreamingHubs;
using System.Xml.Linq;


namespace realtime_game.Server.StreamingHubs
{
    public class RoomContextRepository(IMulticastGroupProvider groupProvider)
    {
        private readonly ConcurrentDictionary<string, RoomContext> contexts =
            new ConcurrentDictionary<string, RoomContext>();

        public RoomContext CreateContext(string roomName)
        {
            var context = new RoomContext(groupProvider, roomName);
            contexts[roomName] = context;
            return context;
        }

        public RoomContext getContext(string roomName)
        {
            if (!contexts.ContainsKey(roomName)) return null;
            return contexts[roomName];
        }

        public void RemoveContext(string roomName)
        {
            if (contexts.Remove(roomName, out var RoomContext)) RoomContext?.Dispose();
        }
    }
}
