using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ValheimSpawnerCooldown
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "alex83690.valheim.spawnercooldown";
        public const string Name = "Spawner Cooldown";
        public const string Version = "1.0.1";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> ClaimOwnership;
        internal static ConfigEntry<bool> DebugLog;
        internal static ConfigEntry<bool> NaturalFirstWave;
        internal static ConfigEntry<float> GlobalMultiplier;

        private float m_flagTimer;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Apply the configured spawn intervals.");
            ClaimOwnership = Config.Bind("General", "ClaimOwnership", true,
                "Take network ownership of a spawner in range when its owner does not run this mod. " +
                "Spawners only run on their owner, so without this the vanilla interval applies whenever another player owns them.");
            DebugLog = Config.Bind("General", "DebugLog", false,
                "Log each spawner prefab with its vanilla and applied values, every ownership claim and the end of each natural phase.");
            NaturalFirstWave = Config.Bind("General", "NaturalFirstWave", true,
                "When this client becomes owner of a spawner, keep the vanilla interval for its first maxNear spawns, " +
                "or until its creature cap is reached, then apply the configured interval.");
            GlobalMultiplier = Config.Bind("Interval", "GlobalMultiplier", 1f,
                new ConfigDescription(
                    "Multiplier applied to every spawner interval, combined with the spawner's own multiplier. 1 = vanilla. " +
                    "Spawners tick every 2 seconds, so intervals below 2 s behave like 2 s.",
                    SpawnerSettings.MultiplierRange));

            Enabled.SettingChanged += (_, _) => SpawnerControl.ApplyAll();
            NaturalFirstWave.SettingChanged += (_, _) => SpawnerControl.ApplyAll();
            GlobalMultiplier.SettingChanged += (_, _) => SpawnerControl.ApplyAll();
            SpawnerSettings.Init(Config);

            new Harmony(Guid).PatchAll();
        }

        private void Update()
        {
            m_flagTimer -= Time.deltaTime;
            if (m_flagTimer > 0f)
            {
                return;
            }
            m_flagTimer = 1f;
            SpawnerControl.UpdateLocalCandidateFlag();
        }
    }

    [HarmonyPatch(typeof(SpawnArea), "Awake")]
    internal static class SpawnAreaAwakePatch
    {
        private static void Postfix(SpawnArea __instance)
        {
            SpawnerControl.Attach(__instance);
        }
    }

    [HarmonyPatch(typeof(SpawnArea), "SpawnOne")]
    internal static class SpawnAreaSpawnOnePatch
    {
        private static void Postfix(SpawnArea __instance, bool __result)
        {
            SpawnerControl control = __instance.GetComponent<SpawnerControl>();
            if (control != null)
            {
                control.OnSpawnAttempt(__result);
            }
        }
    }
}
