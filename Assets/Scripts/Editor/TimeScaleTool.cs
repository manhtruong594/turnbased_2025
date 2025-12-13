using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TimeScaleTool : EditorWindow
{
    static float timeScaleValue = 1;
    static bool turnOn = false;
    [MenuItem("Horus/DebugTool/IncreaseTimeScale &3")]
    static void IncreaseTimeScale()
    {
        TurnOnUI();
        timeScaleValue = Mathf.Min(timeScaleValue + 0.2f, 4);
    }

    [MenuItem("Horus/DebugTool/DeincreaseTimeScale &2")]
    static void DeincreaseTimeScale()
    {
        TurnOnUI();
        timeScaleValue = Mathf.Max(timeScaleValue - 0.2f, 0);
    }

    [MenuItem("Horus/DebugTool/ResetTimeScale &1")]
    static void ResetTimeScale()
    {
        TurnOnUI();
        timeScaleValue = 1f;
    }
    [MenuItem("Horus/DebugTool/OffTimeScaleTool &`")]
    static void OffTimeScaleTool()
    {
        turnOn = false;
        Time.timeScale = 1;
        SceneView.duringSceneGui -= ShowUI;
    }

    static void ShowUI(SceneView obj)
    {
        GUIStyle timeScale = new GUIStyle(GUI.skin.button);
        timeScale.fontStyle = FontStyle.Bold;
        timeScale.fontSize = 10;
        timeScale.alignment = TextAnchor.MiddleCenter;
        timeScale.normal.textColor = Color.white;
       // Debug.Log($"{obj.position.width} -- {obj.position.height}");
        //  obj.BeginWindows();
        Handles.BeginGUI();
        GUI.backgroundColor = Color.red;
        GUI.Box(new Rect(obj.position.width / 2f - 80, obj.position.height - 60, 180, 40), "");
        GUI.Button(new Rect(obj.position.width / 2f - 80, obj.position.height - 60, 180, 20), $"Time Scale : {Mathf.Round(timeScaleValue * 100) / 100f}", timeScale);
        timeScaleValue = GUI.HorizontalSlider(new Rect(obj.position.width / 2f - 50, obj.position.height - 40, 120, 20), timeScaleValue, 0, 4);
        Time.timeScale = timeScaleValue;
        Handles.EndGUI();
        //  obj.EndWindows();
    }

    static void TurnOnUI()
    {
        if (!turnOn)
        {
            turnOn = true;
            SceneView.duringSceneGui += ShowUI;
        }
    }
}
