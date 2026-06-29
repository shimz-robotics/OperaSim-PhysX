using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class taru_collision_publisher : MonoBehaviour
{
    public float inflation = 0.3f;   // ふかしの長さ(m)
    public string proximityTopic = "/zx200/taru_proximity_warning";
    private ROSConnection ros;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<BoolMsg>(proximityTopic);

        // 自分自身だけでなく子も含めてColliderを探す
        var srcCol = GetComponentInChildren<Collider>();
        if (srcCol == null)
        {
            Debug.LogWarning($"{name}: 子も含めてColliderが見つかりません");
            return;
        }

        Debug.Log($"{name}: Collider発見 = {srcCol.GetType().Name} (場所: {srcCol.gameObject.name})");

        // 本体のColliderを取得して、それを基準に拡大トリガーを作る
        //var srcCol = GetComponent<Collider>();
        if (srcCol == null) { Debug.LogWarning($"{name}: Collider無し"); return; }

        // 拡大トリガー用の子オブジェクトを作成
        var probe = new GameObject(name + "_proximity");
        probe.transform.SetParent(transform, false);

        // 本体がBoxColliderなら、sizeをinflation分大きくしたBoxColliderを付ける
        if (srcCol is BoxCollider box)
        {
            var t = probe.AddComponent<BoxCollider>();
            t.center = box.center;
            t.size = box.size + Vector3.one * (inflation * 2f);  // 全方向にinflation
            t.isTrigger = true;
        }
        else if (srcCol is CapsuleCollider cap)
        {
            var t = probe.AddComponent<CapsuleCollider>();
            t.center = cap.center;
            t.radius = cap.radius + inflation;
            t.height = cap.height + inflation * 2f;
            t.direction = cap.direction;
            t.isTrigger = true;
        }
        // 検知ロジックを子に持たせる
        var fwd = probe.AddComponent<ProximityForwarder>();
        fwd.Init(ros, proximityTopic, name);
    }
}

// トリガーに入った/出たを親に転送
public class ProximityForwarder : MonoBehaviour
{
    private ROSConnection ros;
    private string topic;
    private string linkName;

    public void Init(ROSConnection r, string t, string ln)
    {
        ros = r; topic = t; linkName = ln;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Proximity] {linkName} に '{other.name}' が接近");
        ros.Publish(topic, new BoolMsg { data = true });
    }

    void OnTriggerExit(Collider other)
    {
        ros.Publish(topic, new BoolMsg { data = false });
    }

    
}
