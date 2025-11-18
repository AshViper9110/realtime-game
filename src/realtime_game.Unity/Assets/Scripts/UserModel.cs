using Cysharp.Threading.Tasks;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Server.Models.Entities;
using realtime_game.Shared.Interfaces.Services;
using UnityEngine;

public class UserModel : BaseModel
{
    private int userId;
    public async UniTask<bool> RegistUserAsync(string name)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);
        try
        {
            userId = await client.RegistUserAsync(name);
            return true;
        } catch (RpcException e)
        {
            Debug.Log(e);
            return false;
        }
    }

    public async UniTask<User> GetUser(int id)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);
        try
        {
            var user = await client.GetUserAsync(id);
            return user;
        }
        catch (RpcException e)
        {
            Debug.Log(e);
            return null;
        }
    }
}
