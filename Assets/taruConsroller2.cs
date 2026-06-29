using UnityEngine;

public class taruController2 : MonoBehaviour
{
    [Header("Joints (ArticulationBody)")]
    public ArticulationBody swingJoint;
    public ArticulationBody boomJoint;
    public ArticulationBody armJoint;
    public ArticulationBody bucketJoint;

    [Header("Speed (degree/sec)")]
    public float swingSpeed = 60f;
    public float boomSpeed = 10f;
    public float armSpeed = 30f;
    public float bucketSpeed = 30f;

    private int selectedIndex = 0;
    private ArticulationBody[] joints;
    private float[] speeds;
    private float[] targets;  // ← 追加: targetを自分で保持
    private string[] names = { "swing", "boom", "arm", "bucket" };

    void Start()
    {
        joints = new[] { swingJoint, boomJoint, armJoint, bucketJoint };
        speeds = new[] { swingSpeed, boomSpeed, armSpeed, bucketSpeed };
        targets = new float[joints.Length];

        // 初期値を現在のtargetに揃える
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] != null)
                targets[i] = joints[i].xDrive.target;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            selectedIndex = (selectedIndex + 1) % joints.Length;
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            selectedIndex = (selectedIndex - 1 + joints.Length) % joints.Length;

        float input = Input.GetAxis("Vertical");
        if (input != 0 && joints[selectedIndex] != null)
        {
            // 自前の変数に加算(これでswingでも問題なし)
            targets[selectedIndex] += input * speeds[selectedIndex] * Time.deltaTime;

            var joint = joints[selectedIndex];
            var drive = joint.xDrive;

            // Revoluteならclamp、Continuous(swing)はclampしない
            if (selectedIndex != 0)
            {
                targets[selectedIndex] = Mathf.Clamp(
                    targets[selectedIndex], drive.lowerLimit, drive.upperLimit);
            }

            drive.target = targets[selectedIndex];
            joint.xDrive = drive;
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 20),
            $"Selected: {names[selectedIndex]}  (Left/Right to change, Up/Down to move)");
    }
}
