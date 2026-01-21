using Cysharp.Threading.Tasks;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Server.StreamingHubs;
using realtime_game.Shared.Interfaces.StreamingHubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class RoomModel : BaseModel, IRoomHubReceiver
{
    private GrpcChannelx channel;
    public IRoomHub roomHub;

    public Guid ConnectionId { get; set; }

    public Action<JoinedUser> OnJoinedUser { get; set; }
    public Action<Guid> OnLeavedUser { get; set; }
    public Action OnLeftUserAll { get; set; }
    public Action<Vector3, Quaternion> OnMoveUser { get; set; }
    public Action<List<JoinedUser>> OnGameStartedReceived { get; set; }
    public Action<Guid,bool> OnReadyUser { get; set; }
    public Action<List<Guid>> OnGoalUser { get; set; }

    public Action<int> OnItemObjectUser {  set; get; }

    private readonly Dictionary<Guid, JoinedUser> userTable = new();

    GameDirector gameDirector;
    UserListUI userListUI;

    private void Start()
    {
        gameDirector = GetComponent<GameDirector>();
        userListUI = GetComponent<UserListUI>();
    }

    // ============================
    //     StreamingHub 接続
    // ============================
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
    }

    async void OnDestroy() { await DisconnectAsync(); }


    // ============================
    //     Join / Leave
    // ============================
    public async UniTask JoinAsync(string roomName, string userId)
    {
        JoinedUser[] users = await roomHub.JoinAsync(roomName, userId);

        foreach (var user in users)
        {
            userListUI.AddUser(user);
            userTable[user.ConnectionId] = user;

            if (user.ConnectionId != ConnectionId)
                OnJoinedUser?.Invoke(user);
        }
    }

    public async UniTask LeaveAsync()
    {
        await roomHub.LeaveAsync();
        userListUI.SetList();
        OnLeftUserAll?.Invoke();
    }

    public async UniTask StartGameAsync(int vehicleIndex)
    {
        await roomHub.StartGameAsync(vehicleIndex);
        //OnGameStartedReceived?.Invoke();
    }

    public async UniTask GoalAsync()
    {
        await roomHub.AllGoalAsync(ConnectionId);
    }


    // ============================
    //     Server → Client Notice
    // ============================

    public void OnJoin(JoinedUser user)
    {
        Debug.Log($"=== User Joined === {user.ConnectionId} / {user.UserName}");

        userListUI.AddUser(user);
        userTable[user.ConnectionId] = user;

        OnJoinedUser?.Invoke(user);
    }

    public void OnLeave(Guid connectionId)
    {
        userListUI.RemoveUser(connectionId);
        OnLeavedUser?.Invoke(connectionId);
    }

    public void OnMove(Guid connectionId, Vector3 pos, Quaternion rot)
    {
        gameDirector.OnMoveCharacter(connectionId, pos, rot);
    }

    public void OnUserReady(Guid connectionId, bool isReady, int vehicleIndex)
    {
        if (userTable.TryGetValue(connectionId, out var user))
        {
            user.IsReady = isReady;
            user.VehicleIndex = vehicleIndex;   // ★ ここが重要
            Debug.Log($"User {user.UserName} ready={isReady} vehicle={vehicleIndex}");
        }
    }


    // ★ これがサーバーの OnGameStarted() 受信
    public void OnGameStarted(List<JoinedUser> users)
    {
        foreach (var u in users)
            userTable[u.ConnectionId] = u;   // 最新値で上書き

        OnGameStartedReceived?.Invoke(users);
    }
    public void OnGameGoaled(List<Guid> goalOrder)
    {
        OnGoalUser?.Invoke(goalOrder);
    }

    // ============================
    //     Helper API
    // ============================
    public bool CanStartGame()
    {
        return userTable.Values.All(u => u.IsOwner || u.IsReady);
    }

    public JoinedUser GetJoinedUser(Guid connectionId)
    {
        userTable.TryGetValue(connectionId, out var user);
        return user;
    }

    public async UniTask<List<string>> GetRoomListAsync()
    {
        if (roomHub == null)
            return new List<string>();

        return await roomHub.GetRoomListAsync();
    }

    public async UniTask SendReadyAsync(bool isReady, int vehicleIndex)
    {
        await roomHub.ReadyAsync(isReady, vehicleIndex);
    }

    public async Task MoveAsync(Vector3 pos, Quaternion rot)
    {
        await roomHub.MoveAsync(pos, rot);
    }

    public async UniTask ItemObjectAsync(int id)
    {
        await roomHub.ItemObjectAsync(id);
    }

    public void OnItemObject(int id)
    {
        OnItemObjectUser.Invoke(id);
    }
}

