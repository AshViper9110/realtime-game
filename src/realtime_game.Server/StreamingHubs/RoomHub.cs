using Cysharp.Runtime.Multicast;
using MagicOnion;
using MagicOnion.Server.Hubs;
using Microsoft.EntityFrameworkCore;
using realtime_game.Server.Models.Contexts;
using realtime_game.Server.Models.Entities;
using realtime_game.Shared.Interfaces.StreamingHubs;
using UnityEngine;

namespace realtime_game.Server.StreamingHubs
{
    public class RoomHub(RoomContextRepository roomContextRepository) :
        StreamingHubBase<IRoomHub, IRoomHubReceiver>, IRoomHub
    {
        private RoomContextRepository roomContextRepos;
        private RoomContext roomContext;
        private string roomNamed;


        public async Task<JoinedUser[]> JoinAsync(string roomName, int userId)
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"[JOIN REQUEST] roomName={roomName}, userId={userId}, connId={this.ConnectionId}");
            roomNamed = roomName; 
            // --- 1. ルームコンテキスト取得 / 作成 ---
            lock (roomContextRepos)
            {
                Console.WriteLine("[ROOM] Checking room context...");

                this.roomContext = roomContextRepos.getContext(roomName);

                if (this.roomContext == null)
                {
                    Console.WriteLine($"[ROOM] Room not found. Creating new room: {roomName}");
                    this.roomContext = roomContextRepos.CreateContext(roomName);
                    this.roomContext.OwnerConnectionId = this.ConnectionId; // 作成者
                }
                else
                {
                    Console.WriteLine($"[ROOM] Found existing room: {roomName}");
                }
            }

            // --- 2. グループ追加 ---
            Console.WriteLine($"[GROUP] Adding connection {this.ConnectionId} to room group...");
            this.roomContext.Group.Add(this.ConnectionId, Client);

            // --- 3. DB からユーザー取得 ---
            Console.WriteLine($"[DB] Fetching user data from DB: userId={userId}");
            GameDbContext context = new GameDbContext();
            User user = context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                Console.WriteLine($"[ERROR] User not found in database. userId={userId}");
                return Array.Empty<JoinedUser>();
            }

            Console.WriteLine($"[DB] User found: {user.Name} (ID={user.Id})");

            // --- 4. JoinedUser 生成 ---
            var joinedUser = new JoinedUser
            {
                ConnectionId = this.ConnectionId,
                UserData = user,
                IsOwner = this.ConnectionId == this.roomContext.OwnerConnectionId
            };

            // --- 5. ルームにユーザーデータ登録 ---
            Console.WriteLine($"[ROOM] Registering user to room data list...");
            var roomUserData = new RoomUserData()
            {
                JoinedUser = joinedUser,
                pos = Vector3.zero,
                rot = Quaternion.identity
            };
            this.roomContext.RoomUserDataList[this.ConnectionId] = roomUserData;

            // --- 6. 他参加者へ通知 ---
            Console.WriteLine($"[NOTIFY] Broadcasting join event to others in room...");
            this.roomContext.Group.Except([this.ConnectionId]).OnJoin(joinedUser);

            // --- 7. 状態ログ ---
            int count = this.roomContext.RoomUserDataList.Count;
            Console.WriteLine($"[ROOM STATUS] Room '{roomName}' now has {count} users.");
            Console.WriteLine($"[JOIN COMPLETE] {user.Name} joined room '{roomName}'.");
            Console.WriteLine("--------------------------------------------------");

            return this.roomContext.RoomUserDataList
                .Select(f => f.Value.JoinedUser)
                .ToArray();
        }

        protected override ValueTask OnConnected()
        {
            roomContextRepos = roomContextRepository;
            Console.WriteLine($"[CONNECTED] New client connected. ConnectionId={this.ConnectionId}");
            return default;
        }

        public Task<Guid> GetConnectionId()
        {
            Console.WriteLine($"[GET CONNECTION ID] {this.ConnectionId}");
            return Task.FromResult<Guid>(this.ConnectionId);
        }

        public Task LeaveAsync()
        {
            //　退室したことを全メンバーに通知
            this.roomContext.Group.All.OnLeave(this.ConnectionId);

            //　ルーム内のメンバーから自分を削除
            this.roomContext.Group.Remove(this.ConnectionId);

            //　ルームデータから退室したユーザーを削除
            this.roomContext.RoomUserDataList.Remove(this.ConnectionId);
            if (this.roomContext.RoomUserDataList.Count <= 0)
            {
                roomContextRepos.RemoveContext(roomNamed);
            }
            return Task.CompletedTask;
        }


        protected override ValueTask OnDisconnected()
        {
            Console.WriteLine($"[DISCONNECTED] connId={this.ConnectionId}");
            LeaveAsync();
            return CompletedTask;
        }
        public Task<List<string>> GetRoomListAsync()
        {
            lock (roomContextRepos)
            {
                // roomContextRepos にある全ルーム名を取得
                return Task.FromResult(roomContextRepos.GetAllRoomNames().ToList());
            }
        }

        public Task MoveAsync(Vector3 pos, Quaternion rot)
        {
            // 位置と回転を更新
            this.roomContext.RoomUserDataList[this.ConnectionId].pos = pos;
            this.roomContext.RoomUserDataList[this.ConnectionId].rot = rot;

            // 他のクライアントに通知
            this.roomContext.Group
                .Except(this.ConnectionId)
                .OnMove(this.ConnectionId, pos, rot);

            return Task.CompletedTask;
        }

        public Task ReadyAsync(bool isReady)
        {
            this.roomContext.RoomUserDataList[this.ConnectionId].JoinedUser.IsReady = isReady;

            // オーナーに通知
            this.roomContext.Group.Except(this.ConnectionId).OnUserReady(this.ConnectionId, isReady);

            return Task.CompletedTask;
        }
        public Task StartGameAsync()
        {
            // ★ルーム名は roomNamed
            string rn = roomNamed;

            // ★ユーザーデータ取得（JoinedUser）
            if (!roomContext.RoomUserDataList.TryGetValue(this.ConnectionId, out var roomUser))
                return Task.CompletedTask;

            // ★オーナーでなければ開始不可
            if (!roomUser.JoinedUser.IsOwner)
            {
                Console.WriteLine("[START GAME] Only owner can start the game.");
                return Task.CompletedTask;
            }

            Console.WriteLine($"[START GAME] Owner {this.ConnectionId} is starting the game in room {rn}");

            // ★ 全員へブロードキャスト送信
            roomContext.Group.All.OnGameStarted();

            return Task.CompletedTask;
        }
    }
}
