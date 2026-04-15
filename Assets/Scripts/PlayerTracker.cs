using System;
using UnityEditor;
using UnityEngine;
using Vectors;

public class PlayerTracker : MonoBehaviour
{
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private Transform playerPosition;


    void Awake()
    {
        cameraPosition = this.GetComponent<Transform>();
        playerPosition = GameObject.Find("Player").GetComponent<Transform>();
    }

    
}
