using Cysharp.Threading.Tasks;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Server.StreamingHubs;
using realtime_game.Shared.Interfaces.StreamingHubs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class RoomModel : BaseModel, IRoomHubReceiver
{
    private GrpcChannelx channel;
    public IRoomHub roomHub;

    public Guid ConnectionId { get; set; }

    public Action<JoinedUser> OnJoinedUser { get; set; }
    public Action<JoinedUser> OnLeavedUser { get; set; }

    public Action<Vector3, Quaternion> OnMoveUser { get; set; }

    // 接続ユーザー保持（OnLeave のために必要）
    private readonly Dictionary<Guid, JoinedUser> userTable = new();

    GameDirector gameDirector;

    private void Start()
    {
        gameDirector = GetComponent<GameDirector>(); // 自分にアタッチされている場合
    }

    public async UniTask ConnectAsync()
    {
        Debug.Log("Connecting to server...");

        channel = GrpcChannelx.ForAddress(ServerURL);
        roomHub = await StreamingHubClient.ConnectAsync<IRoomHub, IRoomHubReceiver>(channel, this);
        this.ConnectionId = await roomHub.GetConnectionId();

        Debug.Log($"Connected! CID={this.ConnectionId}");
    }

    public async UniTask DisconnectAsync()
    {
        if (roomHub != null) await roomHub.DisposeAsync();
        if (channel != null) await channel.ShutdownAsync();
        roomHub = null;
        channel = null;
    }

    async void OnDestroy() { await DisconnectAsync(); }

    // --- 参加 ---
    public async UniTask JoinAsync(string roomName, int userId)
    {
        JoinedUser[] users = await roomHub.JoinAsync(roomName, userId);

        foreach (var user in users)
        {
            if (user.ConnectionId == this.ConnectionId)
                continue; // ★ 自分はスキップ

            userTable[user.ConnectionId] = user;
            OnJoinedUser?.Invoke(user);
        }
    }

    // --- 退出 ---
    public async UniTask LeaveAsync(string roomName)
    {
        await roomHub.LeaveAsync(roomName);
    }


    public async UniTask MoveAsync(Vector3 pos, Quaternion rot)
    {
        if (roomHub == null) return;
        await roomHub.MoveAsync(pos, rot);
    }

    // --- サーバーからの通知 ---
    public void OnLeave(Guid connectionId)
    {
        Debug.Log($"=== User Leaved === {connectionId}");

        if (userTable.TryGetValue(connectionId, out var user))
        {
            // Unityへ通知
            OnLeavedUser?.Invoke(user);

            // テーブル削除
            userTable.Remove(connectionId);
        }
    }

    // --- サーバーからの通知 ---
    public void OnJoin(JoinedUser user)
    {
        Debug.Log($"=== User Joined === {user.ConnectionId} / {user.UserData.Name}");

        userTable[user.ConnectionId] = user; // 保持

        OnJoinedUser?.Invoke(user);
    }

    public void OnMove(Guid connectionId, Vector3 pos, Quaternion rot)
    {
        gameDirector.OnMoveCharacter(connectionId, pos, rot);
    }

    public async UniTask<List<string>> GetRoomListAsync()
    {
        if (roomHub == null)
        {
            Debug.LogWarning("Not connected to server yet!");
            return new List<string>();
        }

        return await roomHub.GetRoomListAsync();
    }
}
