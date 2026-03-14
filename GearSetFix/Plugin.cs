using Awaken.TG.Main.Memories;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace GearSetFix
{
    [BepInPlugin(
        "KitsuneRhin.GearSetFix",
        "GearSetFix",
        "1.0.0")
     ]

    public class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("GearSetFix initializing");
            _harmony = new Harmony("KitsuneRhin.GearSetFix");

            try
            {
                _harmony.PatchAll();
            }
            catch (System.Exception ex)
            {
                Log.LogError($"Error patching: {ex}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                _harmony?.UnpatchSelf();
                Log.LogInfo("GearSetFix unpatched successfully");
            }
            catch (System.Exception ex)
            {
                Log.LogWarning($"Error unpatching: {ex}");
            }
        }
    }
}
