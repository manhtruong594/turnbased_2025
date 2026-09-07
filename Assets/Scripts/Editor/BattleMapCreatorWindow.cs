using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RedBjorn.ProtoTiles;
using TurnBasedGame.Capture;
using TurnBasedGame.Core;
using TurnBasedGame.Maps;
using TurnBasedGame.Unit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class BattleMapCreatorWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/Maps";

    [SerializeField] private MapSettings sourceMap;
    [SerializeField] private string outputFolder = DefaultOutputFolder;
    [SerializeField] private string mapName = "BattleMap";
    [SerializeField] private int mapCount = 1;
    [SerializeField] private int width = 15;
    [SerializeField] private int height = 11;
    [SerializeField] private int spawnCountPerPlayer = 4;
    [SerializeField] private int capturePointCount = 3;
    [SerializeField] private int seed = 12345;
    [SerializeField, Range(0f, 0.45f)] private float obstacleDensity = 0.12f;
    [SerializeField] private int groundPresetIndex;
    [SerializeField] private int blockedPresetIndex = -1;
    [SerializeField] private Vector2 scroll;

    [MenuItem("Tools/Turn Based/Battle Map Creator")]
    public static void ShowWindow()
    {
        GetWindow<BattleMapCreatorWindow>("Battle Map Creator");
    }

    [MenuItem("Tools/Turn Based/Validate Active Map")]
    public static void ValidateActiveMapMenu()
    {
        ValidateActiveScene(true);
    }

    private void OnEnable()
    {
        minSize = new Vector2(430f, 570f);
        if (sourceMap == null)
        {
            var manager = FindInActiveScene<MapManager>().FirstOrDefault();
            sourceMap = manager != null ? manager.Map : null;
        }

        SelectDefaultPresets();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox(
            "Mỗi map gồm MapSettings và BattleMapDefinition dùng chung một battle scene. Layout vuông được đối xứng theo trục X.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        sourceMap = (MapSettings)EditorGUILayout.ObjectField("MapSettings nguồn", sourceMap, typeof(MapSettings), false);
        if (EditorGUI.EndChangeCheck())
        {
            SelectDefaultPresets();
        }

        outputFolder = EditorGUILayout.TextField("Thư mục đầu ra", outputFolder);
        mapName = EditorGUILayout.TextField("Tên map / tiền tố", mapName);
        mapCount = EditorGUILayout.IntSlider("Số map", mapCount, 1, 30);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
        width = MakeOdd(EditorGUILayout.IntSlider("Chiều rộng", width, 7, 51));
        height = MakeOdd(EditorGUILayout.IntSlider("Chiều cao", height, 7, 51));
        spawnCountPerPlayer = EditorGUILayout.IntSlider("Spawn mỗi người", spawnCountPerPlayer, 1, Mathf.Max(1, height - 2));
        capturePointCount = EditorGUILayout.IntSlider("Capture point", capturePointCount, 1, Mathf.Max(1, height - 2));
        seed = EditorGUILayout.IntField("Seed đầu tiên", seed);
        obstacleDensity = EditorGUILayout.Slider("Mật độ chướng ngại", obstacleDensity, 0f, 0.45f);

        DrawPresetFields();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button(mapCount == 1 ? "Tạo map" : $"Tạo {mapCount} map", GUILayout.Height(34f)))
            {
                CreateMaps();
            }

            if (GUILayout.Button("Kiểm tra map đang mở"))
            {
                ValidateActiveScene(true);
            }
        }

        if (sourceMap != null && GUILayout.Button("Mở Red Bjorn Map Editor"))
        {
            MapWindow.DoShow(sourceMap);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawPresetFields()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tile preset", EditorStyles.boldLabel);
        if (sourceMap == null || sourceMap.Presets == null || sourceMap.Presets.Count == 0)
        {
            EditorGUILayout.HelpBox("MapSettings nguồn chưa có TilePreset.", MessageType.Warning);
            return;
        }

        var names = sourceMap.Presets.Select((preset, index) => $"{index}: {preset.Type}").ToArray();
        groundPresetIndex = EditorGUILayout.Popup("Nền có thể đi", Mathf.Clamp(groundPresetIndex, 0, names.Length - 1), names);

        var blockedNames = new[] { "Không tạo chướng ngại" }.Concat(names).ToArray();
        blockedPresetIndex = EditorGUILayout.Popup("Chướng ngại", Mathf.Clamp(blockedPresetIndex + 1, 0, blockedNames.Length - 1), blockedNames) - 1;
    }

    private void SelectDefaultPresets()
    {
        if (sourceMap == null || sourceMap.Presets == null || sourceMap.Presets.Count == 0)
        {
            groundPresetIndex = 0;
            blockedPresetIndex = -1;
            return;
        }

        groundPresetIndex = sourceMap.Presets.FindIndex(preset => IsPresetWalkable(sourceMap, preset));
        if (groundPresetIndex < 0)
        {
            groundPresetIndex = 0;
        }

        blockedPresetIndex = sourceMap.Presets.FindIndex(preset => !IsPresetWalkable(sourceMap, preset));
    }

    private void CreateMaps()
    {
        if (!TryValidateInputs(out var error))
        {
            EditorUtility.DisplayDialog("Không thể tạo map", error, "Đóng");
            return;
        }

        var sourceMapPath = AssetDatabase.GetAssetPath(sourceMap);
        var safeBaseName = SanitizeName(mapName);
        EnsureAssetFolder(outputFolder);

        var plannedMaps = new List<(string Name, string Folder, string MapPath, string DefinitionPath)>();
        for (var index = 0; index < mapCount; index++)
        {
            var generatedName = mapCount == 1 ? safeBaseName : $"{safeBaseName}_{index + 1:00}";
            var folder = $"{outputFolder.TrimEnd('/')}/{generatedName}";
            plannedMaps.Add((generatedName, folder, $"{folder}/{generatedName}_Map.asset", $"{folder}/{generatedName}_Definition.asset"));
        }

        var existingPath = plannedMaps.SelectMany(item => new[] { item.MapPath, item.DefinitionPath })
            .FirstOrDefault(path => AssetDatabase.LoadMainAssetAtPath(path) != null);
        if (!string.IsNullOrEmpty(existingPath))
        {
            EditorUtility.DisplayDialog("Không thể tạo map", $"Asset đã tồn tại: {existingPath}\nHãy đổi tên để tránh ghi đè.", "Đóng");
            return;
        }

        try
        {
            var catalog = GetOrCreateCatalog();
            for (var index = 0; index < plannedMaps.Count; index++)
            {
                var item = plannedMaps[index];
                EnsureAssetFolder(item.Folder);
                if (!AssetDatabase.CopyAsset(sourceMapPath, item.MapPath))
                {
                    throw new InvalidOperationException($"Không thể sao chép MapSettings tới {item.MapPath}");
                }

                AssetDatabase.Refresh();
                var generatedMap = AssetDatabase.LoadAssetAtPath<MapSettings>(item.MapPath);
                generatedMap.name = $"{item.Name}_Map";
                GenerateTiles(generatedMap, seed + index);
                EditorUtility.SetDirty(generatedMap);

                var definition = CreateInstance<BattleMapDefinitionSO>();
                definition.name = $"{item.Name}_Definition";
                definition.ConfigureGenerated(item.Name, generatedMap, CreateSpawnData(), CreateCaptureData());
                if (!definition.TryValidate(out var definitionError))
                {
                    DestroyImmediate(definition);
                    throw new InvalidOperationException($"Map '{item.Name}' không hợp lệ: {definitionError}");
                }

                AssetDatabase.CreateAsset(definition, item.DefinitionPath);
                catalog.Add(definition);
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Tạo map hoàn tất", $"Đã tạo {plannedMaps.Count} map trong {outputFolder}.", "Đóng");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<BattleMapDefinitionSO>(plannedMaps[^1].DefinitionPath);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Tạo map thất bại", exception.Message, "Đóng");
        }
    }

    private bool TryValidateInputs(out string error)
    {
        if (sourceMap == null)
        {
            error = "Chưa chọn MapSettings nguồn.";
            return false;
        }

        if (sourceMap.Type != GridType.Square)
        {
            error = "Bộ sinh tự động hiện hỗ trợ GridType.Square.";
            return false;
        }

        if (sourceMap.Presets == null || sourceMap.Presets.Count == 0 || groundPresetIndex < 0 || groundPresetIndex >= sourceMap.Presets.Count)
        {
            error = "Tile preset nền không hợp lệ.";
            return false;
        }

        if (!IsPresetWalkable(sourceMap, sourceMap.Presets[groundPresetIndex]))
        {
            error = "Tile preset nền phải là tile có thể di chuyển theo MapRules.";
            return false;
        }

        if (blockedPresetIndex >= sourceMap.Presets.Count)
        {
            error = "Tile preset chướng ngại không hợp lệ.";
            return false;
        }

        if (obstacleDensity > 0f && blockedPresetIndex >= 0 && IsPresetWalkable(sourceMap, sourceMap.Presets[blockedPresetIndex]))
        {
            error = "Tile preset chướng ngại vẫn có thể di chuyển theo MapRules.";
            return false;
        }

        outputFolder = outputFolder.Replace('\\', '/').TrimEnd('/');
        if (string.IsNullOrWhiteSpace(outputFolder) ||
            !(outputFolder.Equals("Assets", StringComparison.Ordinal) || outputFolder.StartsWith("Assets/", StringComparison.Ordinal)))
        {
            error = "Thư mục đầu ra phải nằm trong Assets.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SanitizeName(mapName)))
        {
            error = "Tên map không hợp lệ.";
            return false;
        }

        error = null;
        return true;
    }

    private void GenerateTiles(MapSettings map, int mapSeed)
    {
        var groundPreset = map.Presets[groundPresetIndex];
        var blockedPreset = blockedPresetIndex >= 0 ? map.Presets[blockedPresetIndex] : null;
        var xMin = -width / 2;
        var xMax = width / 2;
        var zMin = -height / 2;
        var zMax = height / 2;
        var spawnRows = EvenlySpacedCoordinates(spawnCountPerPlayer, zMin + 1, zMax - 1);
        var captureRows = EvenlySpacedCoordinates(capturePointCount, zMin + 1, zMax - 1);
        var clearRows = new HashSet<int>(spawnRows.Concat(captureRows)) { 0 };
        var randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(mapSeed);

        var tiles = new List<TileData>(width * height);
        for (var x = xMin; x <= 0; x++)
        {
            for (var z = zMin; z <= zMax; z++)
            {
                var mustRemainOpen = clearRows.Contains(z) || x <= xMin + 1 || x >= -1;
                var useObstacle = blockedPreset != null && !mustRemainOpen && UnityEngine.Random.value < obstacleDensity;
                var preset = useObstacle ? blockedPreset : groundPreset;
                tiles.Add(CreateTile(x, z, preset.Id));

                var mirrorX = -x;
                if (mirrorX != x && mirrorX <= xMax)
                {
                    tiles.Add(CreateTile(mirrorX, z, preset.Id));
                }
            }
        }

        UnityEngine.Random.state = randomState;
        map.Tiles = tiles.OrderBy(tile => tile.TilePos.x).ThenBy(tile => tile.TilePos.z).ToList();
        MapUtils.MarkAreas(map.Tiles, map.Type, map.Presets, map.Rules);
    }

    private List<BattleSpawnPointData> CreateSpawnData()
    {
        var rows = EvenlySpacedCoordinates(spawnCountPerPlayer, -height / 2 + 1, height / 2 - 1);
        var points = new List<BattleSpawnPointData>(rows.Count * 2);
        foreach (var row in rows)
        {
            points.Add(new BattleSpawnPointData(PlayerID.Player1, new Vector3Int(-width / 2 + 1, 0, row)));
            points.Add(new BattleSpawnPointData(PlayerID.Player2, new Vector3Int(width / 2 - 1, 0, row)));
        }
        return points;
    }

    private List<Vector3Int> CreateCaptureData()
    {
        return EvenlySpacedCoordinates(capturePointCount, -height / 2 + 1, height / 2 - 1)
            .Select(row => new Vector3Int(0, 0, row))
            .ToList();
    }

    private static BattleMapCatalogSO GetOrCreateCatalog()
    {
        const string resourcesFolder = "Assets/Resources";
        const string catalogPath = resourcesFolder + "/BattleMapCatalog.asset";
        EnsureAssetFolder(resourcesFolder);
        var catalog = AssetDatabase.LoadAssetAtPath<BattleMapCatalogSO>(catalogPath);
        if (catalog != null)
        {
            return catalog;
        }

        catalog = CreateInstance<BattleMapCatalogSO>();
        AssetDatabase.CreateAsset(catalog, catalogPath);
        return catalog;
    }

    private static TileData CreateTile(int x, int z, string presetId)
    {
        return new TileData
        {
            TilePos = new Vector3Int(x, 0, z),
            Id = presetId,
            PrefabIndex = 0,
            MovableArea = 0,
            SideHeight = new float[6]
        };
    }

    private void ConfigureGeneratedScene(Scene scene, MapSettings map)
    {
        var managers = FindInScene<MapManager>(scene);
        if (managers.Count != 1)
        {
            throw new InvalidOperationException($"Scene mẫu phải có đúng 1 MapManager; tìm thấy {managers.Count}.");
        }

        var mapViews = FindInScene<MapView>(scene);
        var mapView = managers[0].MapView != null && managers[0].MapView.gameObject.scene == scene
            ? managers[0].MapView
            : mapViews.FirstOrDefault(view => view.gameObject.activeInHierarchy);
        if (mapView == null)
        {
            mapView = new GameObject("MapView").AddComponent<MapView>();
            SceneManager.MoveGameObjectToScene(mapView.gameObject, scene);
        }

        mapView.gameObject.SetActive(true);
        foreach (var duplicate in mapViews.Where(view => view != mapView))
        {
            Object.DestroyImmediate(duplicate.gameObject);
        }

        managers[0].Map = map;
        managers[0].MapView = mapView;
        EditorUtility.SetDirty(managers[0]);
        RebuildTilePrefabs(map, mapView);
        RebuildGameplayPoints(scene, map);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private void RebuildTilePrefabs(MapSettings map, MapView mapView)
    {
        for (var index = mapView.transform.childCount - 1; index >= 0; index--)
        {
            Object.DestroyImmediate(mapView.transform.GetChild(index).gameObject);
        }

        var holder = new GameObject("Tiles");
        holder.transform.SetParent(mapView.transform, false);
        foreach (var tile in map.Tiles)
        {
            var preset = map.Presets.FirstOrDefault(candidate => candidate.Id == tile.Id);
            if (preset?.Prefabs == null || preset.Prefabs.Count == 0)
            {
                continue;
            }

            var prefabIndex = Mathf.Clamp(tile.PrefabIndex, 0, preset.Prefabs.Count - 1);
            var prefab = preset.Prefabs[prefabIndex];
            if (prefab == null)
            {
                continue;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, holder.transform) as GameObject;
            if (instance == null)
            {
                continue;
            }

            instance.transform.localRotation = Quaternion.Inverse(holder.transform.rotation);
            instance.transform.position = map.ToWorld(tile.TilePos, map.Edge);
        }
    }

    private void RebuildGameplayPoints(Scene scene, MapSettings map)
    {
        foreach (var point in FindInScene<SpawnPoint>(scene))
        {
            Object.DestroyImmediate(point.gameObject);
        }

        foreach (var point in FindInScene<CapturePoint>(scene))
        {
            Object.DestroyImmediate(point.gameObject);
        }

        var root = FindInSceneTransforms(scene).FirstOrDefault(transform => transform.name == "Generated Map Points");
        if (root != null)
        {
            Object.DestroyImmediate(root.gameObject);
        }

        root = new GameObject("Generated Map Points").transform;
        SceneManager.MoveGameObjectToScene(root.gameObject, scene);
        var spawnRoot = new GameObject("Spawn Points").transform;
        spawnRoot.SetParent(root, false);
        var captureRoot = new GameObject("Capture Points").transform;
        captureRoot.SetParent(root, false);

        var zMin = -height / 2;
        var zMax = height / 2;
        var spawnRows = EvenlySpacedCoordinates(spawnCountPerPlayer, zMin + 1, zMax - 1);
        var spawnPoints = new List<SpawnPoint>(spawnCountPerPlayer * 2);
        for (var index = 0; index < spawnRows.Count; index++)
        {
            spawnPoints.Add(CreateSpawnPoint(map, new Vector3Int(-width / 2 + 1, 0, spawnRows[index]), PlayerID.Player1, index + 1, spawnRoot));
            spawnPoints.Add(CreateSpawnPoint(map, new Vector3Int(width / 2 - 1, 0, spawnRows[index]), PlayerID.Player2, index + 1, spawnRoot));
        }

        var captureRows = EvenlySpacedCoordinates(capturePointCount, zMin + 1, zMax - 1);
        var capturePoints = new List<CapturePoint>(capturePointCount);
        for (var index = 0; index < captureRows.Count; index++)
        {
            var gameObject = new GameObject($"CapturePoint_{index + 1:00}");
            gameObject.transform.SetParent(captureRoot, false);
            gameObject.transform.position = map.ToWorld(new Vector3Int(0, 0, captureRows[index]), map.Edge);
            capturePoints.Add(gameObject.AddComponent<CapturePoint>());
        }

        var unitSpawner = FindInScene<UnitSpawner>(scene).FirstOrDefault();
        if (unitSpawner != null)
        {
            SetObjectList(unitSpawner, "spawnPoints", spawnPoints.Cast<Object>().ToList());
        }

        var captureManager = FindInScene<CapturePointManager>(scene).FirstOrDefault();
        if (captureManager != null)
        {
            SetObjectList(captureManager, "_capturePoints", capturePoints.Cast<Object>().ToList());
        }
    }

    private static SpawnPoint CreateSpawnPoint(MapSettings map, Vector3Int tilePosition, PlayerID owner, int index, Transform parent)
    {
        var gameObject = new GameObject($"SpawnPoint_{owner}_{index:00}");
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.position = map.ToWorld(tilePosition, map.Edge);
        var point = gameObject.AddComponent<SpawnPoint>();
        var serializedPoint = new SerializedObject(point);
        serializedPoint.FindProperty("owner").enumValueIndex = (int)owner - 1;
        serializedPoint.FindProperty("gridPosition").vector3IntValue = tilePosition;
        serializedPoint.ApplyModifiedPropertiesWithoutUndo();
        return point;
    }

    private static void SetObjectList(Object target, string propertyName, IReadOnlyList<Object> values)
    {
        var serializedObject = new SerializedObject(target);
        var property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarning($"Không tìm thấy serialized property '{propertyName}' trên {target.GetType().Name}.", target);
            return;
        }

        property.arraySize = values.Count;
        for (var index = 0; index < values.Count; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool ValidateActiveScene(bool showDialog)
    {
        var scene = SceneManager.GetActiveScene();
        var errors = new List<string>();
        var managers = FindInScene<MapManager>(scene);
        var unitSpawners = FindInScene<UnitSpawner>(scene);
        var captureManagers = FindInScene<CapturePointManager>(scene);
        var mapViews = FindInScene<MapView>(scene).Where(view => view.gameObject.activeInHierarchy).ToList();
        if (managers.Count != 1)
        {
            errors.Add($"Cần đúng 1 MapManager, hiện có {managers.Count}.");
        }

        if (mapViews.Count != 1)
        {
            errors.Add($"Cần đúng 1 MapView đang active, hiện có {mapViews.Count}.");
        }
        if (unitSpawners.Count != 1)
        {
            errors.Add($"Cần đúng 1 UnitSpawner, hiện có {unitSpawners.Count}.");
        }
        if (captureManagers.Count != 1)
        {
            errors.Add($"Cần đúng 1 CapturePointManager, hiện có {captureManagers.Count}.");
        }

        var manager = managers.FirstOrDefault();
        if (manager == null || manager.Map == null)
        {
            errors.Add("MapManager chưa có MapSettings.");
        }
        else
        {
            ValidatePointsAndConnectivity(scene, manager.Map, errors);
        }

        if (errors.Count > 0)
        {
            Debug.LogError("[Battle Map Validation]\n- " + string.Join("\n- ", errors));
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Map chưa hợp lệ", string.Join("\n", errors), "Đóng");
            }
            return false;
        }

        Debug.Log("[Battle Map Validation] Map hợp lệ.");
        if (showDialog)
        {
            EditorUtility.DisplayDialog("Map hợp lệ", "MapSettings, MapView, spawn point, capture point và kết nối đều hợp lệ.", "Đóng");
        }
        return true;
    }

    private static void ValidatePointsAndConnectivity(Scene scene, MapSettings map, ICollection<string> errors)
    {
        if (map.Tiles == null || map.Tiles.Count == 0)
        {
            errors.Add("MapSettings không có tile.");
            return;
        }

        var entity = new MapEntity(map, null);
        var spawnPoints = FindInScene<SpawnPoint>(scene);
        var capturePoints = FindInScene<CapturePoint>(scene);
        if (!spawnPoints.Any(point => point.Owner == PlayerID.Player1))
        {
            errors.Add("Player1 không có spawn point.");
        }
        if (!spawnPoints.Any(point => point.Owner == PlayerID.Player2))
        {
            errors.Add("Player2 không có spawn point.");
        }
        if (capturePoints.Count == 0)
        {
            errors.Add("Không có capture point.");
        }

        var targetPositions = new List<Vector3Int>();
        var occupiedPositions = new HashSet<Vector3Int>();
        foreach (var point in spawnPoints)
        {
            ValidatePoint(point.name, point.transform.position, map, entity, occupiedPositions, targetPositions, errors);
        }
        foreach (var point in capturePoints)
        {
            ValidatePoint(point.name, point.transform.position, map, entity, occupiedPositions, targetPositions, errors);
        }

        if (targetPositions.Count == 0)
        {
            return;
        }

        var reachable = CollectReachable(entity, map.Type, targetPositions[0]);
        foreach (var target in targetPositions.Skip(1).Where(target => !reachable.Contains(target)))
        {
            errors.Add($"Điểm tại tile {target} không nối được với vùng spawn/capture chính.");
        }
    }

    private static void ValidatePoint(string pointName, Vector3 worldPosition, MapSettings map, MapEntity entity,
        ISet<Vector3Int> occupiedPositions, ICollection<Vector3Int> targets, ICollection<string> errors)
    {
        var tilePosition = map.ToTile(worldPosition, map.Edge);
        var tile = entity.Tile(tilePosition);
        if (tile == null || !tile.Vacant)
        {
            errors.Add($"'{pointName}' không nằm trên tile có thể di chuyển.");
            return;
        }

        if (!occupiedPositions.Add(tilePosition))
        {
            errors.Add($"Nhiều gameplay point trùng tile {tilePosition}.");
        }
        targets.Add(tilePosition);
    }

    private static HashSet<Vector3Int> CollectReachable(MapEntity entity, GridType gridType, Vector3Int start)
    {
        var visited = new HashSet<Vector3Int> { start };
        var pending = new Queue<Vector3Int>();
        pending.Enqueue(start);
        var directions = MapUtils.NeighbourDirections(gridType);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var direction in directions)
            {
                var next = current + direction;
                var tile = entity.Tile(next);
                if (tile != null && tile.Vacant && visited.Add(next))
                {
                    pending.Enqueue(next);
                }
            }
        }
        return visited;
    }

    private static bool IsPresetWalkable(MapSettings map, TilePreset preset)
    {
        if (map == null || preset == null)
        {
            return false;
        }

        var data = new TileData { Id = preset.Id, TilePos = Vector3Int.zero, SideHeight = new float[6] };
        return new TileEntity(data, preset, map.Rules).Vacant;
    }

    private static List<int> EvenlySpacedCoordinates(int count, int minimum, int maximum)
    {
        if (count <= 1)
        {
            return new List<int> { Mathf.RoundToInt((minimum + maximum) * 0.5f) };
        }

        var result = new List<int>(count);
        for (var index = 0; index < count; index++)
        {
            var coordinate = Mathf.RoundToInt(Mathf.Lerp(minimum, maximum, index / (float)(count - 1)));
            if (!result.Contains(coordinate))
            {
                result.Add(coordinate);
            }
        }
        return result;
    }

    private static int MakeOdd(int value)
    {
        return value % 2 == 0 ? value + 1 : value;
    }

    private static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        return new string(value.Trim().Where(character => !invalidCharacters.Contains(character) && character != '/').ToArray()).Trim('.');
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        var normalized = assetFolder.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(normalized))
        {
            return;
        }

        var segments = normalized.Split('/');
        var current = segments[0];
        for (var index = 1; index < segments.Length; index++)
        {
            var next = $"{current}/{segments[index]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[index]);
            }
            current = next;
        }
    }

    private static List<T> FindInActiveScene<T>() where T : Component
    {
        return FindInScene<T>(SceneManager.GetActiveScene());
    }

    private static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToList();
    }

    private static List<Transform> FindInSceneTransforms(Scene scene)
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToList();
    }
}
