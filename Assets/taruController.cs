using UnityEngine;

public class taruController : MonoBehaviour
{
    [Header("Joints (ArticulationBody)")]
    public ArticulationBody swingJoint;   // body_link
    public ArticulationBody boomJoint;    // boom_link
    public ArticulationBody armJoint;     // arm_link
    public ArticulationBody bucketJoint;  // bucket_link

    [Header("Speed (degree/sec)")]
    public float speed = 30f;

    private int selectedIndex = 0;
    private ArticulationBody[] joints;
    private string[] names = { "swing", "boom", "arm", "bucket" };

    void Start()
    {
        joints = new[] { swingJoint, boomJoint, armJoint, bucketJoint };
    }

    void Update()
    {
        // 左右キーで関節を切り替え
        if (Input.GetKeyDown(KeyCode.RightArrow))
            selectedIndex = (selectedIndex + 1) % joints.Length;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            selectedIndex = (selectedIndex - 1 + joints.Length) % joints.Length;

        // 上下キーで選択中の関節を動かす
        float input = Input.GetAxis("Vertical");
        if (input != 0 && joints[selectedIndex] != null)
        {

            var joint = joints[selectedIndex];
            var drive = joint.xDrive;

            //Debug.Log($"[Before] selected={names[selectedIndex]}, target={drive.target}, input={input}");

            drive.target += input * speed * Time.deltaTime;
            // Limits内に収める
            drive.target = Mathf.Clamp(drive.target, drive.lowerLimit, drive.upperLimit);
            // Revoluteの場合のみclamp(lowerとupperが両方0でないとき)
            if (joint.jointType == ArticulationJointType.RevoluteJoint &&
                !(drive.lowerLimit == 0 && drive.upperLimit == 0))
            {
                drive.target = Mathf.Clamp(drive.target, drive.lowerLimit, drive.upperLimit);
            }

            joint.xDrive = drive;

            //Debug.Log($"[After]  selected={names[selectedIndex]}, target={joint.xDrive.target}");
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 20),
            $"Selected: {names[selectedIndex]}  (Left/Right to change, Up/Down to move)");
    }
}