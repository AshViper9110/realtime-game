using MagicOnion.Server;
using MagicOnion;
using realtime_game.Shared.Interfaces.Services;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using realtime_game.Server.Models.Entities;
using realtime_game.Server.Models.Contexts;

namespace realtime_game.Server.Interfaces.Services
{
    public class UserService : ServiceBase<IUserService>, IUserService
    {
        ///
        public async UnaryResult<int> RegistUserAsync(string name)
        {
            using var context = new GameDbContext();
            //バリデーションチェック(名前登録済みかどうか)
            if (context.Users.Count() > 0 &&
                  context.Users.Where(user => user.Name == name).Count() > 0)
            {
                throw new ReturnStatusException(Grpc.Core.StatusCode.InvalidArgument, "");
            }
            //テーブルにレコードを追加
            User user = new User();
            user.Name = name;
            user.Token = "";
            user.Created_at = DateTime.Now;
            user.Updated_at = DateTime.Now;
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user.Id;
        }

        public async UnaryResult<User?> GetUserAsync(int id)
        {
            using var context = new GameDbContext();

            var user = await context.Users.FindAsync(id);

            return user; // 見つからなければ null が返る
        }

        public async UnaryResult<User[]> GetAllUserAsync()
        {
            using var context = new GameDbContext();
            var users = await context.Users.ToListAsync();
            return users.ToArray();
        }

        public async UnaryResult<string> GetUserNameAsync(int id)
        {
            using var context = new GameDbContext();

            var user = await context.Users.FindAsync(id);

            if (user == null)
            {
                // 見つからなかった場合は空文字やメッセージを返す
                return "User not found";
            }

            return user.Name;
        }
    }
}
