using Cysharp.Threading.Tasks;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Shared.Interfaces.Services;
using realtime_game.Server.Models.Entities;
using UnityEngine;
using MessagePack.ImmutableCollection;

public class CalculateModel : MonoBehaviour
{
    const string ServerURL = " http://localhost:5244";
    int[] numList = { 1, 5, 2, 7, 6, 4 };
    Number number = new Number();
    UserModel userModel = new UserModel();
    async void Start()
    {
        Debug.Log(await userModel.RegistUserAsync("player1"));
    }

    public async UniTask<string> Log(string text)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.TextLog(text);
        return result;
    }

    public async UniTask<int> Mul(int x, int y)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.MulAsync(x, y);
        return result;
    }

    public async UniTask<int> SumAll(int[] numList)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.SumAllAsync(numList);
        return result;
    }

    public async UniTask<int[]> Colc(int x, int y)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.ColcForOprerationlAsync(x, y);
        return result;
    }

    public async UniTask<float> SumAllNumber(Number numData)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.SumAllNumberAsync(numData);
        return result;
    }
}
