#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using NUnit.Framework;
using TurnBasedGame.Core;
using TurnBasedGame.Multiplayer;
using TurnBasedGame.Unit;
using UnityEditor;
using UnityEngine;

public sealed class MatchRegistryTests
{
    private readonly List<UnityEngine.Object> created = new();

    [TearDown]
    public void Cleanup()
    {
        foreach (var value in created) if (value != null) UnityEngine.Object.DestroyImmediate(value);
        created.Clear();
    }

    private T Component<T>() where T : Component
    {
        var go = new GameObject(typeof(T).Name) { hideFlags = HideFlags.HideAndDontSave };
        go.SetActive(false); // Avoid gameplay Awake/UI requirements in EditMode.
        created.Add(go);
        return go.AddComponent<T>();
    }

    private SpawnPoint Point()
    {
        var point = Component<SpawnPoint>();
        var serialized = new SerializedObject(point);
        serialized.FindProperty("owner").intValue = (int)PlayerID.Player1;
        serialized.FindProperty("gridPosition").vector3IntValue = new Vector3Int(-4, 0, 8);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return point;
    }

    [Test]
    public void RuntimeIdBindsSameHostIdentityToDifferentPeerObjects()
    {
        var host = new MatchRuntimeRegistry(); var client = new MatchRuntimeRegistry();
        var first = Component<UnitController>(); var replica = Component<UnitController>();
        ulong id = host.AllocateUnit(first);
        client.BindUnit(id, replica);
        Assert.That(host.ResolveUnit(id), Is.SameAs(first));
        Assert.That(client.ResolveUnit(id), Is.SameAs(replica));
        Assert.Throws<InvalidOperationException>(() => client.BindUnit(id, Component<UnitController>()));
        host.RemoveUnit(first);
        Assert.That(host.ResolveUnit(id), Is.Null);
        Assert.That(host.AllocateUnit(Component<UnitController>()), Is.GreaterThan(id));
    }

    [Test]
    public void SpawnIdsAreIndependentOfObjectIdentityAndDuplicatesFailAtomically()
    {
        var host = new MatchRuntimeRegistry(); var client = new MatchRuntimeRegistry();
        var first = Point(); var replica = Point();
        host.RegisterSpawnPoints(new[] { first }); client.RegisterSpawnPoints(new[] { replica });
        string id = host.GetSpawnPointId(first);
        Assert.That(id, Is.EqualTo(client.GetSpawnPointId(replica)));
        Assert.That(client.ResolveSpawnPoint(id), Is.SameAs(replica));
        Assert.Throws<InvalidOperationException>(() => host.RegisterSpawnPoints(new[] { first, replica }));
        Assert.That(host.ResolveSpawnPoint(id), Is.SameAs(first));
    }

    [Test]
    public void BakedCatalogResolvesSameContentOnBothPeers()
    {
        MatchContentCatalogBuilder.Rebuild();
        var catalog = AssetDatabase.LoadAssetAtPath<MatchContentCatalog>(MatchContentCatalogBuilder.CatalogPath);
        var host = new MatchContentRegistry(catalog); var client = new MatchContentRegistry(catalog);
        foreach (var entry in catalog.Entries)
        {
            Assert.That(host.GetId(entry.Content), Is.EqualTo(entry.Id));
            Assert.That(client.Resolve<ScriptableObject>(entry.Id), Is.SameAs(entry.Content));
            if (entry.UnitPrefab != null)
                Assert.That(client.ResolveUnitPrefab(host.GetId(entry.UnitPrefab)), Is.SameAs(entry.UnitPrefab));
        }
    }

    [Test]
    public void MissingAndDuplicateContentFailBeforeMatch()
    {
        MatchContentCatalogBuilder.Rebuild();
        var source = AssetDatabase.LoadAssetAtPath<MatchContentCatalog>(MatchContentCatalogBuilder.CatalogPath);
        var catalog = ScriptableObject.CreateInstance<MatchContentCatalog>(); created.Add(catalog);
        catalog.ContentCatalogHash = source.ContentCatalogHash;
        catalog.Entries = new[] { source.Entries[0], source.Entries[0] };
        Assert.Throws<InvalidOperationException>(() => new MatchContentRegistry(catalog));
        catalog.Entries = new[] { new MatchContentCatalog.Entry { Id = "" } };
        Assert.Throws<InvalidOperationException>(() => new MatchContentRegistry(catalog));
    }
}
#endif
