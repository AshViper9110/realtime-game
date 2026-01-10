using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleCon : MonoBehaviour
{
    // 移動先のシーン名
    public string gameSceneName = "SampleScene";

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
}
