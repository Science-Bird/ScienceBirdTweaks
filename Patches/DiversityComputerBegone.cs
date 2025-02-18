using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class DiversityComputerBegone
    {
        internal static bool isDone = false;

        internal static int checkCount = 0;

        internal static string[] diskNames = ["Never Stopping","Ex-37", "Ex-43", "Ex-67", "Ex-507", "Sub-12", "Sub-16", "Sub-30", "Sub-66", "Sub-100", "Sub-507"];

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ResetOnLoad(StartOfRound __instance, string sceneName)
        {
            if (!ScienceBirdTweaks.DiversityComputerBegone.Value)
            {
                return;
            }
            isDone = false;
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Update))]
        [HarmonyPostfix]
        static void DestroyComputer(StartOfRound __instance)
        {
            if (!ScienceBirdTweaks.DiversityComputerBegone.Value)
            {
                return;
            }
            if (!isDone)
            {
                if (!__instance.shipHasLanded)
                {
                    GameObject reader = GameObject.Find("Floppy Reader(Clone)");
                    if (reader == null)
                    {
                        return;
                    }
                    else
                    {
                        Object.Destroy(reader);
                        foreach (string name in diskNames)
                        {
                            GameObject disk = GameObject.Find(name + "(Clone)");
                            if (disk != null)
                            {
                                Object.Destroy(disk);
                            }
                        }
                        isDone = true;
                    }
                    checkCount++;
                    if (checkCount > 5)
                    {
                        isDone = true;
                    }
                }
                else
                {
                    isDone = true;
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc))]
        [HarmonyPostfix]
        static void FindDisks(RoundManager __instance)
        {
            if (!ScienceBirdTweaks.DiversityComputerBegone.Value)
            {
                return;
            }
            foreach (string name in diskNames)
            {
                GameObject disk = GameObject.Find(name + "(Clone)");
                if (disk != null)
                {
                    Object.Destroy(disk);
                }
            }
        }
    }
}
