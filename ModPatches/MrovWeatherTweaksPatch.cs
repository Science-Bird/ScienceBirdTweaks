using System.Text.RegularExpressions;
using HarmonyLib;
using ScienceBirdTweaks.Patches;

namespace ScienceBirdTweaks.ModPatches
{
    [HarmonyPatch]
    public class MrovWeatherTweaksAnnouncementPatch
    {
        public static string[] progressingWeathers;

        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.LateUpdate))]
        [HarmonyPostfix]
        static void TwoRadarMapsWeatherPatch(ManualCameraRenderer __instance)
        {
            if (ScienceBirdTweaks.SolarFlareTwoRadar.Value && ScienceBirdTweaks.zaggyPresent && ScienceBirdTweaks.mrovPresent2 && __instance == PlayerCamPatches.twoRadarCam && __instance.LostSignalUI != null && TimeOfDay.Instance.currentLevelWeather.ToString() == "Solar Flare")
            {
                __instance.LostSignalUI.SetActive(true);
                if (__instance.headMountedCamUI != null)
                {
                    __instance.headMountedCamUI.enabled = false;
                }
                __instance.headMountedCam.enabled = false;

            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetMapScreenInfoToCurrentLevel))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void AddSpaceToMapScreen(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.MrovWeatherTweaksAnnouncement.Value || !ScienceBirdTweaks.mrovPresent4)
                return;

            string levelText = __instance.screenLevelDescription.text;
            if (Regex.IsMatch(levelText, "WEATHER: (.+)\n"))
            {
                string progressingWeather = Regex.Match(levelText, "WEATHER: (.+)\n").Groups[1].Value;// extract weather name
                progressingWeather = Regex.Replace(progressingWeather, "<color=#[\\w\\d]{6}>", "");
                progressingWeather = progressingWeather.Replace("</color>", "");
                if (progressingWeather.Contains(">"))// extract 2 transitioning weathers
                {
                    progressingWeathers = progressingWeather.Split(" > ");
                }
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.ReadDialogue))]
        [HarmonyPrefix]
        public static void DialoguePatch(HUDManager __instance, ref DialogueSegment[] dialogueArray)
        {
            if (!ScienceBirdTweaks.MrovWeatherTweaksAnnouncement.Value || !ScienceBirdTweaks.mrovPresent4)
                return;

            DialogueSegment[] array = dialogueArray;
            if (array[0].bodyText.Contains("The weather will be changing to") && progressingWeathers != null && progressingWeathers.Length >= 2)
            {
                if (TimeOfDay.Instance.currentDayTime < 0.1f)
                {
                    array[0].bodyText = array[0].bodyText = $"The weather is currently {progressingWeathers[0]}.\nIt is forecasted to change to {progressingWeathers[1]}.";
                }
                else
                {
                    array[0].bodyText = array[0].bodyText = $"The weather is now changing from {progressingWeathers[0]} to {progressingWeathers[1]}.";
                }
                dialogueArray = array;
            }
        }
    }
}
