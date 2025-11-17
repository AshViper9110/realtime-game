using MagicOnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace realtime_game.Shared.Interfaces.Services
{
    public interface IUserService : IService<IUserService>
    {
        UnaryResult<int> RegistUserAsync(string name);
    }
}
