using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AbilityWireframeCanvasBuilder
{
    private const float ReferenceWidth = 393f;
    private const float ReferenceHeight = 683f;
    private const string CanvasName = "AbilityWireframeCanvas";
    private const string AssetFolder = "Assets/Generated/AbilityWireframe";

    private static readonly Color BackgroundColor = Hex("43218F");
    private static readonly Color PanelPurple = Hex("5B31AA");
    private static readonly Color SkillGreen = Hex("8BFF12");
    private static readonly Color SkillBlue = Hex("3768C9");
    private static readonly Color DarkBlue = Hex("13295B");
    private static readonly Color Yellow = Hex("FFD829");
    private static readonly Color White = Color.white;

    [MenuItem("Tools/UI/Build Ability Wireframe Canvas")]
    public static void Build()
    {
        SpriteLibrary sprites = SpriteLibrary.LoadOrCreate();
        GameObject oldCanvas = GameObject.Find(CanvasName);

        if (oldCanvas != null)
        {
            UnityEngine.Object.DestroyImmediate(oldCanvas);
        }

        GameObject canvasObject = CreateCanvas();
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        RectTransform screen = CreateImage("Screen", canvasRect, sprites.Square, BackgroundColor, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));

        BuildHeader(screen, sprites);
        BuildSkillTree(screen, sprites);
        BuildFooter(screen, sprites);
        EnsureEventSystem();

        Scene activeScene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = canvasObject;
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        Camera mainCamera = Camera.main;
        canvas.renderMode = mainCamera == null ? RenderMode.ScreenSpaceOverlay : RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCamera;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        return canvasObject;
    }

    private static void BuildHeader(RectTransform parent, SpriteLibrary sprites)
    {
        RectTransform title = CreateText("Title", parent, "ABILITIES", 28, FontStyle.Bold, White, ToCanvasPoint(196, 35), new Vector2(210, 42));
        AddOutline(title.gameObject, Color.black, new Vector2(2f, -2f));

        RectTransform info = CreateImage("InfoButton", parent, sprites.RoundedRect, Hex("6687FF"), ToCanvasPoint(256, 35), new Vector2(25, 25));
        CreateText("Label", info, "i", 18, FontStyle.Bold, White, Vector2.zero, new Vector2(22, 22));

        CreateImage("ProgressLeftCap", parent, sprites.Circle, Hex("8C7CC4"), ToCanvasPoint(21, 70), new Vector2(6, 6));
        CreateImage("ProgressRightCap", parent, sprites.Circle, Hex("8C7CC4"), ToCanvasPoint(366, 70), new Vector2(6, 6));
        CreateLine("ProgressLeft", parent, sprites.Square, Hex("8C7CC4"), ToCanvasPoint(34, 70), ToCanvasPoint(177, 70), 4f);
        CreateLine("ProgressRight", parent, sprites.Square, Hex("8C7CC4"), ToCanvasPoint(211, 70), ToCanvasPoint(353, 70), 4f);
        CreateImage("ProgressMarker", parent, sprites.Diamond, Hex("7568B4"), ToCanvasPoint(194, 70), new Vector2(17, 17));
    }

    private static void BuildSkillTree(RectTransform parent, SpriteLibrary sprites)
    {
        RectTransform connectorLayer = CreateRect("TreeConnectors", parent, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));
        RectTransform nodeLayer = CreateRect("TreeNodes", parent, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight));

        Vector2 topLeft = ToCanvasPoint(106, 143);
        Vector2 topRight = ToCanvasPoint(280, 143);
        Vector2 midLeft = ToCanvasPoint(106, 254);
        Vector2 midRight = ToCanvasPoint(280, 254);
        Vector2 lowLeft = ToCanvasPoint(106, 365);
        Vector2 lowRight = ToCanvasPoint(280, 365);
        Vector2 bottom = ToCanvasPoint(194, 466);

        CreateLine("BranchTopLeft", connectorLayer, sprites.RoundedRect, SkillGreen, topLeft + new Vector2(14, 31), ToCanvasPoint(174, 74), 8f);
        CreateLine("BranchTopRight", connectorLayer, sprites.RoundedRect, SkillGreen, topRight + new Vector2(-14, 31), ToCanvasPoint(213, 74), 8f);
        CreateLine("LeftTopToMid", connectorLayer, sprites.RoundedRect, SkillGreen, topLeft + new Vector2(0, -30), midLeft + new Vector2(0, 30), 8f);
        CreateLine("LeftMidToLow", connectorLayer, sprites.RoundedRect, DarkBlue, midLeft + new Vector2(0, -30), lowLeft + new Vector2(0, 30), 8f);
        CreateLine("RightTopToMid", connectorLayer, sprites.RoundedRect, SkillGreen, topRight + new Vector2(0, -30), midRight + new Vector2(0, 30), 8f);
        CreateLine("RightMidToLow", connectorLayer, sprites.RoundedRect, SkillGreen, midRight + new Vector2(0, -30), lowRight + new Vector2(0, 30), 8f);
        CreateLine("LowLeftToBottom", connectorLayer, sprites.RoundedRect, DarkBlue, lowLeft + new Vector2(20, -25), bottom + new Vector2(-21, 25), 8f);
        CreateLine("LowRightToBottom", connectorLayer, sprites.RoundedRect, SkillGreen, lowRight + new Vector2(-20, -25), bottom + new Vector2(21, 25), 8f);

        CreateSkillNode(nodeLayer, sprites, "TargetNode", topLeft, SkillGreen, SkillBlue, "TGT", "5/5");
        CreateSkillNode(nodeLayer, sprites, "GroupNode", topRight, SkillGreen, SkillBlue, "ALLY", "2/5");
        CreateSkillNode(nodeLayer, sprites, "ArmorNode", midLeft, SkillGreen, SkillBlue, "ARM", "2/5");
        CreateSkillNode(nodeLayer, sprites, "BoxNode", midRight, SkillGreen, SkillBlue, "BOX", "4/5");
        CreateSkillNode(nodeLayer, sprites, "LockedNode", lowLeft, DarkBlue, Hex("1D315F"), "LOCK", "2/5");
        CreateSkillNode(nodeLayer, sprites, "PercentNode", lowRight, SkillGreen, SkillBlue, "%", "4/5");
        CreateSkillNode(nodeLayer, sprites, "EpicNode", bottom, Yellow, SkillBlue, "STAR", "3/5", true);
    }

    private static void BuildFooter(RectTransform parent, SpriteLibrary sprites)
    {
        CreateText("Timer", parent, "15m 2s", 12, FontStyle.Bold, White, ToCanvasPoint(111, 535), new Vector2(75, 22));
        CreateImage("TimerIcon", parent, sprites.Circle, Yellow, ToCanvasPoint(82, 535), new Vector2(19, 19));

        RectTransform addCard = CreateImage("AddAbilitiesPanel", parent, sprites.RoundedRect, SkillBlue, ToCanvasPoint(95, 569), new Vector2(122, 40));
        CreateSkillBadge(addCard, sprites, ToCanvasPointLocal(-40, 0), "3/5");
        AddTextWithOutline(addCard, "Add abilities", 14, ToCanvasPointLocal(18, 0), new Vector2(90, 22));
        RectTransform speed = CreateImage("SpeedUpButton", parent, sprites.RoundedRect, SkillGreen, ToCanvasPoint(124, 600), new Vector2(80, 27));
        AddTextWithOutline(speed, "SPEED UP!", 13, Vector2.zero, new Vector2(76, 22));

        RectTransform queue = CreateImage("SecondQueuePanel", parent, sprites.RoundedRect, Yellow, ToCanvasPoint(272, 569), new Vector2(126, 40));
        CreateImage("PlusCircle", queue, sprites.Circle, SkillBlue, ToCanvasPointLocal(-41, 0), new Vector2(34, 34));
        CreateText("Plus", queue, "+", 27, FontStyle.Bold, White, ToCanvasPointLocal(-41, 0), new Vector2(28, 28));
        AddTextWithOutline(queue, "Second Queue", 14, ToCanvasPointLocal(23, 0), new Vector2(90, 22), Hex("FF7FEA"));
        RectTransform unlock = CreateImage("UnlockButton", parent, sprites.RoundedRect, SkillGreen, ToCanvasPoint(295, 600), new Vector2(80, 27));
        AddTextWithOutline(unlock, "UNLOCK!", 13, Vector2.zero, new Vector2(76, 22));
        CreateImage("Lock", parent, sprites.RoundedRect, Yellow, ToCanvasPoint(340, 549), new Vector2(32, 34));

        CreateImage("Alert", parent, sprites.RoundedRect, Hex("FF362E"), ToCanvasPoint(96, 640), new Vector2(18, 19));
        AddTextWithOutline(parent, "!", 18, ToCanvasPoint(96, 640), new Vector2(14, 18));
        RectTransform cardTab = CreateImage("CardTab", parent, sprites.RoundedRect, DarkBlue, ToCanvasPoint(143, 650), new Vector2(110, 32));
        AddTextWithOutline(cardTab, "CARD", 13, Vector2.zero, new Vector2(90, 20));
        RectTransform abilityTab = CreateImage("AbilitiesTabActive", parent, sprites.RoundedRect, Yellow, ToCanvasPoint(250, 650), new Vector2(105, 38));
        AddTextWithOutline(abilityTab, "ABILITIES", 19, Vector2.zero, new Vector2(100, 25));
    }

    private static void CreateSkillNode(RectTransform parent, SpriteLibrary sprites, string name, Vector2 position, Color outer, Color inner, string icon, string count, bool winged = false)
    {
        RectTransform group = CreateRect(name, parent, position, new Vector2(94, 88));

        if (winged)
        {
            CreateImage("LeftWing", group, sprites.RoundedRect, Yellow, new Vector2(-39, -4), new Vector2(19, 45));
            CreateImage("RightWing", group, sprites.RoundedRect, Yellow, new Vector2(39, -4), new Vector2(19, 45));
        }

        CreateImage("OuterHex", group, sprites.Hex, outer, Vector2.zero, new Vector2(72, 72));
        CreateImage("InnerHex", group, sprites.Hex, inner, Vector2.zero, new Vector2(55, 55));
        RectTransform iconText = CreateText("Icon", group, icon, icon.Length > 3 ? 12 : 19, FontStyle.Bold, White, Vector2.zero, new Vector2(48, 30));
        AddOutline(iconText.gameObject, Color.black, new Vector2(1.4f, -1.4f));
        AddTextWithOutline(group, count, 17, new Vector2(0, -35), new Vector2(48, 22));
    }

    private static void CreateSkillBadge(RectTransform parent, SpriteLibrary sprites, Vector2 position, string count)
    {
        RectTransform badge = CreateRect("SkillBadge", parent, position, new Vector2(34, 34));
        CreateImage("OuterHex", badge, sprites.Hex, SkillGreen, Vector2.zero, new Vector2(33, 33));
        CreateImage("InnerHex", badge, sprites.Hex, SkillBlue, Vector2.zero, new Vector2(25, 25));
        AddTextWithOutline(badge, count, 9, new Vector2(0, -19), new Vector2(26, 12));
    }

    private static RectTransform AddTextWithOutline(RectTransform parent, string text, int size, Vector2 position, Vector2 sizeDelta)
    {
        return AddTextWithOutline(parent, text, size, position, sizeDelta, White);
    }

    private static RectTransform AddTextWithOutline(RectTransform parent, string text, int size, Vector2 position, Vector2 sizeDelta, Color color)
    {
        RectTransform label = CreateText(text.Replace(" ", string.Empty) + "Label", parent, text, size, FontStyle.Bold, color, position, sizeDelta);
        AddOutline(label.gameObject, Color.black, new Vector2(1.4f, -1.4f));
        return label;
    }

    private static RectTransform CreateLine(string name, RectTransform parent, Sprite sprite, Color color, Vector2 start, Vector2 end, float thickness)
    {
        Vector2 delta = end - start;
        RectTransform line = CreateImage(name, parent, sprite, color, start + delta * 0.5f, new Vector2(delta.magnitude, thickness));
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        return line;
    }

    private static RectTransform CreateImage(string name, RectTransform parent, Sprite sprite, Color color, Vector2 position, Vector2 sizeDelta)
    {
        RectTransform rect = CreateRect(name, parent, position, sizeDelta);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return rect;
    }

    private static RectTransform CreateText(string name, RectTransform parent, string text, int size, FontStyle style, Color color, Vector2 position, Vector2 sizeDelta)
    {
        RectTransform rect = CreateRect(name, parent, position, sizeDelta);
        Text label = rect.gameObject.AddComponent<Text>();
        label.text = text;
        label.font = FontProvider.Get();
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        return rect;
    }

    private static RectTransform CreateRect(string name, RectTransform parent, Vector2 position, Vector2 sizeDelta)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private static void AddOutline(GameObject gameObject, Color color, Vector2 distance)
    {
        Outline outline = gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Vector2 ToCanvasPoint(float x, float y)
    {
        return new Vector2(x - ReferenceWidth * 0.5f, ReferenceHeight * 0.5f - y);
    }

    private static Vector2 ToCanvasPointLocal(float x, float y)
    {
        return new Vector2(x, -y);
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        return color;
    }

    private static class FontProvider
    {
        private static Font font;

        public static Font Get()
        {
            if (font != null)
            {
                return font;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
    }

    private sealed class SpriteLibrary
    {
        public Sprite Square { get; private set; }
        public Sprite RoundedRect { get; private set; }
        public Sprite Hex { get; private set; }
        public Sprite Circle { get; private set; }
        public Sprite Diamond { get; private set; }

        public static SpriteLibrary LoadOrCreate()
        {
            Directory.CreateDirectory(AssetFolder);
            SpriteLibrary library = new SpriteLibrary
            {
                Square = CreateSprite("wf_square.png", TextureFactory.Square(16)),
                RoundedRect = CreateSprite("wf_rounded_rect.png", TextureFactory.RoundedRect(64, 64, 12f)),
                Hex = CreateSprite("wf_hex.png", TextureFactory.Hex(96, 96)),
                Circle = CreateSprite("wf_circle.png", TextureFactory.Circle(64, 64)),
                Diamond = CreateSprite("wf_diamond.png", TextureFactory.Diamond(64, 64))
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return library;
        }

        private static Sprite CreateSprite(string fileName, Texture2D texture)
        {
            string path = $"{AssetFolder}/{fileName}";

            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }

    private static class TextureFactory
    {
        public static Texture2D Square(int size)
        {
            return Draw(size, size, (x, y) => true);
        }

        public static Texture2D Circle(int width, int height)
        {
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radius = Mathf.Min(width, height) * 0.48f;
            return Draw(width, height, (x, y) => Vector2.Distance(new Vector2(x, y), center) <= radius);
        }

        public static Texture2D Diamond(int width, int height)
        {
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            return Draw(width, height, (x, y) => Mathf.Abs(x - center.x) / center.x + Mathf.Abs(y - center.y) / center.y <= 1f);
        }

        public static Texture2D Hex(int width, int height)
        {
            Vector2 center = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
            float radius = Mathf.Min(width, height) * 0.48f;
            List<Vector2> points = new List<Vector2>();

            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i + 30f);
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            return Draw(width, height, (x, y) => ContainsPoint(points, new Vector2(x, y)));
        }

        public static Texture2D RoundedRect(int width, int height, float radius)
        {
            return Draw(width, height, (x, y) =>
            {
                float px = Mathf.Min(x, width - 1 - x);
                float py = Mathf.Min(y, height - 1 - y);

                if (px >= radius || py >= radius)
                {
                    return true;
                }

                Vector2 corner = new Vector2(radius, radius);
                Vector2 point = new Vector2(px, py);
                return Vector2.Distance(point, corner) <= radius;
            });
        }

        private static Texture2D Draw(int width, int height, Func<int, int, bool> isInside)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, isInside(x, y) ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return texture;
        }

        private static bool ContainsPoint(IReadOnlyList<Vector2> points, Vector2 point)
        {
            bool inside = false;
            int j = points.Count - 1;

            for (int i = 0; i < points.Count; i++)
            {
                bool intersects = points[i].y > point.y != points[j].y > point.y
                    && point.x < (points[j].x - points[i].x) * (point.y - points[i].y) / (points[j].y - points[i].y) + points[i].x;

                if (intersects)
                {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }
    }
}
