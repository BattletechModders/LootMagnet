
using System.Collections.Generic;
using System.Linq;

namespace LootMagnet
{

    public enum Rep
    {
        LOATHED,
        HATED,
        DISLIKED,
        INDIFFERENT,
        LIKED,
        FRIENDLY,
        HONORED,
        ALLIED
    }

    public class RepCfg
    {
        public Rep Reputation = Rep.INDIFFERENT;
        public float RollupMultiComponent = 0f;
        public float RollupMultiMech = 0f;
        public float HoldbackTrigger = 0f;
        public float HoldbackValueCapMulti = 0f;
    }

    public class HoldbackCfg
    {

        public bool Enabled = true;

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

    public class ModConfig
    {

        // If true, many logs will be printed
        public bool Debug = false;

        public bool DeveloperMode = false;

        // The values used to define the base amounts for rollup
        public float[] RollupMRBValue = new float[] { 40000f, 60000f, 90000f, 130000f, 180000f, 240000f };

        public List<string> RollupBlacklist = new List<string>();
        public List<string> RollupBlacklistTags = new List<string>();

        public int CompensationMaxRollupQuantity = 10;

        public List<RepCfg> Reputation = new List<RepCfg>() { };

        public HoldbackCfg Holdback = new HoldbackCfg();

        public void InitDefaultReputation()
        {
            if (Reputation.Count == 0)
            {
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


        public const string DT_DISPUTE_TITLE = "DISPUTE_TITLE";
        public const string DT_DISPUTE_TEXT = "DISPUTE_TEXT";
        public const string DT_FAILED_TITLE = "DISPUTE_FAIL_TITLE";
        public const string DT_FAILED_TEXT = "DISPUTE_FAIL_TEXT";
        public const string DT_SUCCESS_TITLE = "DISPUTE_SUCCESS_TITLE";
        public const string DT_SUCCESS_TEXT = "DISPUTE_SUCCESS_TEXT";
        public const string DT_ITEM_AND_QUANTITY = "ITEM_AND_QUANTITY";

        public const string DT_BUTTON_ACCEPT = "BUTTON_ACCEPT";
        public const string DT_BUTTON_REFUSE = "BUTTON_REFUSE";
        public const string DT_BUTTON_DISPUTE = "BUTTON_DISPUTE";
        public const string DT_BUTTON_OK = "BUTTON_OK";

        public Dictionary<string, string> DialogText = new Dictionary<string, string>() {
            {  DT_DISPUTE_TITLE, "DISPUTED SALVAGE" },
            {  DT_DISPUTE_TEXT,  "<b>I'm sorry commander, but Section A, Sub-Section 3, Paragraph ii...</b>\n\n" +
                "As the salvage crew picks over the battlefield, you are contacted by the {0} representative. " +
                "They insist the contract terms allows them first rights to the following items:" +
                "\n\n{1}\n\n" +
                "They offer to add the following to the <b>salvage pool</b> in exchange:" +
                "\n\n{2}\n\n" +
                "You may choose to:\n" +
                "<b>Refuse</b>: the disputed salvage is retained, you <b>lose</b> <color=#FF0000>{3}</color> rep.\n" +
                "<b>Accept</b>: the disputed salvage is lost, exchanged items are added to the <b>salvage pool</b>, " +
                "you gain <b>gain</b> <color=#00FF00>{4:+0}</color> rep.\n" +
                "<b>Dispute</b>: you pay <color=#FF0000>{5}</color> in legal fees, and have:\n" +
                    "<line-indent=2px> - {6}% to keep the disputed salvage, and the salvage pool" +
                    "gains {7}-{8} from the compensation offer.\n" +
                    "<line-indent=2px> - {9}% to lose the disputed salvage, and " +
                    "an additional {10}-{11} selections in the salvage pool.\n"
            },

            {  DT_FAILED_TITLE, "FAILED DISPUTE" },
            {  DT_FAILED_TEXT,
                "<b>Judge</b>: Counselor, what evidence do you offer for this new plea of insanity?\n\n" +
                "<b>Attorney</b>: Well, for one, they done hired me to represent them.\n\n" +
                "<b>Judge</b>: Insanity plea is accepted.\n\n" +
                "{0}'s legal team completely ran away with the proceeding, painting {1} in the worst possible light." +
                "You lose salvage rights to all of the following:" +
                "\n\n{2}\n\n" +
                "In addition they claim the following as compensation for legal fees:" +
                "\n\n{3}\n\n"
            },

            {  DT_SUCCESS_TITLE, "SUCCESSFUL DISPUTE" },
            {  DT_SUCCESS_TEXT,
                "<b>Cause 193 of the negotiated mercenary contract clearly states...</b>\n\n" +
                "Your laywer deftly defend your claim with the MRB. You keep your salvage, and gain the following compensation items:" +
                "\n\n{0}\n\n"
            },

            {  DT_ITEM_AND_QUANTITY, "{0} [QTY:{1}]" },

            {  DT_BUTTON_ACCEPT, "Accept" },
            {  DT_BUTTON_REFUSE, "Refuse" },
            {  DT_BUTTON_DISPUTE, "Dispute" },
            {  DT_BUTTON_OK, "OK" },
        };

        public const string LT_QUICK_SELL = "QUICK_SELL";
        public Dictionary<string, string> LocalizedText = new Dictionary<string, string>()
        {
            { LT_QUICK_SELL, "\n(Shift-click to sell)" }
        };

        public void LogConfig()
        {
            Mod.Log.Info?.Write("=== MOD CONFIG BEGIN ===");

            Mod.Log.Info?.Write($"  DEBUG: {this.Debug}");

            string rollupMRBVal = string.Join(", ", RollupMRBValue.Select(v => v.ToString("0.00")).ToArray());
            Mod.Log.Info?.Write($"  MRB Rollup Values: {rollupMRBVal}");
            string rollupBlacklistS = string.Join(", ", RollupBlacklist.ToArray<string>());
            Mod.Log.Info?.Write($"  Rollup Blacklists: {rollupBlacklistS}");
            string rollupBlacklistTagsS = string.Join(", ", RollupBlacklistTags.ToArray<string>());
            Mod.Log.Info?.Write($"  Rollup Blacklist Tags: {rollupBlacklistTagsS}");

            Mod.Log.Info?.Write($"  Compensation Max Rollup Quantity: {CompensationMaxRollupQuantity}");

            Mod.Log.Info?.Write($"FACTION REPUTATION VALUES");
            foreach (RepCfg factionCfg in Reputation)
            {
                Mod.Log.Info?.Write($"  Reputation:{factionCfg.Reputation} ComponentRollup:{factionCfg.RollupMultiComponent} MechRollup:{factionCfg.RollupMultiMech} HoldbackTrigger:{factionCfg.HoldbackTrigger}%");
            }

            Mod.Log.Info?.Write($"HOLDBACK - enabled? {Holdback.Enabled}");
            Mod.Log.Info?.Write($"  Holdback Picks: {Holdback.MechParts[0]} to {Holdback.MechParts[1]}");
            Mod.Log.Info?.Write($"  Rep Range: {Holdback.ReputationRange[0]} to {Holdback.ReputationRange[1]}");
            Mod.Log.Info?.Write($"  Dispute Picks: {Holdback.DisputePicks[0]} to {Holdback.DisputePicks[1]}");
            Mod.Log.Info?.Write($"  Dispute SuccessBase:{Holdback.DisputeSuccessBase} MRBSuccessFactor:{Holdback.DisputeMRBSuccessFactor} " +
                $"SuccessRandomBound:{Holdback.DisputeSuccessRandomBound} MRBFeeFactor:{Holdback.DisputeMRBFeeFactor}");

            Mod.Log.Info?.Write("=== MOD CONFIG END ===");
        }
    }
}
