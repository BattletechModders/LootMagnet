
using BattleTech;
using LowVisibility.Helper;
using LowVisibility.Object;
using LowVisibility.Redzen;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LowVisibility.Helper.MapHelper;
using static LowVisibility.Helper.VisibilityHelper;

namespace LowVisibility {
    static class State {

        // -- Const and statics
        public const string ModSaveSubdir = "LowVisibility";
        public const string ModSavesDir = "ModSaves";

        // Map data
        public static MapConfig MapConfig;

        // -- Mutable state
        public static Dictionary<string, EWState> EWState = new Dictionary<string, EWState>();
        public static Dictionary<string, Dictionary<string, Locks>> PlayerActorLocks 
            = new Dictionary<string, Dictionary<string, Locks>>();
        
        // TODO: Do I need this anymore?
        public static string LastPlayerActor;

        // -- State related to ECM/effects
        public static Dictionary<string, int> ECMJammedActors = new Dictionary<string, int>();
        public static Dictionary<string, int> ECMProtectedActors = new Dictionary<string, int>();
        public static Dictionary<string, int> NarcedActors = new Dictionary<string, int>();
        public static Dictionary<string, int> TaggedActors = new Dictionary<string, int>();
        
        public static bool TurnDirectorStarted = false;
        public const int ResultsToPrecalcuate = 16384;
        public static double[] CheckResults = new double[ResultsToPrecalcuate];
        public static int CheckResultIdx = 0;

        // --- Methods Below ---
        public static void ClearStateOnCombatGameDestroyed() {
            State.EWState.Clear();
            State.PlayerActorLocks.Clear();

            State.LastPlayerActor = null;

            State.ECMJammedActors.Clear();
            State.ECMProtectedActors.Clear();
            State.NarcedActors.Clear();
            State.TaggedActors.Clear();

            State.TurnDirectorStarted = false;
        }

        public static float GetMapVisionRange() {
            if (MapConfig == null) {
                InitMapConfig();
            }
            return MapConfig == null ? 0.0f : MapConfig.visionRange;
        }

        public static float GetVisualIDRange() {
            if (MapConfig == null) {
                InitMapConfig();
            }
            return MapConfig == null ? 0.0f : MapConfig.scanRange;
        }

        public static void InitMapConfig() {
            MapConfig = MapHelper.ParseCurrentMap();            
        }

        // --- Methods for SourceActorLockStates
        public static Dictionary<string, Locks> LastActivatedLocks(CombatGameState Combat) {
            AbstractActor lastActivated = GetLastPlayerActivatedActor(Combat);
            if (!PlayerActorLocks.ContainsKey(lastActivated.GUID)) {
                PlayerActorLocks[lastActivated.GUID] = new Dictionary<string, Locks>();                
            }
            return PlayerActorLocks[lastActivated.GUID];
        }

        public static Locks LastActivatedLocksForTarget(ICombatant target) {
            Dictionary<string, Locks> locks = State.LastActivatedLocks(target.Combat);
            return locks.ContainsKey(target.GUID) ? 
                locks[target.GUID] : new Locks(State.GetLastPlayerActivatedActor(target.Combat), target);
        }

        public static void UpdateActorLocks(AbstractActor source, ICombatant target, VisualScanType visualLock, SensorScanType sensorLock) {
            if (source != null && target != null) {
                Locks newLocks = new Locks(source, target, visualLock, sensorLock);
                if (PlayerActorLocks.ContainsKey(source.GUID)) {
                    PlayerActorLocks[source.GUID][target.GUID] = newLocks;
                } else {
                    PlayerActorLocks[source.GUID] = new Dictionary<string, Locks> {
                        [target.GUID] = newLocks
                    };
                }
            }
        }

        public static Locks LocksForTarget(AbstractActor attacker, ICombatant target) {
            Locks locks = null;
            if (State.PlayerActorLocks.ContainsKey(attacker.GUID)) {
                Dictionary<string, Locks> actorLocks = State.PlayerActorLocks[attacker.GUID];
                if (actorLocks.ContainsKey(target.GUID)) {
                    locks = actorLocks[target.GUID];
                }
            }
            return locks ?? new Locks(attacker, target);
        }

        public static List<Locks> TeamLocksForTarget(ICombatant target) {
            List<Locks> allTargetLocks = new List<Locks>();
            if (State.PlayerActorLocks != null && State.PlayerActorLocks.Count > 0) {
                allTargetLocks = State.PlayerActorLocks
                    .Select(pal => pal.Value)
                    .Where(pald => pald != null && pald.ContainsKey(target.GUID))
                    .Select(pald => pald[target.GUID])
                    .ToList();                    
            }
            return allTargetLocks;
        }

        // --- Methods manipulating EWState
        public static EWState GetEWState(AbstractActor actor) {
            if (!EWState.ContainsKey(actor.GUID)) {
                LowVisibility.Logger.Log($"WARNING: StaticEWState for actor:{CombatantHelper.Label(actor)} was not found. Creating!");
                BuildEWState(actor);
            }
            return EWState[actor.GUID];
        }

        public static void BuildEWState(AbstractActor actor) {
            EWState config = new EWState(actor);
            EWState[actor.GUID] = config;
        }

        // --- Methods manipulating CheckResults
        public static void InitializeCheckResults() {
            LowVisibility.Logger.Log($"Initializing a new random buffer of size:{ResultsToPrecalcuate}");
            Xoshiro256PlusRandomBuilder builder = new Xoshiro256PlusRandomBuilder();
            IRandomSource rng = builder.Create();
            double mean = LowVisibility.Config.ProbabilityMu;
            double stdDev = LowVisibility.Config.ProbabilitySigma;
            ZigguratGaussian.Sample(rng, mean, stdDev, CheckResults);
            CheckResultIdx = 0;
        }

        public static int GetCheckResult() {
            if (CheckResultIdx < 0 || CheckResultIdx > ResultsToPrecalcuate) {
                LowVisibility.Logger.Log($"ERROR: CheckResultIdx of {CheckResultIdx} is out of bounds! THIS SHOULD NOT HAPPEN!");
            }

            double result = CheckResults[CheckResultIdx];
            CheckResultIdx++;

            // Normalize floats to integer buckets for easier comparison
            if (result > 0) {
                result = Math.Floor(result);
            } else if (result < 0) {
                result = Math.Ceiling(result);
            }

            return (int)result;
        }
        
        // The last actor that the player activated. Used to determine visibility in targetingHUD between activations

        public static AbstractActor GetLastPlayerActivatedActor(CombatGameState Combat) {
            if (LastPlayerActor == null) {
                List<AbstractActor> playerActors = HostilityHelper.PlayerActors(Combat);
                LastPlayerActor = playerActors[0].GUID;
            }
            return Combat.FindActorByGUID(LastPlayerActor);
        }

        // --- ECM JAMMING STATE TRACKING ---
        public static int ECMJamming(AbstractActor actor) {
            return ECMJammedActors.ContainsKey(actor.GUID) ? ECMJammedActors[actor.GUID] : 0;
        }

        public static void AddECMJamming(AbstractActor actor, int modifier) {
            if (!ECMJammedActors.ContainsKey(actor.GUID)) {
                ECMJammedActors.Add(actor.GUID, modifier);
            } else if (modifier > ECMJammedActors[actor.GUID]) {
                ECMJammedActors[actor.GUID] = modifier;
            }            
        }
        public static void RemoveECMJamming(AbstractActor actor) {
            if (ECMJammedActors.ContainsKey(actor.GUID)) {
                ECMJammedActors.Remove(actor.GUID);
            }            
        }

        // --- ECM PROTECTION STATE TRACKING
        public static int ECMProtection(ICombatant actor) {
            return ECMProtectedActors.ContainsKey(actor.GUID) ? ECMProtectedActors[actor.GUID] : 0;
        }

        public static void AddECMProtection(ICombatant actor, int modifier) {            
            if (!ECMProtectedActors.ContainsKey(actor.GUID)) {
                ECMProtectedActors.Add(actor.GUID, modifier);
            } else if (modifier > ECMProtectedActors[actor.GUID]) {
                ECMProtectedActors[actor.GUID] = modifier;
            }
        }
        public static void RemoveECMProtection(ICombatant actor) {
            if (ECMProtectedActors.ContainsKey(actor.GUID)) {
                ECMProtectedActors.Remove(actor.GUID);
            }
        }

        // --- ECM NARC EFFECT
        public static int NARCEffect(ICombatant actor) {
            return NarcedActors.ContainsKey(actor.GUID) ? NarcedActors[actor.GUID] : 0;
        }

        public static void AddNARCEffect(ICombatant actor, int modifier) {
            if (!NarcedActors.ContainsKey(actor.GUID)) {
                NarcedActors.Add(actor.GUID, modifier);
            } else if (modifier > NarcedActors[actor.GUID]) {
                NarcedActors[actor.GUID] = modifier;
            }
        }
        public static void RemoveNARCEffect(ICombatant actor) {
            if (NarcedActors != null && actor != null && NarcedActors.ContainsKey(actor.GUID)) {
                NarcedActors.Remove(actor.GUID);
            }
        }

        // --- ECM TAG EFFECT
        public static int TAGEffect(AbstractActor actor) {
            return TaggedActors.ContainsKey(actor.GUID) ? TaggedActors[actor.GUID] : 0;
        }

        public static void AddTAGEffect(AbstractActor actor, int modifier) {
            if (!TaggedActors.ContainsKey(actor.GUID)) {
                TaggedActors.Add(actor.GUID, modifier);
            } else if (modifier > TaggedActors[actor.GUID]) {
                TaggedActors[actor.GUID] = modifier;
            }
        }
        public static void RemoveTAGEffect(AbstractActor actor) {
            if (TaggedActors != null && actor != null && TaggedActors.ContainsKey(actor.GUID)) {
                TaggedActors.Remove(actor.GUID);
            }
        }

        // --- FILE SAVE/READ BELOW ---
        public class SerializationState {
            public Dictionary<string, EWState> staticState;
            public Dictionary<string, Dictionary<string, Locks>> PlayerActorLocks;

            public string LastPlayerActivatedActorGUID;

            public Dictionary<string, int> ecmJammedActors;
            public Dictionary<string, int> ecmProtectedActors;
            public Dictionary<string, int> narcedActors;
            public Dictionary<string, int> taggedActors;
        }

        public static void LoadStateData(string saveFileID) {
            ECMJammedActors.Clear();
            ECMProtectedActors.Clear();
            NarcedActors.Clear();
            TaggedActors.Clear();
            PlayerActorLocks.Clear();
            EWState.Clear();

            string normalizedFileID = saveFileID.Substring(5);
            FileInfo stateFilePath = CalculateFilePath(normalizedFileID);
            if (stateFilePath.Exists) {
                //LowVisibility.Logger.Log($"Reading saved state from file:{stateFilePath.FullName}.");
                // Read the file
                try {
                    SerializationState savedState = null;
                    using (StreamReader r = new StreamReader(stateFilePath.FullName)) {
                        string json = r.ReadToEnd();
                        //LowVisibility.Logger.Log($"State json is: {json}");
                        savedState = JsonConvert.DeserializeObject<SerializationState>(json);
                    }

                    // TODO: NEED TO REFRESH STATIC STATE ON ACTORS
                    State.EWState = savedState.staticState;
                    LowVisibility.Logger.Log($"  -- StaticEWState.count: {savedState.staticState.Count}");

                    State.PlayerActorLocks = savedState.PlayerActorLocks;
                    LowVisibility.Logger.Log($"  -- SourceActorLockStates.count: {savedState.PlayerActorLocks.Count}");

                    State.LastPlayerActor = savedState.LastPlayerActivatedActorGUID;
                    LowVisibility.Logger.Log($"  -- LastPlayerActivatedActorGUID: {LastPlayerActor}");

                    State.ECMJammedActors = savedState.ecmJammedActors;
                    LowVisibility.Logger.Log($"  -- ecmJammedActors.count: {savedState.ecmJammedActors.Count}");
                    State.ECMProtectedActors = savedState.ecmProtectedActors;
                    LowVisibility.Logger.Log($"  -- ecmProtectedActors.count: {savedState.ecmProtectedActors.Count}");
                    State.NarcedActors = savedState.narcedActors;
                    LowVisibility.Logger.Log($"  -- narcedActors.count: {savedState.narcedActors.Count}");
                    State.TaggedActors = savedState.taggedActors;
                    LowVisibility.Logger.Log($"  -- taggedActors.count: {savedState.taggedActors.Count}");

                    LowVisibility.Logger.Log($"Loaded save state from file:{stateFilePath.FullName}.");
                } catch (Exception e) {
                    LowVisibility.Logger.Log($"Failed to read saved state due to e: '{e.Message}'");                    
                }
            } else {
                LowVisibility.Logger.Log($"FilePath:{stateFilePath} does not exist, not loading file.");
            }
        }

        public static void SaveStateData(string saveFileID) {
            string normalizedFileID = saveFileID.Substring(5);
            FileInfo saveStateFilePath = CalculateFilePath(normalizedFileID);
            LowVisibility.Logger.Log($"Saving to filePath:{saveStateFilePath.FullName}.");
            if (saveStateFilePath.Exists) {
                // Make a backup
                saveStateFilePath.CopyTo($"{saveStateFilePath.FullName}.bak", true);
            }

            try {
                SerializationState state = new SerializationState {
                    staticState = State.EWState,
                    PlayerActorLocks = State.PlayerActorLocks,

                    LastPlayerActivatedActorGUID = State.LastPlayerActor,

                    ecmJammedActors = State.ECMJammedActors,
                    ecmProtectedActors = State.ECMProtectedActors,
                    narcedActors = State.NarcedActors,
                    taggedActors = State.TaggedActors
                };
                            
                using (StreamWriter w = new StreamWriter(saveStateFilePath.FullName, false)) {
                    string json = JsonConvert.SerializeObject(state);
                    w.Write(json);
                    LowVisibility.Logger.Log($"Persisted state to file:{saveStateFilePath.FullName}.");
                }
            } catch (Exception e) {
                LowVisibility.Logger.Log($"Failed to persist to disk at path {saveStateFilePath.FullName} due to error: {e.Message}");
            }
        }

        private static FileInfo CalculateFilePath(string saveID) {
            // Starting path should be battletech\mods\KnowYourFoe
            DirectoryInfo modsDir = Directory.GetParent(LowVisibility.ModDir);
            DirectoryInfo battletechDir = modsDir.Parent;

            // We want to write to Battletech\ModSaves\<ModName>
            DirectoryInfo modSavesDir = battletechDir.CreateSubdirectory(ModSavesDir);
            DirectoryInfo modSaveSubdir = modSavesDir.CreateSubdirectory(ModSaveSubdir);
            LowVisibility.Logger.Log($"Mod saves will be written to: ({modSaveSubdir.FullName}).");

            //Finally combine the paths
            string campaignFilePath = Path.Combine(modSaveSubdir.FullName, $"{saveID}.json");
            LowVisibility.Logger.Log($"campaignFilePath is: ({campaignFilePath}).");
            return new FileInfo(campaignFilePath);
        }

    }

    
}
