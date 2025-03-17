using HarmonyLib;
using UnityEngine;
using JLLItemsModule.Components;
using System.Reflection;

namespace ScienceBirdTweaks.ModPatches
{
    public class JLLPatches
    {
        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(typeof(RoundManager), "SyncScrapValuesClientRpc"), postfix: new HarmonyMethod(typeof(FixJLLRandom).GetMethod("FixJNoisemakers")));
        }
    }

    public class FixJLLRandom
    {

        public static void FixJNoisemakers(RoundManager __instance)
        {
            JNoisemakerProp[] jProps = GameObject.FindObjectsOfType<JNoisemakerProp>();
            FieldInfo randomField = AccessTools.Field(typeof(JNoisemakerProp), "noisemakerRandom");
            foreach (JNoisemakerProp jProp in jProps)
            {
                if (randomField.GetValue(jProp) == null)
                {
                    ScienceBirdTweaks.Logger.LogInfo("Found JNoisemakerProp with null random! Fixing...");
                    randomField.SetValue(jProp, new System.Random(StartOfRound.Instance.randomMapSeed + 85));
                }
            }
        }
    }
}
