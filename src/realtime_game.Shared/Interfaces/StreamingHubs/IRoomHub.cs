using MagicOnion;
using realtime_game.Server.StreamingHubs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace realtime_game.Shared.Interfaces.StreamingHubs
{
    public interface IRoomHub : IStreamingHub<IRoomHub, IRoomHubReceiver>
    {
        Task<Guid> GetConnectionId();
        Task<JoinedUser[]> JoinAsync(string roomName, int userId);

        Task LeaveAsync();

        Task<List<string>> GetRoomListAsync();

        Task MoveAsync(Vector3 pos, Quaternion rot);

        Task ReadyAsync(bool isReady);

        Task StartGameAsync();
    }
}