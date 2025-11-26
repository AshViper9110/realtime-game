using Cysharp.Runtime.Multicast;
using realtime_game.Shared.Interfaces.StreamingHubs;


namespace realtime_game.Server.StreamingHubs
{
    public class RoomContext : IDisposable
    {
        public Guid Id { get; }
        public string Name { get; }
        public IMulticastSyncGroup<Guid, IRoomHubReceiver> Group { get; }
        public Dictionary<Guid, RoomUserData> RoomUserDataList { get; } =
            new Dictionary<Guid, RoomUserData>();

        public RoomContext(IMulticastGroupProvider groupProvider, string roomName)
        {
            Id = Guid.NewGuid();
            Name = roomName;
            Group = groupProvider.GetOrAddSynchronousGroup<Guid, IRoomHubReceiver>(roomName);
        }

        public void Dispose() { Group.Dispose(); }
    }
}
