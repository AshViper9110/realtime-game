using UnityEngine;

public class ItemObject : MonoBehaviour
{
    public int instanceId;   // 一意
    public int itemTypeId;   // 0〜3
    GameDirector director;
    [Header("Rotation")]
    [SerializeField]
    private float rotateSpeed = 90f;

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
    void Update()
    {
        // Y軸を一定速度で回転
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }
}