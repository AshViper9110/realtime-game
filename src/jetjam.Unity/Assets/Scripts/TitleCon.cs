using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TitleCon : MonoBehaviour
{
    // 移動先のシーン名
    public string gameSceneName = "SampleScene";
    public GameObject camera;
    public List<GameObject> PlaneObjects;
    Dictionary<GameObject, float> baseY = new Dictionary<GameObject, float>();
    Dictionary<GameObject, Quaternion> baseRot = new Dictionary<GameObject, Quaternion>();

    // 「スタート」ボタン用
    public void OnClickStart()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // 「終了」ボタン用
    public void OnClickQuit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    //Title関連

    void Start()
    {
        foreach (GameObject p in PlaneObjects) baseY[p] = p.transform.position.y;
        foreach (GameObject p in PlaneObjects) baseRot[p] = p.transform.localRotation;
    }
    void Update()
    {
        camera.transform.Rotate(0f, 1f * Time.deltaTime, 0f, Space.World);

        foreach (GameObject p in PlaneObjects)
        {
            float t = Time.time + p.GetInstanceID();

            float pitch = Mathf.Sin(t * 1.2f) * 5f;   // 上下
            float yaw = Mathf.Sin(t * 0.8f) * 1f;   // 左右
            float roll = Mathf.Sin(t * 1.5f) * 10f;  // 傾き（重要）

            p.transform.localRotation =
                baseRot[p] * Quaternion.Euler(pitch, yaw, roll);
        }

        foreach (GameObject p in PlaneObjects)
        {
            float y = baseY[p]
                + Mathf.Sin(Time.time * 1.5f + p.GetInstanceID()) * 0.3f;

            Vector3 pos = p.transform.position;
            pos.y = y;
            p.transform.position = pos;
        }
    }

}
