using System;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TurnBasedGame.Multiplayer.Protocol;

namespace TurnBasedGame.Multiplayer.Prototype
{
    /// <summary>
    /// Prototype biệt lập để kiểm tra NGO + UTP giữa hai process.
    /// Không chứa hoặc thay đổi gameplay state.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class MultiplayerConnectionPrototype : MonoBehaviour
    {
        private const string LogPrefix = "[MP-PROTOTYPE]";
        private const string PingMessage = "prototype-ping-v1";
        private const string AckMessage = "prototype-ack-v1";
        private const string DoneMessage = "prototype-done-v1";
        private const string DoneAckMessage = "prototype-done-ack-v1";
        private const int Nonce = 20260907;
        private const float TimeoutSeconds = 20f;

        private NetworkManager _networkManager;
        private MatchCompatibility _compatibility;
        private bool _isHost;
        private bool _completionReceived;
        private float _deadline;
        private float _quitAt = -1f;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);

            if (!TryReadRole(out _isHost))
            {
                Debug.LogError($"{LogPrefix} FAIL: thiếu -mp-role host|client.");
                Quit(2);
                return;
            }

            ushort port = ReadPort();
            var catalog = new MatchContentRegistry(UnityEngine.Resources.Load<MatchContentCatalog>(MatchContentCatalog.ResourceName));
            _compatibility = new MatchCompatibility { ContentCatalogHash = catalog.ContentCatalogHash };
            var transport = GetComponent<UnityTransport>();
            _networkManager = GetComponent<NetworkManager>();
            if (transport == null || _networkManager == null || _networkManager.NetworkConfig == null)
            {
                Debug.LogError($"{LogPrefix} FAIL: scene thiếu NetworkManager/UnityTransport đã serialize.");
                Quit(3);
                return;
            }

            _networkManager.NetworkConfig.EnableSceneManagement = false;
            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;

            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            bool started = _isHost
                ? _networkManager.StartHost()
                : _networkManager.StartClient();

            if (!started)
            {
                Debug.LogError($"{LogPrefix} FAIL: không thể khởi động {(_isHost ? "host" : "client")}.");
                Quit(4);
                return;
            }

            if (_isHost)
            {
                _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(PingMessage, OnPing);
                _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(DoneMessage, OnDone);
                Debug.Log($"{LogPrefix} HOST_READY port={port}");
            }
            else
            {
                _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(AckMessage, OnAck);
                _networkManager.CustomMessagingManager.RegisterNamedMessageHandler(DoneAckMessage, OnDoneAck);
                Debug.Log($"{LogPrefix} CLIENT_CONNECTING port={port}");
            }

            _deadline = Time.realtimeSinceStartup + TimeoutSeconds;
        }

        private void Update()
        {
            if (_quitAt >= 0f && Time.realtimeSinceStartup >= _quitAt)
            {
                Quit(0);
                return;
            }

            if (_deadline > 0f && Time.realtimeSinceStartup >= _deadline)
            {
                Debug.LogError($"{LogPrefix} FAIL: timeout sau {TimeoutSeconds:0} giây.");
                ShutdownAndQuit(4);
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (_isHost)
            {
                if (clientId != NetworkManager.ServerClientId)
                    Debug.Log($"{LogPrefix} HOST_CLIENT_CONNECTED clientId={clientId}");
                return;
            }

            if (clientId != _networkManager.LocalClientId)
                return;

            Debug.Log($"{LogPrefix} CLIENT_CONNECTED clientId={clientId}");
            SendCompatibility(PingMessage, NetworkManager.ServerClientId);
        }

        private void OnPing(ulong senderClientId, FastBufferReader reader)
        {
            if (!ValidateCompatibility(reader))
            {
                Debug.LogError($"{LogPrefix} FAIL: ping payload không hợp lệ.");
                ShutdownAndQuit(5);
                return;
            }

            Debug.Log($"{LogPrefix} HOST_RECEIVED_PING clientId={senderClientId}");
            SendCompatibility(AckMessage, senderClientId);
        }

        private void OnAck(ulong senderClientId, FastBufferReader reader)
        {
            if (senderClientId != NetworkManager.ServerClientId || !ValidateCompatibility(reader))
            {
                Debug.LogError($"{LogPrefix} FAIL: ack không hợp lệ.");
                ShutdownAndQuit(6);
                return;
            }

            Debug.Log($"{LogPrefix} CLIENT_RECEIVED_ACK");
            SendInt(DoneMessage, NetworkManager.ServerClientId, Nonce);
        }

        private void OnDone(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int nonce);
            if (nonce != Nonce)
            {
                Debug.LogError($"{LogPrefix} FAIL: done payload không hợp lệ.");
                ShutdownAndQuit(7);
                return;
            }

            _completionReceived = true;
            Debug.Log($"{LogPrefix} HOST_RECEIVED_DONE clientId={senderClientId}");
            SendInt(DoneAckMessage, senderClientId, nonce);
        }

        private void OnDoneAck(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int nonce);
            if (senderClientId != NetworkManager.ServerClientId || nonce != Nonce)
            {
                Debug.LogError($"{LogPrefix} FAIL: done ack không hợp lệ.");
                ShutdownAndQuit(8);
                return;
            }

            Debug.Log($"{LogPrefix} CLIENT_PASS: message round-trip hoàn tất; đang ngắt kết nối sạch.");
            _deadline = 0f;
            _networkManager.Shutdown();
            _quitAt = Time.realtimeSinceStartup + 0.5f;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!_isHost || clientId == NetworkManager.ServerClientId)
                return;

            if (!_completionReceived)
            {
                Debug.LogError($"{LogPrefix} FAIL: client ngắt trước khi hoàn tất handshake.");
                ShutdownAndQuit(9);
                return;
            }

            Debug.Log($"{LogPrefix} HOST_PASS: message round-trip và ngắt kết nối sạch.");
            _deadline = 0f;
            _networkManager.Shutdown();
            _quitAt = Time.realtimeSinceStartup + 0.5f;
        }

        private void SendInt(string messageName, ulong clientId, int value)
        {
            using var writer = new FastBufferWriter(sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(value);
            _networkManager.CustomMessagingManager.SendNamedMessage(
                messageName,
                clientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        private void SendCompatibility(string messageName, ulong clientId)
        {
            byte[] payload = MatchProtocol.SerializeCompatibility(_compatibility);
            using var writer = new FastBufferWriter(payload.Length, Allocator.Temp);
            writer.WriteBytesSafe(payload);
            _networkManager.CustomMessagingManager.SendNamedMessage(messageName, clientId, writer,
                NetworkDelivery.ReliableSequenced);
        }

        private bool ValidateCompatibility(FastBufferReader reader)
        {
            if (reader.Length != 70)
            {
                Debug.LogError($"{LogPrefix} REJECT: InvalidPayload");
                return false;
            }
            var payload = new byte[70];
            reader.ReadBytesSafe(ref payload, payload.Length);
            MatchCompatibility peer;
            try { peer = MatchProtocol.DeserializeCompatibility(payload); }
            catch (ArgumentException)
            {
                Debug.LogError($"{LogPrefix} REJECT: InvalidPayload");
                return false;
            }
            var reason = _compatibility.Compare(peer);
            if (reason != CommandReason.None) Debug.LogError($"{LogPrefix} REJECT: {reason}");
            return reason == CommandReason.None;
        }

        private void ShutdownAndQuit(int exitCode)
        {
            _deadline = 0f;
            if (_networkManager != null && _networkManager.IsListening)
                _networkManager.Shutdown();
            Quit(exitCode);
        }

        private static bool TryReadRole(out bool isHost)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "-mp-role", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(args[i + 1], "host", StringComparison.OrdinalIgnoreCase))
                {
                    isHost = true;
                    return true;
                }

                if (string.Equals(args[i + 1], "client", StringComparison.OrdinalIgnoreCase))
                {
                    isHost = false;
                    return true;
                }
            }

            isHost = false;
            return false;
        }

        private static ushort ReadPort()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-mp-port", StringComparison.OrdinalIgnoreCase) &&
                    ushort.TryParse(args[i + 1], out ushort port))
                {
                    return port;
                }
            }

            return 7979;
        }

        private static void Quit(int exitCode)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit(exitCode);
#endif
        }
    }
}
