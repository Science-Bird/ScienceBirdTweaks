using System.Reflection;
using HarmonyLib;
using System;

namespace ScienceBirdTweaks.ModPatches
{
    public class QualityCompanyForkPatch
    {
        public static void DoPatching()
        {
            ScienceBirdTweaks.Harmony?.Patch(AccessTools.Method(AccessTools.TypeByName("QualityCompany.Manager.Saves.SaveManager"), "Save"), prefix: new HarmonyMethod(typeof(QualityCompanyForkPatch).GetMethod("SavePatch")));
        }

        public static bool SavePatch()
        {
            Type saveManagerType = AccessTools.TypeByName("QualityCompany.Manager.Saves.SaveManager");
            if (saveManagerType != null)
            {
                FieldInfo saveFileField = AccessTools.Field(saveManagerType, "_saveFilePath");
                if (saveFileField != null)
                {
                    string savePath = (string)saveFileField.GetValue(saveManagerType);
                    if (savePath == null)
                    {
                        ScienceBirdTweaks.Logger.LogDebug("Null path detected!");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
