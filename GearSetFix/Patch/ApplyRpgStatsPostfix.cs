using Awaken.TG.Main.Heroes;
using GearSetsMod.Core;
using GearSetsMod.UI;
using HarmonyLib;
using System;
using System.Reflection;

namespace GearSetFix.Patch
{
	[HarmonyPatch(typeof(GearSetsUI), "ApplyRpgStats", new Type[] { typeof(Hero), typeof(GearSet) })]
	public static class ApplyRpgStatsPostfix
	{
		static void Postfix(Hero hero, GearSet set)
		{
			Plugin.Log.LogInfo("Postfix entered for ApplyRpgStats");

			if (hero == null)
			{
				Plugin.Log.LogWarning("Hero is null in postfix");
				return;
			}

			var heroRpg = hero.HeroRPGStats;
			if (heroRpg == null)
			{
				Plugin.Log.LogWarning("HeroRPGStats is null in postfix");
				return;
			}

			foreach (var kv in set.RpgStats)
			{
				string statName = kv.Key;
				float desired = kv.Value;
				try
				{
					Plugin.Log.LogInfo($"Processing stat {statName} => {desired}");

					// 1. find property on heroRpg
					var prop = heroRpg.GetType()?.GetProperty(statName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (prop == null)
					{
						Plugin.Log.LogWarning($"Property {statName} not found on HeroRPGStats");
						continue;
					}

					var statObj = prop?.GetValue(heroRpg);
					if (statObj == null)
					{
						Plugin.Log.LogWarning($"Stat object for {statName} was null");
						continue;
					}

					bool wrote = false;

					// Strategy A: call SetTo(float) if available
					var setToMethod = statObj.GetType().GetMethod("SetTo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(float), typeof(bool), typeof(object) }, null)
									 ?? statObj.GetType().GetMethod("SetTo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(float) }, null);
					if (setToMethod != null)
					{
						try
						{
							// prefer the simple overload if present
							if (setToMethod.GetParameters().Length == 1)
							{
								setToMethod.Invoke(statObj, new object[] { desired });
							}
							else
							{
								// try SetTo(value, runHooks: false, context: null) or similar
								var parms = setToMethod.GetParameters();
								object[] args = new object[parms.Length];
								args[0] = desired;
								for (int i = 1; i < parms.Length; i++) args[i] = Type.Missing;
								setToMethod.Invoke(statObj, args);
							}
							Plugin.Log.LogInfo($"Wrote {statName} via SetTo()");
							wrote = true;
						}
						catch (Exception ex)
						{
							Plugin.Log.LogWarning($"SetTo() failed for {statName}: {ex.Message}");
						}
					}

					// Strategy B: set BaseValue property
					if (!wrote)
					{
						var baseProp = statObj.GetType().GetProperty("BaseValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (baseProp != null && baseProp.CanWrite)
						{
							try
							{
								baseProp.SetValue(statObj, desired);
								Plugin.Log.LogInfo($"Wrote {statName} via BaseValue property");
								wrote = true;
							}
							catch (Exception ex)
							{
								Plugin.Log.LogWarning($"Setting BaseValue failed for {statName}: {ex.Message}");
							}
						}
					}

					// Strategy C: set backing field
					if (!wrote)
					{
						var field = statObj.GetType().GetField("_baseValue", BindingFlags.Instance | BindingFlags.NonPublic)
								 ?? statObj.GetType().GetField("BaseValue", BindingFlags.Instance | BindingFlags.NonPublic);
						if (field != null)
						{
							try
							{
								field.SetValue(statObj, desired);
								Plugin.Log.LogInfo($"Wrote {statName} via backing field {field.Name}");
								wrote = true;
							}
							catch (Exception ex)
							{
								Plugin.Log.LogWarning($"Setting field failed for {statName}: {ex.Message}");
							}
						}
					}

					if (!wrote)
					{
						Plugin.Log.LogError($"All write attempts failed for {statName}");
					}
				}
				catch (Exception ex)
				{
					Plugin.Log.LogError($"Unhandled exception while processing {kv.Key}: {ex}");
				}
			}

			// Recalculate pipeline
			try
			{
				Plugin.Log.LogInfo("Triggering recalculation pipeline");
				hero.HeroRPGStats.RecalculateAllStats(false);
				hero.HeroStats.RecalculateAllStats(false);
				int lv = (int)hero.CharacterStats.Level.BaseValue;
				hero.CharacterStats.RecalculateAllStats(lv, lv, false);
				Plugin.Log.LogInfo("Recalculation pipeline complete");
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError($"Recalculation pipeline failed: {ex}");
			}

			// Debug: verification snapshot
			try
			{
				Plugin.Log.LogInfo("Verification snapshot of RPG stats:");
				foreach (var name in new[] { "Strength", "Dexterity", "Perception", "Endurance", "Practicality", "Spirituality" })
				{
					var p = hero.HeroRPGStats.GetType()?.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (p == null) { Plugin.Log.LogWarning($"Verify: {name} property missing"); continue; }
					var s = p?.GetValue(hero.HeroRPGStats);
					if (s == null) { Plugin.Log.LogWarning($"Verify: {name} stat null"); continue; }
					var baseProp = s.GetType()?.GetProperty("BaseValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					var modProp = s.GetType()?.GetProperty("ModifiedValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					var baseVal = baseProp?.GetValue(s);
					var modVal = modProp?.GetValue(s);
					Plugin.Log.LogInfo($"{name} BaseValue={baseVal} ModifiedValue={modVal}");
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogWarning($"Verification snapshot failed: {ex.Message}");
			}
			Plugin.Log.LogInfo("Postfix complete, exiting.");
        }
	}
}
