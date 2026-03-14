using Awaken.TG.Main.Heroes;
using GearSetsMod.UI;
using HarmonyLib;
using System;

namespace GearSetFix.Patch
{
    [HarmonyPatch(typeof(GearSetsUI), "RecalculateStats",
        new Type[] { typeof(Hero) })]
    public static class DisableGearSetModRecalc
    {
        static bool Prefix(Hero hero)
        {
            Plugin.Log.LogInfo("Intercepting trigger for RecalculateStats - skipping original method");
            return false; // Prevent GearSetsUI.RecalculateStats method from running; postfix will run instead using game's internal recalculation
        }
    }
}
