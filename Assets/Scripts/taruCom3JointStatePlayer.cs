using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;          // JointStateMsg はここ
using Unity.Robotics.UrdfImporter;

// joint_states をsubscribeして、記録された関節角度を Unity のrobotに再現するための最小スクリプト。
// 既存の Com3FrontController から「関節を辞書に登録して xDrive.target を設定する」部分だけを流用。
public class taruCom3JointStatePlayer : MonoBehaviour
{
    private ROSConnection ros;

    // 関節名 -> ArticulationBody
    private Dictionary<string, ArticulationBody> joints;

    // subscribeするtopic名。bag に合わせて Inspector から変更可能。
    public string jointStatesTopicName = "[robot_name]/joint_states";

    // JointState の position はラジアン、ArticulationDrive.target は度。
    // 既存コントローラと同じ単位変換にしておく。
    public bool convertRadToDeg = true;

    private EmergencyStop emergencyStop;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        emergencyStop = EmergencyStop.GetEmergencyStop(this.gameObject);

        joints = new Dictionary<string, ArticulationBody>();

        var bodies = this.GetComponentsInChildren<ArticulationBody>();
        Debug.Log($"[JointPlayer] Start called. ArticulationBody count = {bodies.Length}");

        foreach (var joint in this.GetComponentsInChildren<ArticulationBody>())
        {
            var ujoint = joint.GetComponent<UrdfJoint>();
            if (ujoint == null)
            {
                Debug.Log($"[JointPlayer] {joint.name} に UrdfJoint なし → スキップ");
                continue;
            }
            Debug.Log($"[JointPlayer] found UrdfJoint: {ujoint.jointName}");

            // 名前で引けるように登録
            if (!joints.ContainsKey(ujoint.jointName))
            {
                joints.Add(ujoint.jointName, joint);
                Debug.Log($"[JointPlayer] registered joint: {ujoint.jointName}");
            }

            // 位置追従できるように drive を設定（既存の position 制御と同じ考え方）
            ArticulationDrive drive = joint.xDrive;
            //drive.stiffness = 200000;
            //drive.damping = 100000;
            //drive.forceLimit = float.MaxValue;
            if (drive.stiffness == 0)
                drive.stiffness = 200000;
            if (drive.damping == 0)
                drive.damping = 100000;
            if (drive.forceLimit == 0)
                drive.forceLimit = 100000;
            joint.xDrive = drive;
        }
        Debug.Log($"[JointPlayer] 登録完了。joints.Count = {joints.Count}");

        ros.Subscribe<JointStateMsg>(
            Utils.PreprocessNamespace(this.gameObject, jointStatesTopicName),
            OnJointState);
    }

    void OnJointState(JointStateMsg msg)
    {
        if (emergencyStop && emergencyStop.isEmergencyStop)
            return;

        // name[] と position[] は同じインデックスで対応している前提
        for (int i = 0; i < msg.name.Length; i++)
        {
            // position が name より短いケースに備えて防御
            if (i >= msg.position.Length)
                break;

            if (!joints.TryGetValue(msg.name[i], out var joint))
                continue;   // この bag の関節名が URDF に無ければ飛ばす

            float target = (float)msg.position[i];
            if (convertRadToDeg)
                target *= Mathf.Rad2Deg;

            ArticulationDrive drive = joint.xDrive;
            //target = Mathf.Clamp(target, drive.lowerLimit, drive.upperLimit);
            Debug.Log($"{msg.name[i]}: target={target:F1}度 limit=[{drive.lowerLimit:F1}, {drive.upperLimit:F1}] 現在={joint.jointPosition[0] * Mathf.Rad2Deg:F1}度");
            drive.target = target;
            joint.xDrive = drive;
        }
    }
}