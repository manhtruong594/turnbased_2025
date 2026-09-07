using System;
using System.IO;
using TurnBasedGame.Multiplayer.Prototype;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MultiplayerPrototypeBuild
{
    public const string ScenePath = "Assets/Scenes/MultiplayerPrototype.unity";
    private const string DefaultOutput = "Builds/MultiplayerPrototype/MultiplayerPrototype.exe";

    [MenuItem("Tools/Multiplayer/Create Prototype Scene")]
    public static void CreateScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        NewSceneMode mode = SceneManager.sceneCount == 1 && string.IsNullOrEmpty(activeScene.path)
            ? NewSceneMode.Single
            : NewSceneMode.Additive;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
        scene.name = "MultiplayerPrototype";
        var root = new GameObject("PrototypeNetworkManager");
        var transport = root.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        var networkManager = root.AddComponent<Unity.Netcode.NetworkManager>();
        networkManager.NetworkConfig = new Unity.Netcode.NetworkConfig
        {
            NetworkTransport = transport,
            EnableSceneManagement = false
        };
        root.AddComponent<MultiplayerConnectionPrototype>();
        SceneManager.MoveGameObjectToScene(root, scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MP-PROTOTYPE] Đã tạo scene {ScenePath}.");
    }

    [MenuItem("Tools/Multiplayer/Build Windows Prototype")]
    public static void BuildWindowsPrototype()
    {
        Build(DefaultOutput);
    }

    public static void BuildFromCommandLine()
    {
        string output = ReadArgument("-mp-output") ?? DefaultOutput;
        Build(output);
    }

    private static void Build(string output)
    {
        if (!File.Exists(ScenePath))
            CreateScene();

        string outputDirectory = Path.GetDirectoryName(output);
        if (!string.IsNullOrEmpty(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Prototype build thất bại: {report.summary.result}");

        Debug.Log($"[MP-PROTOTYPE] BUILD_PASS output={Path.GetFullPath(output)}");
    }

    private static string ReadArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}
