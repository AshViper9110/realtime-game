using Cysharp.Runtime.Multicast;
using MagicOnion.Server.Hubs;
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


        public async Task<JoinedUser[]> JoinAsync(string roomName, string userName)
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"[JOIN REQUEST] roomName={roomName}, userName={userName}, connId={this.ConnectionId}");
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
            /*Console.WriteLine($"[DB] Fetching user data from DB: userId={userId}");
            GameDbContext context = new GameDbContext();
            User user = context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                Console.WriteLine($"[ERROR] User not found in database. userId={userId}");
                return Array.Empty<JoinedUser>();
            }

            Console.WriteLine($"[DB] User found: {user.Name} (ID={user.Id})");*/

            // --- 4. JoinedUser 生成 ---
            var joinedUser = new JoinedUser
            {
                ConnectionId = this.ConnectionId,
                UserName = userName,
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
            Console.WriteLine($"[JOIN COMPLETE] {userName} joined room '{roomName}'.");
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
            // roomContext が null の場合 → Join 完了前に切断されたケースなので何もしない
            if (roomContext == null)
                return Task.CompletedTask;

            // Group が null の可能性もある
            if (roomContext.Group != null)
            {
                try
                {
                    roomContext.Group.All.OnLeave(this.ConnectionId);
                }
                catch
                {
                    // Broadcast 中に切断されている場合は無視
                }

                roomContext.Group.Remove(this.ConnectionId);
            }

            // ルームデータの削除
            if (roomContext.RoomUserDataList != null)
            {
                roomContext.RoomUserDataList.Remove(this.ConnectionId);

                if (roomContext.RoomUserDataList.Count <= 0)
                {
                    roomContextRepos.RemoveContext(roomNamed);
                }
            }

            return Task.CompletedTask;
        }
        protected override async ValueTask OnDisconnected()
        {
            Console.WriteLine($"[DISCONNECTED] connId={this.ConnectionId}");
            await LeaveAsync();
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
            if (!roomContext.RoomUserDataList.TryGetValue(this.ConnectionId, out var user))
                return Task.CompletedTask;

            // 位置と回転を更新
            this.roomContext.RoomUserDataList[this.ConnectionId].pos = pos;
            this.roomContext.RoomUserDataList[this.ConnectionId].rot = rot;

            // 他のクライアントに通知
            this.roomContext.Group
                .Except(this.ConnectionId)
                .OnMove(this.ConnectionId, pos, rot);

            return Task.CompletedTask;
        }

        public Task ReadyAsync(bool isReady, int vehicleIndex)
        {
            var user = this.roomContext.RoomUserDataList[this.ConnectionId].JoinedUser;
            user.IsReady = isReady;
            user.VehicleIndex = vehicleIndex;

            this.roomContext.Group.Except(this.ConnectionId)
                .OnUserReady(this.ConnectionId, isReady, vehicleIndex);   // ★ 追加

            return Task.CompletedTask;
        }
        public Task StartGameAsync(int vehicleIndex)
        {
            var user1 = this.roomContext.RoomUserDataList[this.ConnectionId].JoinedUser;
            user1.VehicleIndex = vehicleIndex;
            // ★ユーザーデータ取得
            if (!roomContext.RoomUserDataList.TryGetValue(this.ConnectionId, out var roomUser))
                return Task.CompletedTask;

            // ★オーナーでなければ開始不可
            if (!roomUser.JoinedUser.IsOwner)
            {
                Console.WriteLine("[START GAME] Only owner can start the game.");
                return Task.CompletedTask;
            }

            // -----------------------------------------
            // ★ オーナー以外が全員 Ready か確認
            // -----------------------------------------
            bool allNonOwnerReady = roomContext.RoomUserDataList
                .Where(u => !u.Value.JoinedUser.IsOwner)   // ← オーナー以外
                .All(u => u.Value.JoinedUser.IsReady);     // ← Ready 状態？

            if (!allNonOwnerReady)
            {
                Console.WriteLine("[START GAME] Not all non-owner users are ready. Game cannot start.");
                return Task.CompletedTask; // ★開始しない
            }

            foreach (var user in roomContext.RoomUserDataList.Values)
            {
                user.IsGoal = false;
            }

            // -----------------------------------------
            // ★ ゲーム開始（全員 Ready を確認済み）
            // -----------------------------------------
            Console.WriteLine($"[START GAME] Owner {this.ConnectionId} is starting the game in room {roomNamed}");

            var users = roomContext.RoomUserDataList
                .Select(x => x.Value.JoinedUser)
                .ToList();

            roomContext.Group.All.OnGameStarted(users);
            return Task.CompletedTask;
        }
        public Task AllGoalAsync(Guid guid)
        {
            // ユーザーデータ取得
            if (!roomContext.RoomUserDataList.TryGetValue(this.ConnectionId, out var roomUser))
                return Task.CompletedTask;

            // すでにゴール済みなら何もしない（重複防止）
            if (roomUser.IsGoal)
                return Task.CompletedTask;

            Console.WriteLine($"[GOAL] {guid}");

            // ゴール状態にする
            roomUser.IsGoal = true;

            // ゴール順に追加
            roomContext.GoalOrder.Add(roomUser);

            // 全員がゴールしたか判定
            bool allGoal = roomContext.RoomUserDataList.Values
                .All(u => u.IsGoal);

            if (allGoal)
            {
                // Ready を全員 false に戻す
                foreach (var user in roomContext.RoomUserDataList.Values)
                {
                    user.JoinedUser.IsReady = false;
                }

                // ゴール順リストをクライアントへ送信
                roomContext.Group.All.OnGameGoaled(
                    roomContext.GoalOrder
                        .Select(u => u.JoinedUser.ConnectionId) // or Name
                        .ToList()
                );

                ResetGameState();
            }

            return Task.CompletedTask;
        }

        public Task ItemObjectAsync(int id)
        {
            roomContext.Group.All.OnItemObject(id);
            return Task.CompletedTask;
        }
        private void ResetGameState()
        {
            foreach (var user in roomContext.RoomUserDataList.Values)
            {
                user.IsGoal = false;
                user.JoinedUser.IsReady = false;

                user.pos = Vector3.zero;
                user.rot = Quaternion.identity;
            }

            roomContext.GoalOrder.Clear();
        }

        public Task ShotItem(Vector3 pos, Quaternion rot, Vector3 vel, int itemId)
        {
            this.roomContext.Group.Except(this.ConnectionId).OnShotItem(pos, rot, vel, itemId);
            return Task.CompletedTask;
        }
    }
}
