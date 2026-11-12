using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace MyMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.example.mymod";
        public const string PluginName = "MyMod";
        public const string PluginVersion = "1.0.0";

        private Harmony _harmony;

        private void Awake()
        {
            Logger.LogInfo($"{PluginName} v{PluginVersion} is loaded!");

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}

