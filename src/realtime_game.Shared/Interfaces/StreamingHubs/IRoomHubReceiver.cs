using realtime_game.Server.StreamingHubs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace realtime_game.Shared.Interfaces.StreamingHubs
{
    public interface IRoomHubReceiver
    {
        //[クライアントに実装]
        //[サーバーから呼び出す]
        void OnJoin(JoinedUser user);
        void OnLeave(Guid connectionId);
        void OnMove(Guid connectionId, Vector3 pos, Quaternion rot);
        //void OnLeftUserAll();
        void OnUserReady(Guid connectionId, bool isReady, int vehicleIndex);
        void OnGameStarted(List<JoinedUser> users);
        void OnGameGoaled(List<Guid> goalOrder);
        void OnItemObject(int id);
    }
}