using realtime_game.Server.StreamingHubs;
using System;


namespace realtime_game.Shared.Interfaces.StreamingHubs
{
    public interface IRoomHubReceiver
    {
        //[クライアントに実装]
        //[サーバーから呼び出す]

        void OnJoin(JoinedUser user);

        void OnLeave(Guid connectionId);
    }
}
