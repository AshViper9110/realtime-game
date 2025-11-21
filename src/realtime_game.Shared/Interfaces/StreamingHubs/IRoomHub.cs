using MagicOnion;
using realtime_game.Server.StreamingHubs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace realtime_game.Shared.Interfaces.StreamingHubs
{
    public interface IRoomHub : IStreamingHub<IRoomHub, IRoomHubReceiver>
    {
        Task<Guid> GetConnectionId();
        Task<JoinedUser[]> JoinAsync(string roomName, int userId);

        Task LeaveAsync(string roomName);

        Task<List<string>> GetRoomListAsync();
    }
}