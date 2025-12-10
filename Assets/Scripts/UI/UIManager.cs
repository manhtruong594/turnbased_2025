using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TurnBasedGame.UI;
using TurnBasedGame.Resources;
using TurnBasedGame.Core;

public class UIManager : MonoBehaviour
{
    [Header("Top Bar")]
    public TextMeshProUGUI matchStatusText;
    public TextMeshProUGUI timerText;

    [Header("Center Playfield (placeholder)")]
    public RectTransform playfieldArea;

    [Header("Dice & MP")]
    public DiceUI diceUI;
    public MPDisplayUI player1MPDisplay;
    public MPDisplayUI player2MPDisplay;

    [Header("Hand / Card Strip")]
    public RectTransform handContainer;
    public GameObject cardPrefab;

    [Header("Action Buttons")]
    public Button endTurnButton;
    
    [Header("Spawn Panel")]
    public SpawnPanel spawnPanel;

    [Header("Log")]
    public TextMeshProUGUI logText;

    // Demo state
    private float matchTimer = 300f; // 5 minutes
    private List<CardData> demoCards = new List<CardData>();

    void Start()
    {
        // Hook up demo button
        if (endTurnButton) endTurnButton.onClick.AddListener(OnEndTurn);
        // Demo cards
        demoCards.Add(new CardData("Spear Soldier", "Spawn a melee unit", 3));
        demoCards.Add(new CardData("Arc Bolt", "Deal 6 damage to target", 4));
        demoCards.Add(new CardData("Heal Ward", "Restore 8 HP", 5));
        demoCards.Add(new CardData("Fog Step", "Teleport unit", 2));
        PopulateHand(demoCards);
        StartCoroutine(MatchTimerCoroutine());
        
        // Subscribe to spawn events
        SubscribeToSpawnEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromSpawnEvents();
    }
    
    private void SubscribeToSpawnEvents()
    {
        
    }
    
    private void UnsubscribeFromSpawnEvents()
    {
        
    }
    
    

    void PopulateHand(List<CardData> cards)
    {
        foreach (Transform t in handContainer) Destroy(t.gameObject);
        foreach (var c in cards)
        {
            var go = Instantiate(cardPrefab, handContainer);
            var cardUI = go.GetComponent<CardUI>();
            if (cardUI != null) cardUI.Setup(c, this);
        }
    }

    /// <summary>
    /// Tiêu tốn MP của người chơi hiện tại
    /// </summary>
    public void SpendMP(int amount)
    {
        if (MPManager.Instance == null || TurnManager.Instance == null) return;

        PlayerID currentPlayer = TurnManager.Instance.CurrentPlayer;
        bool success = MPManager.Instance.SpendMP(currentPlayer, amount);
        
        if (success)
        {
            Log($"Player {(int)currentPlayer} spent {amount} MP.");
        }
        else
        {
            Log($"Player {(int)currentPlayer} doesn't have enough MP!");
        }
    }

    /// <summary>
    /// Kiểm tra người chơi hiện tại có đủ MP không
    /// </summary>
    public bool HasMP(int cost)
    {
        if (MPManager.Instance == null || TurnManager.Instance == null) return false;

        PlayerID currentPlayer = TurnManager.Instance.CurrentPlayer;
        return MPManager.Instance.HasEnoughMP(currentPlayer, cost);
    }

    IEnumerator MatchTimerCoroutine()
    {
        while (matchTimer > 0f)
        {
            matchTimer -= Time.deltaTime;
            int min = Mathf.FloorToInt(matchTimer / 60f);
            int sec = Mathf.FloorToInt(matchTimer % 60f);
            if (timerText) timerText.text = $"{min:00}:{sec:00}";
            yield return null;
        }
        OnMatchEnd();
    }

    void OnMatchEnd()
    {
        Log("Match time ended.");
    }

    void OnEndTurn()
    {
        Log("End Turn pressed.");
        // simple demo: opponent takes a turn (placeholder)
        StartCoroutine(OpponentTurnDemo());
    }

    IEnumerator OpponentTurnDemo()
    {
        Log("Opponent is thinking...");
        yield return new WaitForSeconds(1.2f);
        // Dice rolling được xử lý bởi DicePhaseManager
        yield return new WaitForSeconds(1.0f);
        Log("Opponent ended turn.");
    }

    public void Log(string s)
    {
        if (logText) logText.text = $"[{System.DateTime.Now:HH:mm:ss}] {s}\n" + logText.text;
        Debug.Log(s);
    }
}

[System.Serializable]
public class CardData
{
    public string title;
    public string description;
    public int cost;
    public CardData(string t, string d, int c) { title = t; description = d; cost = c; }
}
