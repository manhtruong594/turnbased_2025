using System;
using System.IO;
using System.Linq;
using RedBjorn.ProtoTiles;
using TurnBasedGame.Maps;
using TurnBasedGame.Unit;
using TurnBasedGame.Capture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KenneyHudSetup : UCP.Bridge.IUCPScript
{
    public string Name => "kenney-hud-setup";
    public string Description => "Configure Kenney town in HUDScene without entering Play Mode.";
    public object Execute(string parameters)
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/HUDScene.unity")
            throw new InvalidOperationException("HUDScene must be active in Edit Mode.");
        if (parameters.Contains("cleanup"))
        {
            AssetDatabase.DeleteAsset("Assets/Scripts/Editor/KenneyHudSetup.cs");
            return new { cleaned = true };
        }
        var definition = AssetDatabase.LoadAssetAtPath<BattleMapDefinitionSO>("Assets/MyGame/Maps/KenneyTownTest/KenneyTownTest_Definition.asset");
        if (!definition.TryValidate(out var error)) throw new InvalidOperationException(error);
        var manager = UnityEngine.Object.FindObjectsByType<MapManager>(FindObjectsSortMode.None).Single();
        var mediator = UnityEngine.Object.FindObjectsByType<GameMediator>(FindObjectsSortMode.None).Single();
        var mediatorData = new SerializedObject(mediator);
        foreach (var field in new[] { "turnManager", "mpManager", "mapManager", "unitSpawner", "areaPathManager", "capturePointManager", "spellCardManager" })
            if (!mediatorData.FindProperty(field).objectReferenceValue) throw new InvalidOperationException("Missing mediator reference: " + field);
        var entity = new MapEntity(definition.MapSettings, null);
        foreach (var point in definition.SpawnPoints.Select(p => p.GridPosition).Concat(definition.CapturePoints))
            if (entity.Tile(point) == null || !entity.Tile(point).Vacant) throw new InvalidOperationException("Invalid gameplay point " + point);
        if (File.Exists("Artifacts/KenneyTownTest/HUDScene.before.unity")) throw new InvalidOperationException("Backup already exists; refusing repeat setup.");
        File.Copy(scene.path, "Artifacts/KenneyTownTest/HUDScene.before.unity");
        Undo.RecordObject(manager, "Set Kenney map");
        manager.Map = definition.MapSettings;
        var data = new SerializedObject(manager);
        data.FindProperty("_editorTestMap").objectReferenceValue = definition;
        data.ApplyModifiedProperties();
        var view = manager.MapView.transform;
        foreach (var point in UnityEngine.Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Undo.DestroyObjectImmediate(point.gameObject);
        foreach (var point in UnityEngine.Object.FindObjectsByType<CapturePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Undo.DestroyObjectImmediate(point.gameObject);
        for (int i = view.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(view.GetChild(i).gameObject);
        var tiles = new GameObject("Tiles");
        tiles.transform.SetParent(view, false);
        foreach (var tile in manager.Map.Tiles)
        {
            var preset = manager.Map.Presets.First(p => p.Id == tile.Id);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(preset.Prefabs[Mathf.Clamp(tile.PrefabIndex, 0, preset.Prefabs.Count - 1)], tiles.transform);
            instance.transform.position = manager.Map.ToWorld(tile.TilePos, manager.Map.Edge);
        }
        PrefabUtility.InstantiatePrefab(definition.EnvironmentPrefab, view);
        var points = new GameObject("Kenney Gameplay Points");
        points.transform.SetParent(view, false);
        foreach (var point in definition.SpawnPoints)
        {
            var go = new GameObject("SpawnPoint_" + point.Owner + "_" + point.GridPosition);
            go.transform.SetParent(points.transform, false);
            go.transform.position = entity.WorldPosition(point.GridPosition);
            go.AddComponent<SpawnPoint>().Initialize(point.Owner, point.GridPosition);
        }
        foreach (var point in definition.CapturePoints)
        {
            var go = new GameObject("CapturePoint_" + point);
            go.transform.SetParent(points.transform, false);
            go.transform.position = entity.WorldPosition(point);
            go.AddComponent<CapturePoint>().Initialize(point);
        }
        var spawner = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<UnitSpawner>());
        var spawns = spawner.FindProperty("spawnPoints");
        var spawnComponents = points.GetComponentsInChildren<SpawnPoint>();
        spawns.arraySize = spawnComponents.Length;
        for (int i = 0; i < spawns.arraySize; i++) spawns.GetArrayElementAtIndex(i).objectReferenceValue = spawnComponents[i];
        spawner.ApplyModifiedProperties();
        var captures = new SerializedObject(UnityEngine.Object.FindFirstObjectByType<CapturePointManager>());
        var captureList = captures.FindProperty("_capturePoints");
        var captureComponents = points.GetComponentsInChildren<CapturePoint>();
        captureList.arraySize = captureComponents.Length;
        for (int i = 0; i < captureList.arraySize; i++) captureList.GetArrayElementAtIndex(i).objectReferenceValue = captureComponents[i];
        captures.ApplyModifiedProperties();
        var boat = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Boat");
        if (boat) { Undo.RecordObject(boat, "Hide previous map decoration"); boat.SetActive(false); }
        var camera = Camera.main;
        Undo.RecordObjects(new UnityEngine.Object[] { camera, camera.transform }, "Frame Kenney map");
        camera.transform.position = new Vector3(33, 42, -43);
        camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true;
        camera.orthographicSize = 27;
        camera.farClipPlane = Mathf.Max(camera.farClipPlane, 250);
        var controller = camera.GetComponent<IsometricCameraController>();
        var cameraData = new SerializedObject(controller);
        cameraData.FindProperty("zoomMin").floatValue = 8;
        cameraData.FindProperty("zoomMax").floatValue = 40;
        cameraData.ApplyModifiedProperties();
        int missing = scene.GetRootGameObjects().Sum(go => go.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
        var valid = (bool)typeof(BattleMapCreatorWindow).GetMethod("ValidateActiveScene", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { false });
        if (missing != 0 || !valid) throw new InvalidOperationException("Scene validation failed.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return new { saved = true, missingScripts = missing, valid, tiles = manager.Map.Tiles.Count, spawns = spawnComponents.Length, captures = captureComponents.Length, cameraControllerEnabled = controller.enabled, legacyCameraEnabled = camera.GetComponent<RedBjorn.ProtoTiles.Example.CameraController>().enabled };
    }
}
