using Grpc.Net.Client;
using Cysharp.Net.Http;

public static class GrpcChannelProvider
{
    private static GrpcChannel _channel;
    private const string ServerURL =
        "https://ge202400.japaneast.cloudapp.azure.com";

    public static GrpcChannel GetChannel()
    {
        if (_channel != null) return _channel;

        var handler = new YetAnotherHttpHandler
        {
            Http2Only = true,                 // Åö ïKê{
            //SkipCertificateVerification = true // äJî≠éûÇÃÇ›
        };

        _channel = GrpcChannel.ForAddress(
            ServerURL,
            new GrpcChannelOptions
            {
                HttpHandler = handler
            }
        );

        return _channel;
    }

    public static void Dispose()
    {
        _channel?.Dispose();
        _channel = null;
    }
}
