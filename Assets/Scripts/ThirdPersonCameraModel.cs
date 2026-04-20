using System;
using UnityEngine;
using Vectors;

[ExecuteAlways]
[RequireComponent(typeof(VectorRenderer))]
public class ThirdPersonCameraModel : MonoBehaviour
{
    private VectorRenderer vectors;

    [Header("References")]
    public Transform player;

    [Header("Inputs")]
    public float mouseSensitivity = 1f;
    public float zoomSensitivity = 1f;

    [Header("Parameters")]
    public float distance = 5f;
    public float yaw = 0f;
    public float pitch = 20f;

    [Header("Caps")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    public float minDistance = 1f;
    public float maxDistance = 10f;

    [Header("Debug")]
    public bool showBasis = true;

    void OnEnable()
    {
        vectors = GetComponent<VectorRenderer>();
    }

    void Update()
    {
        if(player == null) return;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        float yawRad = yaw * Mathf.Deg2Rad;
        float pitchRad = pitch * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Cos(pitchRad) * Mathf.Cos(yawRad), Mathf.Sin(pitchRad), Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)) * distance;

        Vector3 cameraPosition = player.position + offset;
        transform.position = cameraPosition;

        Vector3 forward = (player.position - cameraPosition).normalized;

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        // Cross(Vector3.up, forward) = (Vector3.upy * forwardz - Vector3.upz * forwardy,
        //                               Vector3.upz * forwardx - Vector3.upx * forwardz,
        //                               Vector3.upx * forwardy - Vector3.upy * forwardx)

        Vector3 up = Vector3.Cross(forward, right).normalized;
        // Cross(forward, right) = (forwardy * rightz - forwardz * righty,
        //                          forwardz * rightx - forwardx * rightz,
        //                          forwardx * righty - forwardy * rightx)
        
        Vector3[] basis = new Vector3[3];
        basis[0] = right;
        basis[1] = up;
        basis[2] = forward;

        // Convert basis to rotation
        Quaternion rotation = Quaternion.LookRotation(basis[2], basis[1]);
        transform.rotation = rotation;

        using (vectors.Begin())
        {
            vectors.Draw(cameraPosition, player.position, Color.yellow);

            if (showBasis)
            {
                float scale = 2f;

                vectors.Draw(cameraPosition, cameraPosition + forward * scale, Color.blue);
                vectors.Draw(cameraPosition, cameraPosition + right * scale, Color.red);
                vectors.Draw(cameraPosition, cameraPosition + up * scale, Color.green);
            }
        }

        MouseInput();
    }

    private void MouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        yaw -= mouseX * mouseSensitivity * 100f * Time.deltaTime;
        pitch -= mouseY * mouseSensitivity * 100f * Time.deltaTime;
        distance -= scroll * zoomSensitivity;
    }
}
