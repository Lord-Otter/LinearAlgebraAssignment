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

        Vector3 helper = Mathf.Abs(forward.y) > 0.999f ? Vector3.forward : Vector3.up;

        Vector3 right = Vector3.Cross(helper, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right).normalized;

        Matrix4x4 rotMatrix = new Matrix4x4();

        rotMatrix.SetColumn(0, new Vector4(right.x, right.y, right.z, 0));
        rotMatrix.SetColumn(1, new Vector4(up.x, up.y, up.z, 0));
        rotMatrix.SetColumn(2, new Vector4(forward.x, forward.y, forward.z, 0));
        rotMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));

        transform.rotation = rotMatrix.rotation;

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
    }
}
