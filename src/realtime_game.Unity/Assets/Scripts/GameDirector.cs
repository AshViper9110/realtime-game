using Cysharp.Threading.Tasks;
using realtime_game.Server.Models.Entities;
using realtime_game.Server.StreamingHubs;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameDirector : MonoBehaviour
{
    public static bool isJoin = false;

    RoomModel roomModel;
    UserModel userModel;

    [SerializeField] GameObject characterPrefab;
    [SerializeField] TMP_InputField roomName;
    [SerializeField] TMP_InputField userID;
    [SerializeField] GameObject bg;
    [SerializeField] GameObject leaveButton;
    [SerializeField] Transform roomListContent;      // ScrollView Content
    [SerializeField] GameObject roomButtonPrefab;    // Button Prefab

    User myself;
    Dictionary<Guid, GameObject> characterList = new Dictionary<Guid, GameObject>();

    async void Start()
    {
        roomModel = GetComponent<RoomModel>();
        await roomModel.ConnectAsync();
        userModel = GetComponent<UserModel>();

        // Event Registration
        roomModel.OnJoinedUser += this.OnJoinedUser;
        roomModel.OnLeavedUser += this.OnLeaveUser;
    }

    public async void LeaveRoom()
    {
        string room = roomName.text;

        await roomModel.LeaveAsync(room);

        bg.SetActive(true);
        leaveButton.SetActive(false);

        isJoin = false;
    }

    public async UniTask JoinRoom(string room)
    {
        if (!int.TryParse(userID.text, out int uid) || uid <= 0)
        {
            Debug.Log("Invalid User ID.");
            return;
        }

        try
        {
            myself = await userModel.GetUser(uid);
            Debug.Log($"Myself Loaded: {myself.Name}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return;
        }

        await roomModel.JoinAsync(room, uid);

        bg.SetActive(false);
        leaveButton.SetActive(true);
        isJoin = true;

        Debug.Log($"Joined room: {room}");
    }

    // --- Callback ---
    private void OnJoinedUser(JoinedUser user)
    {
        // Skip self
        if (user.UserData.Id == myself?.Id) return;

        if (characterList.ContainsKey(user.ConnectionId))
            return;

        GameObject characterObject = Instantiate(characterPrefab);
        characterObject.transform.position = Vector3.zero;

        characterList[user.ConnectionId] = characterObject;

        Debug.Log("=== Joined User ===");
        Debug.Log($"ConnectionId: {user.ConnectionId}");
        Debug.Log($"UserId: {user.UserData.Id}");
        Debug.Log($"UserName: {user.UserData.Name}");
    }

    // --- Callback ---
    private void OnLeaveUser(JoinedUser user)
    {
        if (!characterList.TryGetValue(user.ConnectionId, out GameObject obj)) return;
        Destroy(obj);
        characterList.Remove(user.ConnectionId);
        Debug.Log("=== Leaved User ===");
        Debug.Log($"ConnectionId: {user.ConnectionId}");
        Debug.Log($"UserId: {user.UserData.Id}");
        Debug.Log($"UserName: {user.UserData.Name}");
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
        if (roomName == null || string.IsNullOrEmpty(roomName.text))
        {
            Debug.Log("Room name is empty.");
            return;
        }

        if (!int.TryParse(userID.text, out int uid) || uid <= 0)
        {
            Debug.Log("Invalid User ID.");
            return;
        }

        try
        {
            myself = await userModel.GetUser(uid);
            Debug.Log($"Myself Loaded: {myself.Name}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return;
        }

        string room = roomName.text;
        await roomModel.JoinAsync(room, uid);

        bg.SetActive(false);
        leaveButton.SetActive(true);

        isJoin = true;

        Debug.Log($"Create room: {room}");
    }
    private async void OnApplicationQuit()
    {
        await SafeDisconnect();
    }

    private async void OnDestroy()
    {
        await SafeDisconnect();
    }

    private async UniTask SafeDisconnect()
    {
        if (roomModel == null) return;

        try
        {
            if (isJoin)
            {
                string room = roomName.text;
                await roomModel.LeaveAsync(room);
                Debug.Log("ルームから退出しました: " + room);
            }

            if (roomModel.roomHub != null)
            {
                await roomModel.roomHub.DisposeAsync();
                Debug.Log("HubをDisposeしました");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"切断中にエラーが発生しました: {e.Message}");
        }
    }
}