using UnityEngine;

public class taruTest : MonoBehaviour
{
    public ArticulationBody swingJoint;
    public ArticulationBody boomJoint;
    public ArticulationBody armJoint;
    public ArticulationBody bucketJoint;

    private float swingTarget = 0f;

    void Update()
    {
        float input = Input.GetAxis("Vertical");
        if (input != 0)
        {
            swingTarget += input * 90f * Time.deltaTime;
            var drive = swingJoint.xDrive;
            drive.target = swingTarget;
            swingJoint.xDrive = drive;

            float actual = swingJoint.jointPosition[0] * Mathf.Rad2Deg;
            Debug.Log($"Swing - Target: {swingTarget:F1}, Actual: {actual:F1}");
        }
    }
}
