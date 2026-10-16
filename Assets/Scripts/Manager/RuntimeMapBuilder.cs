using System.Collections.Generic;
using System.Linq;
using RedBjorn.ProtoTiles;
using TurnBasedGame.Capture;
using TurnBasedGame.Core;
using TurnBasedGame.Maps;
using TurnBasedGame.Unit;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class RuntimeMapBuilder
{
    public static bool TryBuild(BattleMapDefinitionSO definition, MapEntity map, MapView mapView, out string error)
    {
        if (definition == null)
        {
            error = "BattleMapDefinition chưa được chọn.";
            return false;
        }

        if (!definition.TryValidate(out error))
        {
            return false;
        }

        if (!ValidateGameplayPoints(definition, map, out error))
        {
            return false;
        }

        DisableSceneObjects<SpawnPoint>();
        DisableSceneObjects<CapturePoint>();

        var root = new GameObject("Runtime Map").transform;
        var tilesHolder = BuildTileVisuals(definition.MapSettings, mapView.transform);
        BuildGameplayPoints(definition, map, root);

        if (definition.EnvironmentPrefab != null)
        {
            var envInstance = Object.Instantiate(definition.EnvironmentPrefab, root);
            StaticBatchingUtility.Combine(envInstance);
        }

        OptimizeGridVisuals(mapView, definition);

        if (tilesHolder != null)
        {
            StaticBatchingUtility.Combine(tilesHolder.gameObject);
        }

        error = null;
        return true;
    }

    private static void OptimizeGridVisuals(MapView mapView, BattleMapDefinitionSO definition)
    {
        if (mapView == null)
        {
            return;
        }

        Transform gridTransform = null;
        for (int i = mapView.transform.childCount - 1; i >= 0; i--)
        {
            var child = mapView.transform.GetChild(i);
            if (child.name == "Grid" && child.gameObject.activeSelf)
            {
                gridTransform = child;
                break;
            }
        }

        if (gridTransform == null)
        {
            return;
        }

        var meshFilters = gridTransform.GetComponentsInChildren<MeshFilter>(false);
        if (meshFilters.Length == 0)
        {
            return;
        }

        var combineInstances = new List<CombineInstance>(meshFilters.Length);
        Material borderMaterial = null;

        for (int i = 0; i < meshFilters.Length; i++)
        {
            var mf = meshFilters[i];
            if (mf == null || mf.sharedMesh == null || mf.transform == gridTransform)
            {
                continue;
            }

            if (borderMaterial == null)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null)
                {
                    borderMaterial = mr.sharedMaterial;
                }
            }

            combineInstances.Add(new CombineInstance
            {
                mesh = mf.sharedMesh,
                transform = gridTransform.worldToLocalMatrix * mf.transform.localToWorldMatrix
            });
        }

        if (combineInstances.Count == 0)
        {
            return;
        }

        var combinedMesh = new Mesh
        {
            name = "Combined_Grid_Borders",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true, false);

        for (int i = gridTransform.childCount - 1; i >= 0; i--)
        {
            var child = gridTransform.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                child.SetActive(false);
                Object.Destroy(child);
            }
            else
            {
                Object.DestroyImmediate(child);
            }
        }

        var gridMf = gridTransform.GetComponent<MeshFilter>();
        if (gridMf == null)
        {
            gridMf = gridTransform.gameObject.AddComponent<MeshFilter>();
        }
        gridMf.sharedMesh = combinedMesh;

        var gridMr = gridTransform.GetComponent<MeshRenderer>();
        if (gridMr == null)
        {
            gridMr = gridTransform.gameObject.AddComponent<MeshRenderer>();
        }
        gridMr.sharedMaterial = borderMaterial != null ? borderMaterial : definition?.MapSettings?.CellMaterial;
    }

    private static Transform BuildTileVisuals(MapSettings settings, Transform mapView)
    {
        var holder = new GameObject("Tiles").transform;
        holder.SetParent(mapView, false);

        foreach (var tile in settings.Tiles)
        {
            var preset = settings.Presets.FirstOrDefault(candidate => candidate.Id == tile.Id);
            if (preset?.Prefabs == null || preset.Prefabs.Count == 0)
            {
                continue;
            }

            var prefab = preset.Prefabs[Mathf.Clamp(tile.PrefabIndex, 0, preset.Prefabs.Count - 1)];
            if (prefab == null)
            {
                continue;
            }

            var instance = Object.Instantiate(prefab, holder);
            instance.transform.position = settings.ToWorld(tile.TilePos, settings.Edge);
            instance.transform.localRotation = Quaternion.Inverse(holder.rotation);
        }

        return holder;
    }

    private static void BuildGameplayPoints(BattleMapDefinitionSO definition, MapEntity map, Transform root)
    {
        var spawnRoot = new GameObject("Spawn Points").transform;
        spawnRoot.SetParent(root, false);
        for (var index = 0; index < definition.SpawnPoints.Count; index++)
        {
            var data = definition.SpawnPoints[index];
            var gameObject = new GameObject($"SpawnPoint_{data.Owner}_{index + 1:00}");
            gameObject.transform.SetParent(spawnRoot, false);
            gameObject.transform.position = map.WorldPosition(data.GridPosition);
            gameObject.AddComponent<SpawnPoint>().Initialize(data.Owner, data.GridPosition);
        }

        var captureRoot = new GameObject("Capture Points").transform;
        captureRoot.SetParent(root, false);
        for (var index = 0; index < definition.CapturePoints.Count; index++)
        {
            var position = definition.CapturePoints[index];
            var gameObject = new GameObject($"CapturePoint_{index + 1:00}");
            gameObject.transform.SetParent(captureRoot, false);
            gameObject.transform.position = map.WorldPosition(position);
            gameObject.AddComponent<CapturePoint>().Initialize(position);
        }
    }

    private static bool ValidateGameplayPoints(BattleMapDefinitionSO definition, MapEntity map, out string error)
    {
        var occupied = new HashSet<Vector3Int>();
        foreach (var point in definition.SpawnPoints)
        {
            if (!IsAvailable(map, point.GridPosition) || !occupied.Add(point.GridPosition))
            {
                error = $"Spawn point không hợp lệ tại tile {point.GridPosition}.";
                return false;
            }
        }

        foreach (var point in definition.CapturePoints)
        {
            if (!IsAvailable(map, point) || !occupied.Add(point))
            {
                error = $"Capture point không hợp lệ tại tile {point}.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool IsAvailable(MapEntity map, Vector3Int position)
    {
        var tile = map.Tile(position);
        return tile != null && tile.Vacant;
    }

    private static void DisableSceneObjects<T>() where T : Component
    {
        var activeScene = SceneManager.GetActiveScene();
        var objects = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in objects)
        {
            if (item.gameObject.scene != activeScene)
            {
                continue;
            }

            item.gameObject.SetActive(false);
            Object.Destroy(item.gameObject);
        }
    }
}
