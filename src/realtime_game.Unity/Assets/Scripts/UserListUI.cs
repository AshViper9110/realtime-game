using UnityEngine;
using TMPro;
using realtime_game.Server.StreamingHubs;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class UserListUI : MonoBehaviour
{
    [SerializeField] Transform content; // ScrollView Content
    [SerializeField] GameObject userItemPrefab;

    Dictionary<Guid, GameObject> items = new();

    public void AddUser(JoinedUser user)
    {
        if (content.GetComponent<VerticalLayoutGroup>() == null)
        {
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        var obj = Instantiate(userItemPrefab, content);
        obj.GetComponentInChildren<TMP_Text>().text = user.UserData.Name;
        items[user.ConnectionId] = obj;
    }

    public void RemoveUser(Guid connectionId)
    {
        if (items.TryGetValue(connectionId, out var obj))
        {
            Destroy(obj);
            items.Remove(connectionId);
        }
    }

    public void SetList()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        items.Clear();
    }
}