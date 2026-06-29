using UnityEngine;

public class TestJoint : MonoBehaviour
{
    public ArticulationBody joint;
    private float target = 0f;

    void Update()
    {
        target += Input.GetAxis("Vertical") * 90f * Time.deltaTime;

        var drive = joint.xDrive;
        drive.target = target;
        joint.xDrive = drive;

        if (Input.GetAxis("Vertical") != 0)
        {
            float actual = joint.jointPosition[0] * Mathf.Rad2Deg;
            Debug.Log($"Target: {target:F1}, Actual: {actual:F1}, " +
                      $"After write: {joint.xDrive.target:F1}");
        }
    }
}