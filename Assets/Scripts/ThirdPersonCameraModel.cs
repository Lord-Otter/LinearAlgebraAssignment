using System;
using System.Collections.Generic;
using UnityEngine;
using Vectors;

[ExecuteAlways]
[RequireComponent(typeof(VectorRenderer))]
public class ThirdPersonCameraModel : MonoBehaviour
{
    public enum CameraInputMode
    {
        LiveMouse,
        Simulated
    }

    [Header("Input Mode")]
    public CameraInputMode inputMode;

    private VectorRenderer vectors;

    [Header("Player Position")]
    public Transform player;
    public Vector3 playerPosition;
    public Vector3 playerRotation;

    [Header("Camera Start Position")]
    public Transform cameraPropStart;
    public Transform cameraPropEnd;
    public float distance = 5f;
    public float yaw = 0f;
    public float pitch = 20f;

    [Header("Camera Movement Caps")]
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public float minDistance = 1f;
    public float maxDistance = 10f;

    [Header("Path Visualization")]
    public GameObject pathDotPrefab;
    public float simulationDuration = 1f;
    public int simulationFPS = 60;
    public bool generatePath = false;

    [Header("Mouse Input")]
    public float totalMouseX;
    public float totalMouseY;
    public float totalScroll;
    private float liveMouseX;
    private float liveMouseY;
    private float liveScroll;

    [Header("Sensitivity")]
    public float mouseSensitivity = 1f;
    public float zoomSensitivity = 1f;

    [Header("Visualization")]
    public bool showBasis = true;
    public bool showPathArrows = true;

    private List<GameObject> pathDots = new List<GameObject>();

    void OnEnable()
    {
        vectors = GetComponent<VectorRenderer>();
    }

    void Update()
    {
        if (player == null || cameraPropStart == null) return;

        SetPlayerPosition();
        UpdateInput();

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        float yawRad = yaw * Mathf.Deg2Rad;
        float pitchRad = pitch * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Cos(pitchRad) * Mathf.Cos(yawRad), Mathf.Sin(pitchRad), Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)) * distance;

        Vector3 cameraPosition = player.position + offset;
        cameraPropStart.position = cameraPosition;

        Vector3 forward = (player.position - cameraPosition).normalized;

        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        // Cross(Vector3.up, forward) = (Vector3.upy * forwardz - Vector3.upz * forwardy,
        //                               Vector3.upz * forwardx - Vector3.upx * forwardz,
        //                               Vector3.upx * forwardy - Vector3.upy * forwardx)

        Vector3 up = Vector3.Cross(right, forward).normalized;
        // Cross(forward, right) = (forwardy * rightz - forwardz * righty,
        //                          forwardz * rightx - forwardx * rightz,
        //                          forwardx * righty - forwardy * rightx)
        
        Vector3[] basis = new Vector3[3];
        basis[0] = right;
        basis[1] = up;
        basis[2] = forward;

        Quaternion rotation = Quaternion.LookRotation(basis[2], basis[1]);
        cameraPropStart.rotation = rotation;

        float simYaw = yaw;
        float simPitch = pitch;
        float simDistance = distance;

        if (inputMode == CameraInputMode.Simulated)
        {
            simYaw   -= totalMouseX * mouseSensitivity;
            simPitch -= totalMouseY * mouseSensitivity;
            simDistance -= totalScroll * zoomSensitivity;
        }

        simPitch = Mathf.Clamp(simPitch, minPitch, maxPitch);
        simDistance = Mathf.Clamp(simDistance, minDistance, maxDistance);

        float simYawRad = simYaw * Mathf.Deg2Rad;
        float simPitchRad = simPitch * Mathf.Deg2Rad;

        Vector3 simOffset = new Vector3(
            Mathf.Cos(simPitchRad) * Mathf.Cos(simYawRad),
            Mathf.Sin(simPitchRad),
            Mathf.Cos(simPitchRad) * Mathf.Sin(simYawRad)
        ) * simDistance;

        Vector3 simCameraPosition = player.position + simOffset;

        if (cameraPropEnd != null)
        {
            cameraPropEnd.position = simCameraPosition;

            Vector3 simForward = (player.position - simCameraPosition).normalized;
            Vector3 simRight = Vector3.Cross(simForward, Vector3.up).normalized;
            Vector3 simUp = Vector3.Cross(simRight, simForward).normalized;

            cameraPropEnd.rotation = Quaternion.LookRotation(simForward, simUp);
        }

        if (vectors == null || cameraPropStart == null || player == null)
            return;

        using (vectors.Begin())
        {
            vectors.Draw(cameraPosition, player.position, Color.yellow);

            foreach (var dot in pathDots)
            {
                if (dot == null) continue;

                var data = dot.GetComponent<CameraPathDot>();
                if (data == null) continue;

                Vector3 from = dot.transform.position;

                if (showPathArrows)
                {
                    Vector3 to = from + data.velocity;
                    vectors.Draw(from, to, Color.cyan);
                }
            }

            if (showBasis)
            {
                float scale = 2f;

                vectors.Draw(cameraPosition, cameraPosition + forward * scale, Color.blue);
                vectors.Draw(cameraPosition, cameraPosition + right * scale, Color.red);
                vectors.Draw(cameraPosition, cameraPosition + up * scale, Color.green);
            }
        }
    }

    private void SetPlayerPosition()
    {
        player.position = playerPosition;
        player.rotation = Quaternion.Euler(playerRotation);
    }

    private void UpdateInput()
    {
        if (inputMode == CameraInputMode.LiveMouse)
        {
            if (!Application.isPlaying)
                return;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            yaw   -= mouseX * mouseSensitivity * 100f * Time.deltaTime;
            pitch -= mouseY * mouseSensitivity * 100f * Time.deltaTime;
            distance -= scroll * zoomSensitivity;
        }
    }

    public void GeneratePath()
    {
        if (player == null || pathDotPrefab == null)
            return;

        ClearPathDots();

        int steps = Mathf.RoundToInt(simulationDuration * simulationFPS);
        if (steps < 1) steps = 1;

        float simYaw = yaw;
        float simPitch = pitch;
        float simDistance = distance;

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;

            float deltaYaw   = -(totalMouseX * t) * mouseSensitivity;
            float deltaPitch = -(totalMouseY * t) * mouseSensitivity;
            float deltaDist  = -(totalScroll * t) * zoomSensitivity;

            float stepYaw = simYaw + deltaYaw;
            float stepPitch = Mathf.Clamp(simPitch + deltaPitch, minPitch, maxPitch);
            float stepDistance = Mathf.Clamp(simDistance + deltaDist, minDistance, maxDistance);

            float yawRad = stepYaw * Mathf.Deg2Rad;
            float pitchRad = stepPitch * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(pitchRad) * Mathf.Cos(yawRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)
            ) * stepDistance;

            Vector3 pos = player.position + offset;

            GameObject dot = Instantiate(pathDotPrefab, pos, Quaternion.identity, transform);

            var data = dot.GetComponent<CameraPathDot>();
            if (data == null)
                data = dot.AddComponent<CameraPathDot>();

            if (pathDots.Count > 0)
            {
                Vector3 prevPos = pathDots[^1].transform.position;
                data.velocity = pos - prevPos;
            }
            else
            {
                data.velocity = Vector3.zero;
            }

            pathDots.Add(dot);
        }
    }

    public void ClearPathDots()
    {
        
        foreach (var dot in pathDots)
        {
            if (dot != null)
            {
    #if UNITY_EDITOR
                DestroyImmediate(dot);
    #else
                Destroy(dot);
    #endif
            }
        }

        pathDots.Clear();

        List<Transform> toDelete = new List<Transform>();

        foreach (Transform child in transform)
        {
            toDelete.Add(child);
        }

        foreach (Transform child in toDelete)
        {
    #if UNITY_EDITOR
            DestroyImmediate(child.gameObject);
    #else
            Destroy(child.gameObject);
    #endif
        }
    }
}

/*using System;
using System.Collections.Generic;
using UnityEngine;
using Vectors;

[ExecuteAlways]
[RequireComponent(typeof(VectorRenderer))]
public class ThirdPersonCameraModel : MonoBehaviour
{
    public enum CameraInputMode
    {
        LiveMouse,
        Simulated
    }

    [Header("Input Mode")]
    public CameraInputMode inputMode;

    private VectorRenderer vectors;

    [Header("Player Position")]
    public Transform player;
    public Vector3 playerPosition;
    public Vector3 playerRotation;

    [Header("Camera Start Position")]
    public Transform cameraPropStart;
    public Transform cameraPropEnd;
    public float distance = 5f;
    public float yaw = 0f;
    public float pitch = 20f;

    [Header("Camera Movement Caps")]
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public float minDistance = 1f;
    public float maxDistance = 10f;

    [Header("Path Visualization")]
    public GameObject pathDotPrefab;
    public float simulationDuration = 1f;
    public int simulationFPS = 60;
    public bool generatePath = false;

    [Header("Mouse Input")]
    public float totalMouseX;
    public float totalMouseY;
    public float totalScroll;
    private float liveMouseX;
    private float liveMouseY;
    private float liveScroll;

    [Header("Sensitivity")]
    public float mouseSensitivity = 1f;
    public float zoomSensitivity = 1f;

    [Header("Visualization")]
    public bool showBasis = true;

    private List<GameObject> pathDots = new List<GameObject>();

    void OnEnable()
    {
        vectors = GetComponent<VectorRenderer>();
    }

    void Update()
    {
        if (player == null || cameraPropStart == null) return;

        SetPlayerPosition();
        UpdateInput();

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        float yawRad = yaw * Mathf.Deg2Rad;
        float pitchRad = pitch * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Cos(pitchRad) * Mathf.Cos(yawRad), Mathf.Sin(pitchRad), Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)) * distance;

        Vector3 cameraPosition = player.position + offset;
        cameraPropStart.position = cameraPosition;

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

        Quaternion rotation = Quaternion.LookRotation(basis[2], basis[1]);
        cameraPropStart.rotation = rotation;

        if (vectors == null || cameraPropStart == null || player == null)
            return;

        if (!Application.isPlaying)
            return;

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
    

    public void GeneratePath()
    {
        ClearPathDots();
        GenerateCameraPath();
    }

    private void SetPlayerPosition()
    {
        player.position = playerPosition;
        player.rotation = Quaternion.Euler(playerRotation);
    }

    private void UpdateInput()
    {
        if (inputMode == CameraInputMode.LiveMouse)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            yaw   -= mouseX * mouseSensitivity * 100f * Time.deltaTime;
            pitch -= mouseY * mouseSensitivity * 100f * Time.deltaTime;
            distance -= scroll * zoomSensitivity;

            Debug.Log($"Scroll: {distance}");
            Debug.Log($"Mouse X: {yaw / 100f * -1f}");
            Debug.Log($"Mouse Y: {pitch / 100f * -1f}");

            return;
        }
    }

    private void GenerateCameraPath()
    {
        if (pathDotPrefab == null || player == null) return;

        int steps = Mathf.Max(1, Mathf.RoundToInt(simulationFPS * simulationDuration));

        ClearPathDots();

        for (int i = 0; i < steps; i++)
        {
            var dot = Instantiate(pathDotPrefab, transform);
            dot.name = "CameraPathDot";
            pathDots.Add(dot);
        }

        float perStepMouseX = totalMouseX / steps;
        float perStepMouseY = totalMouseY / steps;
        float perStepScroll = totalScroll / steps;

        float simYaw = yaw;
        float simPitch = pitch;
        float simDistance = distance;

        for (int i = 0; i <= steps; i++)
        {
            float yawRad = simYaw * Mathf.Deg2Rad;
            float pitchRad = simPitch * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(pitchRad) * Mathf.Cos(yawRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)
            ) * simDistance;

            Vector3 camPos = player.position + offset;

            Vector3 forward = (player.position - camPos).normalized;
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

            if (i == steps)
            {
                if (cameraPropEnd != null)
                {
                    cameraPropEnd.position = camPos;
                    cameraPropEnd.rotation = rot;
                }
            }

            if (i > 0 && i < steps)
            {
                pathDots[i - 1].transform.SetPositionAndRotation(camPos, rot);
            }

            simYaw   -= perStepMouseX * mouseSensitivity * 100f;
            simPitch -= perStepMouseY * mouseSensitivity * 100f;
            simDistance -= perStepScroll * zoomSensitivity;

            simPitch = Mathf.Clamp(simPitch, minPitch, maxPitch);
            simDistance = Mathf.Clamp(simDistance, minDistance, maxDistance);
        }
    }

    public void ClearPathDots()
    {
        // Find ALL child dots, even if list lost references
        List<Transform> toDelete = new List<Transform>();

        foreach (Transform child in transform)
        {
            if (child.name.Contains("CameraPathDot"))
            {
                toDelete.Add(child);
            }
        }

        // Destroy them safely
        foreach (var t in toDelete)
        {
            if (Application.isPlaying)
                Destroy(t.gameObject);
            else
                DestroyImmediate(t.gameObject);
        }

        pathDots.Clear();
    }
}*/
/*
using System;
using UnityEngine;
using Vectors;

[ExecuteAlways]
[RequireComponent(typeof(VectorRenderer))]
public class ThirdPersonCameraModel : MonoBehaviour
{
    public enum CameraInputMode
    {
        LiveMouse,
        Simulated
    }

    [Header("Input Mode")]
    public CameraInputMode inputMode;

    private VectorRenderer vectors;

    [Header("Player Position")]
    public Transform player;
    public Vector3 playerPosition;
    public Vector3 playerRotation;

    [Header("Camera Start Position")]
    public Transform cameraPropStart;
    public Transform cameraPropEnd;
    public float distance = 5f;
    public float yaw = 0f;
    public float pitch = 20f;

    [Header("Camera Movement Caps")]
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public float minDistance = 1f;
    public float maxDistance = 10f;

    [Header("Path Visualization")]
    public GameObject pathDotPrefab;
    public float simulationDuration = 1f;
    public int simulationFPS = 60;
    public bool generatePath = false;

    [Header("Mouse Input")]
    public float totalMouseX;
    public float totalMouseY;
    public float totalScroll;
    private float liveMouseX;
    private float liveMouseY;
    private float liveScroll;

    [Header("Sensitivity")]
    public float mouseSensitivity = 1f;
    public float zoomSensitivity = 1f;

    [Header("Visualization")]
    public bool showPath = true;
    public bool showBasis = true;

    private bool pathDirty = true;

    void OnEnable()
    {
        vectors = GetComponent<VectorRenderer>();
    }

    void Update()
    {
        if (player == null || cameraPropStart == null) return;

        SetPlayerPosition();
        UpdateInput();

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        float yawRad = yaw * Mathf.Deg2Rad;
        float pitchRad = pitch * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Cos(pitchRad) * Mathf.Cos(yawRad), Mathf.Sin(pitchRad), Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)) * distance;

        Vector3 cameraPosition = player.position + offset;
        cameraPropStart.position = cameraPosition;

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

        Quaternion rotation = Quaternion.LookRotation(basis[2], basis[1]);
        cameraPropStart.rotation = rotation;

        if (vectors == null || cameraPropStart == null || player == null)
            return;

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

        if (generatePath && pathDirty && inputMode != CameraInputMode.LiveMouse)
        {
            GenerateCameraPath();
            pathDirty = false;
        }      
    }

    private void SetPlayerPosition()
    {
        player.position = playerPosition;
        player.rotation = Quaternion.Euler(playerRotation);
    }

    private void UpdateInput()
    {
        if (inputMode == CameraInputMode.LiveMouse)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (mouseX != 0 || mouseY != 0 || scroll != 0)
                pathDirty = true;

            yaw   -= mouseX * mouseSensitivity * 100f * Time.deltaTime;
            pitch -= mouseY * mouseSensitivity * 100f * Time.deltaTime;
            distance -= scroll * zoomSensitivity;

            return;
        }
    }

    void OnValidate()
    {
        pathDirty = true;
    }

    private void GenerateCameraPath()
    {
        if (pathDotPrefab == null || player == null) return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        int steps = Mathf.Max(1, Mathf.RoundToInt(simulationFPS * simulationDuration));
        float dt = simulationDuration / steps;

        float perStepMouseX = totalMouseX / steps;
        float perStepMouseY = totalMouseY / steps;
        float perStepScroll = totalScroll / steps;

        float simYaw = yaw;
        float simPitch = pitch;
        float simDistance = distance;

        for (int i = 0; i <= steps; i++)
        {
            float yawRad = simYaw * Mathf.Deg2Rad;
            float pitchRad = simPitch * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(pitchRad) * Mathf.Cos(yawRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(pitchRad) * Mathf.Sin(yawRad)
            ) * simDistance;

            Vector3 camPos = player.position + offset;

            Vector3 forward = (player.position - camPos).normalized;
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

            if (i == steps)
            {
                if (cameraPropEnd != null)
                {
                    cameraPropEnd.position = camPos;
                    cameraPropEnd.rotation = rot;
                }
            }
            else if (i > 0)
            {
                Instantiate(pathDotPrefab, camPos, rot, transform);
            }

            simYaw   -= perStepMouseX * mouseSensitivity * 100f;
            simPitch -= perStepMouseY * mouseSensitivity * 100f;
            simDistance -= perStepScroll * zoomSensitivity;

            simPitch = Mathf.Clamp(simPitch, minPitch, maxPitch);
            simDistance = Mathf.Clamp(simDistance, minDistance, maxDistance);
        }
    }
}
*/

    /*private void MouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        yaw -= mouseX * mouseSensitivity * 100f * Time.deltaTime;
        pitch -= mouseY * mouseSensitivity * 100f * Time.deltaTime;
        distance -= scroll * zoomSensitivity;
    }
}*/