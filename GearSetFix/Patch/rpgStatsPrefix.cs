using Awaken.TG.Main.Heroes;
using GearSetsMod.Core;
using GearSetsMod.UI;
using HarmonyLib;
using System;

namespace GearSetFix.Patch
{
    [HarmonyPatch(typeof(GearSetsUI), "ApplyRpgStats",
        new Type[] { typeof(Hero), typeof(GearSet) })]
    public static class GearSetsUIPatch
    {
        static bool Prefix(Hero hero, GearSet set)
        {
            Plugin.Log.LogInfo("Prefix triggered for ApplyRpgStats");

            var heroStats = hero.HeroRPGStats;

            foreach (var kv in set.RpgStats)
            {
                var statProp = heroStats.GetType().GetProperty(kv.Key);
                if (statProp == null)
                {
                    Plugin.Log.LogWarning($"Stat property {kv.Key} not found");
                    continue;
                }

                var statObj = statProp.GetValue(heroStats);
                if (statObj == null)
                {
                    Plugin.Log.LogWarning($"Stat object for {kv.Key} was null");
                    continue;
                }

                var baseProp = statObj.GetType().GetProperty("BaseValue");
                baseProp.SetValue(statObj, kv.Value);

                Plugin.Log.LogInfo($"Set {kv.Key} BaseValue to {kv.Value}");
            }

            Plugin.Log.LogInfo("Recalculating stats after applying gear set RPG stat changes");

            hero.HeroRPGStats.RecalculateAllStats(false);
            hero.HeroStats.RecalculateAllStats(false);

            int lv = (int)hero.CharacterStats.Level.BaseValue;
            hero.CharacterStats.RecalculateAllStats(lv, lv, false);

            Plugin.Log.LogInfo("Returning false to skip original ApplyRpgStats");
            return false;
        }
    }
}
