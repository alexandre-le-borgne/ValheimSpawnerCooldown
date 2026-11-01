using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ValheimSpawnerCooldown
{
    // SpawnArea.UpdateSpawn only runs on the owner of the spawner's ZDO, so the applied
    // interval matters only there. Ownership rules between clients running this mod:
    // - the owner stamps its session ID on the spawner ZDO (ModOwnerKey);
    // - a spawner whose stamp matches its current owner is never claimed;
    // - otherwise only the candidate with the lowest session ID in trigger range claims it,
    //   candidates being players whose own ZDO carries CandidateKey.
    // Simultaneous claims would leave both clients believing they own the ZDO, because
    // ZDOMan only accepts an incoming owner change with a strictly higher OwnerRevision.
    internal class SpawnerControl : MonoBehaviour
    {
        private const float TickSeconds = 2f;

        private static readonly int ModOwnerKey = "SpawnerCooldown_ModOwner".GetStableHashCode();
        private static readonly int CandidateKey = "SpawnerCooldown_Candidate".GetStableHashCode();

        private static readonly List<SpawnerControl> s_instances = new List<SpawnerControl>();
        private static readonly HashSet<string> s_loggedPrefabs = new HashSet<string>();
        private static readonly List<Player> s_playersInRange = new List<Player>();

        private delegate void GetInstancesFn(SpawnArea area, out int near, out int total);

        private static readonly GetInstancesFn s_getInstances =
            AccessTools.MethodDelegate<GetInstancesFn>(AccessTools.Method(typeof(SpawnArea), "GetInstances"));

        private SpawnArea m_area;
        private ZNetView m_nview;
        private string m_prefabName;
        private float m_vanillaInterval;

        // Natural phase: the first m_maxNear spawns since this client became owner use the
        // vanilla interval, unless the creature cap is reached first. Reset while another
        // client owns the spawner, and implicitly when the spawner is reloaded.
        private int m_naturalSpawns;
        private bool m_naturalPhaseOver;

        private bool InNaturalPhase => Plugin.NaturalFirstWave.Value && !m_naturalPhaseOver;

        internal static void Attach(SpawnArea area)
        {
            ZNetView nview = area.GetComponentInParent<ZNetView>();
            if (nview == null)
            {
                return;
            }

            SpawnerControl control = area.gameObject.AddComponent<SpawnerControl>();
            control.m_area = area;
            control.m_nview = nview;
            control.m_prefabName = Utils.GetPrefabName(nview.gameObject);
            control.m_vanillaInterval = area.m_spawnIntervalSec;
            control.Apply();
            control.LogPrefabOnce();
            s_instances.Add(control);
        }

        internal static void ApplyAll()
        {
            foreach (SpawnerControl control in s_instances)
            {
                control.Apply();
            }
        }

        internal static void UpdateLocalCandidateFlag()
        {
            Player player = Player.m_localPlayer;
            ZNetView nview = player != null ? player.GetComponent<ZNetView>() : null;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            long flag = Plugin.Enabled.Value && Plugin.ClaimOwnership.Value ? 1L : 0L;
            ZDO zdo = nview.GetZDO();
            if (zdo.GetLong(CandidateKey) != flag)
            {
                zdo.Set(CandidateKey, flag);
            }
        }

        private void Start()
        {
            InvokeRepeating(nameof(Tick), Random.Range(0f, TickSeconds), TickSeconds);
        }

        private void OnDestroy()
        {
            s_instances.Remove(this);
        }

        internal void OnSpawnAttempt(bool spawned)
        {
            if (!InNaturalPhase)
            {
                return;
            }

            if (spawned)
            {
                m_naturalSpawns++;
            }
            if (m_naturalSpawns < m_area.m_maxNear && !IsAtCap())
            {
                return;
            }

            m_naturalPhaseOver = true;
            Apply();
            if (Plugin.DebugLog.Value)
            {
                Plugin.Log.LogInfo($"{m_prefabName} at {transform.position}: natural phase over after {m_naturalSpawns} spawn(s).");
            }
        }

        private void Apply()
        {
            m_area.m_spawnIntervalSec = InNaturalPhase
                ? m_vanillaInterval
                : SpawnerSettings.GetInterval(m_prefabName, m_vanillaInterval);
        }

        private bool IsAtCap()
        {
            s_getInstances(m_area, out int near, out int total);
            return near >= m_area.m_maxNear || total >= m_area.m_maxTotal;
        }

        private void ResetNaturalPhase()
        {
            if (m_naturalSpawns == 0 && !m_naturalPhaseOver)
            {
                return;
            }
            m_naturalSpawns = 0;
            m_naturalPhaseOver = false;
            Apply();
        }

        private void Tick()
        {
            if (!m_nview.IsValid())
            {
                return;
            }

            ZDO zdo = m_nview.GetZDO();
            long self = ZDOMan.GetSessionID();
            bool active = Plugin.Enabled.Value;

            if (zdo.IsOwner())
            {
                long stamp = active ? self : 0L;
                if (zdo.GetLong(ModOwnerKey) != stamp)
                {
                    zdo.Set(ModOwnerKey, stamp);
                }
                return;
            }

            ResetNaturalPhase();
            if (!active || !Plugin.ClaimOwnership.Value || !zdo.HasOwner())
            {
                return;
            }

            long owner = zdo.GetOwner();
            if (zdo.GetLong(ModOwnerKey) == owner || !IsElected(self))
            {
                return;
            }

            m_nview.ClaimOwnership();
            zdo.Set(ModOwnerKey, self);
            if (Plugin.DebugLog.Value)
            {
                Plugin.Log.LogInfo($"Claimed {m_prefabName} at {transform.position} from owner {owner}.");
            }
        }

        private bool IsElected(long self)
        {
            s_playersInRange.Clear();
            Player.GetPlayersInRange(m_area.transform.position, m_area.m_triggerDistance, s_playersInRange);

            long lowest = long.MaxValue;
            foreach (Player player in s_playersInRange)
            {
                ZNetView nview = player.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid())
                {
                    continue;
                }

                ZDO zdo = nview.GetZDO();
                long uid = zdo.GetOwner();
                if (uid != 0L && uid < lowest && zdo.GetLong(CandidateKey) != 0L)
                {
                    lowest = uid;
                }
            }
            return lowest == self;
        }

        private void LogPrefabOnce()
        {
            if (!Plugin.DebugLog.Value || !s_loggedPrefabs.Add(m_prefabName))
            {
                return;
            }

            Plugin.Log.LogInfo(
                $"{m_prefabName}: interval {m_vanillaInterval}s -> {SpawnerSettings.GetInterval(m_prefabName, m_vanillaInterval)}s, " +
                $"triggerDistance {m_area.m_triggerDistance}, maxNear {m_area.m_maxNear}, maxTotal {m_area.m_maxTotal}.");
        }
    }
}
