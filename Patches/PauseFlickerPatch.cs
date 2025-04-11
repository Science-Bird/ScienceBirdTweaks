using HarmonyLib;
using UnityEngine.EventSystems;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class PauseFlickerPatch
    {
        [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.Update))]
        [HarmonyPostfix]
        static void UpdateThingy(QuickMenuManager __instance)
        {
            if (ScienceBirdTweaks.PauseMenuFlickerFix.Value && __instance.menuContainer.activeInHierarchy)
            {
                UnityEngine.UI.Button button = EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.animator.SetTrigger("Highlighted");
                }
            }
        }
    }
}
