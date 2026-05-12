#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class UnityMcpBridgeRestarter
{
    private const string RanSessionKey = "Nightfall.UnityMcpBridgeRestarter.HardResetV3.Ran";

    [InitializeOnLoadMethod]
    private static void RestartOnceAfterReload()
    {
        if (SessionState.GetBool(RanSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(RanSessionKey, true);
        EditorApplication.delayCall += HardResetBridgeAndAcp;
    }

    [MenuItem("Tools/TPS/Nightfall/Restart Unity MCP Bridge")]
    public static void RestartBridge()
    {
        RestartBridge(clearAcpState: false);
    }

    [MenuItem("Tools/TPS/Nightfall/Hard Reset Unity MCP + ACP")]
    public static void HardResetBridgeAndAcp()
    {
        RestartBridge(clearAcpState: true);
    }

    private static void RestartBridge(bool clearAcpState)
    {
        Type bridgeType = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType("Unity.AI.MCP.Editor.UnityMCPBridge"))
            .FirstOrDefault(type => type != null);

        if (bridgeType == null)
        {
            Debug.LogWarning("Unity MCP bridge type was not found. Confirm the Unity AI Assistant package is installed.");
            return;
        }

        BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        bridgeType.GetMethod("Stop", flags)?.Invoke(null, null);

        if (clearAcpState)
        {
            ClearAcpRuntimeState();
        }

        bridgeType.GetProperty("Enabled", flags)?.SetValue(null, true);
        bridgeType.GetMethod("Start", flags)?.Invoke(null, null);
        bridgeType.GetMethod("PrintClientInfo", flags)?.Invoke(null, null);

        Debug.Log(clearAcpState
            ? "Nightfall hard-reset Unity MCP bridge and ACP runtime state."
            : "Nightfall requested Unity MCP bridge restart.");
    }

    private static void ClearAcpRuntimeState()
    {
        BindingFlags instanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        BindingFlags staticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        Type trackerType = FindType("Unity.AI.Assistant.Editor.Acp.AcpSessionTracker");
        object trackerInstance = trackerType?.GetProperty("instance", staticFlags)?.GetValue(null);
        if (trackerInstance != null)
        {
            trackerType?.GetMethod("Clear", instanceFlags)?.Invoke(trackerInstance, null);
        }

        Type registryType = FindType("Unity.AI.Assistant.Editor.Acp.AcpSessionRegistry");
        object endAllTask = registryType?.GetMethod("EndAllAsync", staticFlags)?.Invoke(null, null);
        if (endAllTask is Task task)
        {
            task.ContinueWith(completed =>
            {
                if (completed.Exception != null)
                {
                    Debug.LogWarning($"Nightfall ACP session cleanup reported: {completed.Exception.GetBaseException().Message}");
                }
            });
        }

        string[] knownStaleAgentSessions =
        {
            "019e19ad-f551-7d13-937c-7c1f95a7fae2",
            "019e1a2f-6e1e-70a2-a4af-4935af287423"
        };

        foreach (string agentSessionId in knownStaleAgentSessions)
        {
            SessionState.EraseString($"AcpSession.Initialized.{agentSessionId}");
        }
    }

    private static Type FindType(string typeName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(typeName))
            .FirstOrDefault(type => type != null);
    }
}
#endif
