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
    private const double LogRefreshInterval = 0.5;
    private const int InitialLogTailBytes = 256 * 1024;

    private string address = "127.0.0.1";
    private int port = 27982;
    private float clientDelaySeconds = 2f;
    private Process hostProcess;
    private Process clientProcess;
    private string logDirectory;
    private string hostLog = string.Empty;
    private string clientLog = string.Empty;
    private string hostStatus = "Stopped";
    private string clientStatus = "Stopped";
    private double nextLogRefresh;
    private double clientLaunchAt = -1;
    private bool wasTestActive;
    private readonly LogTailState hostLogTail = new LogTailState();
    private readonly LogTailState clientLogTail = new LogTailState();

    [MenuItem("Tools/Multiplayer/Gameplay Test Runner")]
    public static void Open()
    {
        GetWindow<MultiplayerGameplayTestWindow>("Multiplayer Test");
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

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Phase 4 - Multiplayer Gameplay", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Build và chạy hai Windows player. Host là Player1, Client là Player2. " +
            "Cả hai phải báo [MP-GAMEPLAY] READY sequence=1 trước khi thao tác.",
            MessageType.Info);

        address = EditorGUILayout.TextField("Host address", address);
        port = Mathf.Clamp(EditorGUILayout.IntField("Port", port), 1, 65535);
        clientDelaySeconds = EditorGUILayout.Slider("Client delay (seconds)", clientDelaySeconds, 0f, 29f);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !IsRunning(hostProcess) && !IsRunning(clientProcess);
            if (GUILayout.Button("Build Windows Test")) Build();
            GUI.enabled = true;
            if (GUILayout.Button("Open Build Folder")) RevealBuild();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !IsRunning(hostProcess) && !IsRunning(clientProcess);
            if (GUILayout.Button("Start Host")) StartHost(true);
            GUI.enabled = !IsRunning(clientProcess);
            if (GUILayout.Button("Start Client")) StartClient(!IsRunning(hostProcess));
            GUI.enabled = !IsRunning(hostProcess) && !IsRunning(clientProcess);
            if (GUILayout.Button("Start Both")) StartBoth();
            GUI.enabled = IsRunning(hostProcess) || IsRunning(clientProcess) || clientLaunchAt >= 0;
            if (GUILayout.Button("Stop")) StopAll();
            GUI.enabled = true;
        }

        if (clientLaunchAt >= 0)
        {
            double remaining = Math.Max(0, clientLaunchAt - EditorApplication.timeSinceStartup);
            EditorGUILayout.HelpBox($"Client sẽ chạy sau {remaining:0.0} giây.", MessageType.None);
        }

        DrawProcess("Host", hostStatus, hostLog);
        DrawProcess("Client", clientStatus, clientLog);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory);
            if (GUILayout.Button("Open Log Folder")) EditorUtility.RevealInFinder(logDirectory);
            GUI.enabled = true;
            if (GUILayout.Button("Refresh Logs")) RefreshLogs();
        }
    }

    private void DrawProcess(string label, string status, string log)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"{label}: {status}", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(string.IsNullOrEmpty(log) ? "No multiplayer log yet." : log,
            GUILayout.MinHeight(90));
    }

    private void Build()
    {
        try
        {
            MultiplayerPrototypeBuild.BuildWindowsGameplayTest();
        }
        catch (Exception error)
        {
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
        Repaint();
    }

    private void StartHost(bool createLogDirectory)
    {
        if (!ValidateLaunch()) return;
        if (createLogDirectory) CreateLogDirectory();
        hostProcess = Launch("host", HostLogPath);
        SavePid(HostPidKey, hostProcess);
        RefreshLogs();
    }

    private void StartClient(bool createLogDirectory)
    {
        if (!ValidateLaunch()) return;
        if (createLogDirectory || string.IsNullOrEmpty(logDirectory)) CreateLogDirectory();
        clientProcess = Launch("client", ClientLogPath);
        SavePid(ClientPidKey, clientProcess);
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
        hostLog = ReadRelevantLog(HostLogPath, hostLogTail);
        clientLog = ReadRelevantLog(ClientLogPath, clientLogTail);
        hostStatus = GetStatus(hostProcess, hostLog);
        clientStatus = GetStatus(clientProcess, clientLog);
    }

    private static string ReadRelevantLog(string path, LogTailState state)
    {
        if (!string.Equals(state.Path, path, StringComparison.OrdinalIgnoreCase))
            state.Reset(path);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return state.Text;

        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                       FileShare.ReadWrite | FileShare.Delete))
            {
                if (!state.Initialized)
                {
                    state.Position = Math.Max(0, stream.Length - InitialLogTailBytes);
                    state.SkipFirstPartialLine = state.Position > 0;
                    state.Initialized = true;
                }
                else if (stream.Length < state.Position)
                {
                    state.Reset(path);
                    state.Initialized = true;
                }

                stream.Position = state.Position;
                var buffer = new byte[16 * 1024];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    state.Position += bytesRead;
                    state.Append(buffer, bytesRead);
                }
            }

            return state.Text;
        }
        catch (IOException)
        {
            return "Log is currently locked by the player.";
        }
    }

    private static bool IsRelevant(string line)
    {
        return line.IndexOf("[MP-GAMEPLAY]", StringComparison.OrdinalIgnoreCase) >= 0 ||
               line.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) >= 0 ||
               line.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetStatus(Process process, string log)
    {
        if (process == null) return "Stopped";
        if (!IsRunning(process)) return "Exited";
        if (ContainsFailure(log)) return "ERROR";
        if (log.IndexOf("[MP-GAMEPLAY] READY sequence=", StringComparison.Ordinal) >= 0) return "READY";
        return "Starting / waiting for peer";
    }

    private static bool ContainsFailure(string log)
    {
        string[] markers =
        {
            "timeout", "mismatch", "failed", "Cannot start", "disconnected",
            "unloaded", "Another NetworkManager", "Expected -mp-gameplay-role", "Exception"
        };
        foreach (string marker in markers)
            if (log.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    private void StopAll()
    {
        clientLaunchAt = -1;
        Stop(ref clientProcess, ClientPidKey);
        Stop(ref hostProcess, HostPidKey);
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
        hostLogTail.Reset(HostLogPath);
        clientLogTail.Reset(ClientLogPath);
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

    private sealed class LogTailState
    {
        private readonly Queue<string> lines = new Queue<string>(12);
        private readonly StringBuilder partialLine = new StringBuilder();
        private readonly char[] characterBuffer = new char[16 * 1024];
        private Decoder decoder = Encoding.UTF8.GetDecoder();

        public string Path { get; private set; } = string.Empty;
        public long Position { get; set; }
        public bool Initialized { get; set; }
        public bool SkipFirstPartialLine { get; set; }
        public string Text { get; private set; } = string.Empty;

        public void Reset(string path)
        {
            Path = path ?? string.Empty;
            Position = 0;
            Initialized = false;
            SkipFirstPartialLine = false;
            Text = string.Empty;
            lines.Clear();
            partialLine.Clear();
            decoder = Encoding.UTF8.GetDecoder();
        }

        public void Append(byte[] buffer, int count)
        {
            int characterCount = decoder.GetChars(buffer, 0, count, characterBuffer, 0, false);
            for (int index = 0; index < characterCount; index++)
            {
                char character = characterBuffer[index];
                if (character != '\n')
                {
                    partialLine.Append(character);
                    continue;
                }

                string line = partialLine.ToString().TrimEnd('\r');
                partialLine.Clear();
                if (SkipFirstPartialLine)
                {
                    SkipFirstPartialLine = false;
                    continue;
                }

                if (!IsRelevant(line)) continue;
                if (lines.Count == 12) lines.Dequeue();
                lines.Enqueue(line);
            }

            var result = new StringBuilder();
            foreach (string line in lines) result.AppendLine(line);
            Text = result.ToString().TrimEnd();
        }
    }
}
