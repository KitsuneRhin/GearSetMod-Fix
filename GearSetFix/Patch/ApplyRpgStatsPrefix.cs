using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using GearSetsMod.Core;
using GearSetsMod.UI;
using HarmonyLib;
using System;
using System.Reflection;

namespace GearSetFix.Patch
{
    [HarmonyPatch(typeof(GearSetsUI), "ApplyRpgStats", new Type[] { typeof(Hero), typeof(GearSet) })]
    public static class ApplyRpgStatsPrefix
    {
        static bool Prefix(Hero hero, GearSet set)
        {
            Plugin.Log.LogInfo("Intercepting trigger for ApplyRpgStats\nPrefix entered");

                if (hero == null || set == null) { Plugin.Log.LogWarning("Hero or set is null, aborting prefix"); return false; }
            var heroRpg = hero.HeroRPGStats;
                if (heroRpg == null) { Plugin.Log.LogWarning("HeroRPGStats is null, aborting prefix"); return false; }

            foreach (var kv in set.RpgStats)
            {
                string statName = kv.Key;
                float desired = kv.Value;

                try
                {
                    Plugin.Log.LogInfo($"Processing stat {statName} => {desired}");

                    // Reflection to get the stat object from HeroRPGStats
                    var prop = heroRpg.GetType().GetProperty(statName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (prop == null) { Plugin.Log.LogWarning($"Property {statName} not found on HeroRPGStats"); continue; }
                    var statObj = prop.GetValue(heroRpg);
                        if (statObj == null) { Plugin.Log.LogWarning($"Stat object for {statName} was null"); continue; }

                    // Type-path for native stat set: Stat.SetTo(float newValue, [bool runHooks], [ContractContext context])
                    if (statObj is Stat stat)
                    {
                        stat.SetTo(desired, false, null); // runHooks = false to avoid weird side effects; context = null
                        Plugin.Log.LogInfo($"Wrote {statName} via Stat.SetTo()");
                    }
                    else { Plugin.Log.LogWarning($"Stat object for {statName} is not a Stat (type={statObj.GetType().Name})"); }
                }
                catch (Exception ex) { Plugin.Log.LogError($"Unhandled exception while processing {statName}: {ex}"); }
            }

            // Recalculation pipeline - DO NOT call HeroRPGStats.RecalculateAllStats() – it resets from wrapper and undoes changes
            try
            {
                Plugin.Log.LogInfo("Triggering recalculation pipeline");
                hero.HeroStats.RecalculateAllStats(false);
                int lv = (int)hero.CharacterStats.Level.BaseValue;
                hero.CharacterStats.RecalculateAllStats(lv, lv, false);
                Plugin.Log.LogInfo("Recalculation pipeline complete");
            }
            catch (Exception ex) { Plugin.Log.LogError($"Recalculation pipeline failed: {ex}"); }

            // Debug: verification snapshot
            try
            {
                Plugin.Log.LogInfo("Verification snapshot of RPG stats:");
                foreach (var name in new[] { "Strength", "Dexterity", "Perception", "Endurance", "Practicality", "Spirituality" })
                {
                    var p = heroRpg.GetType()
                        .GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p == null) { Plugin.Log.LogWarning($"Verify: {name} property missing"); continue; }

                    var s = p.GetValue(heroRpg);
                    if (s == null) { Plugin.Log.LogWarning($"Verify: {name} stat null"); continue; }

                    var baseProp = s.GetType().GetProperty("BaseValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var modProp = s.GetType().GetProperty("ModifiedValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    var baseVal = baseProp?.GetValue(s);
                    var modVal = modProp?.GetValue(s);

                    Plugin.Log.LogInfo($"{name} BaseValue={baseVal} ModifiedValue={modVal}");
                }
            }
            catch (Exception ex) { Plugin.Log.LogWarning($"Verification snapshot failed: {ex.Message}"); }

            Plugin.Log.LogInfo("Prefix complete, exiting.");
            return false; // Skip original method
        }
    }
}
