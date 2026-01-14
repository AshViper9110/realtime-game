using UnityEngine;

public class BaseModel : MonoBehaviour
{
#if UNITY_EDITOR
    //protected const string ServerURL = "http://employment-blame.gl.at.ply.gg:64097";
    protected const string ServerURL = "http://localhost:25566";
#else
        //protected const string ServerURL = "http://employment-blame.gl.at.ply.gg:64097";
        protected const string ServerURL = "http://localhost:25566";
#endif
}
