using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimatorOverrideTool : EditorWindow
{
    private AnimatorController sourceController;
    private Vector2 scrollPosition;
    private string savePath = "Assets";
    private string assetName = "NewOverrideController";
    private List<ClipOverrideEntry> clipEntries = new();

    [MenuItem("Horus/Tools/Animator Override Creator")]
    private static void Open()
    {
        var window = GetWindow<AnimatorOverrideTool>("Animator Override Creator");
        window.minSize = new Vector2(450, 350);
    }

    private void OnGUI()
    {
        DrawSourceSection();
        DrawClipList();
        DrawCreateSection();
    }

    private void DrawSourceSection()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Source Animator", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        sourceController = (AnimatorController)EditorGUILayout.ObjectField(
            "Animator Controller", sourceController, typeof(AnimatorController), false);

        if (EditorGUI.EndChangeCheck())
            RefreshClipEntries();
    }

    private void DrawClipList()
    {
        if (sourceController == null || clipEntries.Count == 0) return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField($"Animation Clips ({clipEntries.Count})", EditorStyles.boldLabel);

        using var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition);
        scrollPosition = scroll.scrollPosition;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Original Clip", EditorStyles.miniLabel, GUILayout.MinWidth(150));
        EditorGUILayout.LabelField("Override Clip", EditorStyles.miniLabel, GUILayout.MinWidth(150));
        EditorGUILayout.EndHorizontal();

        foreach (var entry in clipEntries)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(entry.Original, typeof(AnimationClip), false, GUILayout.MinWidth(150));
            EditorGUI.EndDisabledGroup();

            entry.Override = (AnimationClip)EditorGUILayout.ObjectField(
                entry.Override, typeof(AnimationClip), false, GUILayout.MinWidth(150));
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawCreateSection()
    {
        if (sourceController == null) return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        assetName = EditorGUILayout.TextField("Asset Name", assetName);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextField("Save Folder", savePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
                savePath = FileUtil.GetProjectRelativePath(selected);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All Overrides", GUILayout.Height(28)))
            clipEntries.ForEach(e => e.Override = null);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Create Override Controller", GUILayout.Height(28)))
            CreateOverrideController();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }

    private void RefreshClipEntries()
    {
        clipEntries.Clear();
        if (sourceController == null) return;

        var clips = sourceController.animationClips.Distinct().ToList();
        foreach (var clip in clips)
            clipEntries.Add(new ClipOverrideEntry { Original = clip });

        assetName = sourceController.name + "_Override";
    }

    private void CreateOverrideController()
    {
        if (sourceController == null) return;

        string fullPath = $"{savePath}/{assetName}.overrideController";
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

        var overrideController = new AnimatorOverrideController(sourceController);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            var entry = clipEntries.Find(e => e.Original == overrides[i].Key);
            if (entry?.Override != null)
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, entry.Override);
        }

        overrideController.ApplyOverrides(overrides);
        AssetDatabase.CreateAsset(overrideController, fullPath);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = overrideController;
        EditorGUIUtility.PingObject(overrideController);
        Debug.Log($"AnimatorOverrideController created at: {fullPath}");
    }

    private class ClipOverrideEntry
    {
        public AnimationClip Original;
        public AnimationClip Override;
    }
}
