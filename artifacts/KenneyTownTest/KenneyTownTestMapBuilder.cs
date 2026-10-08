using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedBjorn.ProtoTiles;
using TurnBasedGame.Capture;
using TurnBasedGame.Core;
using TurnBasedGame.Maps;
using TurnBasedGame.Unit;
using UCP.Bridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class KenneyTownTestMapBuilder : IUCPScript
{
    private const string Folder = "Assets/MyGame/Maps/KenneyTownTest";
    private const string Models = "Assets/MyGame/Models/kenney_fantasy-town-kit_2.0/Models/FBX format/";
    private const string RootName = "Kenney Town Test Map";
    private readonly HashSet<Vector3Int> blocked = new();
    public string Name => "kenney-town-build";
    public string Description => "Create the Kenney town test map in SampleScene, or validate with params containing validate.";

    public object Execute(string paramsJson)
    {
        if (paramsJson.Contains("validate")) return Validate();
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open SampleScene in Edit Mode first.");
        if (GameObject.Find(RootName) != null || AssetDatabase.IsValidFolder(Folder))
            throw new InvalidOperationException("KenneyTownTest already exists; refusing to overwrite.");
        var source = AssetDatabase.LoadAssetAtPath<MapSettings>("Assets/MyGame/Maps/BattleMap_01/BattleMap_01_Map.asset");
        if (source == null || source.Type != GridType.Square) throw new InvalidOperationException("Square source map missing.");
        Directory.CreateDirectory("Artifacts/KenneyTownTest");
        // Preserve the user's saved scene before adding the test map.
        File.Copy(scene.path, "Artifacts/KenneyTownTest/SampleScene.before.unity", false);
        AssetDatabase.CreateFolder("Assets/MyGame/Maps", "KenneyTownTest");
        AssetDatabase.CreateFolder(Folder, "Materials");
        AssetDatabase.CreateFolder(Folder, "Prefabs");
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Kenney town test map");
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create town");
        var view = Child("MapView", root.transform).gameObject.AddComponent<MapView>();
        var environment = Child("Town Environment", view.transform);
        var grass = Material("Grass", new Color(0.35f, 0.48f, 0.24f));
        var earth = Material("Earth", new Color(0.25f, 0.20f, 0.15f));
        var stone = Material("Sandstone", new Color(0.63f, 0.58f, 0.44f));
        var blue = Material("Player Blue", new Color(0.14f, 0.51f, 0.83f));
        var red = Material("Player Red", new Color(0.84f, 0.27f, 0.20f));
        var gold = Material("Capture Gold", new Color(0.95f, 0.72f, 0.23f));
        Box("Earth foundation", environment, new Vector3(0, -0.7f, 0), new Vector3(42.6f, 1f, 34.6f), earth);
        var grassPrefab = TilePrefab("Grass", grass, false);
        var roadPrefab = TilePrefab("Paving", grass, true);
        var map = Object.Instantiate(source);
        map.name = "KenneyTownTest_Map";
        map.Edge = 2;
        map.Presets = new List<TilePreset>();
        map.Tiles = new List<TileData>();
        var ground = Preset("Grass", grassPrefab, new Color(0.35f, 0.48f, 0.24f), source.Presets[0].Tags);
        var paving = Preset("Paving", roadPrefab, new Color(0.65f, 0.62f, 0.52f), source.Presets[0].Tags);
        var obstacle = Preset("Blocked garden / building", grassPrefab, new Color(0.6f, 0.25f, 0.2f), source.Presets[1].Tags);
        var blockedPaving = Preset("Blocked plaza / market", roadPrefab, new Color(0.65f, 0.38f, 0.2f), source.Presets[1].Tags);
        map.Presets.AddRange(new[] { ground, paving, obstacle, blockedPaving });

        var buildings = Child("Houses", environment);
        foreach (int x in new[] { -7, -4, 3, 6 })
        foreach (int z in new[] { -6, 5 })
        {
            House(buildings, x, z, (x == -4 || x == 3) ? 2 : 1);
            for (int dx = 0; dx < 2; dx++) for (int dz = 0; dz < 2; dz++) Block(x + dx, z + dz);
        }
        var market = Child("Market", environment);
        foreach (int x in new[] { -5, 5 })
        foreach (int z in new[] { -2, 2 })
        {
            Model(x < 0 ? "stall-green" : "stall-red", market, new Vector3(x * 2, 0, z * 2), 1.7f, z < 0 ? 180 : 0);
            Block(x, z);
        }
        var fountain = Child("Fountain garden", environment);
        Model("fountain-round", fountain, new Vector3(0, 0, 6), 1.8f);
        Model("fountain-center", fountain, new Vector3(0, 0, 6), 1.8f);
        for (int x = -1; x <= 1; x++) for (int z = 2; z <= 4; z++) Block(x, z);
        var nature = Child("Trees and rocks", environment);
        foreach (int x in new[] { -10, 10 })
        foreach (int z in new[] { -7, -3, 3, 7 })
        {
            Model(z % 3 == 0 ? "tree-high-round" : "tree", nature, new Vector3(x * 2, 0, z * 2), 1.25f, z * 23);
            Block(x, z);
        }
        foreach (int x in new[] { -7, -3, 3, 7 })
        {
            Model("tree", nature, new Vector3(x * 2, 0, -16), 1.15f, x * 17);
            Block(x, -8);
            Model("rock-large", nature, new Vector3(x * 2, 0, 16), 1.3f, x * 32);
            Block(x, 8);
        }
        foreach (int x in new[] { -7, 7 })
        {
            Model("cart", market, new Vector3(x * 2, 0, 0), 1.3f, 90);
            Block(x, 0);
        }
        var border = Child("Boundary", environment);
        for (int x = -10; x <= 10; x++)
        {
            Box("North curb", border, new Vector3(x * 2, -0.05f, 17.15f), new Vector3(1.94f, 0.3f, 0.3f), stone);
            Box("South curb", border, new Vector3(x * 2, -0.05f, -17.15f), new Vector3(1.94f, 0.3f, 0.3f), stone);
        }
        for (int z = -8; z <= 8; z++)
        {
            Box("West curb", border, new Vector3(-21.15f, -0.05f, z * 2), new Vector3(0.3f, 0.3f, 1.94f), stone);
            Box("East curb", border, new Vector3(21.15f, -0.05f, z * 2), new Vector3(0.3f, 0.3f, 1.94f), stone);
        }
        var tiles = Child("Tiles", view.transform);
        for (int x = -10; x <= 10; x++) for (int z = -8; z <= 8; z++)
        {
            var pos = new Vector3Int(x, 0, z);
            bool road = Math.Abs(x) <= 1 || Math.Abs(x) == 9 || Math.Abs(z) <= 3 || Math.Abs(z) == 7;
            var preset = blocked.Contains(pos) ? (road ? blockedPaving : obstacle) : (road ? paving : ground);
            map.Tiles.Add(new TileData { TilePos = pos, Id = preset.Id, PrefabIndex = 0, SideHeight = new float[6] });
            var tile = (GameObject)PrefabUtility.InstantiatePrefab(preset.PrefabCurrent, tiles);
            tile.name = $"Tile_{x}_{z}_{preset.Type}";
            tile.transform.localPosition = map.ToWorld(pos, map.Edge);
        }
        MapUtils.MarkAreas(map.Tiles, map.Type, map.Presets, map.Rules);
        AssetDatabase.CreateAsset(map, Folder + "/KenneyTownTest_Map.asset");
        var spawnData = new List<BattleSpawnPointData>();
        var spawnPoints = new List<SpawnPoint>();
        var points = Child("Gameplay Points", root.transform);
        foreach (int x in new[] { -9, 9 }) foreach (int z in new[] { -6, 6 })
        {
            var pos = new Vector3Int(x, 0, z);
            var owner = x < 0 ? PlayerID.Player1 : PlayerID.Player2;
            spawnData.Add(new BattleSpawnPointData(owner, pos));
            var point = Child($"Spawn_{owner}_{z}", points);
            point.position = map.ToWorld(pos, map.Edge);
            var spawn = point.gameObject.AddComponent<SpawnPoint>();
            spawn.Initialize(owner, pos);
            spawnPoints.Add(spawn);
            Marker("Spawn pad", environment, point.position, x < 0 ? blue : red, 1.55f);
        }
        var captures = new List<Vector3Int> { new(0, 0, -6), new(0, 0, 0), new(0, 0, 6) };
        var capturePoints = new List<CapturePoint>();
        foreach (var pos in captures)
        {
            var point = Child($"Capture_{pos.z}", points);
            point.position = map.ToWorld(pos, map.Edge);
            capturePoints.Add(point.gameObject.AddComponent<CapturePoint>());
            Marker("Capture pad", environment, point.position, gold, 1.5f);
        }
        var envPrefab = PrefabUtility.SaveAsPrefabAsset(environment.gameObject, Folder + "/Prefabs/KenneyTown_Environment.prefab");
        var definition = ScriptableObject.CreateInstance<BattleMapDefinitionSO>();
        definition.ConfigureGenerated("Kenney — Town Square", map, spawnData, captures);
        var defSO = new SerializedObject(definition);
        defSO.FindProperty("_environmentPrefab").objectReferenceValue = envPrefab;
        defSO.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(definition, Folder + "/KenneyTownTest_Definition.asset");
        var manager = root.AddComponent<MapManager>();
        manager.Map = map;
        manager.MapView = view;
        var managerSO = new SerializedObject(manager);
        managerSO.FindProperty("_editorTestMap").objectReferenceValue = definition;
        managerSO.ApplyModifiedPropertiesWithoutUndo();
        var spawner = root.AddComponent<UnitSpawner>();
        SetList(spawner, "spawnPoints", spawnPoints.Cast<Object>().ToArray());
        var captureManager = root.AddComponent<CapturePointManager>();
        SetList(captureManager, "_capturePoints", capturePoints.Cast<Object>().ToArray());
        var camera = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Camera>()).First(c => c.CompareTag("MainCamera"));
        Undo.RecordObjects(new Object[] { camera, camera.transform }, "Frame town camera");
        camera.transform.position = new Vector3(33, 42, -43);
        camera.transform.LookAt(new Vector3(0, 0, 0));
        camera.orthographic = true;
        camera.orthographicSize = 27;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 250;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.16f, 0.22f, 0.26f);
        var light = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Light>()).FirstOrDefault(l => l.type == LightType.Directional);
        if (light != null)
        {
            Undo.RecordObjects(new Object[] { light, light.transform }, "Light town");
            light.transform.rotation = Quaternion.Euler(48, -30, 0);
            light.intensity = 1.6f;
            light.color = new Color(1, 0.94f, 0.83f);
        }
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(new Vector3(0, 0, 0), Quaternion.Euler(50, -38, 0), 37);
        Selection.activeGameObject = root;
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(undo);
        return Validate();
    }

    private void Block(int x, int z) => blocked.Add(new Vector3Int(x, 0, z));

    private static Transform Child(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Material Material(string name, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
        mat.SetFloat("_Smoothness", 0.08f);
        AssetDatabase.CreateAsset(mat, Folder + "/Materials/" + name + ".mat");
        return mat;
    }

    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static GameObject Model(string name, Transform parent, Vector3 position, float scale = 1, float yaw = 0)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + name + ".fbx");
        if (prefab == null) throw new InvalidOperationException("Missing model: " + name);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    private static GameObject TilePrefab(string name, Material grass, bool road)
    {
        var root = new GameObject(name);
        Box("Ground", root.transform, new Vector3(0, road ? -0.19f : -0.14f, 0), new Vector3(2, 0.28f, 2), grass);
        if (road) Model("road", root.transform, new Vector3(0, -0.05f, 0), 2);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Prefabs/" + name + ".prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static TilePreset Preset(string name, GameObject prefab, Color color, List<TileTag> tags)
    {
        return new TilePreset { Id = GUID.Generate().ToString(), Type = name, MapColor = color,
            Prefabs = new List<GameObject> { prefab }, Tags = new List<TileTag>(tags), GridOffset = 0 };
    }

    private static void House(Transform parent, int x, int z, int floors)
    {
        var house = Child($"House_{x}_{z}", parent);
        house.localPosition = new Vector3(x * 2 + 1, 0, z * 2 + 1);
        // Kenney walls sit on the negative X boundary of a unit module.
        for (int floor = 0; floor < floors; floor++) for (int side = 0; side < 4; side++)
            Model(floor == 0 && side == (z < 0 ? 1 : 3) ? "wall-door" : "wall-window-shutters",
                house, new Vector3(0, floor * 3, 0), 1).transform.localScale = new Vector3(4, 3, 4);
        // Rotate each wall after creation; all share the same central pivot.
        for (int i = 0; i < house.childCount; i++) house.GetChild(i).localRotation = Quaternion.Euler(0, (i % 4) * 90, 0);
        var roof = Model("roof-high-point", house, new Vector3(0, floors * 3, 0));
        roof.transform.localScale = new Vector3(4, 1.8f, 4);
        Model("chimney", house, new Vector3(0.9f, floors * 3 + 0.8f, 0.8f), 1.25f);
        Model("lantern", house, new Vector3(z < 0 ? -1.8f : 1.8f, 0, z < 0 ? 2.1f : -2.1f), 1.2f);
    }

    private static void Marker(string name, Transform parent, Vector3 position, Material material, float diameter)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position + Vector3.up * 0.025f;
        go.transform.localScale = new Vector3(diameter, 0.025f, diameter);
        go.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    private static void SetList(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static object Validate()
    {
        var definition = AssetDatabase.LoadAssetAtPath<BattleMapDefinitionSO>(Folder + "/KenneyTownTest_Definition.asset");
        if (definition == null || !definition.TryValidate(out _)) throw new InvalidOperationException("Definition invalid.");
        var map = definition.MapSettings;
        var entity = new MapEntity(map, null);
        var walkable = new HashSet<Vector3Int>(map.Tiles.Where(t => entity.Tile(t.TilePos).Vacant).Select(t => t.TilePos));
        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(definition.SpawnPoints[0].GridPosition);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if (!walkable.Contains(p) || !visited.Add(p)) continue;
            foreach (var d in entity.NeighboursDirection) queue.Enqueue(p + d);
        }
        var points = definition.SpawnPoints.Select(p => p.GridPosition).Concat(definition.CapturePoints).ToArray();
        if (visited.Count != walkable.Count || points.Any(p => !visited.Contains(p)) || points.Distinct().Count() != points.Length)
            throw new InvalidOperationException("Disconnected or invalid gameplay points.");
        var root = GameObject.Find(RootName);
        int missing = root.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        int badMaterials = root.GetComponentsInChildren<Renderer>(true).Count(r => r.sharedMaterials.Any(m => m == null || m.shader == null || !m.shader.isSupported));
        if (missing != 0 || badMaterials != 0) throw new InvalidOperationException("Missing script/material/shader.");
        return new { tiles = map.Tiles.Count, walkable = walkable.Count, blocked = map.Tiles.Count - walkable.Count,
            connected = visited.Count, spawnPoints = definition.SpawnPoints.Count, capturePoints = definition.CapturePoints.Count,
            missingScripts = missing, badMaterials, scene = SceneManager.GetActiveScene().path, saved = !SceneManager.GetActiveScene().isDirty };
    }
}
