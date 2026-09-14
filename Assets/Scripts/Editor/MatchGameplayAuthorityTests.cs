#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TurnBasedGame.Command;
using TurnBasedGame.Core;
using TurnBasedGame.Multiplayer;
using TurnBasedGame.Multiplayer.Protocol;
using TurnBasedGame.Resources;
using TurnBasedGame.Skills;
using TurnBasedGame.SpellCard;
using TurnBasedGame.Unit;
using UnityEditor;
using UnityEngine;

public sealed class MatchGameplayAuthorityTests
{
    private const string Match = "0123456789abcdef0123456789abcdef";
    private readonly Dictionary<FieldInfo, object> saved = new();
    private readonly List<UnityEngine.Object> created = new();
    private SpellCardData card;
    private MPManager mp;
    private SpellCardManager spells;

    private void Static(Type type, string field, object value)
    {
        var info = type.GetField(field, BindingFlags.Static | BindingFlags.NonPublic);
        if (!saved.ContainsKey(info)) saved.Add(info, info.GetValue(null));
        info.SetValue(null, value);
    }

    private static void Field(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private T Component<T>() where T : Component
    {
        var go = new GameObject(typeof(T).Name) { hideFlags = HideFlags.HideAndDontSave };
        go.SetActive(false);
        created.Add(go);
        return go.AddComponent<T>();
    }

    [SetUp]
    public void Setup()
    {
        if (EditorApplication.isPlaying) Assert.Ignore("Run these isolated fixtures in EditMode.");
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
            Assert.Ignore("Do not replace an active network session.");
        foreach (var type in new[] { typeof(TurnManager), typeof(MPManager), typeof(UnitSpawner), typeof(SpellCardManager),
            typeof(MapManager), typeof(GameMediator), typeof(TileHazardManager), typeof(TurnBasedGame.Capture.CapturePointManager) })
            Static(type, "<Instance>k__BackingField", null);
        Static(typeof(LocalMatchAuthority), "<Transport>k__BackingField", null);
        Static(typeof(LocalMatchAuthority), "CommandCommitted", null);
        Static(typeof(LocalMatchAuthority), "<Runtime>k__BackingField", new MatchRuntimeRegistry());
        Static(typeof(LocalMatchAuthority), "<MatchId>k__BackingField", Match);
        Static(typeof(LocalMatchAuthority), "random", new MatchRandom(1));
        Static(typeof(LocalMatchAuthority), "diceTurn", -1);
        Static(typeof(LocalMatchAuthority), "usedDice", 0);
        Static(typeof(LocalMatchAuthority), "<LastDiceValue>k__BackingField", 0);
        card = ScriptableObject.CreateInstance<SpellCardData>(); created.Add(card);
        card.spellEffect = new HealEffect();
        var catalog = ScriptableObject.CreateInstance<MatchContentCatalog>(); created.Add(catalog);
        catalog.ContentCatalogHash = new string('a', 64);
        catalog.Entries = new[] { new MatchContentCatalog.Entry { Id = new string('b', 32), Content = card } };
        Static(typeof(LocalMatchAuthority), "<Content>k__BackingField", new MatchContentRegistry(catalog));
        Static(typeof(LocalMatchAuthority), "gate", new MatchCommandGate(Match, LocalMatchAuthority.Compatibility));
        var turn = Component<TurnManager>();
        Static(typeof(TurnManager), "<Instance>k__BackingField", turn);
        Field(turn, "currentPlayer", PlayerID.Player1); Field(turn, "turnCount", 3); Field(turn, "currentState", TurnState.Player1Turn);
        mp = Component<MPManager>();
        Static(typeof(MPManager), "<Instance>k__BackingField", mp);
        mp.Initialize(Component<GameMediator>());
        Field(mp, "player1MP", 4); Field(mp, "player2MP", 5);
        spells = Component<SpellCardManager>();
        Static(typeof(SpellCardManager), "<Instance>k__BackingField", spells);
        spells.InitializeHand(PlayerID.Player1, new[] { card });
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (var item in created) if (item != null) UnityEngine.Object.DestroyImmediate(item);
        created.Clear();
        foreach (var pair in saved) pair.Key.SetValue(null, pair.Value);
        saved.Clear();
    }

    private MatchCommandDto Command(MatchCommandKind kind) => new MatchCommandDto
    {
        MatchId = Match, Compatibility = LocalMatchAuthority.Compatibility, Actor = PlayerId.Player1,
        CommandId = 1, ClientSequence = 1, ExpectedTurn = 3, Kind = kind
    };

    private void RejectWithoutMutation(MatchCommandDto command, CommandReason reason)
    {
        var rng = LocalMatchAuthority.Random.Capture();
        var result = LocalMatchAuthority.Submit(command, PlayerId.Player1);
        Assert.That(result.Reason, Is.EqualTo(reason));
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Acknowledgement.StateChanges, Is.Empty);
        Assert.That(mp.GetCurrentMP(PlayerID.Player1), Is.EqualTo(4));
        Assert.That(spells.GetHand(PlayerID.Player1).Count, Is.EqualTo(1));
        Assert.That(LocalMatchAuthority.Random.Capture().State, Is.EqualTo(rng.State));
        Assert.That(LocalMatchAuthority.Submit(command, PlayerId.Player1).Reason, Is.EqualTo(reason));
    }

    [Test]
    public void AttackCannotControlEnemyUnit()
    {
        var unit = Component<UnitController>();
        var stats = (UnitRuntimeStats)typeof(UnitController).GetField("runtimeStats", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(unit);
        stats.SetOwner(PlayerID.Player2);
        var command = Command(MatchCommandKind.NormalAttack);
        command.UnitRuntimeId = LocalMatchAuthority.Runtime.AllocateUnit(unit);
        command.SkillContentId = new string('c', 32);
        RejectWithoutMutation(command, CommandReason.InvalidOwner);
    }

    [Test]
    public void WrongTurnDoesNotConsumeSpellOrMana()
    {
        var command = Command(MatchCommandKind.CastSpell);
        command.CardInstanceId = spells.GetCardInstanceId(PlayerID.Player1, card);
        command.ExpectedTurn = 2;
        RejectWithoutMutation(command, CommandReason.WrongTurn);
    }

    [Test]
    public void CardInstanceFromOpponentHandIsRejected()
    {
        spells.InitializeHand(PlayerID.Player2, new[] { card });
        var command = Command(MatchCommandKind.CastSpell);
        command.CardInstanceId = spells.GetCardInstanceId(PlayerID.Player2, card);
        RejectWithoutMutation(command, CommandReason.InvalidCard);
    }

    [Test]
    public void InvalidSpellTileDoesNotConsumeCardOrMana()
    {
        var command = Command(MatchCommandKind.CastSpell);
        command.CardInstanceId = spells.GetCardInstanceId(PlayerID.Player1, card);
        RejectWithoutMutation(command, CommandReason.InvalidTarget);
    }

    [Test]
    public void DiceCommitsManaOnceAndRejectsSameDieWithNewCommandId()
    {
        var command = Command(MatchCommandKind.RollDice); command.DiceIndex = 1;
        var first = LocalMatchAuthority.Submit(command, PlayerId.Player1);
        Assert.That(first.Succeeded, Is.True);
        int mana = mp.GetCurrentMP(PlayerID.Player1);
        Assert.That(mana, Is.EqualTo(4 + first.Acknowledgement.DiceValue));
        Assert.That(LocalMatchAuthority.Submit(command, PlayerId.Player1).ServerSequence, Is.EqualTo(first.ServerSequence));
        var rng = LocalMatchAuthority.Random.Capture();
        command.CommandId = 2; command.ClientSequence = 2;
        Assert.That(LocalMatchAuthority.Submit(command, PlayerId.Player1).Reason, Is.EqualTo(CommandReason.InvalidState));
        Assert.That(mp.GetCurrentMP(PlayerID.Player1), Is.EqualTo(mana));
        Assert.That(LocalMatchAuthority.Random.Capture().State, Is.EqualTo(rng.State));
    }
}
#endif
