using UnityEngine;

public class ItemObject : MonoBehaviour
{
    public int instanceId;   // 一意
    public int itemTypeId;   // 0〜3
    GameDirector director;

    public void Initialize(int instanceId, int itemTypeId, GameDirector gd)
    {
        this.instanceId = instanceId;
        this.itemTypeId = itemTypeId;
        director = gd;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        director.OnItemPicked(instanceId);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}