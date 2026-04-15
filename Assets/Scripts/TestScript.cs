using System;
using UnityEditor;
using UnityEngine;
using Vectors;

[ExecuteAlways]
[RequireComponent(typeof(VectorRenderer))]
public class Example : MonoBehaviour {
    
    [NonSerialized] 
    private VectorRenderer vectors;

    [SerializeField]
    public Vector3 vectorA = new Vector3(3, 0, 0);
    
    [SerializeField]
    public Vector3 vectorB = new Vector3(0, 3, 0);
    
    [SerializeField]
    public Vector3 vectorC = new Vector3(0, 0, 3);
    
    void OnEnable() {
        vectors = GetComponent<VectorRenderer>();
    }

    void Update()
    {
        using (vectors.Begin()) {
            vectors.Draw(Vector3.zero, vectorA, Color.red);
            vectors.Draw(Vector3.zero, vectorB, Color.green);
            vectors.Draw(Vector3.zero, vectorC, Color.blue);
        }
    }
}

[CustomEditor(typeof(Example))]
public class ExampleGUI : Editor {
    void OnSceneGUI() {
        var ex = target as Example;
        if (ex == null) return;

        EditorGUI.BeginChangeCheck();
        var a = Handles.PositionHandle(ex.vectorA, Quaternion.identity);
        var b = Handles.PositionHandle(ex.vectorB, Quaternion.identity);
        var c = Handles.PositionHandle(ex.vectorC, Quaternion.identity);

        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(target, "Vector Positions");
            ex.vectorA = a;
            ex.vectorB = b;
            ex.vectorC = c;
            EditorUtility.SetDirty(target);
        }
    }
}