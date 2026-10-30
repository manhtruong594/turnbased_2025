#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UCP.Bridge;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Tool and UCP script to discover all Attack & Buff animations in Assets/MyGame/Animations,
/// unpack them from FBX files into standalone .anim clips, preserve all AnimationEvents,
/// and reassign references inside AnimatorControllers and AnimatorOverrideControllers.
/// </summary>
public class AnimationUnpackerTool : EditorWindow, IUCPScript
{
    public string Name => "animation-unpacker";
    public string Description => "Find attack/buff animations in Assets/MyGame/Animations, unpack them into standalone .anim files, preserve AnimationEvents, and reassign references in Animator Controllers.";

    [Serializable]
    public class UnpackParams
    {
        public string action = "unpack"; // "scan" or "unpack"
        public string className = "all";  // "all" or specific class name (e.g. "Archer", "Adjudicator")
        public bool dryRun = false;
        public bool includeAttack = true;
        public bool includeBuff = true;
    }

    public class AnimationCandidate
    {
        public string className;
        public string fbxAssetPath;
        public AnimationClip sourceClip;
        public string animType; // "Attack" or "Buff"
        public string targetAnimPath;
        public bool alreadyUnpacked;
        public AnimationClip existingUnpackedClip;
        public List<AnimationEventInfo> events = new();
        public List<string> referencingControllers = new();
        public bool isSelected = true;
    }

    public class AnimationEventInfo
    {
        public float time;
        public string functionName;
        public string stringParameter;
        public float floatParameter;
        public int intParameter;
    }

    // UI State
    private Vector2 _scrollPosition;
    private string _searchFilter = "";
    private int _selectedClassIndex = 0;
    private bool _filterAttack = true;
    private bool _filterBuff = true;
    private List<AnimationCandidate> _candidates = new();
    private bool _isScanning = false;
    private string _statusMessage = "Click 'Scan Animations' to inspect all attack and buff clips.";

    private static readonly string[] ClassNames =
    {
        "All", "Adjudicator", "Archer", "Assassin", "Berserker",
        "Halberdier", "Herbalist", "Knight", "Magician", "Militia", "Smasher"
    };

    [MenuItem("Tools/Animation/Unpack Attack & Buff Animations")]
    public static void OpenWindow()
    {
        var window = GetWindow<AnimationUnpackerTool>("Animation Unpacker");
        window.minSize = new Vector2(680, 480);
        window.ScanAnimations("All", true, true);
    }

    #region IUCPScript Implementation

    public object Execute(string paramsJson)
    {
        var p = string.IsNullOrEmpty(paramsJson)
            ? new UnpackParams()
            : JsonUtility.FromJson<UnpackParams>(paramsJson);

        if (p == null) p = new UnpackParams();

        string action = (p.action ?? "unpack").ToLowerInvariant();
        string targetClass = string.IsNullOrEmpty(p.className) ? "all" : p.className;

        var candidates = CollectCandidates(targetClass, p.includeAttack, p.includeBuff);

        if (action == "scan" || p.dryRun)
        {
            return new
            {
                success = true,
                action = "scan",
                totalFound = candidates.Count,
                attackCount = candidates.Count(c => c.animType == "Attack"),
                buffCount = candidates.Count(c => c.animType == "Buff"),
                candidates = candidates.Select(c => new
                {
                    className = c.className,
                    clipName = c.sourceClip.name,
                    animType = c.animType,
                    fbxPath = c.fbxAssetPath,
                    targetPath = c.targetAnimPath,
                    alreadyUnpacked = c.alreadyUnpacked,
                    eventCount = c.events.Count,
                    events = c.events.Select(e => $"{e.functionName}@{e.time:F2}s ({e.stringParameter})").ToList(),
                    referencedIn = c.referencingControllers
                }).ToList()
            };
        }

        // Execute Unpack & Reassign
        var result = ExecuteUnpackAndReassign(candidates);
        return result;
    }

    #endregion

    #region Core Logic

    public static List<AnimationCandidate> CollectCandidates(string targetClass, bool includeAttack, bool includeBuff)
    {
        var results = new List<AnimationCandidate>();
        string rootDir = "Assets/MyGame/Animations";
        if (!AssetDatabase.IsValidFolder(rootDir)) return results;

        // Map existing Controllers and Overrides to check references
        var controllerReferences = BuildControllerReferenceMap();

        string[] subDirs = Directory.GetDirectories(rootDir);
        foreach (var dir in subDirs)
        {
            string folderName = Path.GetFileName(dir);
            if (!folderName.EndsWith(" Anims", StringComparison.OrdinalIgnoreCase)) continue;

            string cls = folderName.Substring(0, folderName.Length - " Anims".Length);
            if (!string.Equals(targetClass, "all", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(targetClass, cls, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Find all FBX files in this class folder
            string[] fbxFiles = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Replace('\\', '/'))
                .ToArray();

            foreach (var fbxPath in fbxFiles)
            {
                var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__"))
                    .ToList();

                foreach (var clip in clips)
                {
                    if (!IsAttackOrBuff(clip.name, fbxPath, out string animType)) continue;

                    if (animType == "Attack" && !includeAttack) continue;
                    if (animType == "Buff" && !includeBuff) continue;

                    string safeName = MakeValidFileName(clip.name);
                    if (safeName == "Take 001" || safeName.StartsWith("Take_") || string.Equals(safeName, "Default Take", StringComparison.OrdinalIgnoreCase))
                    {
                        safeName = Path.GetFileNameWithoutExtension(fbxPath);
                    }
                    string targetAnimPath = $"{dir.Replace('\\', '/')}/{safeName}.anim";

                    var candidate = new AnimationCandidate
                    {
                        className = cls,
                        fbxAssetPath = fbxPath,
                        sourceClip = clip,
                        animType = animType,
                        targetAnimPath = targetAnimPath,
                        alreadyUnpacked = File.Exists(targetAnimPath),
                        existingUnpackedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(targetAnimPath)
                    };

                    // Collect events from clip
                    var events = AnimationUtility.GetAnimationEvents(clip);
                    if (events == null || events.Length == 0)
                    {
                        events = ExtractEventsFromImporter(fbxPath, clip);
                    }

                    if (events != null)
                    {
                        foreach (var ev in events)
                        {
                            candidate.events.Add(new AnimationEventInfo
                            {
                                time = ev.time,
                                functionName = ev.functionName,
                                stringParameter = ev.stringParameter,
                                floatParameter = ev.floatParameter,
                                intParameter = ev.intParameter
                            });
                        }
                    }

                    // Check referencing controllers
                    if (controllerReferences.TryGetValue(clip, out var controllerList))
                    {
                        candidate.referencingControllers.AddRange(controllerList);
                    }

                    results.Add(candidate);
                }
            }
        }

        return results;
    }

    public static object ExecuteUnpackAndReassign(List<AnimationCandidate> candidates)
    {
        var logs = new List<string>();
        var unpackedList = new List<object>();
        var clipMapping = new Dictionary<AnimationClip, AnimationClip>();

        try
        {
            AssetDatabase.StartAssetEditing();

            int total = candidates.Count;
            for (int i = 0; i < total; i++)
            {
                var c = candidates[i];
                if (!c.isSelected) continue;

                EditorUtility.DisplayProgressBar("Unpacking Animations", $"Processing {c.sourceClip.name} ({i + 1}/{total})...", (float)i / total);

                // 1. Duplicate & Serialize
                AnimationClip newClip = new AnimationClip();
                EditorUtility.CopySerialized(c.sourceClip, newClip);
                newClip.name = Path.GetFileNameWithoutExtension(c.targetAnimPath);

                // 2. Preserve AnimationEvents
                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(c.sourceClip);
                if (events == null || events.Length == 0)
                {
                    events = ExtractEventsFromImporter(c.fbxAssetPath, c.sourceClip);
                }

                if (events != null && events.Length > 0)
                {
                    AnimationUtility.SetAnimationEvents(newClip, events);
                }

                // 3. Create standalone .anim file
                if (File.Exists(c.targetAnimPath))
                {
                    AssetDatabase.DeleteAsset(c.targetAnimPath);
                }
                AssetDatabase.CreateAsset(newClip, c.targetAnimPath);

                var loadedAsset = AssetDatabase.LoadAssetAtPath<AnimationClip>(c.targetAnimPath);
                clipMapping[c.sourceClip] = loadedAsset != null ? loadedAsset : newClip;

                logs.Add($"Unpacked [{c.animType}] '{c.sourceClip.name}' -> '{c.targetAnimPath}' with {events?.Length ?? 0} events.");
                unpackedList.Add(new
                {
                    clip = c.sourceClip.name,
                    type = c.animType,
                    targetPath = c.targetAnimPath,
                    eventCount = events?.Length ?? 0
                });
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 4. Reassign references in AnimatorControllers & AnimatorOverrideControllers
        var updatedControllers = ReassignAllControllers(clipMapping, logs);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return new
        {
            success = true,
            totalUnpacked = unpackedList.Count,
            unpackedClips = unpackedList,
            updatedControllers = updatedControllers,
            logs = logs
        };
    }

    private static List<string> ReassignAllControllers(Dictionary<AnimationClip, AnimationClip> clipMapping, List<string> logs)
    {
        var updatedList = new List<string>();
        string rootDir = "Assets/MyGame/Animations";

        // 1. Process regular AnimatorControllers
        string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController", new[] { rootDir });
        foreach (var guid in controllerGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) continue;

            bool modified = false;
            foreach (var layer in controller.layers)
            {
                if (layer.stateMachine != null)
                {
                    modified |= ReplaceClipsInStateMachine(layer.stateMachine, clipMapping, controller, path, logs);
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(controller);
                updatedList.Add(path);
                logs.Add($"Reassigned references in AnimatorController: '{path}'");
            }
        }

        // 2. Process AnimatorOverrideControllers
        string[] overrideGuids = AssetDatabase.FindAssets("t:AnimatorOverrideController", new[] { rootDir });
        foreach (var guid in overrideGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (overrideController == null) continue;

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);

            bool modified = false;
            for (int i = 0; i < overrides.Count; i++)
            {
                var pair = overrides[i];
                if (pair.Value != null && clipMapping.TryGetValue(pair.Value, out var newClip))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, newClip);
                    modified = true;
                    logs.Add($"Override in '{overrideController.name}': Replaced '{pair.Value.name}' with unpacked '{newClip.name}'");
                }
            }

            if (modified)
            {
                overrideController.ApplyOverrides(overrides);
                EditorUtility.SetDirty(overrideController);
                updatedList.Add(path);
                logs.Add($"Reassigned overrides in AnimatorOverrideController: '{path}'");
            }
        }

        return updatedList;
    }

    private static bool ReplaceClipsInStateMachine(
        AnimatorStateMachine sm,
        Dictionary<AnimationClip, AnimationClip> clipMapping,
        AnimatorController controller,
        string controllerPath,
        List<string> logs)
    {
        bool modified = false;

        foreach (var childState in sm.states)
        {
            var state = childState.state;
            if (state == null || state.motion == null) continue;

            if (state.motion is AnimationClip clip)
            {
                if (clipMapping.TryGetValue(clip, out var newClip))
                {
                    Undo.RecordObject(controller, "Reassign Unpacked Animation Clip");
                    state.motion = newClip;
                    modified = true;
                    logs.Add($"Controller '{Path.GetFileName(controllerPath)}' State '{state.name}': Swapped '{clip.name}' -> '{newClip.name}'");
                }
            }
            else if (state.motion is BlendTree blendTree)
            {
                modified |= ReplaceClipsInBlendTree(blendTree, clipMapping, controller, logs);
            }
        }

        foreach (var childSm in sm.stateMachines)
        {
            if (childSm.stateMachine != null)
            {
                modified |= ReplaceClipsInStateMachine(childSm.stateMachine, clipMapping, controller, controllerPath, logs);
            }
        }

        return modified;
    }

    private static bool ReplaceClipsInBlendTree(
        BlendTree blendTree,
        Dictionary<AnimationClip, AnimationClip> clipMapping,
        AnimatorController controller,
        List<string> logs)
    {
        bool modified = false;
        var children = blendTree.children;

        for (int i = 0; i < children.Length; i++)
        {
            var child = children[i];
            if (child.motion is AnimationClip clip && clipMapping.TryGetValue(clip, out var newClip))
            {
                Undo.RecordObject(controller, "Reassign BlendTree Clip");
                child.motion = newClip;
                children[i] = child;
                modified = true;
            }
            else if (child.motion is BlendTree subTree)
            {
                modified |= ReplaceClipsInBlendTree(subTree, clipMapping, controller, logs);
            }
        }

        if (modified)
        {
            blendTree.children = children;
        }

        return modified;
    }

    private static AnimationEvent[] ExtractEventsFromImporter(string fbxPath, AnimationClip clip)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return null;

        var clipAnims = importer.clipAnimations;
        if (clipAnims == null || clipAnims.Length == 0)
            clipAnims = importer.defaultClipAnimations;

        var matching = clipAnims?.FirstOrDefault(c =>
            string.Equals(c.name, clip.name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.takeName, clip.name, StringComparison.OrdinalIgnoreCase));

        if (matching != null && matching.events != null && matching.events.Length > 0)
        {
            return matching.events.Select(e => new AnimationEvent
            {
                time = e.time * clip.length, // Convert normalized time to actual seconds
                functionName = e.functionName,
                stringParameter = e.stringParameter,
                floatParameter = e.floatParameter,
                intParameter = e.intParameter,
                objectReferenceParameter = e.objectReferenceParameter
            }).ToArray();
        }

        return null;
    }

    private static Dictionary<AnimationClip, List<string>> BuildControllerReferenceMap()
    {
        var map = new Dictionary<AnimationClip, List<string>>();
        string rootDir = "Assets/MyGame/Animations";

        string[] guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { rootDir });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) continue;

            foreach (var clip in controller.animationClips)
            {
                if (clip == null) continue;
                if (!map.TryGetValue(clip, out var list))
                {
                    list = new List<string>();
                    map[clip] = list;
                }
                string name = Path.GetFileName(path);
                if (!list.Contains(name)) list.Add(name);
            }
        }

        string[] overrideGuids = AssetDatabase.FindAssets("t:AnimatorOverrideController", new[] { rootDir });
        foreach (var guid in overrideGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (overrideController == null) continue;

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);

            foreach (var pair in overrides)
            {
                if (pair.Value != null)
                {
                    if (!map.TryGetValue(pair.Value, out var list))
                    {
                        list = new List<string>();
                        map[pair.Value] = list;
                    }
                    string name = Path.GetFileName(path);
                    if (!list.Contains(name)) list.Add(name);
                }
            }
        }

        return map;
    }

    public static bool IsAttackOrBuff(string clipName, string fbxPath, out string animType)
    {
        animType = "Unknown";
        string combined = $"{clipName} {Path.GetFileNameWithoutExtension(fbxPath)}".ToLowerInvariant();

        // 1. Buff keywords
        string[] buffKeywords = { "buff", "boost", "pray", "holyflame", "selfbuff", "weaponbuff", "detectionhide" };
        foreach (var kw in buffKeywords)
        {
            if (combined.Contains(kw))
            {
                animType = "Buff";
                return true;
            }
        }

        // 2. Attack keywords
        string[] attackKeywords =
        {
            "attack", "atk", "strike", "shot", "firing", "charging",
            "whirlwind", "magmaeruption", "rapidfireball", "risingattack",
            "tornadoshot", "tornadoloop", "tornadoend", "elimination", "entangle",
            "cast", "skrot", "sk15", "sealchain", "leapattack",
            "hammer", "fastcri", "skilldoom"
        };

        foreach (var kw in attackKeywords)
        {
            if (combined.Contains(kw))
            {
                animType = "Attack";
                return true;
            }
        }

        return false;
    }

    private static string MakeValidFileName(string name)
    {
        string invalid = new string(Path.GetInvalidFileNameChars()) + ":/\\|";
        foreach (char c in invalid)
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    #endregion

    #region GUI Presentation (Minimalist & Native-like)

    private void OnGUI()
    {
        DrawHeaderToolbar();
        DrawCandidateList();
        DrawFooterStatusBar();
    }

    private void DrawHeaderToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh", "Scan animations in project"), EditorStyles.toolbarButton, GUILayout.Width(28)))
        {
            ScanAnimations(ClassNames[_selectedClassIndex], _filterAttack, _filterBuff);
        }

        GUILayout.Label("Class:", EditorStyles.miniLabel, GUILayout.Width(38));
        int newClassIndex = EditorGUILayout.Popup(_selectedClassIndex, ClassNames, EditorStyles.toolbarPopup, GUILayout.Width(100));
        if (newClassIndex != _selectedClassIndex)
        {
            _selectedClassIndex = newClassIndex;
            ScanAnimations(ClassNames[_selectedClassIndex], _filterAttack, _filterBuff);
        }

        GUILayout.Space(8);

        bool newAttack = GUILayout.Toggle(_filterAttack, "Attack", EditorStyles.toolbarButton);
        bool newBuff = GUILayout.Toggle(_filterBuff, "Buff", EditorStyles.toolbarButton);
        if (newAttack != _filterAttack || newBuff != _filterBuff)
        {
            _filterAttack = newAttack;
            _filterBuff = newBuff;
            ScanAnimations(ClassNames[_selectedClassIndex], _filterAttack, _filterBuff);
        }

        GUILayout.FlexibleSpace();

        _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(160));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCandidateList()
    {
        if (_candidates == null || _candidates.Count == 0)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            GUIStyle centered = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 12 };
            GUILayout.Label(_isScanning ? "Scanning animations..." : "No attack/buff animations found for selected filter.", centered);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            return;
        }

        using var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition);
        _scrollPosition = scroll.scrollPosition;

        EditorGUILayout.Space(4);

        // Header table
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        bool selectAll = _candidates.All(c => c.isSelected);
        bool toggleAll = EditorGUILayout.Toggle(selectAll, GUILayout.Width(24));
        if (toggleAll != selectAll)
        {
            _candidates.ForEach(c => c.isSelected = toggleAll);
        }

        GUILayout.Label("Class", EditorStyles.miniBoldLabel, GUILayout.Width(75));
        GUILayout.Label("Type", EditorStyles.miniBoldLabel, GUILayout.Width(50));
        GUILayout.Label("Clip / Source FBX", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
        GUILayout.Label("Events", EditorStyles.miniBoldLabel, GUILayout.Width(60));
        GUILayout.Label("Status", EditorStyles.miniBoldLabel, GUILayout.Width(110));
        GUILayout.Label("Controllers", EditorStyles.miniBoldLabel, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        // Rows
        for (int i = 0; i < _candidates.Count; i++)
        {
            var c = _candidates[i];

            if (!string.IsNullOrEmpty(_searchFilter))
            {
                bool matches = c.sourceClip.name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               c.className.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!matches) continue;
            }

            // Alternating subtle background
            Color rowBg = (i % 2 == 0)
                ? (EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 0.4f) : new Color(0.9f, 0.9f, 0.9f, 0.4f))
                : Color.clear;

            Rect rowRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rowRect, rowBg);

            c.isSelected = EditorGUILayout.Toggle(c.isSelected, GUILayout.Width(24));

            GUILayout.Label(c.className, EditorStyles.miniLabel, GUILayout.Width(75));

            // Type badge
            Color oldColor = GUI.color;
            GUI.color = c.animType == "Attack" ? new Color(1f, 0.65f, 0.65f) : new Color(0.65f, 0.9f, 1f);
            GUILayout.Label($"[{c.animType}]", EditorStyles.miniBoldLabel, GUILayout.Width(50));
            GUI.color = oldColor;

            // Clip & FBX path tooltip
            var content = new GUIContent(c.sourceClip.name, $"{c.fbxAssetPath}\n-> {c.targetAnimPath}");
            GUILayout.Label(content, EditorStyles.label, GUILayout.ExpandWidth(true));

            // Events count with tooltip
            string eventTooltip = c.events.Count > 0
                ? string.Join("\n", c.events.Select(e => $"• {e.functionName} @ {e.time:F2}s ({e.stringParameter})"))
                : "No events";
            var eventContent = new GUIContent($"{c.events.Count} ev", eventTooltip);
            GUILayout.Label(eventContent, EditorStyles.miniLabel, GUILayout.Width(60));

            // Status: Unpacked or FBX Sub-asset
            if (c.alreadyUnpacked)
            {
                GUI.color = new Color(0.5f, 1f, 0.5f);
                GUILayout.Label(".anim exists", EditorStyles.miniLabel, GUILayout.Width(110));
                GUI.color = oldColor;
            }
            else
            {
                GUILayout.Label("FBX Sub-asset", EditorStyles.miniLabel, GUILayout.Width(110));
            }

            // Controllers count / tooltip
            string ctrlTooltip = c.referencingControllers.Count > 0
                ? string.Join("\n", c.referencingControllers)
                : "Not referenced";
            var ctrlContent = new GUIContent($"{c.referencingControllers.Count} refs", ctrlTooltip);
            GUILayout.Label(ctrlContent, EditorStyles.miniLabel, GUILayout.Width(120));

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawFooterStatusBar()
    {
        EditorGUILayout.Space(2);
        Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.15f));

        EditorGUILayout.BeginHorizontal(GUILayout.Height(30));

        int selectedCount = _candidates.Count(c => c.isSelected);
        int totalEvents = _candidates.Where(c => c.isSelected).Sum(c => c.events.Count);
        GUILayout.Label($"Selected: {selectedCount}/{_candidates.Count} clips | {totalEvents} events to preserve", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Dry Run / Validate", GUILayout.Width(140), GUILayout.Height(24)))
        {
            var dryResult = Execute(JsonUtility.ToJson(new UnpackParams
            {
                action = "scan",
                className = ClassNames[_selectedClassIndex].ToLowerInvariant()
            }));
            Debug.Log($"[AnimationUnpacker] Dry Run completed: {dryResult}");
            _statusMessage = "Dry run completed. Check Unity Console for details.";
        }

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.2f, 0.5f, 0.8f);
        if (GUILayout.Button($"Unpack & Reassign ({selectedCount})", GUILayout.Width(180), GUILayout.Height(24)))
        {
            if (selectedCount == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select at least one animation to unpack.", "OK");
            }
            else if (EditorUtility.DisplayDialog(
                "Confirm Unpack & Reassign",
                $"Are you sure you want to unpack {selectedCount} animation clips to standalone .anim files and reassign them in AnimatorControllers?\n\nThis will preserve all {totalEvents} AnimationEvents.",
                "Execute", "Cancel"))
            {
                var result = ExecuteUnpackAndReassign(_candidates.Where(c => c.isSelected).ToList());
                ScanAnimations(ClassNames[_selectedClassIndex], _filterAttack, _filterBuff);
                EditorUtility.DisplayDialog("Complete", $"Unpack completed successfully! Check console for details.", "OK");
            }
        }
        GUI.backgroundColor = prevBg;

        EditorGUILayout.EndHorizontal();
    }

    private void ScanAnimations(string className, bool includeAttack, bool includeBuff)
    {
        _isScanning = true;
        try
        {
            _candidates = CollectCandidates(className, includeAttack, includeBuff);
            _statusMessage = $"Found {_candidates.Count} clips ({_candidates.Count(c => c.animType == "Attack")} Attack, {_candidates.Count(c => c.animType == "Buff")} Buff).";
        }
        finally
        {
            _isScanning = false;
        }
    }

    #endregion
}
#endif

