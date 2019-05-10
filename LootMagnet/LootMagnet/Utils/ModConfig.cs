
using System.Collections.Generic;
using System.Linq;
using static LootMagnet.LootMagnet;

namespace LootMagnet {

    public enum Rep {
        LOATHED,
        HATED,
        DISLIKED,
        INDIFFERENT,
        LIKED,
        FRIENDLY,
        HONORED,
        ALLIED
    }

    public class RepCfg {
        public Rep Reputation = Rep.INDIFFERENT;
        public float RollupMultiComponent = 0f;
        public float RollupMultiMech = 0f;
        public float HoldbackTrigger = 0f;
        public float HoldbackValueCapMulti = 0f;
    }

    public class HoldbackCfg {

        public int[] MechParts = new int[] { 1, 6 };

        public int[] ReputationRange = new int[] { 2, 6 };

        public int[] DisputePicks = new int[] { 1, 6 };

        // The base % success rate for a dispute
        public float DisputeSuccessBase = 50.0f;

        // How much your MRB rating (from 0-5) impacts your dispute success
        public float DisputeMRBSuccessFactor = 10.0f;

        // The factor that randomly modifies your success
        public float DisputeSuccessRandomBound = 0.2f;

        // How much of a fee you have to pay to dispute a contract
        public float DisputeMRBFeeFactor = -0.1f;

    }

    public class ModConfig {

        // If true, many logs will be printed
        public bool Debug = false;

        public bool DeveloperMode = false;

        // The values used to define the base amounts for rollup
        public float[] RollupMRBValue = new float[] { 40000f, 60000f, 90000f, 130000f, 180000f, 240000f };

        public List<RepCfg> Reputation = new List<RepCfg>() {};

        public HoldbackCfg Holdback = new HoldbackCfg();

        public void InitDefaultReputation() {
            if (Reputation.Count == 0) {
                Reputation = new List<RepCfg>() {
                    new RepCfg{ Reputation = Rep.LOATHED, RollupMultiComponent = 0f, RollupMultiMech = 0f, HoldbackTrigger = 60f, HoldbackValueCapMulti = 0.2f },
                    new RepCfg{ Reputation = Rep.HATED, RollupMultiComponent = 0f, RollupMultiMech = 0f, HoldbackTrigger = 48f, HoldbackValueCapMulti = 0.3f },
                    new RepCfg{ Reputation = Rep.DISLIKED, RollupMultiComponent = 0f, RollupMultiMech = 0f, HoldbackTrigger = 32f, HoldbackValueCapMulti = 0.4f },
                    new RepCfg{ Reputation = Rep.INDIFFERENT, RollupMultiComponent = 1f, RollupMultiMech = 0f, HoldbackTrigger = 16f, HoldbackValueCapMulti = 0.6f },
                    new RepCfg{ Reputation = Rep.LIKED, RollupMultiComponent = 5f, RollupMultiMech = 0f, HoldbackTrigger = 8f, HoldbackValueCapMulti = 0.8f },
                    new RepCfg{ Reputation = Rep.FRIENDLY, RollupMultiComponent = 9f, RollupMultiMech = 20f, HoldbackTrigger = 4f, HoldbackValueCapMulti = 1f },
                    new RepCfg{ Reputation = Rep.HONORED, RollupMultiComponent = 13f, RollupMultiMech = 30f, HoldbackTrigger = 2f, HoldbackValueCapMulti = 1.25f },
                    new RepCfg{ Reputation = Rep.ALLIED, RollupMultiComponent = 21f, RollupMultiMech = 180f, HoldbackTrigger = 1f, HoldbackValueCapMulti = 2f },
                };
            }
        }

        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");

            Mod.Log.Info($"  DEBUG: {this.Debug}");

            string rollupMRBVal = string.Join(", ", RollupMRBValue.Select(v => v.ToString("0.00")).ToArray());
            Mod.Log.Info($"  MRB Rollup Values: {rollupMRBVal}");

            Mod.Log.Info($"FACTION REPUTATION VALUES");
            foreach (RepCfg factionCfg in Reputation) {
                Mod.Log.Info($"  Reputation:{factionCfg.Reputation} ComponentRollup:{factionCfg.RollupMultiComponent} MechRollup:{factionCfg.RollupMultiMech} HoldbackTrigger:{factionCfg.HoldbackTrigger}%");
            }

            Mod.Log.Info($"HOLDBACK VALUES");
            //Mod.Logger.Log($"  Holdback Picks: {Holdback.PickRange[0]} to {Holdback.PickRange[1]}");

            //Mod.Logger.Log($"  Reputation ACCEPT:x{Holdback.RepMultiAccept} REFUSE:x{Holdback.RepMultiRefuse} DISPUTE_MRB:x{Holdback.RepMultiDisputeMRB}");
            //Mod.Logger.Log($"  Reputation DISPUTE_SUCCESS:x{Holdback.RepMultiDisputeSuccess} DISPUTE_FAIL:x{Holdback.RepMultiDisputeFail} DISPUTE_CRIT_FAIL:x{Holdback.RepMultiDisputeCriticalFail}");

            //Mod.Logger.Log($"  Dispute Chance BASE:{Holdback.DisputeSuccessBase}% CRIT_CHANCE:{Holdback.DisputeCritChance}% " +
            //    $"MRB_FACTOR:{Holdback.DisputeMRBSuccessFactor}% RANDOM:{Holdback.DisputeSuccessRandomBound}%");

            //Mod.Logger.Log($"  MRB_Fees:x{Holdback.DisputeMRBFeeFactor} FAIL_PAYOUT:x{Holdback.DisputeFailPayoutFactor} CRIT_FAIL_PAYOUT:X{Holdback.DisputeCritFailPayoutFactor}");

            Mod.Log.Info("=== MOD CONFIG END ===");
        }
    }
}
