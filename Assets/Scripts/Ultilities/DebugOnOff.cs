using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugOnOff : MonoBehaviour
{
    private void Awake()
    {
        DebugLog("Awake");
    }

    private void OnEnable()
    {
        DebugLog("OnEnable");
    }

    private void Start()
    {
        DebugLog("Start");
    }

    private void OnDisable()
    {
        DebugLog("OnDisable");
    }

    private void OnDestroy()
    {
        DebugLog("OnDestroy");
    }

    private void DebugLog(string methodName)
    {
        Debug.Log($"[Frame: {Time.frameCount}] InstanceID [{this.transform.GetInstanceID()}] [{gameObject.name}] {methodName}",gameObject);
    }
}
