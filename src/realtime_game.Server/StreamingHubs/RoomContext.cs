using Cysharp.Runtime.Multicast;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
        public Guid OwnerConnectionId { get; set; } // ルーム作成者

        public RoomContext(IMulticastGroupProvider groupProvider, string roomName)
        {
            Id = Guid.NewGuid();
            Name = roomName;
            Group = groupProvider.GetOrAddSynchronousGroup<Guid, IRoomHubReceiver>(roomName);
        }

        public void Dispose() { Group.Dispose(); }
    }
}
