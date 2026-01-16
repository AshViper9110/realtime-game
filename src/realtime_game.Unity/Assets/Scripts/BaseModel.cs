using UnityEngine;

public class BaseModel : MonoBehaviour
{
#if UNITY_EDITOR
    //protected const string ServerURL = "http://employment-blame.gl.at.ply.gg:64097";
    protected const string ServerURL = "http://ge202400.japaneast.cloudapp.azure.com:25566";
#else
        //protected const string ServerURL = "http://employment-blame.gl.at.ply.gg:64097";
        protected const string ServerURL = "http://ge202400.japaneast.cloudapp.azure.com:25566";
#endif
}
