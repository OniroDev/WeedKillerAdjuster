using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;

namespace OniroDev
{
    [Serializable]
    class WeedKillerConfig : SyncedInstance<WeedKillerConfig>
    {
        public const float DEFAULT_KILL_RATE = 1f;
        public const bool DEFAULT_CHARGING = false;
        public static ConfigEntry<float> weedKillRate;
        public static ConfigEntry<float> weedKillTank;
        public static ConfigEntry<bool> usesBattery;

        public WeedKillerConfig(ConfigFile configFile)
        {
            InitInstance(this);
            configFile.SaveOnConfigSet = false;
            weedKillRate = configFile.Bind<float>("Weed Killer Settings",
                                                "Vain Shroud Kill Rate",
                                                DEFAULT_KILL_RATE,
                                                "Adjust the kill rate at which Vain Shrouds will die." +
                                                "\nIf you change the rate to 7, they will die 7 times faster than normal.");
            WeedKillerAdjusterLogger.Log($"weedKillRate; VALUE LOAD FROM CONFIG: {weedKillRate.Value}");
            weedKillTank = configFile.Bind<float>("Weed Killer Settings",
                                                "Weed Killer Base Fuel Tank",
                                                DEFAULT_KILL_RATE,
                                                "Adjust the fuel capacity of the Weed Killer spray bottle" +
                                                "\nIf you change the rate to 7, they will contain 7 times more spray fuel than normal.");
            WeedKillerAdjusterLogger.Log($"weedKillTank; VALUE LOAD FROM CONFIG: {weedKillTank.Value}");
            usesBattery = configFile.Bind<bool>("Weed Killer Settings",
                                                "Use battery charges",
                                                DEFAULT_CHARGING,
                                                "Enables the Weed Killer spray bottle to use batteries and be rechargeable in the company ship" +
                                                "\nIf set to TRUE, the value for the Weed Killer Base Fuel Tank is ignored");
            WeedKillerAdjusterLogger.Log($"usesBattery; VALUE LOAD FROM CONFIG: {usesBattery.Value}");

            ClearOrphanedEntries(configFile);
            configFile.Save();
            configFile.SaveOnConfigSet = true;
        }

        static void ClearOrphanedEntries(ConfigFile configFile)
        {
            PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(configFile);
            orphanedEntries.Clear();
        }

        public static void RequestSync()
        {
            if (!IsClient) return;

            using FastBufferWriter stream = new(IntSize, Allocator.Temp);
            MessageManager.SendNamedMessage($"{PluginInfo.PLUGIN_GUID}_OnRequestConfigSync", 0uL, stream);
        }

        public static void OnRequestSync(ulong clientId, FastBufferReader _)
        {
            if (!IsHost) return;

            WeedKillerAdjusterLogger.Log($"Config sync request received from client: {clientId}");

            byte[] array = SerializeToBytes(Instance);
            int value = array.Length;

            using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

            try
            {
                stream.WriteValueSafe(in value, default);
                stream.WriteBytesSafe(array);

                MessageManager.SendNamedMessage($"{PluginInfo.PLUGIN_GUID}_OnReceiveConfigSync", clientId, stream);
            }
            catch (Exception e)
            {
                WeedKillerAdjusterLogger.Log($"Error occurred syncing config with client: {clientId}\n{e}");
            }
        }

        public static void OnReceiveSync(ulong _, FastBufferReader reader)
        {
            if (!reader.TryBeginRead(IntSize))
            {
                WeedKillerAdjusterLogger.LogError("Config sync error: Could not begin reading buffer.");
                return;
            }

            reader.ReadValueSafe(out int val, default);
            if (!reader.TryBeginRead(val))
            {
                WeedKillerAdjusterLogger.LogError("Config sync error: Host could not sync.");
                return;
            }

            byte[] data = new byte[val];
            reader.ReadBytesSafe(ref data, val);

            SyncInstance(data);

            WeedKillerAdjusterLogger.Log("Successfully synced config with host.");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        public static void InitializeLocalPlayer()
        {
            if (IsHost)
            {
                MessageManager.RegisterNamedMessageHandler($"{PluginInfo.PLUGIN_GUID}_OnRequestConfigSync", OnRequestSync);
                Synced = true;

                return;
            }

            Synced = false;
            MessageManager.RegisterNamedMessageHandler($"{PluginInfo.PLUGIN_GUID}_OnReceiveConfigSync", OnReceiveSync);
            RequestSync();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
        public static void PlayerLeave()
        {
            WeedKillerConfig.RevertSync();
        }

    }
}
