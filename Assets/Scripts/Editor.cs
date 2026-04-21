using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ThirdPersonCameraModel))]
public class ThirdPersonCameraModelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ThirdPersonCameraModel script = (ThirdPersonCameraModel)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Camera Path"))
        {
            Undo.RecordObject(script, "Generate Camera Path");
            script.GeneratePath();
            EditorUtility.SetDirty(script);
        }

        if (GUILayout.Button("Clear Path"))
        {
            Undo.RecordObject(script, "Clear Camera Path");
            script.ClearPathDots();
            EditorUtility.SetDirty(script);
        }
    }
}
