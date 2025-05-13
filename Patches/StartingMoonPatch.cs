using System.Linq;
using HarmonyLib;
using System.Text.RegularExpressions;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class StartingMoonPatch
    {
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPrefix]
        static void OnStart(StartOfRound __instance)
        {
            string moon = Regex.Replace(ScienceBirdTweaks.StartingMoon.Value, "^.\\d+\\s", "");// this regex removes numbers from the start (with check for additional character to catch Hyx)
            if (moon == "Experimentation" || moon == "") { return; }
            SelectableLevel newStartLevel = UnityEngine.Resources.FindObjectsOfTypeAll<SelectableLevel>().Where(x => Regex.Replace(x.PlanetName, "^.\\d+\\s", "") == moon).First();
            if (newStartLevel != null)
            {
                ScienceBirdTweaks.Logger.LogDebug($"New start level ID: {newStartLevel.levelID}, new start level name: {newStartLevel.PlanetName}");
                __instance.defaultPlanet = newStartLevel.levelID;
            }
        }
    }
}
