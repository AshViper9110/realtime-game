using MagicOnion.Server.Hubs;
using realtime_game.Server.Models.Contexts;
using realtime_game.Server.Models.Entities;
using realtime_game.Shared.Interfaces.StreamingHubs;

namespace realtime_game.Server.StreamingHubs
{
    public class RoomHub(RoomContextRepository roomContextRepository) :
        StreamingHubBase<IRoomHub, IRoomHubReceiver>, IRoomHub
    {
        private RoomContextRepository roomContextRepos;
        private RoomContext roomContext;

        public async Task<JoinedUser[]> JoinAsync(string roomName, int userId)
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"[JOIN REQUEST] roomName={roomName}, userId={userId}, connId={this.ConnectionId}");

            // --- 1. ルームコンテキスト取得 / 作成 ---
            lock (roomContextRepos)
            {
                Console.WriteLine("[ROOM] Checking room context...");

                this.roomContext = roomContextRepos.getContext(roomName);

                if (this.roomContext == null)
                {
                    Console.WriteLine($"[ROOM] Room not found. Creating new room: {roomName}");
                    this.roomContext = roomContextRepos.CreateContext(roomName);
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
                UserData = user
            };

            // --- 5. ルームにユーザーデータ登録 ---
            Console.WriteLine($"[ROOM] Registering user to room data list...");
            var roomUserData = new RoomUserData() { JoinedUser = joinedUser };
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

        public Task LeaveAsync(string roomName)
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"[LEAVE REQUEST] roomName={roomName}, connId={this.ConnectionId}");

            if (roomContext == null)
            {
                Console.WriteLine("[WARNING] LeaveAsync called but roomContext is null.");
                return Task.CompletedTask;
            }

            Console.WriteLine("[NOTIFY] Broadcasting leave to all...");
            this.roomContext.Group.All.OnLeave(this.ConnectionId);

            Console.WriteLine("[GROUP] Removing from room group...");
            this.roomContext.Group.Remove(this.ConnectionId);

            Console.WriteLine("[ROOM] Removing user data...");
            this.roomContext.RoomUserDataList.Remove(this.ConnectionId);

            int count = this.roomContext.Group.Count();
            Console.WriteLine($"[ROOM STATUS] After leaving, {count} users in room.");

            if (count <= 0)
            {
                Console.WriteLine($"[ROOM DELETE] No users left. Removing room '{roomName}'...");
                roomContextRepos.RemoveContext(roomName);
            }

            Console.WriteLine("[LEAVE COMPLETE]");
            Console.WriteLine("--------------------------------------------------");

            return Task.CompletedTask;
        }

        protected override ValueTask OnDisconnected()
        {
            Console.WriteLine($"[DISCONNECTED] connId={this.ConnectionId}");
            return default;
        }
        public Task<List<string>> GetRoomListAsync()
        {
            lock (roomContextRepos)
            {
                // roomContextRepos にある全ルーム名を取得
                return Task.FromResult(roomContextRepos.GetAllRoomNames().ToList());
            }
        }
    }
}
