using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ValheimSpawnerCooldown
{
    // Spawner prefabs only exist once ZNetScene is loaded with a world, so the prefabs and
    // vanilla intervals found there are cached in the config to list them from game start.
    internal static class SpawnerSettings
    {
        private const string Section = "Spawners";

        internal static readonly AcceptableValueRange<float> MultiplierRange = new AcceptableValueRange<float>(0.1f, 20f);

        private static readonly Dictionary<string, ConfigEntry<float>> s_multipliers = new Dictionary<string, ConfigEntry<float>>();
        private static readonly Dictionary<string, float> s_vanillaIntervals = new Dictionary<string, float>();

        private static ConfigFile s_config;
        private static ConfigEntry<string> s_knownSpawners;

        internal static void Init(ConfigFile config)
        {
            s_config = config;
            s_knownSpawners = config.Bind("Internal", "KnownSpawners", "",
                new ConfigDescription(
                    "Spawner prefabs and vanilla intervals found in the last loaded world, used to list them before a world is loaded. Maintained by the mod.",
                    null, new ConfigurationManagerAttributes { Browsable = false }));

            foreach (string item in s_knownSpawners.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = item.LastIndexOf(':');
                if (separator > 0
                    && float.TryParse(item.Substring(separator + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float vanilla))
                {
                    Register(item.Substring(0, separator), vanilla);
                }
            }
        }

        internal static float GetInterval(string prefabName, float vanilla)
        {
            if (!Plugin.Enabled.Value)
            {
                return vanilla;
            }
            float specific = s_multipliers.TryGetValue(prefabName, out ConfigEntry<float> entry) ? entry.Value : 1f;
            return vanilla * Plugin.GlobalMultiplier.Value * specific;
        }

        internal static void RegisterFromPrefabs(IEnumerable<GameObject> prefabs)
        {
            foreach (GameObject prefab in prefabs)
            {
                SpawnArea area = prefab != null ? prefab.GetComponentInChildren<SpawnArea>(true) : null;
                if (area != null)
                {
                    Register(prefab.name, area.m_spawnIntervalSec);
                }
            }

            string known = string.Join(";", s_vanillaIntervals
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Key + ":" + pair.Value.ToString(CultureInfo.InvariantCulture))
                .ToArray());
            if (s_knownSpawners.Value != known)
            {
                s_knownSpawners.Value = known;
            }
        }

        private static void Register(string prefabName, float vanilla)
        {
            s_vanillaIntervals[prefabName] = vanilla;
            if (s_multipliers.ContainsKey(prefabName))
            {
                return;
            }

            ConfigEntry<float> entry = s_config.Bind(Section, prefabName, 1f,
                new ConfigDescription(
                    $"Multiplier of the {prefabName} spawn interval (vanilla {vanilla.ToString("0.#", CultureInfo.InvariantCulture)} s), applied on top of GlobalMultiplier.",
                    MultiplierRange, new ConfigurationManagerAttributes { CustomDrawer = DrawMultiplier }));
            entry.SettingChanged += (_, _) => SpawnerControl.ApplyAll();
            s_multipliers[prefabName] = entry;
        }

        private static void DrawMultiplier(ConfigEntryBase entry)
        {
            float value = (float)entry.BoxedValue;
            float slider = GUILayout.HorizontalSlider(value, MultiplierRange.MinValue, MultiplierRange.MaxValue, GUILayout.ExpandWidth(true));
            if (!Mathf.Approximately(slider, value))
            {
                entry.BoxedValue = Mathf.Round(slider * 10f) / 10f;
            }

            string prefabName = entry.Definition.Key;
            float vanilla = s_vanillaIntervals[prefabName];
            float interval = GetInterval(prefabName, vanilla);
            GUILayout.Label($"x{value:0.0} = {interval:0.#} s (vanilla {vanilla:0.#} s)", GUILayout.ExpandWidth(false));
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    internal static class ZNetSceneAwakePatch
    {
        private static void Postfix(ZNetScene __instance)
        {
            SpawnerSettings.RegisterFromPrefabs(__instance.m_prefabs);
        }
    }
}
