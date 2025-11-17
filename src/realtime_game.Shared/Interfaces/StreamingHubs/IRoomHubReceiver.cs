using realtime_game.Server.StreamingHubs;


namespace realtime_game.Shared.Interfaces.StreamingHubs
{
    public interface IRoomHubReceiver
    {
        //[クライアントに実装]
        //[サーバーから呼び出す]

        void OnJoin(JoinedUser user);
    }
}
