using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

/// <summary>
/// このGameObjectの衝突判定をROSに送信する。
/// bucket_link、arm_linkなど、衝突を検知したいパーツに個別にアタッチする。
/// </summary>
public class CollisionPublisher : MonoBehaviour
{
    [Header("ROS Settings")]
    [Tooltip("このパーツの衝突をPublishするトピック名。パーツごとに変える")]
    public string topicName = "/taru_collision";

    private ROSConnection ros;
    private BoolMsg collisionMsg;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<BoolMsg>(topicName);
        collisionMsg = new BoolMsg();
    }

    //void OnCollisionStay(Collision other)
    //{
    //    collisionMsg.data = true;
    //    ros.Publish(topicName, collisionMsg);
    //    Debug.Log($"[{gameObject.name}] Collision Stay with {other.gameObject.name} → {topicName}");
    //}

    void OnTriggerEnter(Collider other)
    {
        collisionMsg.data = true;
        ros.Publish(topicName, collisionMsg);
        Debug.Log($"[{gameObject.name}] が {other.gameObject.name} に衝突する1m手間です → {topicName}");
    }

    //void OnCollisionEnter(Collision other)
    //{
    //    collisionMsg.data = true;
    //    ros.Publish(topicName, collisionMsg);
    //    Debug.Log($"[{gameObject.name}] Collision Enter with {other.gameObject.name} → {topicName}");
    //}

    //void OnCollisionExit(Collision other)
    //{
    //    collisionMsg.data = false;
    //    ros.Publish(topicName, collisionMsg);
    //    Debug.Log($"[{gameObject.name}] Collision Exit with {other.gameObject.name} → {topicName}");
    //}
}