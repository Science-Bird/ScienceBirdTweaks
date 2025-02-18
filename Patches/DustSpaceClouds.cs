using HarmonyLib;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class DustSpaceClouds
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetMapScreenInfoToCurrentLevel))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void AddSpaceToMapScreen(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.DustSpaceClouds.Value)
            {
                return;
            }
            string levelText = __instance.screenLevelDescription.text;
            if (levelText.Contains("DustClouds"))
            {
                levelText = levelText.Replace("DustClouds", "Dust Clouds");
                __instance.screenLevelDescription.text = levelText;
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNode))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter("mrov.terminalformatter")]
        static void AddSpaceToTerminal(Terminal __instance)
        {
            if (!ScienceBirdTweaks.DustSpaceClouds.Value)
            {
                return;
            }
            if (__instance.currentText.Contains("DustClouds"))
            {
                __instance.currentText = __instance.currentText.Replace("DustClouds", "Dust Clouds");
                __instance.screenText.text = __instance.currentText;
            }
        }
    }
}
