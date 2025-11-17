using realtime_game.Server.StreamingHubs;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
    [SerializeField] RoomModel roomModel;
    [SerializeField] int userId = 0;
    [SerializeField] TMP_InputField roomName;

    async void Start()
    {
        roomModel.OnJoinedUser += this.OnJoinedUser;
        await roomModel.ConnectAsync();
    }

    public async void JoinRoom()
    {
        // 入力チェック（null だけじゃダメ。空文字も弾く）
        if (roomName == null || string.IsNullOrEmpty(roomName.text))
        {
            Debug.Log("Room name is empty.");
            return;
        }

        if (userId == 0)
        {
            Debug.Log("User ID is 0. Set a valid ID.");
            return;
        }

        string room = roomName.text;

        await roomModel.JoinAsync(room, userId);
        Debug.Log($"Join room: {room}");
    }

    // ★ ここではオブジェクト生成しない（ログだけ）
    private void OnJoinedUser(JoinedUser user)
    {
        Debug.Log("=== Joined User ===");
        Debug.Log($"ConnectionId: {user.ConnectionId}");
        Debug.Log($"UserId: {user.UserData.Id}");
        Debug.Log($"UserName: {user.UserData.Name}");
    }
}
