using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TurnBasedGame.Multiplayer;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Bake existing asset GUIDs and references; never write IDs into source assets or change their .meta.
public sealed class MatchContentCatalogBuilder : IPreprocessBuildWithReport
{
    public const string CatalogPath = "Assets/Resources/Multiplayer/MatchContentCatalog.asset";
    public int callbackOrder => 0;

    [InitializeOnLoadMethod]
    private static void ScheduleCatalog()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !BuildPipeline.isBuildingPlayer)
                Rebuild();
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            try { Rebuild(); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.isPlaying = false; }
        };
    }

    public void OnPreprocessBuild(BuildReport report) => Rebuild();

    [MenuItem("Tools/Multiplayer/Rebuild Content Catalog")]
    public static void Rebuild()
    {
        var entries = new List<MatchContentCatalog.Entry>();
        var sourcePaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/MyGame" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var unit = prefab != null ? prefab.GetComponent<UnitController>() : null;
            if (unit == null) continue;
            if (unit.UnitData == null) throw new BuildFailedException($"Missing UnitData: {path}");
            string dataPath = AssetDatabase.GetAssetPath(unit.UnitData);
            entries.Add(new MatchContentCatalog.Entry
            {
                Id = AssetDatabase.AssetPathToGUID(dataPath), Content = unit.UnitData, UnitPrefab = unit
            });
            sourcePaths.Add(path);
            sourcePaths.Add(dataPath);
        }
        AddData<SkillBase>(entries, sourcePaths);
        AddData<SpellCardData>(entries, sourcePaths);
        // Map/scene differences can change grid and spawn-point identity even with identical unit data.
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            sourcePaths.Add(AssetDatabase.GUIDToAssetPath(guid));

        entries.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        var manifest = new StringBuilder();
        foreach (string path in sourcePaths.OrderBy(AssetDatabase.AssetPathToGUID, StringComparer.Ordinal))
            manifest.Append(AssetDatabase.AssetPathToGUID(path)).Append(':')
                .Append(AssetDatabase.GetAssetDependencyHash(path)).Append('\n');
        using var sha = SHA256.Create();
        string hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(manifest.ToString())))
            .Replace("-", "").ToLowerInvariant();
        var candidate = ScriptableObject.CreateInstance<MatchContentCatalog>();
        try
        {
            candidate.Entries = entries.ToArray();
            candidate.ContentCatalogHash = hash;
            _ = new MatchContentRegistry(candidate); // Fail before replacing a valid catalog.
            var existing = AssetDatabase.LoadAssetAtPath<MatchContentCatalog>(CatalogPath);
            if (existing != null && existing.ContentCatalogHash == hash && existing.Entries != null &&
                existing.Entries.Length == candidate.Entries.Length &&
                existing.Entries.Zip(candidate.Entries, (a, b) => a != null && a.Id == b.Id &&
                    a.Content == b.Content && a.UnitPrefab == b.UnitPrefab).All(equal => equal)) return;
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Multiplayer")) AssetDatabase.CreateFolder("Assets/Resources", "Multiplayer");
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<MatchContentCatalog>();
                AssetDatabase.CreateAsset(existing, CatalogPath);
            }
            existing.Entries = candidate.Entries;
            existing.ContentCatalogHash = hash;
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            Debug.Log($"[MP-CATALOG] PASS entries={entries.Count} hash={hash}");
        }
        finally { UnityEngine.Object.DestroyImmediate(candidate); }
    }

    private static void AddData<T>(List<MatchContentCatalog.Entry> entries, HashSet<string> paths) where T : ScriptableObject
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/Scripts/Data" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            entries.Add(new MatchContentCatalog.Entry { Id = guid, Content = AssetDatabase.LoadAssetAtPath<T>(path) });
            paths.Add(path);
        }
    }
}
