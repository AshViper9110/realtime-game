using MagicOnion.Server;
using MagicOnion;
using realtime_game.Shared.Interfaces.Services;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using realtime_game.Server.Models.Entities;

namespace realtime_game.Server.Services
{
    /// <summary>
    /// 初めてのRPCサービス
    /// </summary>
    public class CalculateService : ServiceBase<ICalculateService>, ICalculateService
    {
        List<string> Players = new List<string>();
        /// <summary>
        /// 乗算処理を行う
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>xとyの乗算値</returns>
        public async UnaryResult<int> MulAsync(int x, int y)
        {
            Console.WriteLine("Received:" + x + "," + y );
            return x * y;
        }

        public async UnaryResult<string> TextLog(string text)
        {
            Console.WriteLine($"Log:{text}");
            return text;
        }

        public async UnaryResult<string> PlayerJoin(string name)
        {
            Console.WriteLine($"Join:{name}");
            Players.Add( name );
            for (int i = 0; i < Players.Count; i++)
            {
                Console.WriteLine($"ID:{i} Name:{Players[i]}");
            }
            return name;
        }

        public async UnaryResult<string> PlayerGet(int id)
        {
            if (Players.Count != 0)
            {
                Console.WriteLine($"ChackID:{id} => Name:{Players[id]}");
                return Players[id];
            }
            else
            {
                return "Not Player";
            }
        }

        public async UnaryResult<int> SumAllAsync(int[] sumList)
        {
            int sumAll = 0;
            foreach (int sum in sumList)
            {
                sumAll += sum;
            }
            return sumAll;
        }
        public async UnaryResult<int[]> ColcForOprerationlAsync(int x, int y)
        {
            int[] colcNum = new int[4]; 
            for (int i = 0;i < colcNum.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        colcNum[i] = x + y;
                        break;
                    case 1:
                        colcNum[i] = x - y;
                        break;
                    case 2:
                        colcNum[i] = x * y;
                        break;
                    case 3:
                        colcNum[i] = x / y;
                        break;
                }
            }
            return colcNum;
        }
        public async UnaryResult<float> SumAllNumberAsync(Number numData)
        {
            return numData.x + numData.y + numData.z;
        }
    }
}
