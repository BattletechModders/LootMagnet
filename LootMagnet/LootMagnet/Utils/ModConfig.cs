
using System.Collections.Generic;
using System.Linq;

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
    }

    public class HoldbackCfg {
        public int[] PickRange = new int[] { 2, 6 };

        // Faction reputation gain is you accept a dispute
        public float RepMultiAccept = 0.5f;

        // Faction reputation loss if you refuse a dispute
        public float RepMultiRefuse = -2.5f;

        // MRB Reputation loss when demanding a dispute
        public float RepMultiDisputeMRB = -0.3f;

        // Reptuation loss if the dispute succeeded
        public float RepMultiDisputeSuccess = -0.8f;

        // Reptuation loss if the dispute failed
        public float RepMultiDisputeFail = -0.3f;

        // Reptuation loss if the dispute critically failed
        public float RepMultiDisputeCriticalFail = -0.1f;

        // The base % success rate for a dispute
        public float DisputeSuccessBase = 40.0f;

        // The critical fail/success rate for a dispute
        public float DisputeCritChance = 5.0f;

        // How much your MRB rating (from 0-5) impacts your dispute success
        public float DisputeMRBSuccessFactor = 10.0f;

        // A random amount that reduces your success chances
        public int DisputeSuccessRandomBound = 10;

        // How much of a fee you have to pay to dispute a contract
        public float DisputeMRBFeeFactor = -0.1f;

        // How much of a fee you pay in a failed dispute
        public float DisputeFailPayoutFactor = -0.5f;

        // How much of a fee you pay in a critically failed dispute
        public float DisputeCritFailPayoutFactor = -1.5f;
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
                    new RepCfg{ Reputation = Rep.LOATHED, RollupMultiComponent = 0f, RollupMultiMech = 0f, HoldbackTrigger = 60f },
                    new RepCfg{ Reputation = Rep.HATED, RollupMultiComponent = 0f, RollupMultiMech = 0f, HoldbackTrigger = 48f },
                    new RepCfg{ Reputation = Rep.DISLIKED, RollupMultiComponent = 0f, RollupMultiMech = 0f, HoldbackTrigger = 32f },
                    new RepCfg{ Reputation = Rep.INDIFFERENT, RollupMultiComponent = 1f, RollupMultiMech = 0f, HoldbackTrigger = 16f },
                    new RepCfg{ Reputation = Rep.LIKED, RollupMultiComponent = 5f, RollupMultiMech = 0f, HoldbackTrigger = 8f },
                    new RepCfg{ Reputation = Rep.FRIENDLY, RollupMultiComponent = 9f, RollupMultiMech = 20f, HoldbackTrigger = 4f },
                    new RepCfg{ Reputation = Rep.HONORED, RollupMultiComponent = 13f, RollupMultiMech = 30f, HoldbackTrigger = 2f },
                    new RepCfg{ Reputation = Rep.ALLIED, RollupMultiComponent = 21f, RollupMultiMech = 180f, HoldbackTrigger = 1f },
                };
            }
        }

        public void LogConfig() {
            LootMagnet.Logger.Log("=== MOD CONFIG BEGIN ===");

            LootMagnet.Logger.Log($"  DEBUG: {this.Debug}");

            string rollupMRBVal = string.Join(", ", RollupMRBValue.Select(v => v.ToString("0.00")).ToArray());
            LootMagnet.Logger.Log($"  MRB Rollup Values: {rollupMRBVal}");

            LootMagnet.Logger.Log($"FACTION REPUTATION VALUES");
            foreach (RepCfg factionCfg in Reputation) {
                LootMagnet.Logger.Log($"  Reputation:{factionCfg.Reputation} ComponentRollup:{factionCfg.RollupMultiComponent} MechRollup:{factionCfg.RollupMultiMech} HoldbackTrigger:{factionCfg.HoldbackTrigger}%");
            }

            LootMagnet.Logger.Log($"HOLDBACK VALUES");
            LootMagnet.Logger.Log($"  Holdback Picks: {Holdback.PickRange[0]} to {Holdback.PickRange[1]}");

            LootMagnet.Logger.Log($"  Reputation ACCEPT:x{Holdback.RepMultiAccept} REFUSE:x{Holdback.RepMultiRefuse} DISPUTE_MRB:x{Holdback.RepMultiDisputeMRB}");
            LootMagnet.Logger.Log($"  Reputation DISPUTE_SUCCESS:x{Holdback.RepMultiDisputeSuccess} DISPUTE_FAIL:x{Holdback.RepMultiDisputeFail} DISPUTE_CRIT_FAIL:x{Holdback.RepMultiDisputeCriticalFail}");

            LootMagnet.Logger.Log($"  Dispute Chance BASE:{Holdback.DisputeSuccessBase}% CRIT_CHANCE:{Holdback.DisputeCritChance}% " +
                $"MRB_FACTOR:{Holdback.DisputeMRBSuccessFactor}% RANDOM:{Holdback.DisputeSuccessRandomBound}%");

            LootMagnet.Logger.Log($"  MRB_Fees:x{Holdback.DisputeMRBFeeFactor} FAIL_PAYOUT:x{Holdback.DisputeFailPayoutFactor} CRIT_FAIL_PAYOUT:X{Holdback.DisputeCritFailPayoutFactor}");

            LootMagnet.Logger.Log("=== MOD CONFIG END ===");
        }
    }
}
