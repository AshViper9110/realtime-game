using MagicOnion;
using realtime_game.Server.Models.Entities;

namespace realtime_game.Shared.Interfaces.Services
{
    public interface IUserService : IService<IUserService>
    {
        UnaryResult<int> RegistUserAsync(string name);

        UnaryResult<User?> GetUserAsync(int id);
    }
}