using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;

public static class MultiplayerTestLogParser
{
    private static readonly Regex AuditRegex = new Regex(
        @"\[MP-AUDIT\]\s+match=(?<match>[^\s]+)\s+player=(?<player>\d+)\s+command=(?<cmd>-?\d+)\s+turn=(?<turn>\d+)\s+sequence=(?<seq>\d+)\s+reason=(?<reason>\w+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ReadyRegex = new Regex(
        @"\[MP-GAMEPLAY\]\s+READY\s+sequence=(?<seq>\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryParseAudit(string line, out AuditLogData audit)
    {
        audit = null;
        if (string.IsNullOrEmpty(line) || line.IndexOf("[MP-AUDIT]", StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        Match match = AuditRegex.Match(line);
        if (!match.Success) return false;

        audit = new AuditLogData
        {
            MatchId = match.Groups["match"].Value,
            Player = int.TryParse(match.Groups["player"].Value, out int p) ? p : 0,
            CommandId = long.TryParse(match.Groups["cmd"].Value, out long cmd) ? cmd : 0,
            Turn = int.TryParse(match.Groups["turn"].Value, out int t) ? t : 0,
            Sequence = ulong.TryParse(match.Groups["seq"].Value, out ulong seq) ? seq : 0,
            Reason = match.Groups["reason"].Value
        };
        return true;
    }

    public static bool TryParseReady(string line, out ulong sequence)
    {
        sequence = 0;
        if (string.IsNullOrEmpty(line) || line.IndexOf("[MP-GAMEPLAY] READY", StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        Match match = ReadyRegex.Match(line);
        if (!match.Success) return false;

        return ulong.TryParse(match.Groups["seq"].Value, out sequence);
    }

    public static LogCategory Classify(string line, AuditLogData audit)
    {
        if (string.IsNullOrEmpty(line)) return LogCategory.All;

        if (audit != null)
        {
            return audit.IsSuccess ? LogCategory.Audit : LogCategory.Warning;
        }

        if (line.IndexOf("[MP-GAMEPLAY] READY", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("[MP-GAMEPLAY] BUILD_PASS", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("[MP-PROTOTYPE] BUILD_PASS", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return LogCategory.Milestone;
        }

        if (line.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("Gameplay peer disconnected", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("MP_COMMAND_RATE_LIMIT", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("MP_CONTROL_RATE_LIMIT", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("Cannot start gameplay transport", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return LogCategory.Error;
        }

        if (line.IndexOf("[MP-RESYNC]", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("Snapshot retry", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("warning", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("mismatch", StringComparison.OrdinalIgnoreCase) >= 0 ||
            line.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return LogCategory.Warning;
        }

        if (line.IndexOf("[MP-AUDIT]", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return LogCategory.Audit;
        }

        return LogCategory.All;
    }

    public static bool TryGetDiagnostic(string line, AuditLogData audit, out string title, out string cause, out string suggestion, out MessageType severity)
    {
        title = string.Empty;
        cause = string.Empty;
        suggestion = string.Empty;
        severity = MessageType.Info;

        if (audit != null && !audit.IsSuccess)
        {
            severity = MessageType.Warning;
            switch (audit.Reason)
            {
                case "NotEnoughMP":
                    title = $"Lệnh #{audit.CommandId} bị từ chối: Không đủ MP";
                    cause = $"Player {audit.Player} phát lệnh lúc Turn {audit.Turn} nhưng lượng MP hiện tại không đủ.";
                    suggestion = "Kiểm tra chi phí MP trên UI hoặc logic trừ MP trước khi cho phép phát lệnh.";
                    return true;

                case "WrongTurn":
                case "StaleTurn":
                    title = $"Lệnh #{audit.CommandId} bị từ chối: Sai lượt ({audit.Reason})";
                    cause = $"Lệnh gửi với ExpectedTurn={audit.Turn} không khớp lượt hiện tại trên Host hoặc đã quá hạn.";
                    suggestion = "Kiểm tra timer của lượt hoặc cấm input của người chơi khi chưa đến lượt.";
                    return true;

                case "RateLimited":
                    title = $"Cảnh báo bảo mật: Vượt giới hạn message (RateLimited)";
                    cause = $"Player {audit.Player} gửi message vượt mức token bucket (Burst 20, hồi 10 msg/s).";
                    suggestion = "Kiểm tra spam click hoặc coroutine/loop phát lệnh liên tục từ Client.";
                    severity = MessageType.Error;
                    return true;

                case "InvalidSequence":
                    title = $"Lệch sequence đồng bộ (InvalidSequence)";
                    cause = $"Client gửi sequence={audit.Sequence} không khớp thứ tự mà Host đang chờ.";
                    suggestion = "Hệ thống sẽ tự động kích hoạt Resync để khôi phục snapshot mới nhất.";
                    return true;

                case "InvalidPayload":
                case "InvalidCommand":
                    title = $"Lệnh không hợp lệ ({audit.Reason})";
                    cause = "DTO không vượt qua kiểm tra cấu trúc (vượt 1024 bytes, tọa độ ngoài biên hoặc enum sai).";
                    suggestion = "Kiểm tra tính tương thích giữa Client DTO và Host validation rules.";
                    return true;

                default:
                    title = $"Lệnh #{audit.CommandId} bị từ chối ({audit.Reason})";
                    cause = $"Host từ chối thực thi lệnh của Player {audit.Player} tại Turn {audit.Turn} (Sequence={audit.Sequence}).";
                    suggestion = "Kiểm tra log chi tiết trong console để xác định điều kiện vi phạm.";
                    return true;
            }
        }

        if (!string.IsNullOrEmpty(line))
        {
            if (line.IndexOf("Gameplay peer disconnected", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                title = "Mất kết nối LAN (Peer Disconnected)";
                cause = "Process Client đã tắt hoặc bị ngắt kết nối. Host đã dọn dẹp và kết thúc trận an toàn.";
                suggestion = "Khởi động lại Client bằng nút 'Relaunch Client' hoặc 'Start Both' để test trận mới.";
                severity = MessageType.Warning;
                return true;
            }

            if (line.IndexOf("Replica state hash mismatch", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                title = "Lệch State Hash (State Desync Mismatch)";
                cause = "Sau khi apply snapshot, hash của Client không khớp với hash gốc từ Host.";
                suggestion = "Kiểm tra các trường dữ liệu gameplay chưa được serialize đồng nhất trong MatchSnapshotProtocol.";
                severity = MessageType.Error;
                return true;
            }

            if (line.IndexOf("MP_COMMAND_RATE_LIMIT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("MP_CONTROL_RATE_LIMIT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                title = "Peer bị ngắt kết nối do vượt giới hạn Rate Limit";
                cause = "Client gửi message vượt ngân sách cho phép, Host đã chủ động ngắt socket để bảo vệ tài nguyên.";
                suggestion = "Kiểm tra tần suất phát command hoặc handshake control message.";
                severity = MessageType.Error;
                return true;
            }

            if (line.IndexOf("Cannot start gameplay transport", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                title = "Không thể khởi động mạng (Socket Error)";
                cause = "Không thể bind IP hoặc Port (có thể trùng Port 27982 với process khác đang chạy ngầm).";
                suggestion = "Nhấn 'Stop All' để kill các process cũ hoặc đổi sang Port khác.";
                severity = MessageType.Error;
                return true;
            }
        }

        return false;
    }
}

