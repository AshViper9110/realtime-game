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
        Task<JoinedUser[]> JoinAsync(string roomName, string userName);

        Task LeaveAsync();

        Task<List<string>> GetRoomListAsync();

        Task MoveAsync(Vector3 pos, Quaternion rot);

        Task ReadyAsync(bool isReady, int vehicleIndex);

        Task StartGameAsync(int vehicleIndex);

        Task AllGoalAsync(Guid guid);
    }
}