using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility: MenuItem "Tools/Build Card Battle Arena UI"
/// Builds the full Card Battle Arena main-menu UI on the active scene.
/// </summary>
public static class CardBattleArenaUIBuilder
{
    // ── Colour palette ──────────────────────────────────────────────────────
    static readonly Color C_Header      = new Color(0.33f, 0.43f, 0.48f, 1f);   // dark slate
    static readonly Color C_Background  = new Color(0.88f, 0.91f, 0.93f, 1f);   // light gray
    static readonly Color C_ProgressBar = new Color(0.55f, 0.78f, 0.94f, 1f);   // sky blue
    static readonly Color C_Panel       = new Color(0.96f, 0.98f, 1.00f, 1f);   // near-white
    static readonly Color C_PanelBorder = new Color(0.64f, 0.82f, 0.94f, 1f);   // light blue
    static readonly Color C_Button      = new Color(0.40f, 0.69f, 0.87f, 1f);   // button blue
    static readonly Color C_ButtonPress = new Color(0.28f, 0.55f, 0.73f, 1f);   // darker blue
    static readonly Color C_BottomNav   = new Color(0.80f, 0.88f, 0.92f, 1f);   // light blue-gray
    static readonly Color C_Gray        = new Color(0.72f, 0.76f, 0.79f, 1f);   // gray
    static readonly Color C_Text        = new Color(0.18f, 0.20f, 0.22f, 1f);   // near-black
    static readonly Color C_White       = Color.white;

    [MenuItem("Tools/Build Card Battle Arena UI")]
    public static void BuildUI()
    {
        // ── EventSystem ─────────────────────────────────────────────────────
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // ── Canvas ──────────────────────────────────────────────────────────
        var canvasGO = new GameObject("MainMenuCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create MainMenuCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1366, 768);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Full-screen background ──────────────────────────────────────────
        var bg = MakeImage(canvasGO, "Background", C_Background);
        Stretch(bg);

        // ── Header bar ─────────────────────────────────────────────────────
        var header = MakeImage(canvasGO, "Header", C_Header);
        AnchorTop(header, 0, 65f);
        var titleTMP = MakeText(header, "TitleText", "CARD BATTLE ARENA", 28, C_White, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(titleTMP.gameObject);

        // ── Progress bar ────────────────────────────────────────────────────
        var progressBar = MakeImage(canvasGO, "ProgressBar", C_ProgressBar);
        var pbRT = progressBar.GetComponent<RectTransform>();
        pbRT.anchorMin        = new Vector2(0, 1);
        pbRT.anchorMax        = new Vector2(1, 1);
        pbRT.pivot            = new Vector2(0.5f, 1f);
        pbRT.anchoredPosition = new Vector2(0, -65f);
        pbRT.sizeDelta        = new Vector2(0, 12f);

        // ── Content area (between header and bottom nav) ────────────────────
        var contentGO = new GameObject("ContentArea");
        contentGO.transform.SetParent(canvasGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(8f,  68f);
        contentRT.offsetMax = new Vector2(-8f, -83f);

        BuildLeftPanel(contentGO);
        BuildCenterPanel(contentGO);
        BuildRightPanel(contentGO);

        // ── Bottom navigation ────────────────────────────────────────────────
        BuildBottomNav(canvasGO);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;
        Debug.Log("[CardBattleArenaUI] ✅ UI built on scene: " + SceneManager.GetActiveScene().name);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  LEFT PANEL
    // ════════════════════════════════════════════════════════════════════════
    static void BuildLeftPanel(GameObject parent)
    {
        var panel = MakeImage(parent, "LeftPanel", C_Panel);
        AddOutline(panel, C_PanelBorder);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 0);
        rt.anchorMax        = new Vector2(0, 1);
        rt.pivot            = new Vector2(0, 0.5f);
        rt.sizeDelta        = new Vector2(212f, 0);
        rt.anchoredPosition = Vector2.zero;

        // Avatar box
        var avatar = MakeImage(panel, "AvatarFrame", C_Gray);
        AddOutline(avatar, C_PanelBorder, 2f);
        var aRT = avatar.GetComponent<RectTransform>();
        aRT.anchorMin        = new Vector2(0.5f, 1f);
        aRT.anchorMax        = new Vector2(0.5f, 1f);
        aRT.pivot            = new Vector2(0.5f, 1f);
        aRT.sizeDelta        = new Vector2(78f, 78f);
        aRT.anchoredPosition = new Vector2(0, -18f);

        // avatar inner icon placeholder
        var innerIcon = MakeImage(avatar, "AvatarIcon", new Color(0.55f, 0.58f, 0.61f, 1f));
        StretchPad(innerIcon, 12f);

        // Player Level
        var lvlTMP = MakeText(panel, "PlayerLevelText", "PLAYER LEVEL: 15", 13, C_Text, FontStyles.Bold, TextAlignmentOptions.Center);
        PinTop(lvlTMP.gameObject, -110f, 26f);
        StretchH(lvlTMP.gameObject, 8f);

        // Coins row
        var coinsRow = new GameObject("CoinsRow");
        coinsRow.transform.SetParent(panel.transform, false);
        var crRT = coinsRow.AddComponent<RectTransform>();
        crRT.anchorMin        = new Vector2(0, 1f);
        crRT.anchorMax        = new Vector2(1f, 1f);
        crRT.pivot            = new Vector2(0.5f, 1f);
        crRT.offsetMin        = new Vector2(28f, 0);
        crRT.offsetMax        = new Vector2(-8f, 0);
        crRT.sizeDelta        = new Vector2(crRT.sizeDelta.x, 22f);
        crRT.anchoredPosition = new Vector2(crRT.anchoredPosition.x, -142f);

        var coinCircle = MakeImage(coinsRow, "CoinIcon", C_Gray);
        var ccRT = coinCircle.GetComponent<RectTransform>();
        ccRT.anchorMin        = new Vector2(0f, 0.5f);
        ccRT.anchorMax        = new Vector2(0f, 0.5f);
        ccRT.pivot            = new Vector2(0f, 0.5f);
        ccRT.sizeDelta        = new Vector2(16f, 16f);
        ccRT.anchoredPosition = Vector2.zero;

        var coinsTMP = MakeText(coinsRow, "CoinsText", "COINS: 2250", 13, C_Text, FontStyles.Normal, TextAlignmentOptions.Left);
        var cRT = coinsTMP.GetComponent<RectTransform>();
        cRT.anchorMin = Vector2.zero;
        cRT.anchorMax = Vector2.one;
        cRT.offsetMin = new Vector2(20f, 0);
        cRT.offsetMax = Vector2.zero;

        // Settings & Tutorial icon-buttons (side by side)
        var settingsBtn = MakeIconButton(panel, "SettingsButton", "SETTINGS");
        var sbRT = settingsBtn.GetComponent<RectTransform>();
        sbRT.anchorMin        = new Vector2(0.05f, 0f);
        sbRT.anchorMax        = new Vector2(0.49f, 0f);
        sbRT.pivot            = new Vector2(0f, 0f);
        sbRT.sizeDelta        = new Vector2(0, 88f);
        sbRT.anchoredPosition = new Vector2(0, 28f);

        var tutorialBtn = MakeIconButton(panel, "TutorialButton", "TUTORIAL");
        var tbRT = tutorialBtn.GetComponent<RectTransform>();
        tbRT.anchorMin        = new Vector2(0.51f, 0f);
        tbRT.anchorMax        = new Vector2(0.95f, 0f);
        tbRT.pivot            = new Vector2(0f, 0f);
        tbRT.sizeDelta        = new Vector2(0, 88f);
        tbRT.anchoredPosition = new Vector2(0, 28f);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CENTER PANEL
    // ════════════════════════════════════════════════════════════════════════
    static void BuildCenterPanel(GameObject parent)
    {
        var panel = MakeImage(parent, "CenterPanel", C_Panel);
        AddOutline(panel, C_PanelBorder);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(220f, 0);
        rt.offsetMax = new Vector2(-288f, 0);

        // Title
        var titleTMP = MakeText(panel, "MainMenuTitle", "MAIN MENU", 26, C_Text, FontStyles.Bold, TextAlignmentOptions.Center);
        PinTop(titleTMP.gameObject, -18f, 40f);
        StretchH(titleTMP.gameObject, 10f);

        // Three main buttons – evenly spaced vertically
        MakeMenuButton(panel, "PlayButton",       "PLAY",       new Vector2(0, -10f));
        MakeMenuButton(panel, "CollectionButton", "COLLECTION", new Vector2(0, -80f));
        MakeMenuButton(panel, "ShopButton",       "SHOP",       new Vector2(0, -150f));
    }

    static void MakeMenuButton(GameObject parent, string name, string label, Vector2 centerOffset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(340f, 58f);
        rt.anchoredPosition = centerOffset;

        var img = go.AddComponent<Image>();
        img.color = C_Button;
        AddOutline(go, C_ButtonPress, 2f);

        var btn = go.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = C_Button;
        bc.highlightedColor = C_Button * 1.12f;
        bc.pressedColor     = C_ButtonPress;
        bc.selectedColor    = C_Button;
        btn.colors = bc;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 22;
        tmp.color     = C_White;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  RIGHT PANEL
    // ════════════════════════════════════════════════════════════════════════
    static void BuildRightPanel(GameObject parent)
    {
        var panel = new GameObject("RightPanel");
        panel.transform.SetParent(parent.transform, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1, 0);
        rt.anchorMax        = new Vector2(1, 1);
        rt.pivot            = new Vector2(1, 0.5f);
        rt.sizeDelta        = new Vector2(278f, 0);
        rt.anchoredPosition = Vector2.zero;

        // Daily Reward sub-panel (top 55%)
        var daily = MakeImage(panel, "DailyRewardPanel", C_Panel);
        AddOutline(daily, C_PanelBorder);
        var dRT = daily.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0, 0.44f);
        dRT.anchorMax = new Vector2(1, 1f);
        dRT.offsetMin = Vector2.zero;
        dRT.offsetMax = Vector2.zero;

        var drTitle = MakeText(daily, "DailyRewardTitle", "DAILY REWARD", 13, C_Text, FontStyles.Bold, TextAlignmentOptions.Center);
        PinTop(drTitle.gameObject, -8f, 26f);
        StretchH(drTitle.gameObject, 8f);

        // Chest circle
        var chest = MakeImage(daily, "ChestCircle", C_Gray);
        AddOutline(chest, C_ProgressBar, 3f);
        var cRT = chest.GetComponent<RectTransform>();
        cRT.anchorMin        = new Vector2(0.5f, 0.5f);
        cRT.anchorMax        = new Vector2(0.5f, 0.5f);
        cRT.pivot            = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta        = new Vector2(88f, 88f);
        cRT.anchoredPosition = new Vector2(0, -8f);

        var chestInner = MakeImage(chest, "ChestIcon", new Color(0.58f, 0.60f, 0.63f, 1f));
        StretchPad(chestInner, 16f);

        // Quests sub-panel (bottom 40%)
        var quests = MakeImage(panel, "QuestsPanel", Color.clear);
        var qpRT = quests.GetComponent<RectTransform>();
        qpRT.anchorMin = new Vector2(0, 0f);
        qpRT.anchorMax = new Vector2(1, 0.42f);
        qpRT.offsetMin = Vector2.zero;
        qpRT.offsetMax = Vector2.zero;
        quests.GetComponent<Image>().raycastTarget = false;

        var qtTitle = MakeText(quests, "QuestsTitle", "QUESTS", 15, C_Text, FontStyles.Bold, TextAlignmentOptions.Left);
        PinTop(qtTitle.gameObject, -4f, 28f);
        StretchH(qtTitle.gameObject, 4f);

        // 3 quest slot placeholders
        for (int i = 0; i < 3; i++)
        {
            float xStart = i / 3f;
            float xEnd   = (i + 1) / 3f;
            var slot = MakeImage(quests, $"QuestSlot{i + 1}", new Color(0.80f, 0.87f, 0.91f, 1f));
            var sRT  = slot.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(xStart, 0f);
            sRT.anchorMax = new Vector2(xEnd,   0.72f);
            float lPad = i == 0 ? 0 : 4f;
            float rPad = i == 2 ? 0 : 4f;
            sRT.offsetMin = new Vector2(lPad,  4f);
            sRT.offsetMax = new Vector2(-rPad, 0f);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  BOTTOM NAVIGATION
    // ════════════════════════════════════════════════════════════════════════
    static void BuildBottomNav(GameObject parent)
    {
        var nav = MakeImage(parent, "BottomNav", C_BottomNav);
        var rt  = nav.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 0);
        rt.anchorMax        = new Vector2(1, 0);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(0, 60f);
        rt.anchoredPosition = Vector2.zero;

        // House icon button (separate small square)
        var homeIcon = MakeImage(nav, "HomeIconButton", new Color(0.82f, 0.89f, 0.93f, 1f));
        AddOutline(homeIcon, C_PanelBorder);
        var hiRT = homeIcon.GetComponent<RectTransform>();
        hiRT.anchorMin        = new Vector2(0.5f, 0.5f);
        hiRT.anchorMax        = new Vector2(0.5f, 0.5f);
        hiRT.pivot            = new Vector2(1f, 0.5f);
        hiRT.sizeDelta        = new Vector2(46f, 46f);
        hiRT.anchoredPosition = new Vector2(-200f, 0);
        homeIcon.AddComponent<Button>();

        var houseInner = MakeImage(homeIcon, "HouseIcon", new Color(0.50f, 0.55f, 0.60f, 1f));
        StretchPad(houseInner, 10f);

        // HOME button
        MakeNavButton(nav, "HomeButton",        "HOME",         new Vector2(-146f, 0), new Vector2(80f, 46f));
        MakeNavButton(nav, "FriendsButton",     "FRIENDS",      new Vector2( -50f, 0), new Vector2(90f, 46f));
        MakeNavButton(nav, "LeaderboardButton", "LEADERBOARD",  new Vector2(  60f, 0), new Vector2(120f, 46f));
    }

    static void MakeNavButton(GameObject parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.82f, 0.89f, 0.93f, 1f);
        AddOutline(go, C_PanelBorder);

        var btn = go.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = new Color(0.82f, 0.89f, 0.93f, 1f);
        bc.highlightedColor = C_Button;
        bc.pressedColor     = C_ButtonPress;
        btn.colors = bc;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 13;
        tmp.color     = C_Text;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════════
    static GameObject MakeImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    static TextMeshProUGUI MakeText(GameObject parent, string name, string text, int size,
        Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        return tmp;
    }

    static GameObject MakeIconButton(GameObject parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = C_Button;
        var btn = go.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = C_Button;
        bc.highlightedColor = C_Button * 1.12f;
        bc.pressedColor     = C_ButtonPress;
        btn.colors = bc;

        // Icon placeholder
        var icon = MakeImage(go, "Icon", new Color(0.80f, 0.85f, 0.88f, 1f));
        var iRT  = icon.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0.18f, 0.35f);
        iRT.anchorMax = new Vector2(0.82f, 0.88f);
        iRT.offsetMin = Vector2.zero;
        iRT.offsetMax = Vector2.zero;

        // Label
        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0f);
        tRT.anchorMax = new Vector2(1f, 0.34f);
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 11;
        tmp.color     = C_White;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static void AddOutline(GameObject go, Color color, float size = 1f)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor    = color;
        o.effectDistance = new Vector2(size, -size);
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void StretchH(GameObject go, float hPad)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
        rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
        rt.offsetMin = new Vector2(hPad, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-hPad, rt.offsetMax.y);
    }

    static void StretchPad(GameObject go, float pad)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }

    // Anchor to top edge; anchoredY is negative offset from top
    static void AnchorTop(GameObject go, float anchoredY, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(1, 1);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, anchoredY);
        rt.sizeDelta        = new Vector2(0, height);
    }

    // Anchor top-left of element relative to parent top (y negative = down)
    static void PinTop(GameObject go, float yFromTop, float height)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, yFromTop);
        rt.sizeDelta        = new Vector2(rt.sizeDelta.x, height);
    }
}
