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

/*
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
    public float yaw = 0f;     // θ
    public float pitch = 20f;  // φ

    [Header("Caps")]
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public float minDistance = 1f;
    public float maxDistance = 10f;

    void OnEnable()
    {
        vectors = GetComponent<VectorRenderer>();
    }

    void Update()
    {
        if (player == null) return;

        // Clamp parameters
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // --- MODEL STEP 1: Camera Position ---
        Vector3 c = CameraPosition(player.position, distance, yaw, pitch);
        transform.position = c;

        // --- MODEL STEP 2: Basis Construction ---
        Basis basis = ComputeBasis(c, player.position);

        // --- MODEL STEP 3: Rotation Matrix ---
        Matrix3x3 R = BuildRotationMatrix(basis);

        // Convert matrix → Unity rotation (still needed for rendering)
        transform.rotation = R.ToQuaternion();

        // Debug drawing
        DrawDebug(c, basis);
    }

    // ===============================
    // MODEL FUNCTIONS
    // ===============================

    // Camera position: spherical → Cartesian
    Vector3 CameraPosition(Vector3 p, float d, float yawDeg, float pitchDeg)
    {
        float θ = yawDeg * Mathf.Deg2Rad;
        float φ = pitchDeg * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Cos(φ) * Mathf.Cos(θ),
            Mathf.Sin(φ),
            Mathf.Cos(φ) * Mathf.Sin(θ)
        );

        return p + d * offset;
    }

    // Basis = {right, up, forward}
    Basis ComputeBasis(Vector3 c, Vector3 p)
    {
        Vector3 f = (p - c).normalized;

        Vector3 worldUp = Vector3.up;

        // Handle degeneracy (important for correctness)
        if (Mathf.Abs(Vector3.Dot(f, worldUp)) > 0.99f)
            worldUp = Vector3.right;

        Vector3 r = Vector3.Cross(worldUp, f).normalized;
        Vector3 u = Vector3.Cross(f, r).normalized;

        return new Basis(r, u, f);
    }

    // Rotation matrix from basis
    Matrix3x3 BuildRotationMatrix(Basis b)
    {
        return new Matrix3x3(
            b.right,
            b.up,
            b.forward
        );
    }

    // ===============================
    // DEBUG
    // ===============================

    void DrawDebug(Vector3 c, Basis b)
    {
        using (vectors.Begin())
        {
            vectors.Draw(c, player.position, Color.yellow);

            float s = 2f;
            vectors.Draw(c, c + b.forward * s, Color.blue);
            vectors.Draw(c, c + b.right * s, Color.red);
            vectors.Draw(c, c + b.up * s, Color.green);
        }
    }
}

//
// ===== DATA STRUCTURES =====
//

// Represents a basis (coordinate frame)
public struct Basis
{
    public Vector3 right;
    public Vector3 up;
    public Vector3 forward;

    public Basis(Vector3 r, Vector3 u, Vector3 f)
    {
        right = r;
        up = u;
        forward = f;
    }
}

// Represents a 3×3 matrix
public struct Matrix3x3
{
    public Vector3 c0, c1, c2; // columns

    public Matrix3x3(Vector3 col0, Vector3 col1, Vector3 col2)
    {
        c0 = col0;
        c1 = col1;
        c2 = col2;
    }

    // Convert matrix → quaternion (explicit math)
    public Quaternion ToQuaternion()
    {
        float trace = c0.x + c1.y + c2.z;

        if (trace > 0)
        {
            float s = Mathf.Sqrt(trace + 1f) * 2f;
            float w = 0.25f * s;
            float x = (c2.y - c1.z) / s;
            float y = (c0.z - c2.x) / s;
            float z = (c1.x - c0.y) / s;
            return new Quaternion(x, y, z, w);
        }
        else
        {
            // Fallback (simplified for assignment)
            return Quaternion.LookRotation(c2, c1);
        }
    }
}

🧠 Why this is now a model

Your code now explicitly shows:

1. Position equation
CameraPosition(...)
2. Basis construction
ComputeBasis(...)
3. Rotation matrix
Matrix3x3 R = BuildRotationMatrix(...)
4. Transformation pipeline
(p, d, θ, φ) → c → basis → R → rotation
🎯 What your teacher will like
You separated math from implementation
You named the mathematical concepts
You explicitly constructed a matrix
You handled degenerate cases

This is no longer just “Unity code” — it’s a clear linear algebra model implemented in code.

If you want to go one step further (for top marks), I can help you:

Add the full 4×4 transformation matrix
Or write the exact report that matches this code line-by-line
*/