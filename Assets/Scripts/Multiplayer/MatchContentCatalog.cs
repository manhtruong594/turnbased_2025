using System;
using System.Collections.Generic;
using TurnBasedGame.Multiplayer.Protocol;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEngine;

namespace TurnBasedGame.Multiplayer
{
    public sealed class MatchContentCatalog : ScriptableObject
    {
        public const string ResourceName = "Multiplayer/MatchContentCatalog";
        [Serializable]
        public sealed class Entry
        {
            public string Id;
            public ScriptableObject Content;
            public UnitController UnitPrefab;
        }
        public string ContentCatalogHash;
        public Entry[] Entries = Array.Empty<Entry>();
    }

    public sealed class MatchContentRegistry
    {
        private readonly Dictionary<string, MatchContentCatalog.Entry> entries = new(StringComparer.Ordinal);
        private readonly Dictionary<UnityEngine.Object, string> ids = new();
        public string ContentCatalogHash { get; }

        public MatchContentRegistry(MatchContentCatalog catalog)
        {
            if (catalog == null || !MatchProtocol.IsHex(catalog.ContentCatalogHash, 64) ||
                catalog.Entries == null || catalog.Entries.Length == 0)
                throw new InvalidOperationException("Multiplayer content catalog is missing or invalid. Rebuild it in Tools > Multiplayer.");
            ContentCatalogHash = catalog.ContentCatalogHash;
            foreach (var entry in catalog.Entries)
            {
                if (entry == null || !MatchProtocol.IsHex(entry.Id, 32) || entry.Content == null)
                    throw new InvalidOperationException("Content entry has a missing ID/reference.");
                if (!(entry.Content is UnitData) && !(entry.Content is SkillBase) && !(entry.Content is SpellCardData))
                    throw new InvalidOperationException($"Unsupported content: {entry.Id}");
                if (entry.Content is UnitData && (entry.UnitPrefab == null || entry.UnitPrefab.UnitData != entry.Content))
                    throw new InvalidOperationException($"Unit prefab/data mismatch: {entry.Id}");
                if (!entries.TryAdd(entry.Id, entry) || !ids.TryAdd(entry.Content, entry.Id))
                    throw new InvalidOperationException($"Duplicate content ID/reference: {entry.Id}");
                if (entry.UnitPrefab != null && !ids.TryAdd(entry.UnitPrefab, entry.Id))
                    throw new InvalidOperationException($"Duplicate unit prefab: {entry.Id}");
            }
            foreach (var entry in entries.Values)
            {
                if (!(entry.Content is UnitData data)) continue;
                foreach (var skill in data.StartingSkills)
                    if (skill == null || !ids.ContainsKey(skill))
                        throw new InvalidOperationException($"Unit {entry.Id} has a missing/unregistered starting skill.");
            }
        }

        public string GetId(UnityEngine.Object content) => content != null && ids.TryGetValue(content, out var id) ? id : null;
        public T Resolve<T>(string id) where T : ScriptableObject =>
            id != null && entries.TryGetValue(id, out var entry) ? entry.Content as T : null;
        public UnitController ResolveUnitPrefab(string id) =>
            id != null && entries.TryGetValue(id, out var entry) ? entry.UnitPrefab : null;
    }
}
