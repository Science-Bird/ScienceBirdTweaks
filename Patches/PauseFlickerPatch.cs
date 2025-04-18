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
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                {
                    UnityEngine.UI.Button button = EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.Button>();
                    if (button != null && button.animator != null)
                    {
                        button.animator.SetTrigger("Highlighted");
                    }
                }
            }
        }
    }
}
