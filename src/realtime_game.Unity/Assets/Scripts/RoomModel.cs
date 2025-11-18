using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Server.StreamingHubs;
using realtime_game.Shared.Interfaces.StreamingHubs;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomModel : BaseModel, IRoomHubReceiver
{
    private GrpcChannelx channel;
    public IRoomHub roomHub;

    public Guid ConnectionId { get; set; }

    public Action<JoinedUser> OnJoinedUser { get; set; }
    public Action<JoinedUser> OnLeavedUser { get; set; }

    // 接続中ユーザーを保持（OnLeave のときために必要）
    private readonly Dictionary<Guid, JoinedUser> userTable = new();

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
        roomHub = null;
        channel = null;
    }

    async void OnDestroy() { DisconnectAsync(); }

    // --- 入室 ---
    public async UniTask JoinAsync(string roomName, int userId)
    {
        JoinedUser[] users = await roomHub.JoinAsync(roomName, userId);

        foreach (var user in users)
        {
            userTable[user.ConnectionId] = user; // ←保持

            OnJoinedUser?.Invoke(user);        }
    }

    // --- 退室 ---
    public async UniTask LeaveAsync(string roomName)
    {
        await roomHub.LeaveAsync(roomName);
        roomHub = null;
    }

    // --- サーバから「誰かが抜けた」通知 ---
    public void OnLeave(Guid connectionId)
    {
        Debug.Log($"=== User Leaved === {connectionId}");

        if (userTable.TryGetValue(connectionId, out var user))
        {
            // Unity側へ通知
            OnLeavedUser?.Invoke(user);

            // テーブルから削除
            userTable.Remove(connectionId);
        }
    }

    // --- サーバからの入室通知 ---
    public void OnJoin(JoinedUser user)
    {
        Debug.Log($"=== User Joined === {user.ConnectionId} / {user.UserData.Name}");

        userTable[user.ConnectionId] = user; // ←保持

        OnJoinedUser?.Invoke(user);
    }
}
