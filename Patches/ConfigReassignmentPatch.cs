using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;

namespace ScienceBirdTweaks.Patches
{
    [HarmonyPatch]
    public class ConfigReassignmentPatch
    {
        public static Dictionary<(string, string), (string, string)> entryDict = new Dictionary<(string, string), (string, string)>
        {
            { ("9. Blackout", "Apparatus Hazard Blackout"), ("9. Blackout", "Apparatus Hazard Shutdown") },
            { ("9. Blackout", "Breaker Hazard Blackout"), ("9. Blackout", "Breaker Hazard Shutdown") },
            { ("Ship Tweaks", "Rotating Floodlight"), ("2. Ship Additions", "Rotating Floodlight") },
            { ("Ship Tweaks", "Rotate Floodlight Upon Landing"), ("2. Ship Additions", "Rotate Floodlight Upon Landing") },
            { ("Ship Tweaks", "Ship Floodlight Rotation Speed"), ("2. Ship Additions", "Ship Floodlight Rotation Speed") },
            { ("Ship Tweaks", "Ship Floodlight Angle"), ("2. Ship Additions", "Ship Floodlight Angle") },
            { ("Ship Tweaks", "Ship Floodlight Range"), ("2. Ship Additions", "Ship Floodlight Range") },
            { ("3. General Tweaks", "Monitor Transition Fix"), ("3. General Tweaks", "Ship Monitor Fixes") },
            { ("A. Mod Tweaks", "LLL Unlock Syncing"), ("X. Mod Tweaks", "LLL - Unlock Syncing") },
            { ("A. Mod Tweaks", "Wesley Moons Video Tape Skip"), ("X. Mod Tweaks", "Wesleys Moons - Video Tape Skip") },
            { ("A. Mod Tweaks", "ShipWindowsBeta Shutter Fix"), ("X. Mod Tweaks", "ShipWindows - Shutter Fix") },
            { ("A. Mod Tweaks", "ShipWindowsBeta Shutter Sound Effect"), ("X. Mod Tweaks", "ShipWindows - Shutter Sound Effect") },
            { ("A. Mod Tweaks", "Weather Tweaks Announcement Change"), ("X. Mod Tweaks", "Weather Tweaks - Announcement Change") },
            { ("A. Mod Tweaks", "Smart Cupboard Mrov Terminal Stock"), ("X. Mod Tweaks", "SSS Smart Cupboard + Mrov Terminal Stock") },
            { ("Mod Tweaks", "LLL Unlock Syncing"), ("X. Mod Tweaks", "LLL - Unlock Syncing") },
            { ("Mod Tweaks", "Wesley Moons Video Tape Skip"), ("X. Mod Tweaks", "Wesleys Moons - Video Tape Skip") },
            { ("Mod Tweaks", "ShipWindowsBeta Shutter Fix"), ("X. Mod Tweaks", "ShipWindows - Shutter Fix") },
            { ("Mod Tweaks", "ShipWindowsBeta Shutter Sound Effect"), ("X. Mod Tweaks", "ShipWindows - Shutter Sound Effect") },
            { ("Mod Tweaks", "Weather Tweaks Announcement Change"), ("X. Mod Tweaks", "Weather Tweaks - Announcement Change") },
            { ("Mod Tweaks", "Smart Cupboard Mrov Terminal Stock"), ("X. Mod Tweaks", "SSS Smart Cupboard + Mrov Terminal Stock") }
        };

        public static Dictionary<string, string> sectionDict = new Dictionary<string, string>
        {
            { "Ship Tweaks", "1 i. Ship Tweaks" },
            { "Ship Tweaks Collider Sizes", "1 ii. Ship Tweaks Collider Sizes" },
            { "Ship Tweaks Removals", "1 iii. Ship Tweaks Removals" },
            { "General Tweaks", "3. General Tweaks" },
            { "Gameplay Tweaks", "4. Enemy Tweaks" },
            { "Zap Gun & Hazards", "5. Zap Gun & Hazards" },
            { "Better Dust Clouds", "6. Better Dust Clouds" },
            { "Selective Scrap Keeping", "7. Selective Scrap Keeping" },
            { "Shotgun QOL", "8. Shotgun QOL" },
            { "Blackout", "9. Blackout" },
            { "Mod Tweaks", "X. Mod Tweaks" },
            { "A. Mod Tweaks", "X. Mod Tweaks" },
            { "B. Interior Scrap Bonus", "Y. Interior Scrap Bonus" },
            { "C. Technical", "Z. Technical" },
        };

        [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.Start))]
        [HarmonyPostfix]
        static void ConfigOrphanReassignment(QuickMenuManager __instance)
        {

            //List<string> methodNames = AccessTools.GetMethodNames(ScienceBirdTweaks.Instance.Config.GetType());
            //foreach (string name in methodNames)
            //{
            //    ScienceBirdTweaks.Logger.LogDebug($"method: {name}");
            //}

            //List<string> propNames = AccessTools.GetPropertyNames(ScienceBirdTweaks.Instance.Config.GetType());
            //foreach (string name in propNames)
            //{
            //    ScienceBirdTweaks.Logger.LogDebug($"method: {name}");
            //}

            //List<string> fieldNames = AccessTools.GetFieldNames(ScienceBirdTweaks.Instance.Config.GetType());
            //foreach (string name in fieldNames)
            //{
            //    ScienceBirdTweaks.Logger.LogDebug($"field: {name}");
            //}

            //MethodInfo testMethod = AccessTools.Method(ScienceBirdTweaks.Instance.Config.GetType(), "System.Collections.Generic.IDictionary<BepInEx.Configuration.ConfigDefinition,BepInEx.Configuration.ConfigEntryBase>.Values");
            //if (testMethod != null)
            //{
            //    ScienceBirdTweaks.Logger.LogDebug("1");
            //    ICollection<ConfigEntryBase> realList = (ICollection<ConfigEntryBase>)testMethod.Invoke(ScienceBirdTweaks.Instance.Config.GetType(), new object[]{});
            //    foreach (ConfigEntryBase rEntry in realList)
            //    {
            //        ScienceBirdTweaks.Logger.LogDebug($"r - {rEntry.Definition.Section}/{rEntry.Definition.Key} : {rEntry.BoxedValue} {rEntry.BoxedValue.GetType()}, {rEntry.DefaultValue} {rEntry.DefaultValue.GetType()}");
            //    }
            //}


            ConfigEntryBase[] entryArray = ScienceBirdTweaks.Instance.Config.GetConfigEntries();// it says this is deprecated but the "alternative" is practically inaccessible even using AccessTools
            foreach (ConfigEntryBase dEntry in entryArray)
            {
                //ScienceBirdTweaks.Logger.LogDebug($"d - {dEntry.Definition.Section}/{dEntry.Definition.Key} : {dEntry.BoxedValue} {dEntry.BoxedValue.GetType()}, {dEntry.DefaultValue} {dEntry.DefaultValue.GetType()}");
            }


            var orphanedEntriesProperty = ScienceBirdTweaks.Instance.Config.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<ConfigDefinition, string> orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProperty!.GetValue(ScienceBirdTweaks.Instance.Config, null);
            List<ConfigDefinition> orphanKeys = new List<ConfigDefinition>(orphanedEntries.Keys);
            foreach (var entry in orphanKeys)
            {
                if (entryDict.TryGetValue((entry.Section, entry.Key), out (string,string) newDef))
                {
                    ConfigDefinition orphanConfigDef = new ConfigDefinition(entry.Section, entry.Key);
                    ConfigDefinition newConfigDef = new ConfigDefinition(newDef.Item1, newDef.Item2);
                    //ScienceBirdTweaks.Logger.LogDebug($"Potential orphan: {orphanConfigDef.Section},{orphanConfigDef.Key} | {newConfigDef.Section},{newConfigDef.Key}");
                    if (orphanedEntries.TryGetValue(orphanConfigDef, out string valueString))
                    {
                        MigrateOrphanValues(orphanConfigDef, newConfigDef, valueString, orphanedEntries, entryArray);
                    }
                }
                else if (sectionDict.TryGetValue(entry.Section, out string newSection))
                {
                    ConfigDefinition orphanConfigDef = new ConfigDefinition(entry.Section, entry.Key);
                    ConfigDefinition newConfigDef = new ConfigDefinition(newSection, entry.Key);
                    //ScienceBirdTweaks.Logger.LogDebug($"Potential orphan: {orphanConfigDef.Section},{orphanConfigDef.Key} | {newConfigDef.Section},{newConfigDef.Key}");
                    if (orphanedEntries.TryGetValue(orphanConfigDef, out string valueString))
                    {
                        MigrateOrphanValues(orphanConfigDef, newConfigDef, valueString, orphanedEntries, entryArray);
                    }
                }
            }

            if (ScienceBirdTweaks.ClearOrphans.Value)
            {
                var orphanedEntriesFinal = (Dictionary<ConfigDefinition, string>)orphanedEntriesProperty!.GetValue(ScienceBirdTweaks.Instance.Config, null);
                orphanedEntries.Clear();
                ScienceBirdTweaks.ClearOrphans.Value = false;
            }
            ScienceBirdTweaks.Instance.Config.Save();
        }

        static void MigrateOrphanValues(ConfigDefinition orphanConfigDef, ConfigDefinition newConfigDef, string valueString, Dictionary<ConfigDefinition, string> orphanedEntries, ConfigEntryBase[] entryArray)
        {
            var orphanValue = GetValueFromString(valueString);
            if (orphanValue == null) { return; }
            //ScienceBirdTweaks.Logger.LogDebug($"orphan val: {orphanValue}, {orphanValue.GetType()}");

            //if (entryArray.Any(x => x.Definition == newConfigDef))
            //{
            //    ConfigEntryBase baseEntry = entryArray.Where(x => x.Definition == newConfigDef).First();
            //    ScienceBirdTweaks.Logger.LogDebug($"Found matching entry: {baseEntry.BoxedValue} ({baseEntry.BoxedValue.GetType()}), {baseEntry.DefaultValue} ({baseEntry.DefaultValue.GetType()})");
            //}

            if (orphanValue is bool orphanBool)
            {
                //if (entryArray.Any(x => x.Definition == newConfigDef))
                //{
                //    ConfigEntryBase baseEntry = entryArray.Where(x => x.Definition == newConfigDef).First();

                //    //ScienceBirdTweaks.Logger.LogDebug($"Found matching entry: {baseEntry.BoxedValue} ({baseEntry.BoxedValue.GetType()}), {baseEntry.DefaultValue} ({baseEntry.DefaultValue.GetType()}) | {(bool)baseEntry.BoxedValue != (bool)baseEntry.DefaultValue}");
                //    //ScienceBirdTweaks.Logger.LogDebug($"Fuller check: {entryArray.Any(x => x.Definition == newConfigDef && (bool)x.BoxedValue != (bool)x.DefaultValue)}");
                //}
                //else
                //{
                //    return;
                //}
                if (entryArray.Any(x => x.Definition == newConfigDef && x.BoxedValue.GetType() == typeof(bool) && (bool)x.BoxedValue == (bool)x.DefaultValue) && ScienceBirdTweaks.Instance.Config.TryGetEntry<bool>(newConfigDef, out var boolEntry))
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"Migrating orphan: {orphanConfigDef.Section}/{orphanConfigDef.Key} : {orphanBool} >>> {newConfigDef.Section}/{newConfigDef.Key} : {boolEntry.Value}");
                    boolEntry.Value = orphanBool;
                    orphanedEntries.Remove(orphanConfigDef);
                }
            }
            else if (orphanValue is float orphanFloat)
            {
                if (entryArray.Any(x => x.Definition == newConfigDef && x.BoxedValue.GetType() == typeof(float) && (float)x.BoxedValue == (float)x.DefaultValue) && ScienceBirdTweaks.Instance.Config.TryGetEntry<float>(newConfigDef, out var floatEntry))
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"Migrating orphan: {orphanConfigDef.Section}/{orphanConfigDef.Key} : {orphanFloat} >>> {newConfigDef.Section}/{newConfigDef.Key} : {floatEntry.Value}");
                    floatEntry.Value = orphanFloat;
                    orphanedEntries.Remove(orphanConfigDef);
                }
                else if (entryArray.Any(x => x.Definition == newConfigDef && x.BoxedValue.GetType() == typeof(int) && (int)x.BoxedValue == (int)x.DefaultValue) && Mathf.Abs(orphanFloat - Mathf.Floor(orphanFloat + 0.000001f)) < 0.0000001f && ScienceBirdTweaks.Instance.Config.TryGetEntry<int>(newConfigDef, out var intEntry))
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"Migrating orphan: {orphanConfigDef.Section}/{orphanConfigDef.Key} : {Mathf.FloorToInt(orphanFloat + 0.000001f)} >>> {newConfigDef.Section}/{newConfigDef.Key} : {intEntry.Value}");
                    intEntry.Value = Mathf.FloorToInt(orphanFloat + 0.000001f);
                    orphanedEntries.Remove(orphanConfigDef);
                }
            }
            else if (orphanValue is string orphanString)
            {
                if (entryArray.Any(x => x.Definition == newConfigDef && x.BoxedValue.GetType() == typeof(string) && (string)x.BoxedValue == (string)x.DefaultValue) && ScienceBirdTweaks.Instance.Config.TryGetEntry<string>(newConfigDef, out var stringEntry))
                {
                    //ScienceBirdTweaks.Logger.LogDebug($"Migrating orphan: {orphanConfigDef.Section}/{orphanConfigDef.Key} : {orphanString} >>> {newConfigDef.Section}/{newConfigDef.Key} : {stringEntry.Value}");
                    stringEntry.Value = orphanString;
                    orphanedEntries.Remove(orphanConfigDef);
                }
            }
        }

        static object GetValueFromString(string configValue)
        {
            if (bool.TryParse(configValue, out bool bResult))
            {
                return bResult;
            }
            if (float.TryParse(configValue, out float fResult))
            {
                return fResult;
            }
            else
            {
                return configValue;
            }
        }
    }
}
