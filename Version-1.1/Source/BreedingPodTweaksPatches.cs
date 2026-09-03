using HarmonyLib;
using Timberborn.AutomationBuildings;
using Timberborn.BlockingSystem;

namespace Calloatti.BreedingPodTweaks
{
  [HarmonyPatch(typeof(BlockableObject))]
  public static class BlockableObjectPatch
  {
    public static bool IsEnabled = false;

    [HarmonyPatch(nameof(BlockableObject.Block))]
    [HarmonyPrefix]
    public static bool BlockPrefix(BlockableObject __instance, object blocker)
    {
      if (!IsEnabled) return true;
      var tweaks = __instance.GetComponent<BreedingPodTweaks>();
      if (tweaks == null) return true;
      return tweaks._evaluatingLocally;
    }

    [HarmonyPatch(nameof(BlockableObject.Unblock))]
    [HarmonyPrefix]
    public static bool UnblockPrefix(BlockableObject __instance, object blocker)
    {
      if (!IsEnabled) return true;
      var tweaks = __instance.GetComponent<BreedingPodTweaks>();
      if (tweaks == null) return true;
      return tweaks._evaluatingLocally;
    }
  }

  [HarmonyPatch(typeof(PausableBuildingTerminal))]
  public static class PausableBuildingTerminalPatch
  {
    [HarmonyPatch("UpdateBlockable")]
    [HarmonyPrefix]
    public static bool UpdateBlockablePrefix(PausableBuildingTerminal __instance)
    {
      return __instance.GetComponent<BreedingPodTweaks>() == null;
    }

    [HarmonyPatch("OnPausedChanged")]
    [HarmonyPrefix]
    public static bool OnPausedChangedPrefix(PausableBuildingTerminal __instance)
    {
      return __instance.GetComponent<BreedingPodTweaks>() == null;
    }
  }
}
