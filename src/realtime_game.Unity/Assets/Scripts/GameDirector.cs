using Cysharp.Threading.Tasks;
using DG.Tweening;
using realtime_game.Server.StreamingHubs;
using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class GameDirector : MonoBehaviour
{
    public static bool isStart = false;

    RoomModel roomModel;
    UserModel userModel;
    UserListUI userListUI;

    // ================= Vehicle =================

    [Serializable]
    public class VehicleDef
    {
        public int id;                // 固定ID
        public GameObject prefab;     // 対応する機体
    }

    [Header("Vehicle Prefabs")]
    [SerializeField] VehicleDef[] vehicles;

    Dictionary<int, GameObject> vehicleMap;

    // ================= UI =================

    [SerializeField] TMP_InputField roomName;
    [SerializeField] TMP_InputField userName;
    [SerializeField] GameObject bg;
    [SerializeField] GameObject leaveButton;
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomButtonPrefab;
    [SerializeField] GameObject planeModel;
    [SerializeField] GameObject Menu;
    [SerializeField] GameObject startButton;
    [SerializeField] GameObject readyButton;
    [SerializeField] GameObject roomCreatePanel;
    [SerializeField] GameObject player;
    [SerializeField] Text rankingText;
    [SerializeField] GameObject nameTagPrefab;
    [SerializeField] private SplineContainer spline;
    public GameObject spownpoint;
    public Transform vehicleView;
    public TextMeshProUGUI countdownText;

    GameObject localPlayerModel;
    GameObject vehiclePreview;
    public int vehicleIndex = 0;     // VehicleID

    bool isGoalSent = false;
    string myself;
    List<GameObject> nameTags = new();

    Dictionary<Guid, GameObject> characterList = new();
    float sendInterval = 0.1f;
    float lastSendTime = 0;

    public static class TextLimitUtil
    {
        public static string Clamp(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var info = new StringInfo(text);
            int length = info.LengthInTextElements;

            if (length <= max) return text;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < max; i++)
            {
                sb.Append(info.SubstringByTextElements(i, 1));
            }

            return sb.ToString();
        }
    }

    public bool CheckString(string text)
    {
        bool isNullOrEmpty = string.IsNullOrEmpty(text);

        if (isNullOrEmpty)
        {
            Debug.Log("String is null or empty");
        }

        return isNullOrEmpty;
    }

    // ================= Racer =================

    private List<Racer> racers = new();

    public class Racer
    {
        public Guid id;
        public Transform tf;
        public int vehicleIndex;
        public float progress;
        private float lastProgress;
        private Vector3 lastPosition;

        public void UpdateProgress(SplineContainer splineContainer)
        {
            var spline = splineContainer.Spline;
            SplineUtility.GetNearestPoint(spline, tf.position, out _, out float t);
            float cur = t * spline.GetLength();

            Vector3 dir = (tf.position - lastPosition).normalized;
            Vector3 splineDir = Vector3.Normalize(spline.EvaluateTangent(t));
            if (Vector3.Dot(dir, splineDir) < -0.5f) return;

            if (cur > lastProgress)
            {
                progress = cur;
                lastProgress = cur;
            }
            lastPosition = tf.position;
        }
    }

    // ================= Awake =================

    void Awake()
    {
        vehicleMap = vehicles.ToDictionary(v => v.id, v => v.prefab);
    }

    // ================= Start =================

    async void Start()
    {
        roomModel = GetComponent<RoomModel>();
        await roomModel.ConnectAsync();
        userModel = GetComponent<UserModel>();
        userListUI = GetComponent<UserListUI>();

        roomModel.OnJoinedUser += _ => { };
        roomModel.OnLeavedUser += _ => { };
        roomModel.OnLeftUserAll += () => { };
        roomModel.OnGameStartedReceived += OnGameStarted;
        roomModel.OnGoalUser += OnGameGoal;

        startButton.SetActive(false);
        readyButton.SetActive(false);
        bg.SetActive(true);
        leaveButton.SetActive(false);
        Menu.SetActive(false);

        UpdateLocalVehiclePreview();
        RefreshRoomList();

        racers.Add(new Racer
        {
            id = roomModel.ConnectionId,
            tf = player.transform,
            vehicleIndex = vehicleIndex
        });

        player.transform.position = spownpoint.transform.position;
    }

    // ================= Update =================

    private void LateUpdate()
    {
        if (!isStart) return;

        if (Time.time - lastSendTime >= sendInterval)
        {
            lastSendTime = Time.time;
            SendMoveMessage().Forget();
        }

        foreach (var r in racers)
            r.UpdateProgress(spline);

        var sorted = racers.OrderByDescending(r => r.progress).ToList();
        rankingText.text = $"{sorted.FindIndex(r => r.id == roomModel.ConnectionId) + 1} 位";
    }

    private async UniTaskVoid SendMoveMessage()
    {
        var rb = player.GetComponent<Rigidbody>();
        if (!rb) return;

        await roomModel.MoveAsync(
            rb.position + rb.linearVelocity * 0.2f,
            planeModel.transform.rotation
        );
    }

    // ================= Room =================

    public async void CreateRoom()
    {
        myself = TextLimitUtil.Clamp(userName.text, 10);
        if (CheckString(roomName.text))
        {
            Debug.Log("RoomName empty");
            return;
        }
        await roomModel.JoinAsync(TextLimitUtil.Clamp(roomName.text, 10), myself);
        SetupRoomUI(roomModel.GetJoinedUser(roomModel.ConnectionId));
        player.transform.position = spownpoint.transform.position;
        player.transform.rotation = Quaternion.identity;
        roomCreatePanel.SetActive(false);
    }

    public async void RefreshRoomList()
    {
        foreach (Transform t in roomListContent)
            Destroy(t.gameObject);

        var rooms = await roomModel.GetRoomListAsync();

        foreach (var roomName in rooms)
        {
            var item = Instantiate(roomButtonPrefab, roomListContent);

            var roomNameText = item.transform.Find("RoomNameText")
                .GetComponent<TMP_Text>();
            var playerCountText = item.transform.Find("PlayerCountText")
                .GetComponent<TMP_Text>();
            var joinButton = item.transform.Find("JoinButton")
                .GetComponent<Button>();

            roomNameText.text = roomName;
            playerCountText.text = "1 / 4";

            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => JoinRoom(roomName).Forget());
        }

        // レイアウトを強制再計算
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            (RectTransform)roomListContent
        );
    }


    public async UniTask JoinRoom(string room)
    {
        myself = TextLimitUtil.Clamp(userName.text, 10);
        if (CheckString(userName.text))
        {
            Debug.Log("UserName empty");
            return;
        }
        await roomModel.JoinAsync(room, myself);
        SetupRoomUI(roomModel.GetJoinedUser(roomModel.ConnectionId));
        player.transform.position = spownpoint.transform.position;
        player.transform.rotation = Quaternion.identity;
    }

    void SetupRoomUI(JoinedUser user)
    {
        Menu.SetActive(true);
        bg.SetActive(false);
        leaveButton.SetActive(true);
        startButton.SetActive(user.IsOwner);
        readyButton.SetActive(!user.IsOwner);
    }

    public void OnReadyButtonClicked()
    {
        roomModel.SendReadyAsync(true, vehicleIndex).Forget();
        readyButton.SetActive(false);
    }

    public async void UpdateStartButton()
    {
        await roomModel.StartGameAsync(vehicleIndex);
    }

    public async void LeaveRoom()
    {
        await roomModel.LeaveAsync();

        bg.SetActive(true);
        Menu.SetActive(false);
        leaveButton.SetActive(false);
        startButton.SetActive(false);
        readyButton.SetActive(false);
        isStart = false;

        foreach (var obj in characterList.Values)
            Destroy(obj);
        characterList.Clear();
        racers.RemoveAll(r => r.id != roomModel.ConnectionId);

        racers[0].tf = player.transform;
        RefreshRoomList();
    }

    public void RoomCreatePanel(bool active)
    {
        if (CheckString(userName.text)) 
        {
            Debug.Log("UserName empty");
            return;
        }
        roomCreatePanel.SetActive(active);
    }

    public void BackTitleScene()
    {
        SceneManager.LoadScene("TitleScene");
    }

    // ================= Game Start =================

    public void OnGameStarted(List<JoinedUser> users)
    {
        foreach (var obj in characterList.Values)
            Destroy(obj);

        characterList.Clear();
        racers.Clear();

        foreach (var user in users)
        {
            int id = user.VehicleIndex;
            var prefab = vehicleMap[id];

            if (user.ConnectionId == roomModel.ConnectionId)
            {
                SpawnLocalPlayerModel(vehicleIndex);
                racers.Add(new Racer
                {
                    id = user.ConnectionId,
                    tf = player.transform,
                    vehicleIndex = id
                });
            }
            else
            {
                var obj = Instantiate(prefab, spownpoint.transform.position, Quaternion.identity);
                characterList[user.ConnectionId] = obj;

                // 名前タグ生成
                var tag = Instantiate(nameTagPrefab, obj.transform);
                tag.transform.localPosition = new Vector3(0, 7f, 0); // 機体の上

                tag.SetActive(false);
                nameTags.Add(tag);

                var text = tag.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                text.text = user.UserName;   // ネットワークのユーザー名

                racers.Add(new Racer
                {
                    id = user.ConnectionId,
                    tf = obj.transform,
                    vehicleIndex = id
                });
            }
        }

        Menu.SetActive(false);
        startButton.SetActive(false);
        readyButton.SetActive(false);
        StartCoroutine(StartAfterDelay());
    }

    void SpawnLocalPlayerModel(int vehicleId)
    {
        if (localPlayerModel != null)
            Destroy(localPlayerModel);

        var prefab = vehicleMap[vehicleId];

        localPlayerModel = Instantiate(
            prefab,
            planeModel.transform.position,
            planeModel.transform.rotation,
            planeModel.transform
        );
    }

    IEnumerator StartAfterDelay()
    {
        countdownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        countdownText.text = "START";
        yield return new WaitForSeconds(1f);

        countdownText.gameObject.SetActive(false);

        foreach (var tag in nameTags)
            tag.SetActive(true);

        isStart = true;
    }

    // ================= Vehicle Select =================

    public void SelectVehicle(int delta)
    {
        var ids = vehicles.Select(v => v.id).OrderBy(x => x).ToList();
        int cur = ids.IndexOf(vehicleIndex);
        cur = (cur + delta + ids.Count) % ids.Count;
        vehicleIndex = ids[cur];
        UpdateLocalVehiclePreview();
    }

    void UpdateLocalVehiclePreview()
    {
        if (vehiclePreview != null)
            Destroy(vehiclePreview);

        var prefab = vehicleMap[vehicleIndex];

        vehiclePreview = Instantiate(
            prefab,
            vehicleView.position,
            vehicleView.rotation,
            vehicleView
        );
    }

    // ================= Goal =================

    public void SendGoal()
    {
        if (isGoalSent) return;
        isGoalSent = true;
        roomModel.GoalAsync().Forget();
    }

    public void OnGameGoal(List<Guid> goalOrder)
    {
        isStart = false;

        // 順位表示
        userListUI.ShowRanking(goalOrder, id => roomModel.GetJoinedUser(id));

        // メニューへ戻す
        StartCoroutine(ReturnToMenuAfterGoal());
    }
    IEnumerator ReturnToMenuAfterGoal()
    {
        // 結果表示時間
        yield return new WaitForSeconds(3f);

        // メニューを表示
        Menu.SetActive(true);
        leaveButton.SetActive(true);

        var user = roomModel.GetJoinedUser(roomModel.ConnectionId);
        startButton.SetActive(user.IsOwner);
        readyButton.SetActive(!user.IsOwner);

        // フラグとプレイヤー状態をリセット
        isGoalSent = false;

        player.transform.position = spownpoint.transform.position;
        player.transform.rotation = Quaternion.identity;
    }


    public void OnMoveCharacter(Guid connectionId, Vector3 pos, Quaternion rot)
    {
        // 自分は動かさない（自分は Rigidbody で制御）
        if (connectionId == roomModel.ConnectionId)
            return;

        if (!characterList.TryGetValue(connectionId, out var obj))
            return;

        obj.transform.DOMove(pos, 0.2f);
        obj.transform.DORotateQuaternion(rot, 0.2f);
    }

}
