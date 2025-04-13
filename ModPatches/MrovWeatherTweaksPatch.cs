using System.Text.RegularExpressions;
using HarmonyLib;

namespace ScienceBirdTweaks.ModPatches
{
    [HarmonyPatch]
    public class MrovWeatherTweaksAnnouncementPatch
    {
        public static string[] progressingWeathers;

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetMapScreenInfoToCurrentLevel))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void AddSpaceToMapScreen(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.MrovWeatherTweaksAnnouncement.Value || !ScienceBirdTweaks.mrovPresent4)
                return;

            ScienceBirdTweaks.Logger.LogDebug(__instance.screenLevelDescription.text);
            string levelText = __instance.screenLevelDescription.text;
            if (Regex.IsMatch(levelText, "WEATHER: (.+)\n"))
            {
                string progressingWeather = Regex.Match(levelText, "WEATHER: (.+)\n").Groups[1].Value;// extract weather name
                ScienceBirdTweaks.Logger.LogDebug(progressingWeather);
                progressingWeather = Regex.Replace(progressingWeather, "<color=#[\\w\\d]{6}>", "");
                progressingWeather = progressingWeather.Replace("</color>", "");
                ScienceBirdTweaks.Logger.LogDebug(progressingWeather);
                if (progressingWeather.Contains(">"))// extract 2 transitioning weathers
                {
                    ScienceBirdTweaks.Logger.LogDebug(progressingWeather.Split(" > "));
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
                    ScienceBirdTweaks.Logger.LogDebug(array[0].bodyText);
                }
                else
                {
                    array[0].bodyText = array[0].bodyText = $"The weather is now changing from {progressingWeathers[0]} to {progressingWeathers[1]}";
                    ScienceBirdTweaks.Logger.LogDebug(array[0].bodyText);
                }
                dialogueArray = array;
                ScienceBirdTweaks.Logger.LogDebug(dialogueArray[0].bodyText);
            }
        }
    }
}
