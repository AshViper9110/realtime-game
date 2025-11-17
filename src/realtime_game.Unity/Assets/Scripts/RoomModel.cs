using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Server.StreamingHubs;
using realtime_game.Shared.Interfaces.StreamingHubs;
using System;
using UnityEngine;

public class RoomModel : BaseModel, IRoomHubReceiver
{
    private GrpcChannelx channel;
    private IRoomHub roomHub;

    public Guid ConnectionId { get; set; }
    public Action<JoinedUser> OnJoinedUser {  get; set; }

    public async UniTask ConnectAsync()
    {
        channel = GrpcChannelx.ForAddress(ServerURL);
        roomHub = await StreamingHubClient.ConnectAsync<IRoomHub, IRoomHubReceiver>(channel, this);
        this.ConnectionId = await roomHub.GetConnectionId();
    }
    public async UniTask DisconnectAsync()
    {
        if (roomHub != null) await roomHub.DisposeAsync();
        if (channel != null) await channel.ShutdownAsync();
        roomHub = null; channel = null;
    }

    async void OnDestroy() { DisconnectAsync(); }

    //　入室
    public async UniTask JoinAsync(string roomName, int userId)
    {
        JoinedUser[] users = await roomHub.JoinAsync(roomName, userId);
        foreach (var user in users)
        {
            if (OnJoinedUser != null)
            {
                Debug.Log($"=== User Joined ===");
                Debug.Log($"ConnectionId: {ConnectionId}");
                Debug.Log($"UserId: {user.UserData.Id}");
                Debug.Log($"UserName: {user.UserData.Name}");
                OnJoinedUser(user);
            }
        }
    }

    //　入室通知 (IRoomHubReceiverインタフェースの実装)
    public void OnJoin(JoinedUser user)
    {
        if (OnJoinedUser != null)
        {
            Debug.Log($"=== User Joined ===");
            Debug.Log($"ConnectionId: {ConnectionId}");
            Debug.Log($"UserId: {user.UserData.Id}");
            Debug.Log($"UserName: {user.UserData.Name}");
            OnJoinedUser(user);
        }
    }
}
