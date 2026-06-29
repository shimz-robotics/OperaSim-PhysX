using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;          // JointStateMsg
using Unity.Robotics.UrdfImporter;

public class taruJointStatePlayer : MonoBehaviour
{
    private ROSConnection ros;
    public string jointStatesTopic = "/zx200/joint_states";

    //// スムーズ化
    //private List<float> latestPos = new List<float>();   // 最新の受信角度
    //private List<float> latestVel = new List<float>();    // 最新の受信速度(rad/s)
    //private float lastStamp;
    //private float recvInterval = 0.1f;

    private ArticulationBody rootBody;
    // 関節名 -> この関節がSetJointPositionsの配列の何番目か
    private Dictionary<string, int> jointIndex = new Dictionary<string, int>();
    // 現在の全関節角度（SetJointPositionsに渡す配列）
    private List<float> jointPositions = new List<float>();

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        // ルートのArticulationBodyを取得（isRoot なものを探す）
        var allBodies = GetComponentsInChildren<ArticulationBody>();
        foreach (var ab in allBodies)
        {
            if (ab.isRoot) { rootBody = ab; break; }
        }
        if (rootBody == null)
        {
            Debug.LogError("[DirectPlayer] ルートArticulationBodyが見つかりません");
            return;
        }

        // 各ArticulationBodyの「自由度を持つ関節」をインデックス順に対応づける
        // SetJointPositionsの配列は、各bodyのdofを順に並べたもの
        var bodies = GetComponentsInChildren<ArticulationBody>();
        int dofIndex = 0;
        foreach (var ab in bodies)
        {
            // 自由度を持つ関節（Revolute/Continuous/Prismatic）のみ配列に位置を持つ
            if (ab.jointType == ArticulationJointType.RevoluteJoint ||
                ab.jointType == ArticulationJointType.PrismaticJoint)
            {
                var ujoint = ab.GetComponent<UrdfJoint>();
                string jname = (ujoint != null) ? ujoint.jointName : ab.name;
                jointIndex[jname] = dofIndex;
                Debug.Log($"[DirectPlayer] joint '{jname}' -> index {dofIndex}");
                dofIndex += ab.dofCount;   // 通常1
            }
        }

        // 現在の関節角度を取得して初期化
        rootBody.GetJointPositions(jointPositions);
        Debug.Log($"[DirectPlayer] DOF合計 = {jointPositions.Count}, 対応関節数 = {jointIndex.Count}");

        ////スムーズ
        //rootBody.GetJointPositions(jointPositions);
        //latestPos = new List<float>(jointPositions);
        //latestVel = new List<float>();
        //for (int i = 0; i < jointPositions.Count; i++) latestVel.Add(0f);

        ros.Subscribe<JointStateMsg>(jointStatesTopic, OnJointState);
    }

    private List<float> target;

    void OnJointState(JointStateMsg msg)
    {
        if (target == null)
        {
            target = new List<float>();
            rootBody.GetJointPositions(target);
        }
        for (int i = 0; i < msg.name.Length; i++)
        {
            if (i >= msg.position.Length) break;
            if (jointIndex.TryGetValue(msg.name[i], out int idx))
                if (idx < target.Count)
                    target[idx] = (float)msg.position[i];
        }
    }

    void FixedUpdate()
    {
        if (target != null)
            rootBody.SetJointPositions(target);   // 毎物理フレーム同じ値を設定し続ける
    }

    //void OnJointState(JointStateMsg msg)
    //{

    //    // 最新の全関節角度を取得
    //    rootBody.GetJointPositions(jointPositions);

    //    for (int i = 0; i < msg.name.Length; i++)
    //    {
    //        if (i >= msg.position.Length) break;
    //        if (jointIndex.TryGetValue(msg.name[i], out int idx))
    //        {
    //            if (idx < jointPositions.Count)
    //                jointPositions[idx] = (float)msg.position[i];   // ラジアンのまま
    //        }
    //    }

    //    // 物理を介さず関節角度を直接セット
    //    rootBody.SetJointPositions(jointPositions);

    //    // 設定直後に実際の値を読み返してログ
    //    var check = new List<float>();
    //    rootBody.GetJointPositions(check);
    //    Debug.Log($"boom設定={jointPositions[1]:F3} 読返し={check[1]:F3}");
    //}

}
