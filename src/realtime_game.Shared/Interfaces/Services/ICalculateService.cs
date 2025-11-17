using MagicOnion;
using MessagePack;
using realtime_game.Server.Models.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace realtime_game.Shared.Interfaces.Services
{
    [MessagePackObject]
    public class Number
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;
        [Key(2)]
        public float z;
    }

    /// <summary>
    /// 初めてのRPCサービス
    /// </summary>
    public interface ICalculateService :IService<ICalculateService>
    {
        /// <summary>
        /// 乗算
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        UnaryResult<int> MulAsync(int x, int y);

        UnaryResult<string> TextLog(string text);

        UnaryResult<string> PlayerJoin(string name);

        UnaryResult<string> PlayerGet(int id);

        UnaryResult<int> SumAllAsync(int[] numList);

        UnaryResult<int[]> ColcForOprerationlAsync(int x, int y);

        UnaryResult<float> SumAllNumberAsync(Number numData);
    }
}
