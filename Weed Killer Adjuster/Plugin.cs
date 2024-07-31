using BepInEx;
using HarmonyLib;
using OniroDev.patch;
using Unity.Netcode;

namespace OniroDev;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class WeedKillerAdjusterBase : BaseUnityPlugin
{

    internal static CustomMessagingManager MessageManager => NetworkManager.Singleton.CustomMessagingManager;
    internal static bool IsClient => NetworkManager.Singleton.IsClient;
    internal static bool IsHost => NetworkManager.Singleton.IsHost;
    internal static bool IsSynced = false;
    public static WeedKillerAdjusterBase Instance { get; set; }

    private readonly Harmony _harmony = new(PluginInfo.PLUGIN_GUID);

    public WeedKillerAdjusterBase()
    {
        Instance = this;
    }
    internal static WeedKillerConfig BoundConfig { get; private set; } = null!;
    private void Awake()
    {

        WeedKillerAdjusterLogger.Initialize(PluginInfo.PLUGIN_GUID);
        WeedKillerAdjusterLogger.Log($"WEEDKILLERADJUSTER MOD STARTING UP {PluginInfo.PLUGIN_GUID}");
        
        BoundConfig = new WeedKillerConfig(base.Config);
        
        WeedKillerAdjusterLogger.Log($"Applying patches...");
        ApplyPluginPatch();
        WeedKillerAdjusterLogger.Log($"Patches applied");
        
    }
    private void ApplyPluginPatch()
    {
        _harmony.PatchAll(typeof(WeedKillerAdjusterBase));
        _harmony.PatchAll(typeof(WeedKillerPatch));
        _harmony.PatchAll(typeof(WeedKillerConfig));
    }
}
