using Cysharp.Threading.Tasks;
using DG.Tweening;
using realtime_game.Server.Models.Entities;
using realtime_game.Server.StreamingHubs;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameDirector : MonoBehaviour
{
    public static bool isStart = false;

    RoomModel roomModel;
    UserModel userModel;
   

    [SerializeField] GameObject characterPrefab;
    [SerializeField] TMP_InputField roomName;
    [SerializeField] TMP_InputField userName;
    [SerializeField] GameObject bg;
    [SerializeField] GameObject leaveButton;
    [SerializeField] Transform roomListContent;      // ScrollView Content
    [SerializeField] GameObject roomButtonPrefab;    // Button Prefab
    [SerializeField] GameObject planeModel;
    [SerializeField] GameObject Menu;
    [SerializeField] GameObject startButton; // ゲーム開始ボタン
    [SerializeField] GameObject readyButton; // 準備ボタン
    [SerializeField] GameObject player;

    string myself;
    Dictionary<Guid, GameObject> characterList = new Dictionary<Guid, GameObject>();
    float sendInterval = 0.1f; // 0.1 秒に1回 = 1秒で10回
    float lastSendTime = 0;

    async void Start()
    {
        roomModel = GetComponent<RoomModel>();
        await roomModel.ConnectAsync();
        userModel = GetComponent<UserModel>();
        // Event Registration
        roomModel.OnJoinedUser += this.OnJoinedUser;
        roomModel.OnLeavedUser += this.OnLeaveUser;
        roomModel.OnLeftUserAll += this.OnLeftUserAll;
        roomModel.OnGameStartedReceived += this.OnGameStarted;
        //ObjectのActiveSet
        startButton.SetActive(false);
        readyButton.SetActive(false);
        bg.SetActive(true);
        leaveButton.SetActive(false);
        Menu.SetActive(false);
        player.transform.position = Vector3.zero;
        //player.transform.rotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        if (!isStart) return;

        if (Time.time - lastSendTime >= sendInterval)
        {
            lastSendTime = Time.time;
            SendMoveMessage().Forget();
        }
    }

    private async UniTaskVoid SendMoveMessage()
    {
        Vector3 pos = planeModel.transform.position;
        Quaternion rot = planeModel.transform.rotation;

        await roomModel.MoveAsync(pos, rot);
    }

    public async void LeaveRoom()
    {
        string room = roomName.text;

        await roomModel.LeaveAsync();

        bg.SetActive(true);
        leaveButton.SetActive(false);
        isStart = false;
        player.transform.position = Vector3.zero;
        player.transform.rotation = Quaternion.identity;
    }

    public async UniTask JoinRoom(string room)
    {
        myself = userName.text;
        await roomModel.JoinAsync(room, myself);
        JoinedUser joinedUser = roomModel.GetJoinedUser(roomModel.ConnectionId);
        SetupRoomUI(joinedUser);

        Debug.Log($"Joined room: {room}");
    }

    private void SetupRoomUI(JoinedUser joinedUser)
    {
        Menu.SetActive(true);
        bg.SetActive(false);
        leaveButton.SetActive(true);
        if (joinedUser.IsOwner)
        {
            startButton.SetActive(true);
            readyButton.SetActive(false);
        }
        else
        {
            startButton.SetActive(false);
            readyButton.SetActive(true);
        }
    }

    public async void UpdateStartButton()
    {
        await roomModel.StartGameAsync();
    }

    public void OnReadyButtonClicked()
    {
        roomModel.SendReadyAsync(true).Forget();
        readyButton.SetActive(false); // 一度押したら非表示
    }

    // --- Callback ---
    private void OnJoinedUser(JoinedUser user)
    {
        // Skip self
        if (user.UserName == myself) return;

        if (characterList.ContainsKey(user.ConnectionId))
            return;

        GameObject characterObject = Instantiate(characterPrefab);
        characterObject.transform.position = Vector3.zero;

        characterList[user.ConnectionId] = characterObject;

        Debug.Log("=== Joined User ===");
        Debug.Log($"ConnectionId: {user.ConnectionId}");
        Debug.Log($"UserName: {user.UserName}");
    }

    // --- Callback ---
    private void OnLeaveUser(Guid connectionId)
    {
        // いない人は退室できない
        if (!characterList.ContainsKey(connectionId))
        {
            return;
        }

        Destroy(characterList[connectionId]); // 対象のオブジェクトを削除
        characterList.Remove(connectionId); // リストから対象のデータを削除
    }

    private void OnLeftUserAll()
    {
        // 自分以外のオブジェクトを削除する
        List<Guid> connectionIdList = characterList.Keys.ToList();
        foreach (Guid connectionId in connectionIdList)
        {
            // 一人分の退室処理
            OnLeaveUser(connectionId);
        }
    }

    public async void RefreshRoomList()
    {
        // Add VerticalLayoutGroup if missing
        if (roomListContent.GetComponent<VerticalLayoutGroup>() == null)
        {
            var layout = roomListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        // Clear buttons
        foreach (Transform child in roomListContent)
            Destroy(child.gameObject);

        // Get room list from server
        List<string> rooms = await roomModel.GetRoomListAsync();
        foreach (var room in rooms)
        {
            var btnObj = Instantiate(roomButtonPrefab, roomListContent);
            btnObj.GetComponentInChildren<TMP_Text>().text = room;
            btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(async () =>
            {
                await JoinRoom(room);
            });
        }
    }

    public async void CreateRoom()
    {
        myself = userName.text;

        string room = roomName.text;
        await roomModel.JoinAsync(room, myself);

        JoinedUser joinedUser = roomModel.GetJoinedUser(roomModel.ConnectionId);
        SetupRoomUI(joinedUser);

        Debug.Log($"Create room: {room}");
    }

    public void OnMoveCharacter(Guid connectionId, Vector3 pos, Quaternion rot)
    {
        if (!characterList.TryGetValue(connectionId, out var character))
        {
            Debug.LogWarning($"Character with ConnectionId {connectionId} not found!");
            return;
        }
        character.transform.DOMove(pos, 0.1f);
        //character.transform.position = pos;
        character.transform.DORotateQuaternion(rot, 0.1f);
        //character.transform.rotation = rot;
    }

    public void OnGameStarted()
    {
        Debug.Log("★ GAME STARTED ★");

        Menu.SetActive(false);
        startButton.SetActive(false);
        readyButton.SetActive(false);
        isStart = true;

        //StartGameplay();
    }
}