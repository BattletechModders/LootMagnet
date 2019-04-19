using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace LootMagnet {

    class Helper {

        public static float GetComponentSalvageThreshold() {
            float multi = LootMagnet.Config.RollupFactionComponentMulti[FactionCfgIdx()];
            return GetSalvageThreshold(multi);
        }

        public static float GetMechSalvageThreshold() {
            float multi = LootMagnet.Config.RollupFactionMechMulti[FactionCfgIdx()];
            return GetSalvageThreshold(multi);
        }

        private static float GetSalvageThreshold(float factionMulti) {
            float rollup = LootMagnet.Config.RollupMRBValue[MRBCfgIdx()];
            float result = (float)Math.Floor(rollup * factionMulti);
            LootMagnet.Logger.LogIfDebug($"rollup:{rollup} x multi:{factionMulti} = result:{result}");
            return result;
        }

        public static float GetHoldbackTriggerChance() {
            return LootMagnet.Config.HoldbackTriggerChance[FactionCfgIdx()];
        }

        // Rollup the salvage into buckets
        public static List<SalvageDef> RollupSalvage(List<SalvageDef> rawSalvage) {
            
            // Rollup items with more than one instance, and that aren't mech chassis
            List<SalvageDef> toRollup = rawSalvage.Where(sd => sd.Count > 1 && sd?.Description?.Cost != 0 && sd.Type != SalvageDef.SalvageType.CHASSIS).ToList();
            List<SalvageDef> rolledUpSalvage = rawSalvage.Except(toRollup).ToList();

            float componentThreshold = LootMagnet.Config.DeveloperMode ? 999999999f : GetComponentSalvageThreshold();
            float mechThreshold = LootMagnet.Config.DeveloperMode ? 999999999f : GetMechSalvageThreshold();
            foreach (SalvageDef rawDef in toRollup) {
                LootMagnet.Logger.LogIfDebug($"Found {rawDef.Count} of salvage:'{rawDef?.Description?.Name}' / '{rawDef?.Description.Id}' with rewardId:'{rawDef?.RewardID}'");

                if (rawDef.Type == SalvageDef.SalvageType.COMPONENT && componentThreshold > 0 ) {
                    LootMagnet.Logger.Log($"Rolling up {rawDef.Count} of component salvage:'{rawDef?.Description?.Name}' with threshold:{componentThreshold.ToString("0")}");
                    RollupSalvageDef(rawDef, componentThreshold, rolledUpSalvage);
                } else if (rawDef.Type == SalvageDef.SalvageType.MECH_PART && mechThreshold > 0) {
                    LootMagnet.Logger.Log($"Rolling up {rawDef.Count} of mech part salvage:'{rawDef?.Description?.Name}' with threshold:{mechThreshold.ToString("0")}");
                    RollupSalvageDef(rawDef, mechThreshold, rolledUpSalvage);
                } else {
                    rolledUpSalvage.Add(rawDef);
                }
            } 

            return rolledUpSalvage;
        }

        private static void RollupSalvageDef(SalvageDef salvageDef, float threshold, List<SalvageDef> salvage) {
            int rollupCount = (int)Math.Ceiling(threshold / salvageDef.Description.Cost);
            LootMagnet.Logger.LogIfDebug($"threshold:{threshold.ToString("0")} / cost:{salvageDef?.Description?.Cost} = result:{rollupCount}");

            if (rollupCount > 1) {
                int buckets = (int)Math.Floor(salvageDef.Count / (double)rollupCount);
                int remainder = salvageDef.Count % rollupCount;
                LootMagnet.Logger.LogIfDebug($"count:{salvageDef.Count} / limit:{rollupCount} = buckets:{buckets}, remainder:{remainder}");

                int i = 0;
                for (i = 0; i < buckets; i++) {
                    SalvageDef bucketDef = CloneToXName(salvageDef, rollupCount, i);
                    salvage.Add(bucketDef);
                }

                if (remainder != 0) {
                    SalvageDef remainderDef = CloneToXName(salvageDef, remainder, i + 1);
                    salvage.Add(remainderDef);
                }
            } else {
                // There's not enough value to rollup, so just add to salvage as is and let the player pick 1 by 1
                salvage.Add(salvageDef);
            }
        }

        public static void HoldbackSalvage(Contract contract, AAR_SalvageScreen salvageScreen) {

            if (!SimGameState.DoesFactionGainReputation(State.Employer) && State.Employer != Faction.ComStar) {
                LootMagnet.Logger.Log($"Employer faction {State.Employer} cannot accrue reputation, skipping.");
                return;
            }

            List<SalvageDef> postHoldbackSalvage = new List<SalvageDef>();

            SimGameState sgs = UnityGameInstance.BattleTechGame.Simulation;
            CombatGameState cgs = UnityGameInstance.BattleTechGame.Combat;
            SimGameConstants sgc = UnityGameInstance.BattleTechGame.Simulation.Constants;

            // Sort by cost descending
            List<SalvageDef> sortedSalvage = new List<SalvageDef>();
            sortedSalvage.AddRange(contract.SalvageResults);
            sortedSalvage.Sort(new SalvageDefByCostDescendingComparer());

            int holdbackPicks = LootMagnet.Config.HoldbackPicks[FactionCfgIdx()];
            LootMagnet.Logger.Log($"Employer is holding back: {holdbackPicks} picks");
            bool employerHeldbackItems = holdbackPicks > 0;
            
            List<string> heldbackItemsDesc = new List<string>();
            List<int> heldbackItemsCost = new List<int>();
            foreach (SalvageDef sDef in sortedSalvage) {
                if (holdbackPicks > 0) {
                    if (sDef.Type == SalvageDef.SalvageType.COMPONENT) {
                        // If we're not a mechpart, hold back all items in the bundle
                        LootMagnet.Logger.Log($"Employer is holding back {sDef.Count} items of type:{sDef.Description.Name} as one pick.");
                        State.HeldbackItems.Add(sDef);
                        holdbackPicks--;
                        heldbackItemsDesc.Add($"{sDef.Description.Name} [QTY:{sDef.Count}]");
                        heldbackItemsCost.Add(sDef.Description.Cost * sDef.Count);
                    } else {
                        // If we are a mechpart, each part counts as one item for holdback purposes
                        if (sDef.Count >= holdbackPicks) {
                            sDef.Count = sDef.Count - holdbackPicks;
                            LootMagnet.Logger.Log($"Employer is holding back {holdbackPicks} mech parts/chassis of type:{sDef.Description.Name} as {sDef.Count} picks.");
                            if (sDef.Count == 0) {
                                State.HeldbackItems.Add(sDef);
                            } else {
                                SalvageDef partialDef = new SalvageDef(sDef);
                                partialDef.Count = holdbackPicks;
                                State.HeldbackItems.Add(sDef);
                            }
                            heldbackItemsDesc.Add($"{sDef.Description.Name} [QTY:{holdbackPicks}]");
                            heldbackItemsCost.Add((int)Math.Ceiling((sDef.Description.Cost * holdbackPicks) / (double)sgc.Story.DefaultMechPartMax));
                            holdbackPicks = 0;
                        } else {
                            LootMagnet.Logger.Log($"Employer is holding back {sDef.Count} mech parts/chassis of type:{sDef.Description.Name} as {sDef.Count} picks.");
                            State.HeldbackItems.Add(sDef);
                            holdbackPicks = holdbackPicks - sDef.Count;
                            heldbackItemsDesc.Add($"{sDef.Description.Name} [QTY:{sDef.Count}]");
                            heldbackItemsCost.Add((int)Math.Ceiling((sDef.Description.Cost * sDef.Count) / (double)sgc.Story.DefaultMechPartMax));
                        }
                    }
                } else {
                    LootMagnet.Logger.LogIfDebug($"Player retains item:{sDef.Description.Name}");
                    postHoldbackSalvage.Add(sDef);
                }
            }

            if (employerHeldbackItems) {
                string itemDescs = string.Join("\n  - ", heldbackItemsDesc.ToArray());

                int acceptRepBonus = (int)Math.Ceiling(contract.GetMaxPossibleReputation(sgc) * LootMagnet.Config.HoldbackAcceptMulti);
                void acceptAction() { AcceptAction(sgs, contract, salvageScreen, acceptRepBonus); }
                LootMagnet.Logger.LogIfDebug($"acceptRepBonus: {acceptRepBonus}");

                int refuseRepPenalty = (int)Math.Ceiling(contract.GetMaxPossibleReputation(sgc) * LootMagnet.Config.HoldbackRefusalMulti);
                void refuseAction() { RefuseAction(sgs, contract, salvageScreen, refuseRepPenalty); }
                LootMagnet.Logger.LogIfDebug($"refuseRepPenalty: {refuseRepPenalty}");

                int disputeRepPenalty = (int)Math.Ceiling(contract.GetMaxPossibleReputation(sgc) * LootMagnet.Config.HoldbackDisputeMulti);
                float disputeMRBModifier = LootMagnet.Config.HoldbackDisputeMRBFactor * sgs.GetCurrentMRBLevel();
                float disputeCritChance = LootMagnet.Config.HoldbackDisputeCriticalChance;
                float disputeSuccessChance = LootMagnet.Config.HoldbackDisputeBaseChance + disputeMRBModifier;
                float disputeFailChance = 100f - (2f * disputeCritChance) - disputeSuccessChance;

                // TODO: Apply ShopModifier to mechparts but not equipment? Or multiply critical failures by some a ount?
                int disputePayout = (int)Math.Ceiling(heldbackItemsCost.Aggregate((x, y) => x + y) * sgc.Finances.ShopSellModifier * LootMagnet.Config.HoldbackDisputePayoutMulti);
                int criticalPayout = (int)Math.Ceiling(disputePayout * LootMagnet.Config.HoldbackDisputeCriticalPayoutMulti);
                LootMagnet.Logger.LogIfDebug($"disputeSuccessChance {disputeSuccessChance} = base {LootMagnet.Config.HoldbackDisputeBaseChance} " +
                    $" + MRBModifier: {disputeMRBModifier}, disputeCritChance:{disputeCritChance}, " +
                    $"payout: {SimGameState.GetCBillString(disputePayout)}");
                void disputeAction() { DisputeAction(sgs, contract, salvageScreen, refuseRepPenalty, disputeSuccessChance, disputeCritChance, disputePayout); }

                GenericPopup gp = GenericPopupBuilder.Create(
                    "DISPUTED SALVAGE", 
                    $"<b>I'm sorry commander, but Section A, Sub-Section 3, Paragraph ii...</b>\n\n" +
                    $"Your employer invokes a contract clause that allows them to withhold the following items:" + 
                    $"\n\n{itemDescs}\n\n" + 
                    $"If you <b>Accept</b>, you may not salvage the items and <b>gain</b> {acceptRepBonus} reputation.\n" +
                    $"If you <b>Refuse</b>, you may salvage the items but <b>lose</b> {refuseRepPenalty} reputation.\n" +
                    $"If you <b>Dispute</b>, you lose {disputeRepPenalty} reputation with the <b>MRB</b> and have a:\n" +
                    $"<line-indent=2px> - {disputeCritChance}% chance that you keep the items, and gain {SimGameState.GetCBillString(criticalPayout)}.\n" +
                    $"<line-indent=2px> - {disputeSuccessChance}% chance of retaining the items in the salvage pool.\n" +
                    $"<line-indent=2px> - {disputeFailChance}% chance of losing the items and must pay {SimGameState.GetCBillString(disputePayout)} for legal fees\n" +
                    $"<line-indent=2px> - {disputeCritChance}% chance of losing <b>all</b> salvage and must pay {SimGameState.GetCBillString(criticalPayout)} for legal fees.\n"
                    )
                    .AddButton("Accept", acceptAction, true, null) // accept holdback, gain slight reputation boost
                    .AddButton("Dispute", disputeAction, true, null) // dispute with MSRB, greater chance based upon MSRB rating. Lose less rep, on a failed dispute lose MSRB rating as well 
                    .AddButton("Refuse", refuseAction, true, null) // forcibly refuse claims, take reputation hit equal to cost
                    .Render();

                TextMeshProUGUI contentText = (TextMeshProUGUI)Traverse.Create(gp).Field("_contentText").GetValue();
                contentText.alignment = TextAlignmentOptions.Left;

            }

            return;
        }

        public static void AcceptAction(SimGameState simGameState, Contract contract, AAR_SalvageScreen salvageScreen, int reputationModifier) {
            int repBefore = simGameState.GetRawReputation(State.Employer);
            simGameState.AddReputation(State.Employer, reputationModifier, false);
            State.EmployerRepRaw = simGameState.GetRawReputation(State.Employer);
            LootMagnet.Logger.Log($"Player accepted holdback. {State.Employer} reputation {repBefore} + {reputationModifier} modifier = {State.EmployerRepRaw}.");

            // Remove the disputed items
            List<SalvageDef> finalPotentialSalvage = (List<SalvageDef>)Traverse.Create(contract).Field("finalPotentialSalvage").GetValue();
            foreach (SalvageDef salvageDef in State.HeldbackItems) {
                SalvageDef sdef1 = finalPotentialSalvage.Find((SalvageDef x) => x.Description.Id == salvageDef.Description.Id);
                finalPotentialSalvage.Remove(sdef1);
                
                SalvageDef sdef2 = contract.SalvageResults.Find((SalvageDef x) => x.Description.Id == salvageDef.Description.Id);
                contract.SalvageResults.Remove(sdef2);

                salvageScreen.RemoveFromSalvageSelection(salvageDef);
            }

            State.Reset();
            salvageScreen.OnCompleted();
        }

        public static void RefuseAction(SimGameState simGameState, Contract contract, AAR_SalvageScreen salvageScreen, int reputationModifier) {
            int repBefore = simGameState.GetRawReputation(State.Employer);
            simGameState.AddReputation(State.Employer, reputationModifier, false);
            State.EmployerRepRaw = simGameState.GetRawReputation(State.Employer);
            LootMagnet.Logger.Log($"Player refused holdback. {State.Employer} reputation {repBefore} + {reputationModifier} modifier = {State.EmployerRepRaw}.");

            State.Reset();
        }

        public static void DisputeAction(SimGameState simGameState, Contract contract, AAR_SalvageScreen salvageScreen, int reputationModifier, float successChance, float criticalChance, int payout) {
            int repBefore = simGameState.GetRawReputation(Faction.MercenaryReviewBoard);
            simGameState.AddReputation(Faction.MercenaryReviewBoard, reputationModifier, false);
            int repAfter = simGameState.GetRawReputation(Faction.MercenaryReviewBoard);
            LootMagnet.Logger.Log($"Player refused holdback. {Faction.MercenaryReviewBoard} reputation {repBefore} + {reputationModifier} modifier = {repAfter}.");

            float roll = LootMagnet.Random.Next(101);
            if (roll >= (100f - criticalChance)) {
                // Critical success
                LootMagnet.Logger.Log($"Critical success on dispute from roll {roll}. Player gains {SimGameState.GetCBillString(payout)}");
            } else if (roll >= (100f - criticalChance - successChance)) {
                // Normal success
                LootMagnet.Logger.Log($"Successful dispute from roll {roll}. No impact");                
            } else if (roll <= criticalChance) {
                // Critical failure
                LootMagnet.Logger.Log($"Critical failure on dispute from roll {roll}. Player loses {SimGameState.GetCBillString(payout)}");
            } else {
                // Regular failure
                LootMagnet.Logger.Log($"Failure during dispute from roll {roll}. Player loses {SimGameState.GetCBillString(payout)}");
            }

            // Remove the disputed items
            List<SalvageDef> finalPotentialSalvage = (List<SalvageDef>)Traverse.Create(contract).Field("finalPotentialSalvage").GetValue();

            State.Reset();
        }

        public class SalvageDefByCostDescendingComparer : IComparer<SalvageDef> {
            public int Compare(SalvageDef x, SalvageDef y) {
                if (object.ReferenceEquals(x, y))
                    return 0;
                if (x == null || x.Description == null)
                    return -1;
                if (y == null || y.Description == null)
                    return 1;

                return -1 * x.Description.Cost.CompareTo(y.Description.Cost);
            }
        }

        // This always returns a quantity of 1!
        public static SalvageDef CloneToXName(SalvageDef salvageDef, int quantity, int count) {

            string uiNameWithQuantity = $"{salvageDef.Description.UIName} <lowercase>[QTY:{quantity}]</lowercase>";
            DescriptionDef newDescDef = new DescriptionDef(
                salvageDef.Description.Id,
                salvageDef.Description.Name,
                salvageDef.Description.Details,
                salvageDef.Description.Icon,
                salvageDef.Description.Cost,
                salvageDef.Description.Rarity,
                salvageDef.Description.Purchasable,
                salvageDef.Description.Manufacturer,
                salvageDef.Description.Model,
                uiNameWithQuantity
            );

            SalvageDef newDef = new SalvageDef(salvageDef) {
                Description = newDescDef,
                RewardID = $"{salvageDef.RewardID}_c{count}_qty{quantity}",
                Count = 1
            };

            return newDef;
        }

        private static int FactionCfgIdx() {
            int cfgIdx = 0;
            switch (State.EmployerRep) {
                case SimGameReputation.LOATHED:
                    cfgIdx = 0;
                    break;
                case SimGameReputation.HATED:
                    cfgIdx = 1;
                    break;
                case SimGameReputation.DISLIKED:
                    cfgIdx = 2;
                    break;
                case SimGameReputation.INDIFFERENT:
                    cfgIdx = 3;
                    break;
                case SimGameReputation.LIKED:
                    cfgIdx = 4;
                    break;
                case SimGameReputation.FRIENDLY:
                    cfgIdx = 5;
                    break;
                case SimGameReputation.HONORED:
                default:
                    cfgIdx = 6;
                    break;
            }

            // Check for allied
            if (State.IsEmployerAlly) {
                cfgIdx = 7;
            }

            return cfgIdx;
        }

        private static int MRBCfgIdx() {
            if (State.MRBRating <= 0) {
                return 0;
            } else if (State.MRBRating >= 5) {
                return 5;
            } else {
                return State.MRBRating;
            }
        }
    }
}
