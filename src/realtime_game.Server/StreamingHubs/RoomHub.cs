using MagicOnion.Server.Hubs;
using realtime_game.Server.Models.Contexts;
using realtime_game.Server.Models.Entities;
using realtime_game.Shared.Interfaces.StreamingHubs;
using System.Runtime.InteropServices;

namespace realtime_game.Server.StreamingHubs
{
    public class RoomHub(RoomContextRepository roomContextRepository) :
        StreamingHubBase<IRoomHub, IRoomHubReceiver>, IRoomHub
    {
        // クラスの一部として定義されているフィールド
        private RoomContextRepository roomContextRepos; // ルーム全体を管理するリポジトリ（部屋ごとの状態を保持）
        private RoomContext roomContext;                // 現在接続している部屋の情報（ルーム単位の状態）

        // ユーザーがルームに参加する際に呼ばれる非同期メソッド
        public async Task<JoinedUser[]> JoinAsync(string roomName, int userId)
        {
            // --- 1. ルームコンテキストの取得・作成 ---
            // 複数スレッドからアクセスされる可能性があるため lock で排他制御
            lock (roomContextRepos)
            {
                // 既存のルームを取得
                this.roomContext = roomContextRepos.getContext(roomName);

                // ルームが存在しない場合は新規作成
                if (this.roomContext == null)
                {
                    Console.WriteLine($"[ROOM] Create Room: {roomName}");
                    this.roomContext = roomContextRepos.CreateContext(roomName);
                }
            }

            // --- 2. クライアントをルーム内のグループに追加 ---
            this.roomContext.Group.Add(this.ConnectionId, Client);

            // --- 3. データベースからユーザー情報を取得 ---
            GameDbContext context = new GameDbContext();
            User user = context.Users.FirstOrDefault(user => user.Id == userId);
            if (user == null)
            {
                Console.WriteLine($"[ERROR] User not found: userId={userId}");
                return Array.Empty<JoinedUser>();
            }

            // --- 4. JoinedUser オブジェクトを生成 ---
            var joinedUser = new JoinedUser
            {
                ConnectionId = this.ConnectionId,
                UserData = user
            };

            // --- 5. ルーム内ユーザーデータを作成し登録 ---
            var roomUserData = new RoomUserData() { JoinedUser = joinedUser };
            this.roomContext.RoomUserDataList[ConnectionId] = roomUserData;

            // --- 6. 他の参加者に「誰かが参加した」ことを通知 ---
            this.roomContext.Group.Except([this.ConnectionId]).OnJoin(joinedUser);

            // --- 🔍 ログ出力：参加者情報を表示 ---
            Console.WriteLine($"[JOIN] User '{user.Name}' (ID={user.Id}) joined room '{roomName}'.");
            Console.WriteLine($"[ROOM STATUS] {roomName}: {this.roomContext.RoomUserDataList.Count} users now connected.");

            // --- 7. 現在のルーム内の全参加者リストを返す ---
            return this.roomContext.RoomUserDataList
                .Select(f => f.Value.JoinedUser)
                .ToArray();
        }

        // クライアント接続時に呼ばれる
        protected override ValueTask OnConnected()
        {
            // グローバルリポジトリをフィールドにセット
            roomContextRepos = roomContextRepository;
            return default;
        }

        // クライアント切断時に呼ばれる（現状は未処理）
        protected override ValueTask OnDisconnected()
        {
            return default;
        }

        // クライアントの接続IDを取得
        public Task<Guid> GetConnectionId()
        {
            return Task.FromResult<Guid>(this.ConnectionId);
        }
    }
}
