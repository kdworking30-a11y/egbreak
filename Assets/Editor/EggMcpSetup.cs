// Editor-only local MCP setup for this project. No player-build dependency.
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;

[InitializeOnLoad]
public static class EggMcpSetup
{
    private const string DoneKey = "Egg.McpSetup.Started";
    static EggMcpSetup()
    {
        if (!Application.isBatchMode && !SessionState.GetBool(DoneKey, false))
            EditorApplication.delayCall += Start;
    }

    [MenuItem("Tools/Egg/Connect Local Unity MCP")]
    public static async void Start()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Start;
            return;
        }
        SessionState.SetBool(DoneKey, true);
        try
        {
            EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
            EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
            EditorPrefs.SetString("MCPForUnity.UvxPath", "/opt/homebrew/bin/uvx");
            EditorConfigurationCache.Instance.SetUseHttpTransport(true);
            if (!MCPServiceLocator.Server.IsLocalHttpServerReachable() &&
                !MCPServiceLocator.Server.StartLocalHttpServer(quiet: true))
            {
                Debug.LogError("[Egg MCP] Local server could not start. Open Window > MCP for Unity.");
                return;
            }
            for (int i = 0; i < 120; i++)
            {
                if (MCPServiceLocator.Server.IsLocalHttpServerReachable())
                {
                    bool connected = await MCPServiceLocator.Bridge.StartAsync();
                    Debug.Log(connected ? "[Egg MCP] Connected: " + Application.dataPath : "[Egg MCP] Bridge failed to connect.");
                    return;
                }
                await Task.Delay(500);
            }
            Debug.LogError("[Egg MCP] Server startup timed out. Open Window > MCP for Unity.");
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }
}
