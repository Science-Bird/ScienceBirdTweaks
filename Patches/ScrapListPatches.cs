using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ScrapListPatches
    {
        public static List<string> itemsToMute = new List<string>();
        public static List<string> animatedItemList = new List<string>();
        public static List<string> itemDayBlacklist = new List<string>();

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        static void MuteItems(GameNetworkManager __instance)
        {
            if (ScienceBirdTweaks.SingleItemBlacklist.Value != "")
            {
                itemDayBlacklist = ScienceBirdTweaks.SingleItemBlacklist.Value.Replace(", ", ",").Split(",").ToList();
                itemDayBlacklist = itemDayBlacklist.ConvertAll(x => x.ToLower());
                ScienceBirdTweaks.Logger.LogDebug("Setting single item blacklist!");
            }

            if (ScienceBirdTweaks.MuteScrapList.Value != "")
            {
                itemsToMute = ScienceBirdTweaks.MuteScrapList.Value.Replace(", ", ",").Split(",").ToList();

                itemsToMute = itemsToMute.ConvertAll(x => x.ToLower());
                ScienceBirdTweaks.Logger.LogDebug("Muting items!");
                MuteAnimated();
                MutePeriodic();
                if (itemsToMute.Contains("clock"))
                {
                    itemsToMute.Remove("clock");
                    MuteClock();
                }
                if (itemsToMute.Contains("heart"))
                {
                    itemsToMute.Remove("heart");
                    MuteHeart();
                }
                foreach (string name in itemsToMute)// each prior function removes items from the list, so this is to catch all other items (just makes sure it doesn't have any looping audio, e.g. radioactive barrels)
                {
                    Item[] items = UnityEngine.Resources.FindObjectsOfTypeAll<Item>();
                    foreach (Item item in items)
                    {
                        if (item.itemName.ToLower() == name && item.spawnPrefab != null)
                        {
                            AudioSource[] audios = item.spawnPrefab.GetComponentsInChildren<AudioSource>();
                            foreach (AudioSource audio in audios)
                            {
                                audio.loop = false;
                            }
                        }
                    }
                }
            }
        }

        public static void MuteAnimated()
        {
            AnimatedItem[] animatedItems = UnityEngine.Resources.FindObjectsOfTypeAll<AnimatedItem>();
            foreach (AnimatedItem item in animatedItems)
            {
                if (itemsToMute.Contains(item.itemProperties.itemName.ToLower()))
                {
                    animatedItemList.Add(item.itemProperties.itemName.ToLower());
                    item.grabAudio = null;
                    item.dropAudio = null;
                    item.noiseLoudness = 0f;
                    item.noiseRange = 0f;
                    itemsToMute.Remove(item.itemProperties.itemName.ToLower());
                }
            }
        }

        public static void MutePeriodic()
        {
            RandomPeriodicAudioPlayer[] periodicPlayers = UnityEngine.Resources.FindObjectsOfTypeAll<RandomPeriodicAudioPlayer>().Where(x => (bool)x.gameObject.GetComponent<GrabbableObject>() && itemsToMute.Contains(x.gameObject.GetComponent<GrabbableObject>().itemProperties.itemName.ToLower())).ToArray();
            foreach (RandomPeriodicAudioPlayer player in periodicPlayers)
            {
                player.audioChancePercent = 0f;
                itemsToMute.Remove(player.gameObject.GetComponent<GrabbableObject>().itemProperties.itemName.ToLower());
            }
        }
        public static void MuteClock()
        {
            ClockProp[] clocks = UnityEngine.Resources.FindObjectsOfTypeAll<ClockProp>();
            foreach (ClockProp clock in clocks)
            {
                clock.tickAudio.volume = 0f;
            }
        }

        public static void MuteHeart()
        {
            LoopShapeKey[] hearts = UnityEngine.Resources.FindObjectsOfTypeAll<LoopShapeKey>();
            foreach (LoopShapeKey heart in hearts)
            {
                AudioSource heartAudio = heart.repeatingAudioSource;
                if (heartAudio != null)
                {
                    heartAudio.mute = true;
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void AnimatedItemExtraCheck(StartOfRound __instance, string sceneName)
        {
            if (ScienceBirdTweaks.MuteScrapList.Value == "") { return; }

            if (sceneName == "SampleSceneRelay")
            {
                AnimatedItem[] animatedItems = UnityEngine.Resources.FindObjectsOfTypeAll<AnimatedItem>();
                foreach (AnimatedItem item in animatedItems)
                {
                    if (animatedItemList.Contains(item.itemProperties.itemName.ToLower()))
                    {
                        if (item.grabAudio != null || item.dropAudio != null || item.noiseLoudness != 0f || item.noiseRange != 0f)
                        {
                            ScienceBirdTweaks.Logger.LogDebug("Fixing animated item!");
                            item.grabAudio = null;
                            item.dropAudio = null;
                            item.noiseLoudness = 0f;
                            item.noiseRange = 0f;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnScrapInLevel))]
        [HarmonyPrefix]
        static void ScrapGenerationPrefix(RoundManager __instance)
        {
            if (ScienceBirdTweaks.SingleItemBlacklist.Value == "" || itemDayBlacklist.Count <= 0) { return; }

            for (int i = 0; i < __instance.currentLevel.spawnableScrap.Count; i++)
            {
                Item scrapItem = __instance.currentLevel.spawnableScrap[i].spawnableItem;
                if (scrapItem != null && itemDayBlacklist.Contains(scrapItem.itemName.ToLower()))
                {
                    if (scrapItem.twoHanded)
                    {
                        itemDayBlacklist.Remove(scrapItem.itemName.ToLower());
                    }
                    else
                    {
                        //ScienceBirdTweaks.Logger.LogDebug($"Temporarily setting {scrapItem.itemName} to two handed!");
                        scrapItem.twoHanded = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnScrapInLevel))]
        [HarmonyPostfix]
        static void ScrapGenerationPostfix(RoundManager __instance)
        {
            if (ScienceBirdTweaks.SingleItemBlacklist.Value == "" || itemDayBlacklist.Count <= 0) { return; }

            for (int i = 0; i < __instance.currentLevel.spawnableScrap.Count; i++)
            {
                Item scrapItem = __instance.currentLevel.spawnableScrap[i].spawnableItem;
                if (scrapItem != null && itemDayBlacklist.Contains(scrapItem.itemName.ToLower()))
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"Resetting {scrapItem.itemName}!");
                    scrapItem.twoHanded = false;
                }
            }
        }
    }
}
