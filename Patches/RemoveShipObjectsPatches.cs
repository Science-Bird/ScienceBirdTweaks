using UnityEngine;
using HarmonyLib;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class RemoveShipObjectsPatches
    {

        static void DestroyObject(string objectString, bool soft = false)// general removal function
        {
            GameObject shipObject = GameObject.Find("/Environment/HangarShip/" + objectString);
            if (shipObject != null)
            {
                if (soft)
                {
                    shipObject.SetActive(false);
                }
                else
                {
                    Object.Destroy(shipObject);
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPostfix]
        static void OnInitialLoad(StartOfRound __instance)
        {
            TryRemovals();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientConnect))]
        [HarmonyPostfix]
        static void OnConnectionServer(StartOfRound __instance)
        {
            TryRemovals();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc))]
        [HarmonyPostfix]
        static void OnConnectionClients(StartOfRound __instance)
        {
            TryRemovals();
        }

        static void TryRemovals()
        {
            if (ScienceBirdTweaks.RemoveClipboard.Value)
            {
                DestroyObject("ClipboardManual");
            }
            if (ScienceBirdTweaks.RemoveStickyNote.Value)
            {
                DestroyObject("StickyNoteItem");
            }
            if (ScienceBirdTweaks.RemoveLongTube.Value)
            {
                DestroyObject("BezierCurve");
            }
            if (ScienceBirdTweaks.RemoveGenerator.Value)
            {
                DestroyObject("DoorGenerator");
            }
            if (ScienceBirdTweaks.RemoveHelmet.Value)
            {
                DestroyObject("ScavengerModelSuitParts/Circle.001");
            }
            if (ScienceBirdTweaks.RemoveOxygenTanks.Value)
            {
                DestroyObject("ScavengerModelSuitParts/Circle.002");
            }
            if (ScienceBirdTweaks.RemoveBoots.Value)
            {
                DestroyObject("ScavengerModelSuitParts/Circle.004");
            }
            if (ScienceBirdTweaks.RemoveAirFilter.Value)
            {
                DestroyObject("ShipModels2b/AirFilterThing");
            }
            if (ScienceBirdTweaks.RemoveMonitorWires.Value)
            {
                DestroyObject("WallCords");
            }
            if (ScienceBirdTweaks.RemoveBatteries.Value)
            {
                DestroyObject("SmallDetails/BatteryPack");
                DestroyObject("SmallDetails/BatterySingle");
                DestroyObject("SmallDetails/BatterySingle (1)");
                DestroyObject("SmallDetails/BatterySingle (2)");
            }
            if (ScienceBirdTweaks.RemoveExteriorCam.Value)
            {
                DestroyObject("Cameras/FrontDoorSecurityCam", true);
            }
            if (ScienceBirdTweaks.RemoveInteriorCam.Value)
            {
                DestroyObject("Cameras/ShipCamera", true);
            }
            if (ScienceBirdTweaks.RemoveDoorMonitor.Value)
            {
                DestroyObject("ShipModels2b/MonitorWall/SingleScreen", true);
            }
        }
    }
}

