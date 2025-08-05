using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class OccupancyPatch
    {
        public static List<Material?> posterMats;
        public static int playerCount = 0;

        public static void LoadAssets()
        {
            posterMats = new List<Material?>();
            posterMats.Add(null);
            posterMats.Add(null);
            posterMats.Add(null);
            posterMats.Add(null);// pad list so indices line up with number featured on the poster
            string scribbleString = "";
            if (ScienceBirdTweaks.OccupancyScribble.Value)
            {
                scribbleString = "x";
            }
            for (int i = 4; i <= 17; i++)
            {
                posterMats.Add((Material)ScienceBirdTweaks.TweaksAssets.LoadAsset($"Poster{i}" + scribbleString));
            }
        }

        public static void UpdatePoster(StartOfRound round)
        {
            if (!ScienceBirdTweaks.DynamicOccupancySign.Value && ScienceBirdTweaks.OccupancyFixedValue.Value == "None") { return; }

            playerCount = round.connectedPlayersAmount + 1;
            GameObject occupancyPoster = GameObject.Find("HangarShip/Plane.001");
            if (occupancyPoster == null) { return; }

            Material[] mats = occupancyPoster.GetComponent<MeshRenderer>().materials;
            if (ScienceBirdTweaks.OccupancyFixedValue.Value != "None")// fixed value override
            {
                if (ScienceBirdTweaks.OccupancyFixedValue.Value == "Infinite")
                {
                    mats[0] = posterMats[17];
                }
                else
                {
                    int num = int.Parse(ScienceBirdTweaks.OccupancyFixedValue.Value);
                    mats[0] = posterMats[num];
                }
            }
            else if (playerCount > 4 && playerCount < 17)// dynamic value checks
            {
                mats[0] = posterMats[playerCount];
            }
            else if (playerCount <= 4)
            {
                mats[0] = posterMats[4];
            }
            else
            {
                mats[0] = posterMats[17];
            }
            occupancyPoster.GetComponent<MeshRenderer>().materials = mats;
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadUnlockables))]
        [HarmonyPostfix]
        static void OnInitialLoad(StartOfRound __instance)
        {
            UpdatePoster(__instance);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientConnect))]
        [HarmonyPostfix]
        static void OnConnectionServer(StartOfRound __instance)
        {
            UpdatePoster(__instance);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc))]
        [HarmonyPostfix]
        static void OnConnectionClients(StartOfRound __instance)
        {
            UpdatePoster(__instance);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
        [HarmonyPostfix]
        static void OnDisconnection(StartOfRound __instance)
        {
            UpdatePoster(__instance);
        }
    }
}

