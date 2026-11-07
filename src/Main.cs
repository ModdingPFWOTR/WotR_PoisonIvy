using System;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using UnityModManagerNet;

namespace PoisonIvy
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry;
        public static Harmony HarmonyInstance;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            HarmonyInstance = new Harmony(modEntry.Info.Id);
            modEntry.OnGUI = OnGUI;

            try
            {
                HarmonyInstance.PatchAll();
                Log("Poison Ivy loaded.");
            }
            catch (Exception ex)
            {
                Log("Harmony patch failed: " + ex);
                return false;
            }

            return true;
        }

        public static void Log(string message)
        {
            ModEntry?.Logger.Log(message);
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UnityEngine.GUILayout.Label("Adds the Poison Ivy druid archetype, based on Nature Fang from Tabletop Tweaks.");
            UnityEngine.GUILayout.Label("Create a new Druid and choose Poison Ivy.");
        }
    }

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    static class BlueprintsReadyPatch
    {
        static void Postfix()
        {
            try
            {
                Content.Create();
            }
            catch (Exception ex)
            {
                Main.Log("Poison Ivy setup failed: " + ex);
            }
        }
    }
}
