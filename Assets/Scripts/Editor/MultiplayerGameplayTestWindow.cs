using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public sealed class MultiplayerGameplayTestWindow : EditorWindow
{
    private const string HostPidKey = "MultiplayerGameplayTest.HostPid";
    private const string ClientPidKey = "MultiplayerGameplayTest.ClientPid";
    private const string LogDirectoryKey = "MultiplayerGameplayTest.LogDirectory";
    private const string BuildOutput = "Builds/MultiplayerGameplay/MultiplayerGameplay.exe";
    private const double LogRefreshInterval = 0.3;
    private const int MaxLogEntries = 1000;
    private const int InitialLogTailBytes = 256 * 1024;

    private string address = "127.0.0.1";
    private int port = 27982;
    private float clientDelaySeconds = 2f;
    private Process hostProcess;
    private Process clientProcess;
    private string logDirectory;
    private double nextLogRefresh;
    private double clientLaunchAt = -1;
    private bool wasTestActive;

    // Pipeline & Diagnostics
    private PipelineStage currentStage = PipelineStage.Unstarted;
    private SyncHealthStatus syncHealth = SyncHealthStatus.Healthy;
    private readonly ParticipantState hostState = new ParticipantState();
    private readonly ParticipantState clientState = new ParticipantState();
    private readonly DiagnosticInfo diagnostic = new DiagnosticInfo();

    // Logs & UI
    private readonly List<MultiplayerLogEntry> allLogs = new List<MultiplayerLogEntry>();
    private readonly LogTailReader hostLogReader = new LogTailReader();
    private readonly LogTailReader clientLogReader = new LogTailReader();
    private LogCategory selectedCategory = LogCategory.All;
    private string searchQuery = string.Empty;
    private bool autoScroll = true;
    private bool shouldScrollToBottom;
    private Vector2 logScrollPosition;
    private Vector2 windowScrollPosition;

    // Styles
    private GUIStyle richLabelStyle;
    private GUIStyle stepBoxStyle;
    private GUIStyle cardBoxStyle;
    private bool stylesInitialized;

    [MenuItem("Tools/Multiplayer/Gameplay Test Runner")]
    [MenuItem("Horus/Multiplayer Dashboard")]
    public static void Open()
    {
        var window = GetWindow<MultiplayerGameplayTestWindow>("Multiplayer Dashboard");
        window.minSize = new Vector2(620, 680);
    }

    private void OnEnable()
    {
        logDirectory = SessionState.GetString(LogDirectoryKey, string.Empty);
        hostProcess = RestoreProcess(HostPidKey);
        clientProcess = RestoreProcess(ClientPidKey);
        wasTestActive = IsRunning(hostProcess) || IsRunning(clientProcess);
        EditorApplication.update += Tick;
        RefreshLogs();
    }

    private void OnDisable()
    {
        EditorApplication.update -= Tick;
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;
        richLabelStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = true,
            fontSize = 11
        };
        stepBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 11,
            fixedHeight = 28
        };
        cardBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8)
        };
        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();
        windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition);

        DrawHeaderToolbar();
        EditorGUILayout.Space(6);

        DrawPipelineStepper();
        EditorGUILayout.Space(6);

        DrawDualStatusDashboard();
        EditorGUILayout.Space(6);

        DrawSyncHealthBanner();
        EditorGUILayout.Space(6);

        DrawDiagnosticSpotlight();
        EditorGUILayout.Space(6);

        DrawLogInspector();
        EditorGUILayout.Space(6);

        DrawTestUtilities();
        EditorGUILayout.Space(8);

        EditorGUILayout.EndScrollView();
    }

    #region ── Top Toolbar ──

    private void DrawHeaderToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("MULTIPLAYER GAMEPLAY TEST DASHBOARD", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        address = EditorGUILayout.TextField("Host Address", address);
        port = Mathf.Clamp(EditorGUILayout.IntField("Port", port), 1, 65535);
        if (GUILayout.Button("Localhost", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            address = "127.0.0.1";
        }
        EditorGUILayout.EndHorizontal();

        clientDelaySeconds = EditorGUILayout.Slider("Client Launch Delay (s)", clientDelaySeconds, 0f, 15f);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !IsRunning(hostProcess) && !IsRunning(clientProcess) && clientLaunchAt < 0;
        if (GUILayout.Button("▶ START BOTH", GUILayout.Height(26))) StartBoth();
        if (GUILayout.Button("▶ Start Host", GUILayout.Height(26))) StartHost(true);
        GUI.enabled = !IsRunning(clientProcess);
        if (GUILayout.Button("▶ Start Client", GUILayout.Height(26))) StartClient(!IsRunning(hostProcess));

        GUI.enabled = IsRunning(hostProcess) || IsRunning(clientProcess) || clientLaunchAt >= 0;
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("⏹ STOP ALL", GUILayout.Height(26))) StopAll();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        if (GUILayout.Button("🔨 Build", GUILayout.Height(26), GUILayout.Width(65)))
        {
            EditorApplication.delayCall += Build;
        }
        EditorGUILayout.EndHorizontal();

        if (clientLaunchAt >= 0)
        {
            double remaining = Math.Max(0, clientLaunchAt - EditorApplication.timeSinceStartup);
            EditorGUILayout.HelpBox($"Client sẽ tự khởi chạy sau {remaining:0.0} giây...", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region ── Pipeline Stepper ──

    private void DrawPipelineStepper()
    {
        EditorGUILayout.LabelField("TIẾN TRÌNH TRỰC QUAN (LIFECYCLE PIPELINE)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        DrawStepBadge("1. BUILD", currentStage >= PipelineStage.Building, currentStage == PipelineStage.Building);
        GUILayout.Label("➔", GUILayout.Width(14));
        DrawStepBadge("2. PROCESSES", currentStage >= PipelineStage.ProcessesRunning, currentStage == PipelineStage.ProcessesRunning);
        GUILayout.Label("➔", GUILayout.Width(14));
        DrawStepBadge("3. HANDSHAKE", currentStage >= PipelineStage.HandshakeReady, currentStage == PipelineStage.HandshakeReady);
        GUILayout.Label("➔", GUILayout.Width(14));
        DrawStepBadge("4. IN MATCH", currentStage >= PipelineStage.InMatch, currentStage == PipelineStage.InMatch);
        GUILayout.Label("➔", GUILayout.Width(14));
        DrawStepBadge("5. FINISHED", currentStage == PipelineStage.Finished, false);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawStepBadge(string label, bool passed, bool active)
    {
        Color previousColor = GUI.backgroundColor;
        if (active) GUI.backgroundColor = new Color(1f, 0.85f, 0.3f); // Vàng đang chạy
        else if (passed) GUI.backgroundColor = new Color(0.4f, 0.85f, 0.4f); // Xanh hoàn thành
        else GUI.backgroundColor = new Color(0.75f, 0.75f, 0.75f); // Xám chưa tới

        GUILayout.Box(label, stepBoxStyle, GUILayout.ExpandWidth(true));
        GUI.backgroundColor = previousColor;
    }

    #endregion

    #region ── Dual-Column Monitor ──

    private void DrawDualStatusDashboard()
    {
        EditorGUILayout.BeginHorizontal();

        // Host Column
        EditorGUILayout.BeginVertical(cardBoxStyle, GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.47f));
        DrawParticipantHeader("HOST (Player 1)", hostState, true);
        EditorGUILayout.LabelField($"• Server Sequence:", hostState.LastSequence.ToString());
        EditorGUILayout.LabelField($"• Current Turn:", hostState.CurrentTurn > 0 ? hostState.CurrentTurn.ToString() : "N/A");
        EditorGUILayout.LabelField($"• Active Player:", hostState.ActivePlayer > 0 ? $"Player {hostState.ActivePlayer}" : "N/A");
        EditorGUILayout.EndVertical();

        // Client Column
        EditorGUILayout.BeginVertical(cardBoxStyle, GUILayout.ExpandWidth(true));
        DrawParticipantHeader("CLIENT (Player 2)", clientState, false);
        EditorGUILayout.LabelField($"• Handshake Seq:", clientState.LastSequence > 0 ? clientState.LastSequence.ToString() : "N/A");
        EditorGUILayout.LabelField($"• Resync Count:", clientState.ResyncCount.ToString());
        EditorGUILayout.LabelField($"• Connection:", clientState.IsAlive ? (clientState.IsReady ? "Ready (Synchronized)" : "Connecting...") : "Offline");
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawParticipantHeader(string title, ParticipantState state, bool isHost)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        Color pillColor = Color.gray;
        string pillText = state.StatusText;
        if (state.IsReady) { pillColor = new Color(0.3f, 0.8f, 0.3f); pillText = "READY"; }
        else if (state.IsAlive) { pillColor = new Color(1f, 0.8f, 0.2f); pillText = "RUNNING"; }
        else if (pillText == "ERROR") { pillColor = new Color(0.9f, 0.3f, 0.3f); }

        Color prev = GUI.color;
        GUI.color = pillColor;
        GUILayout.Label($"[{pillText}]", EditorStyles.boldLabel, GUILayout.Width(75));
        GUI.color = prev;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"• PID: {(state.Pid > 0 ? state.Pid.ToString() : "None")}");
    }

    private void DrawSyncHealthBanner()
    {
        Color prev = GUI.backgroundColor;
        string bannerText;
        MessageType msgType;

        switch (syncHealth)
        {
            case SyncHealthStatus.Healthy:
                GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
                bannerText = "SYNC HEALTH: 🟢 HEALTHY — Phiên chơi ổn định, không có xung đột Sequence hay Resync.";
                msgType = MessageType.Info;
                break;
            case SyncHealthStatus.Resyncing:
                GUI.backgroundColor = new Color(1f, 0.95f, 0.6f);
                bannerText = "SYNC HEALTH: 🟡 RESYNCING — Phát hiện yêu cầu đồng bộ lại Snapshot/Retry.";
                msgType = MessageType.Warning;
                break;
            default:
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
                bannerText = "SYNC HEALTH: 🔴 FAULT / DESYNC — Phát hiện lỗi mạng, lệch Hash hoặc ngắt kết nối.";
                msgType = MessageType.Error;
                break;
        }

        EditorGUILayout.HelpBox(bannerText, msgType);
        GUI.backgroundColor = prev;
    }

    #endregion

    #region ── Diagnostic Spotlight ──

    private void DrawDiagnosticSpotlight()
    {
        if (!diagnostic.HasActiveError) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"⚠️ {diagnostic.Title}", EditorStyles.boldLabel);
        if (GUILayout.Button("Bỏ qua", EditorStyles.miniButton, GUILayout.Width(60)))
        {
            diagnostic.Clear();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(diagnostic.RootCause))
            EditorGUILayout.LabelField($"• Nguyên nhân: {diagnostic.RootCause}", EditorStyles.wordWrappedLabel);
        if (!string.IsNullOrEmpty(diagnostic.Suggestion))
            EditorGUILayout.LabelField($"• Gợi ý xử lý: {diagnostic.Suggestion}", EditorStyles.wordWrappedLabel);

        EditorGUILayout.EndVertical();
    }

    #endregion

    #region ── Log Inspector ──

    private void DrawLogInspector()
    {
        EditorGUILayout.LabelField("NHẬT KÝ TIẾN TRÌNH & COMMAND AUDIT", EditorStyles.boldLabel);

        // Filter Toolbar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        DrawCategoryTab("Tất cả", LogCategory.All);
        DrawCategoryTab("Milestones 🏁", LogCategory.Milestone);
        DrawCategoryTab("Audit ⚡", LogCategory.Audit);
        DrawCategoryTab("Cảnh báo ⚠️", LogCategory.Warning);
        DrawCategoryTab("Lỗi ❌", LogCategory.Error);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Search & Controls Toolbar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(200));
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(45))) searchQuery = string.Empty;

        bool prevAutoScroll = autoScroll;
        autoScroll = GUILayout.Toggle(autoScroll, "Auto-scroll", EditorStyles.toolbarButton, GUILayout.Width(75));
        if (!prevAutoScroll && autoScroll)
        {
            shouldScrollToBottom = true;
        }

        if (GUILayout.Button("Copy Report", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            CopyLogReport();
        }
        if (GUILayout.Button("Clear Logs", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            allLogs.Clear();
            diagnostic.Clear();
        }
        EditorGUILayout.EndHorizontal();

        // Log Content ScrollView
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(240));
        if (shouldScrollToBottom && Event.current.type == EventType.Layout)
        {
            logScrollPosition.y = float.MaxValue;
            shouldScrollToBottom = false;
        }
        logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition);

        int shownCount = 0;
        for (int i = 0; i < allLogs.Count; i++)
        {
            var entry = allLogs[i];
            if (!MatchesFilter(entry)) continue;

            shownCount++;
            string formattedText = FormatLogEntry(entry);
            EditorGUILayout.LabelField(formattedText, richLabelStyle);
        }

        if (shownCount == 0)
        {
            EditorGUILayout.LabelField("<color=#888888>Chưa có log phù hợp với bộ lọc.</color>", richLabelStyle);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawCategoryTab(string label, LogCategory category)
    {
        int count = GetCategoryCount(category);
        string text = $"{label} ({count})";
        bool isSelected = selectedCategory == category;
        if (GUILayout.Toggle(isSelected, text, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) != isSelected)
        {
            selectedCategory = category;
        }
    }

    private int GetCategoryCount(LogCategory category)
    {
        if (category == LogCategory.All) return allLogs.Count;
        int count = 0;
        for (int i = 0; i < allLogs.Count; i++)
        {
            if (allLogs[i].Category == category) count++;
        }
        return count;
    }

    private bool MatchesFilter(MultiplayerLogEntry entry)
    {
        if (selectedCategory != LogCategory.All && entry.Category != selectedCategory)
            return false;

        if (!string.IsNullOrEmpty(searchQuery))
        {
            if (entry.Message.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) < 0 &&
                entry.RawLine.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        return true;
    }

    private string FormatLogEntry(MultiplayerLogEntry entry)
    {
        string roleTag = entry.IsHost ? "<color=#00E5FF>[HOST]</color>" : "<color=#FF80AB>[CLIENT]</color>";
        string timeTag = $"<color=#888888>{entry.Timestamp:HH:mm:ss}</color>";
        string content;

        switch (entry.Category)
        {
            case LogCategory.Milestone:
                content = $"<color=#00E676>🏁 {entry.Message}</color>";
                break;
            case LogCategory.Audit:
                if (entry.Audit != null && entry.Audit.IsSuccess)
                    content = $"<color=#B9F6CA>⚡ [CMD #{entry.Audit.CommandId}] P{entry.Audit.Player} -> OK (Turn {entry.Audit.Turn}, Seq {entry.Audit.Sequence})</color>";
                else
                    content = $"<color=#FFD54F>⚡ {entry.Message}</color>";
                break;
            case LogCategory.Warning:
                content = $"<color=#FFD54F>⚠️ {entry.Message}</color>";
                break;
            case LogCategory.Error:
                content = $"<color=#FF5252>❌ {entry.Message}</color>";
                break;
            default:
                content = entry.Message;
                break;
        }

        return $"{timeTag} {roleTag} {content}";
    }

    private void CopyLogReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== MULTIPLAYER TEST REPORT ===");
        sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Host PID: {hostState.Pid} | Client PID: {clientState.Pid}");
        sb.AppendLine($"Host State: {hostState.StatusText} (Seq: {hostState.LastSequence}, Turn: {hostState.CurrentTurn})");
        sb.AppendLine($"Client State: {clientState.StatusText} (Seq: {clientState.LastSequence}, Resyncs: {clientState.ResyncCount})");
        sb.AppendLine($"Sync Health: {syncHealth}");
        sb.AppendLine();
        sb.AppendLine("=== LOG ENTRIES ===");
        for (int i = 0; i < allLogs.Count; i++)
        {
            var e = allLogs[i];
            string role = e.IsHost ? "[HOST]" : "[CLIENT]";
            sb.AppendLine($"{e.Timestamp:HH:mm:ss} {role} [{e.Category}] {e.Message}");
        }
        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        EditorUtility.DisplayDialog("Đã sao chép", "Toàn bộ báo cáo phiên test đã được chép vào Clipboard.", "OK");
    }

    #endregion

    #region ── Test Utilities ──

    private void DrawTestUtilities()
    {
        EditorGUILayout.LabelField("TIỆN ÍCH KIỂM THỬ (TEST UTILITIES)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = IsRunning(clientProcess);
        if (GUILayout.Button("⚡ Kill Client (Test Disconnect)", GUILayout.Height(24)))
        {
            KillClientProcess();
        }

        GUI.enabled = !IsRunning(clientProcess) && IsRunning(hostProcess);
        if (GUILayout.Button("🔄 Relaunch Client", GUILayout.Height(24)))
        {
            StartClient(false);
        }

        GUI.enabled = true;
        if (GUILayout.Button("🧪 Open Test Runner", GUILayout.Height(24)))
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }

        if (GUILayout.Button("📂 Log Folder", GUILayout.Height(24), GUILayout.Width(90)))
        {
            if (!string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory))
                EditorUtility.RevealInFinder(logDirectory);
            else
                EditorUtility.DisplayDialog("Chưa có log", "Chưa có phiên test nào được tạo.", "OK");
        }

        if (GUILayout.Button("📁 Build Folder", GUILayout.Height(24), GUILayout.Width(95)))
        {
            RevealBuild();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void KillClientProcess()
    {
        if (IsRunning(clientProcess))
        {
            try
            {
                clientProcess.Kill();
                allLogs.Add(new MultiplayerLogEntry(DateTime.Now, false, LogCategory.Warning,
                    "[FAULT-INJECTION] Client process killed manually to test Host peer disconnect response.", string.Empty));
                TrimLogs();
                if (autoScroll) shouldScrollToBottom = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Cannot kill client process: {ex.Message}");
            }
        }
    }

    #endregion

    #region ── Process & LifeCycle Management ──

    private void Build()
    {
        try
        {
            currentStage = PipelineStage.Building;
            Repaint();
            MultiplayerPrototypeBuild.BuildWindowsGameplayTest();
            allLogs.Add(new MultiplayerLogEntry(DateTime.Now, true, LogCategory.Milestone,
                "Gameplay Build Succeeded: Builds/MultiplayerGameplay/MultiplayerGameplay.exe", string.Empty));
            TrimLogs();
            if (autoScroll) shouldScrollToBottom = true;
            currentStage = PipelineStage.Unstarted;
        }
        catch (Exception error)
        {
            currentStage = PipelineStage.Finished;
            syncHealth = SyncHealthStatus.Fault;
            diagnostic.Set("Build thất bại", error.Message, "Kiểm tra Unity Console để xem lỗi biên dịch chi tiết.", MessageType.Error);
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Gameplay build failed", error.Message, "OK");
        }
    }

    private void StartBoth()
    {
        if (!ValidateLaunch()) return;
        CreateLogDirectory();
        StartHost(false);
        if (!IsRunning(hostProcess)) return;
        clientLaunchAt = EditorApplication.timeSinceStartup + clientDelaySeconds;
        currentStage = PipelineStage.ProcessesRunning;
        Repaint();
    }

    private void StartHost(bool createLogDirectory)
    {
        if (!ValidateLaunch()) return;
        if (createLogDirectory) CreateLogDirectory();
        hostProcess = Launch("host", HostLogPath);
        SavePid(HostPidKey, hostProcess);
        hostState.Reset();
        hostState.Pid = hostProcess.Id;
        hostState.IsAlive = true;
        currentStage = PipelineStage.ProcessesRunning;
        RefreshLogs();
    }

    private void StartClient(bool createLogDirectory)
    {
        if (!ValidateLaunch()) return;
        if (createLogDirectory || string.IsNullOrEmpty(logDirectory)) CreateLogDirectory();
        clientProcess = Launch("client", ClientLogPath);
        SavePid(ClientPidKey, clientProcess);
        clientState.Reset();
        clientState.Pid = clientProcess.Id;
        clientState.IsAlive = true;
        currentStage = PipelineStage.ProcessesRunning;
        RefreshLogs();
    }

    private bool ValidateLaunch()
    {
        if (string.IsNullOrWhiteSpace(address) || address.IndexOf('"') >= 0)
        {
            EditorUtility.DisplayDialog("Invalid address", "Host address is required.", "OK");
            return false;
        }

        if (!File.Exists(BuildPath))
        {
            EditorUtility.DisplayDialog("Build not found",
                "Build Windows Test trước khi chạy multiplayer.", "OK");
            return false;
        }

        return true;
    }

    private Process Launch(string role, string logPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = BuildPath,
            Arguments = $"-mp-gameplay-role {role} -mp-address \"{address}\" -mp-port {port} -logFile \"{logPath}\"",
            WorkingDirectory = Path.GetDirectoryName(BuildPath),
            UseShellExecute = false
        };
        Process process = Process.Start(startInfo);
        if (process == null) throw new InvalidOperationException($"Cannot start gameplay {role} process.");
        return process;
    }

    private void Tick()
    {
        if (clientLaunchAt >= 0 && EditorApplication.timeSinceStartup >= clientLaunchAt)
        {
            clientLaunchAt = -1;
            StartClient(false);
        }

        bool isTestActive = clientLaunchAt >= 0 || IsRunning(hostProcess) || IsRunning(clientProcess);
        if (!isTestActive)
        {
            if (!wasTestActive) return;
            wasTestActive = false;
            currentStage = PipelineStage.Finished;
            RefreshLogs();
            Repaint();
            return;
        }

        wasTestActive = true;
        if (EditorApplication.timeSinceStartup < nextLogRefresh) return;
        nextLogRefresh = EditorApplication.timeSinceStartup + LogRefreshInterval;
        RefreshLogs();
        Repaint();
    }

    private void RefreshLogs()
    {
        ReadStreamLogs(HostLogPath, hostLogReader, true);
        ReadStreamLogs(ClientLogPath, clientLogReader, false);
        TrimLogs();

        UpdateParticipantStatus(hostProcess, hostState);
        UpdateParticipantStatus(clientProcess, clientState);
        UpdatePipelineAndSyncHealth();
    }

    private void TrimLogs()
    {
        if (allLogs.Count > MaxLogEntries)
        {
            allLogs.RemoveRange(0, allLogs.Count - MaxLogEntries);
        }
    }

    private void ReadStreamLogs(string path, LogTailReader reader, bool isHost)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        var newLines = reader.ReadNewLines(path);
        for (int i = 0; i < newLines.Count; i++)
        {
            string line = newLines[i];
            ProcessLogLine(line, isHost);
        }
    }

    private void ProcessLogLine(string line, bool isHost)
    {
        DateTime now = DateTime.Now;
        bool isAudit = MultiplayerTestLogParser.TryParseAudit(line, out var audit);
        bool isReady = MultiplayerTestLogParser.TryParseReady(line, out var readySeq);
        LogCategory category = MultiplayerTestLogParser.Classify(line, audit);

        if (isAudit)
        {
            hostState.LastSequence = audit.Sequence;
            hostState.CurrentTurn = audit.Turn;
            hostState.ActivePlayer = audit.Player;
            if (currentStage < PipelineStage.InMatch) currentStage = PipelineStage.InMatch;
        }

        if (isReady)
        {
            if (isHost)
            {
                hostState.IsReady = true;
                hostState.LastSequence = readySeq;
            }
            else
            {
                clientState.IsReady = true;
                clientState.LastSequence = readySeq;
            }
        }

        if (line.IndexOf("[MP-RESYNC]", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("Snapshot retry", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            clientState.ResyncCount++;
            syncHealth = SyncHealthStatus.Resyncing;
        }

        if (MultiplayerTestLogParser.TryGetDiagnostic(line, audit, out var title, out var cause, out var suggestion, out var severity))
        {
            diagnostic.Set(title, cause, suggestion, severity);
            if (severity == MessageType.Error) syncHealth = SyncHealthStatus.Fault;
            else if (severity == MessageType.Warning && syncHealth == SyncHealthStatus.Healthy) syncHealth = SyncHealthStatus.Resyncing;
        }

        var entry = new MultiplayerLogEntry(now, isHost, category, line, line, audit);
        allLogs.Add(entry);
        if (autoScroll) shouldScrollToBottom = true;
    }

    private void UpdateParticipantStatus(Process process, ParticipantState state)
    {
        state.IsAlive = IsRunning(process);
        if (process == null) { state.StatusText = "Stopped"; return; }
        if (!state.IsAlive) { state.StatusText = "Exited"; return; }
        if (state.IsReady) { state.StatusText = "READY"; return; }
        state.StatusText = "Running";
    }

    private void UpdatePipelineAndSyncHealth()
    {
        if (!hostState.IsAlive && !clientState.IsAlive && clientLaunchAt < 0)
        {
            if (currentStage != PipelineStage.Building) currentStage = PipelineStage.Finished;
            return;
        }

        if (hostState.IsReady && clientState.IsReady)
        {
            if (currentStage < PipelineStage.HandshakeReady) currentStage = PipelineStage.HandshakeReady;
            if (hostState.CurrentTurn > 0 && currentStage < PipelineStage.InMatch) currentStage = PipelineStage.InMatch;
        }
        else if (hostState.IsAlive || clientState.IsAlive)
        {
            if (currentStage < PipelineStage.ProcessesRunning) currentStage = PipelineStage.ProcessesRunning;
        }

        if (syncHealth != SyncHealthStatus.Fault && !diagnostic.HasActiveError)
        {
            if (clientState.ResyncCount > 0) syncHealth = SyncHealthStatus.Resyncing;
            else syncHealth = SyncHealthStatus.Healthy;
        }
    }

    private void StopAll()
    {
        clientLaunchAt = -1;
        Stop(ref clientProcess, ClientPidKey);
        Stop(ref hostProcess, HostPidKey);
        hostState.Reset();
        clientState.Reset();
        currentStage = PipelineStage.Finished;
        RefreshLogs();
        Repaint();
    }

    private static void Stop(ref Process process, string key)
    {
        if (IsRunning(process) && IsGameplayBuild(process))
        {
            try
            {
                process.Kill();
                process.WaitForExit(2000);
            }
            catch (InvalidOperationException) { }
        }
        process?.Dispose();
        process = null;
        SessionState.EraseInt(key);
    }

    private static bool IsGameplayBuild(Process process)
    {
        try
        {
            return string.Equals(Path.GetFullPath(process.MainModule.FileName), BuildPath,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception error) when (error is InvalidOperationException || error is System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static bool IsRunning(Process process)
    {
        if (process == null) return false;
        try { return !process.HasExited; }
        catch (InvalidOperationException) { return false; }
    }

    private static Process RestoreProcess(string key)
    {
        int pid = SessionState.GetInt(key, 0);
        if (pid <= 0) return null;
        try
        {
            Process process = Process.GetProcessById(pid);
            return IsGameplayBuild(process) ? process : null;
        }
        catch (ArgumentException)
        {
            SessionState.EraseInt(key);
            return null;
        }
    }

    private static void SavePid(string key, Process process)
    {
        SessionState.SetInt(key, process.Id);
    }

    private void CreateLogDirectory()
    {
        logDirectory = Path.Combine(Path.GetFullPath("Logs/MultiplayerGameplay"),
            DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(logDirectory);
        SessionState.SetString(LogDirectoryKey, logDirectory);
        hostLogReader.Reset(HostLogPath);
        clientLogReader.Reset(ClientLogPath);
        allLogs.Clear();
        diagnostic.Clear();
        hostState.Reset();
        clientState.Reset();
        currentStage = PipelineStage.ProcessesRunning;
        syncHealth = SyncHealthStatus.Healthy;
    }

    private static void RevealBuild()
    {
        string directory = Path.GetDirectoryName(BuildPath);
        if (Directory.Exists(directory)) EditorUtility.RevealInFinder(directory);
        else EditorUtility.DisplayDialog("Build not found", "Build Windows Test trước.", "OK");
    }

    private static string BuildPath => Path.GetFullPath(BuildOutput);
    private string HostLogPath => string.IsNullOrEmpty(logDirectory) ? string.Empty : Path.Combine(logDirectory, "host-gameplay.log");
    private string ClientLogPath => string.IsNullOrEmpty(logDirectory) ? string.Empty : Path.Combine(logDirectory, "client-gameplay.log");

    #endregion

    #region ── File Tail Stream Reader ──

    private sealed class LogTailReader
    {
        private readonly StringBuilder partialLine = new StringBuilder();
        private readonly byte[] byteBuffer = new byte[16 * 1024];
        private readonly char[] characterBuffer = new char[16 * 1024];
        private Decoder decoder = Encoding.UTF8.GetDecoder();

        public string Path { get; private set; } = string.Empty;
        public long Position { get; private set; }
        public bool Initialized { get; private set; }
        public bool SkipFirstPartialLine { get; private set; }

        public void Reset(string path)
        {
            Path = path ?? string.Empty;
            Position = 0;
            Initialized = false;
            SkipFirstPartialLine = false;
            partialLine.Clear();
            decoder = Encoding.UTF8.GetDecoder();
        }

        public List<string> ReadNewLines(string path)
        {
            var result = new List<string>();
            if (!string.Equals(Path, path, StringComparison.OrdinalIgnoreCase))
                Reset(path);

            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return result;

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                if (!Initialized)
                {
                    Position = Math.Max(0, stream.Length - InitialLogTailBytes);
                    SkipFirstPartialLine = Position > 0;
                    Initialized = true;
                }
                else if (stream.Length < Position)
                {
                    Reset(path);
                    Initialized = true;
                }

                stream.Position = Position;
                int bytesRead;

                while ((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                {
                    Position += bytesRead;
                    int charCount = decoder.GetChars(byteBuffer, 0, bytesRead, characterBuffer, 0, false);
                    for (int i = 0; i < charCount; i++)
                    {
                        char c = characterBuffer[i];
                        if (c != '\n')
                        {
                            partialLine.Append(c);
                            continue;
                        }

                        string line = partialLine.ToString().TrimEnd('\r');
                        partialLine.Clear();

                        if (SkipFirstPartialLine)
                        {
                            SkipFirstPartialLine = false;
                            continue;
                        }

                        if (!IsRelevantLine(line)) continue;
                        result.Add(line);
                    }
                }
            }
            catch (IOException)
            {
                // File locked by player process, skip this tick
            }

            return result;
        }

        private static bool IsRelevantLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            return line.IndexOf("[MP-", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   line.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   line.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   line.IndexOf("Snapshot retry", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   line.IndexOf("Gameplay peer disconnected", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   line.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   line.IndexOf("mismatch", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    #endregion
}
