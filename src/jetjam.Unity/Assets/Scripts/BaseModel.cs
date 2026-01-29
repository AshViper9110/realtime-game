using UnityEngine;

public class BaseModel : MonoBehaviour
{
    protected readonly Grpc.Core.CallOptions commonCallOptions =
        new Grpc.Core.CallOptions().WithHeaders(new Grpc.Core.Metadata
        {
            { "X-Game-Id", "ge202410" }
        });
}
