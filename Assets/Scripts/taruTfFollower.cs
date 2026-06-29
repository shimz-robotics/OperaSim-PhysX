using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Tf2;          // TFMessageMsg

public class taruTfFollower : MonoBehaviour
{
    private ROSConnection ros;
    public string tfTopic = "/tf";
    public string tfStaticTopic = "/tf_static";

    // tfのchild_frame_id -> Unityの該当Transform
    private Dictionary<string, Transform> linkMap = new Dictionary<string, Transform>();

    // tfのchild_frame_id -> 親からの相対変換(最新)
    private Dictionary<string, (Vector3 pos, Quaternion rot)> latestTf
        = new Dictionary<string, (Vector3, Quaternion)>();

    // tf側にあるがUnity側に無い中間フレーム(track_link)を、どのUnityリンクに合流させるか
    // tf: base_link -> track_link -> body_link
    // Unity:           base_link -> body_link
    // track_link の変換は body_link 側に合成して吸収する
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        // Unityの各リンクを名前で集める（このスクリプトはzx200ルートに付ける想定）
        RegisterLink("base_link");
        RegisterLink("body_link");
        RegisterLink("boom_link");
        RegisterLink("arm_link");
        RegisterLink("bucket_link");
        RegisterLink("bucket_end_link");

        // 対象GameObjectを先に集める
        var abs = GetComponentsInChildren<ArticulationBody>(true);
        var targets = new List<GameObject>();
        foreach (var ab in abs) targets.Add(ab.gameObject);

        // ステップ1: 全ArticulationBodyを破棄（先に全部消す）
        foreach (var ab in abs) DestroyImmediate(ab);

        // ステップ2: 破棄が済んでからkinematic Rigidbodyを付ける
        foreach (var go in targets)
        {
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        ros.Subscribe<TFMessageMsg>(tfTopic, OnTf);
        ros.Subscribe<TFMessageMsg>(tfStaticTopic, OnTf);

        Debug.Log($"[TfFollower] 登録 {linkMap.Count}, AB破棄/RB付与 {targets.Count}");
    }

    void RegisterLink(string frame)
    {
        var found = FindDeep(transform, frame);
        if (found != null) linkMap[frame] = found;
        else Debug.LogWarning($"[TfFollower] Unityに '{frame}' が見つかりません");
    }

    // 子孫から名前一致でTransformを探す
    Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    void OnTf(TFMessageMsg msg)
    {
        foreach (var ts in msg.transforms)
        {
            string child = ts.child_frame_id;
            var tr = ts.transform.translation;
            var ro = ts.transform.rotation;

            // ROS座標 -> Unity座標 へ変換
            Vector3 pos = new Vector3<FLU>(
                (float)tr.x, (float)tr.y, (float)tr.z).toUnity;
            Quaternion rot = new Quaternion<FLU>(
                (float)ro.x, (float)ro.y, (float)ro.z, (float)ro.w).toUnity;

            latestTf[child] = (pos, rot);
        }
    }

    void Update()
    {
        // 各リンクに「親フレームからの相対変換」を適用
        // body_link は tf では track_link の子。track_link は base_link に対し固定なので
        // body_link の相対変換をそのまま base_link 直下の body_link に適用してよい。
        ApplyLocal("body_link");
        ApplyLocal("boom_link");
        ApplyLocal("arm_link");
        ApplyLocal("bucket_link");
        ApplyLocal("bucket_end_link");

        // base_link 自体（車体の移動）。tfに base_link のワールド変換があれば適用。
        // 走行が無くワールド原点固定ならこの行は効果なし。
        if (latestTf.TryGetValue("base_link", out var b) && linkMap.TryGetValue("base_link", out var bt))
        {
            bt.localPosition = b.pos;
            bt.localRotation = b.rot;
        }
    }

    void ApplyLocal(string frame)
    {
        if (linkMap.TryGetValue(frame, out var t) &&
            latestTf.TryGetValue(frame, out var v))
        {
            t.localPosition = v.pos;
            t.localRotation = v.rot;
        }
    }
}