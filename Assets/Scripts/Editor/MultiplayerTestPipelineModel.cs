using System;
using UnityEditor;

public enum PipelineStage
{
    Unstarted,
    Building,
    ProcessesRunning,
    HandshakeReady,
    InMatch,
    Finished
}

public enum SyncHealthStatus
{
    Healthy,
    Resyncing,
    Fault
}

public enum LogCategory
{
    All,
    Milestone,
    Audit,
    Warning,
    Error
}

[Serializable]
public sealed class AuditLogData
{
    public string MatchId = string.Empty;
    public int Player;
    public long CommandId;
    public int Turn;
    public ulong Sequence;
    public string Reason = "None";

    public bool IsSuccess => string.Equals(Reason, "None", StringComparison.OrdinalIgnoreCase);
}

[Serializable]
public sealed class MultiplayerLogEntry
{
    public DateTime Timestamp;
    public bool IsHost;
    public LogCategory Category;
    public string Message;
    public string RawLine;
    public AuditLogData Audit;

    public MultiplayerLogEntry(DateTime timestamp, bool isHost, LogCategory category, string message, string rawLine, AuditLogData audit = null)
    {
        Timestamp = timestamp;
        IsHost = isHost;
        Category = category;
        Message = message;
        RawLine = rawLine;
        Audit = audit;
    }
}

public sealed class ParticipantState
{
    public int Pid;
    public bool IsAlive;
    public bool IsReady;
    public string StatusText = "Stopped";
    public ulong LastSequence;
    public int CurrentTurn;
    public int ActivePlayer;
    public int ResyncCount;

    public void Reset()
    {
        Pid = 0;
        IsAlive = false;
        IsReady = false;
        StatusText = "Stopped";
        LastSequence = 0;
        CurrentTurn = 0;
        ActivePlayer = 0;
        ResyncCount = 0;
    }
}

public sealed class DiagnosticInfo
{
    public bool HasActiveError { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string RootCause { get; private set; } = string.Empty;
    public string Suggestion { get; private set; } = string.Empty;
    public MessageType Severity { get; private set; } = MessageType.Info;

    public void Clear()
    {
        HasActiveError = false;
        Title = string.Empty;
        RootCause = string.Empty;
        Suggestion = string.Empty;
        Severity = MessageType.Info;
    }

    public void Set(string title, string rootCause, string suggestion, MessageType severity)
    {
        HasActiveError = true;
        Title = title;
        RootCause = rootCause;
        Suggestion = suggestion;
        Severity = severity;
    }
}

