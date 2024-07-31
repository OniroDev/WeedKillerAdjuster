using HarmonyLib;
using UnityEngine;

namespace OniroDev.patch
{
    [HarmonyPatch(typeof(SprayPaintItem))]
    public class WeedKillerPatch
    {
        private static AccessTools.FieldRef<SprayPaintItem, float> sprayPaintTank =
            AccessTools.FieldRefAccess<SprayPaintItem, float>("sprayCanTank");

        private static float tempCanTank;

        [HarmonyPatch(nameof(SprayPaintItem.Start))]
        [HarmonyPostfix]
        public static void WeedKillerKillRatePatch(ref float ___killWeedSpeed)
        {
            WeedKillerAdjusterLogger.Log($"weedkillerKillRatePatch: {WeedKillerConfig.weedKillRate.Value}");
            ___killWeedSpeed *= WeedKillerConfig.weedKillRate.Value;
        }

        [HarmonyPatch(typeof(GrabbableObject), "ChargeBatteries")]
        [HarmonyPrefix]
        private static void WeedKillerChargeBatteries(SprayPaintItem __instance)
        {
            if (__instance.isWeedKillerSprayBottle && WeedKillerConfig.usesBattery.Value)
            {
                sprayPaintTank(__instance) = __instance.insertedBattery.charge;
            }
        }

        [HarmonyPatch(typeof(SprayPaintItem), "Start")]
        [HarmonyPostfix]
        private static void Start(SprayPaintItem __instance)
        {
            
            if (__instance.isWeedKillerSprayBottle && WeedKillerConfig.usesBattery.Value)
            {
                __instance.insertedBattery = new Battery(false, sprayPaintTank(__instance));
                __instance.itemProperties.requiresBattery = true;
            }
        }

        [HarmonyPatch(typeof(SprayPaintItem), "LateUpdate")]
        [HarmonyPrefix]
        private static void PreWeedKillerLateUpdate(SprayPaintItem __instance)
        {
            tempCanTank = sprayPaintTank(__instance);
        }

        [HarmonyPatch(typeof(SprayPaintItem), "LateUpdate")]
        [HarmonyPostfix]
        private static void PostWeedKillerLateUpdate(SprayPaintItem __instance, ref bool ___isSpraying)
        {
            if (__instance.isWeedKillerSprayBottle)
            {
                if (__instance.isHeld && ___isSpraying)
                {
                    sprayPaintTank(__instance) = Mathf.Max(tempCanTank - Time.deltaTime / (15f * WeedKillerConfig.weedKillTank.Value), 0f);
                }
                if (WeedKillerConfig.usesBattery.Value)
                {
                    __instance.insertedBattery.charge = sprayPaintTank(__instance);
                }
            }
        }
    }
}
