using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Steamworks.ServerList;
using UnityEngine;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ScrapMutePatches
    {
        public static List<string> itemsToMute = new List<string>();
        public static List<string> animatedItemList = new List<string>();

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        [HarmonyPostfix]
        static void MuteItems(GameNetworkManager __instance)
        {
            if (ScienceBirdTweaks.MuteScrapList.Value == "") { return; }

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

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPostfix]
        static void ShellPrefabCheck(StartOfRound __instance, string sceneName)
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
    }
}
