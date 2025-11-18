using realtime_game.Server.Models.Entities;
using realtime_game.Server.StreamingHubs;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
    public static bool isJoin = false;

    RoomModel roomModel;
    UserModel userModel;

    [SerializeField] GameObject characterPrefab;
    [SerializeField] TMP_InputField roomName;
    [SerializeField] TMP_InputField userID;
    [SerializeField] GameObject joinButton;
    [SerializeField] GameObject leaveButton;

    User myself;
    Dictionary<Guid, GameObject> characterList = new Dictionary<Guid, GameObject>();

    async void Start()
    {
        roomModel = GetComponent<RoomModel>();
        userModel = GetComponent<UserModel>();

        // イベント購読
        roomModel.OnJoinedUser += this.OnJoinedUser;
        roomModel.OnLeavedUser += this.OnLeaveUser;

        await roomModel.ConnectAsync();
    }

    public async void LeaveRoom()
    {
        string room = roomName.text;

        await roomModel.LeaveAsync(room);

        roomName.gameObject.SetActive(true);
        userID.gameObject.SetActive(true);
        joinButton.SetActive(true);
        leaveButton.SetActive(false);

        isJoin = false;
    }

    public async void JoinRoom()
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

        roomName.gameObject.SetActive(false);
        userID.gameObject.SetActive(false);
        joinButton.SetActive(false);
        leaveButton.SetActive(true);

        isJoin = true;

        Debug.Log($"Join room: {room}");
    }

    // --- 誰かが入った時 ---
    private void OnJoinedUser(JoinedUser user)
    {
        // 自分はスキップ
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

    // --- 誰かが抜けた時 ---
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
}