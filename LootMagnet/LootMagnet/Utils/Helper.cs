using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace LootMagnet {

    public class Dispute {

        public enum Outcome {
            CRITICAL_SUCCESS,
            SUCCESS,
            FAILURE,
            CRITICAL_FAILURE
        }

        public float CriticalChance;
        public float SuccessChance;
        public float FailChance;

        public int MRBFees;
        public int MRBRepPenalty;

        public int SuccessRepPenalty;

        public int FailPayout;
        public int FailRepPenalty;

        public int CritFailPayout;
        public int CritFailRepPenalty;

        public Dispute(Contract contract, SimGameConstants sgc) {
            int maxRep = contract.GetMaxPossibleReputation(sgc);
            int maxMoney = contract.InitialContractValue;
            LootMagnet.Logger.Log($"Disputing contract: ({contract.Name}) - maximumReputation:{maxRep} maxMoney:{SimGameState.GetCBillString(maxMoney)}");

            this.MRBFees = (int)Math.Ceiling(maxMoney * LootMagnet.Config.Holdback.DisputeMRBFeeFactor);
            this.MRBRepPenalty = (int)Math.Ceiling(maxRep * LootMagnet.Config.Holdback.RepMultiDisputeMRB);
            LootMagnet.Logger.Log($"  MRB fees:{SimGameState.GetCBillString(MRBFees)} repPenalty:{MRBRepPenalty}");

            this.CriticalChance = LootMagnet.Config.Holdback.DisputeCritChance;
            float rawSuccessChance = LootMagnet.Config.Holdback.DisputeSuccessBase + LootMagnet.Config.Holdback.DisputeMRBSuccessFactor * Helper.MRBCfgIdx();
            float successRand = LootMagnet.Random.Next(LootMagnet.Config.Holdback.DisputeSuccessRandomBound + 1);
            this.SuccessChance = rawSuccessChance - successRand;
            this.FailChance = 100f - (2f * this.CriticalChance) - this.SuccessChance;
            LootMagnet.Logger.Log($"  CritSuccess:{CriticalChance}%  Success:{SuccessChance}% Failure:{FailChance}% CritFailure:{CriticalChance}%");

            this.SuccessRepPenalty = (int)Math.Ceiling(maxRep * LootMagnet.Config.Holdback.RepMultiDisputeSuccess);
            LootMagnet.Logger.Log($"  Success repPenalty:{SuccessRepPenalty}");

            this.FailPayout = (int)Math.Ceiling(maxMoney * LootMagnet.Config.Holdback.DisputeFailPayoutFactor);
            this.FailRepPenalty = (int)Math.Ceiling(maxRep * LootMagnet.Config.Holdback.RepMultiDisputeFail);
            LootMagnet.Logger.Log($"  Failure payout:{SimGameState.GetCBillString(FailPayout)} repPenalty:{FailRepPenalty}");

            this.CritFailPayout = (int)Math.Ceiling(maxMoney * LootMagnet.Config.Holdback.DisputeCritFailPayoutFactor);
            this.CritFailRepPenalty = (int)Math.Ceiling(maxRep * LootMagnet.Config.Holdback.RepMultiDisputeCriticalFail);
            LootMagnet.Logger.Log($"  Critical Failure payout:{SimGameState.GetCBillString(CritFailPayout)} repPenalty:{CritFailRepPenalty}");
        }

        public Outcome GetOutcome() {
            float roll = LootMagnet.Random.Next(100);
            if (roll < CriticalChance) {
                LootMagnet.Logger.Log($"Roll {roll} < {CriticalChance}, yields critical failure.");
                return Outcome.CRITICAL_FAILURE;
            } else if (roll >= 99 - CriticalChance) {
                LootMagnet.Logger.Log($"Roll {roll} >= {99 - CriticalChance}, yields critical success.");
                return Outcome.CRITICAL_SUCCESS;
            } else if (roll >= 99 - CriticalChance - SuccessChance) {
                LootMagnet.Logger.Log($"Roll {roll} >= {99 - CriticalChance - SuccessChance}, yields success.");
                return Outcome.SUCCESS;
            } else {
                LootMagnet.Logger.Log($"Roll {roll} yields failure.");
                return Outcome.FAILURE;
            }
        }
    }


    public class Helper {

        public static float GetSalvageThreshold(bool forMech=false) {
            RepCfg repCfg = LootMagnet.Config.Reputation.Find(r => r.Reputation == (Rep)FactionCfgIdx());
            float multi = forMech ? repCfg.RollupMultiMech : repCfg.RollupMultiComponent;

            float rollup = LootMagnet.Config.RollupMRBValue[MRBCfgIdx()];
            float result = (float)Math.Floor(rollup * multi);

            LootMagnet.Logger.LogIfDebug($"rollup:{rollup} x multi:{multi} = result:{result}");
            return result;
        }

        public static float GetHoldbackTriggerChance() {
            RepCfg repCfg = LootMagnet.Config.Reputation.Find(r => r.Reputation == (Rep)FactionCfgIdx());
            return repCfg.HoldbackTrigger;
        }

        // Rollup the salvage into buckets
        public static List<SalvageDef> RollupSalvage(List<SalvageDef> rawSalvage) {
            
            // Rollup items with more than one instance, and that aren't mech chassis
            List<SalvageDef> toRollup = rawSalvage.Where(sd => sd.Count > 1 && sd?.Description?.Cost != 0 && sd.Type != SalvageDef.SalvageType.CHASSIS).ToList();
            List<SalvageDef> rolledUpSalvage = rawSalvage.Except(toRollup).ToList();

            float componentThreshold = LootMagnet.Config.DeveloperMode ? 999999999f : GetSalvageThreshold(false);
            float mechThreshold = LootMagnet.Config.DeveloperMode ? 999999999f : GetSalvageThreshold(true);
            foreach (SalvageDef rawDef in toRollup) {
                LootMagnet.Logger.LogIfDebug($"Found {rawDef.Count} of salvage:'{rawDef?.Description?.Name}' / '{rawDef?.Description.Id}' with rewardId:'{rawDef?.RewardID}'");

                if (rawDef.Type == SalvageDef.SalvageType.COMPONENT && componentThreshold > 0 ) {
                    LootMagnet.Logger.Log($"Rolling up {rawDef.Count} of component salvage:'{rawDef?.Description?.Name}' with value:{rawDef.Description.Cost} threshold:{componentThreshold.ToString("0")}");
                    RollupSalvageDef(rawDef, componentThreshold, rolledUpSalvage);
                } else if (rawDef.Type == SalvageDef.SalvageType.MECH_PART && mechThreshold > 0) {
                    LootMagnet.Logger.Log($"Rolling up {rawDef.Count} of mech part salvage:'{rawDef?.Description?.Name}' with value:{rawDef.Description.Cost} threshold:{mechThreshold.ToString("0")}");
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

            int holdbackPicks = LootMagnet.Random.Next(LootMagnet.Config.Holdback.PickRange[0], LootMagnet.Config.Holdback.PickRange[1] + 1);
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
                string itemDescs = " -" + string.Join("\n -", heldbackItemsDesc.ToArray());

                int acceptRepBonus = (int)Math.Ceiling(contract.GetMaxPossibleReputation(sgc) * LootMagnet.Config.Holdback.RepMultiAccept);
                void acceptAction() { AcceptAction(sgs, contract, salvageScreen, acceptRepBonus); }
                LootMagnet.Logger.LogIfDebug($"acceptRepBonus: {acceptRepBonus}");

                int refuseRepPenalty = (int)Math.Ceiling(contract.GetMaxPossibleReputation(sgc) * LootMagnet.Config.Holdback.RepMultiRefuse);
                void refuseAction() { RefuseAction(sgs, contract, salvageScreen, refuseRepPenalty); }
                LootMagnet.Logger.LogIfDebug($"refuseRepPenalty: {refuseRepPenalty}");

                Dispute dispute = new Dispute(contract, sgc);
                void disputeAction() { DisputeAction(sgs, contract, salvageScreen, dispute); }

                GenericPopup gp = GenericPopupBuilder.Create(
                    "DISPUTED SALVAGE", 
                    $"<b>I'm sorry commander, but Section A, Sub-Section 3, Paragraph ii...</b>\n\n" +
                    $"Your employer invokes a contract clause that allows them to withhold the following items:" + 
                    $"\n\n{itemDescs}\n\n" + 
                    $"If you <b>Accept</b>, you lose the disputed salvage but <b>gain</b> <color=#00FF00>{acceptRepBonus:+0}</color> rep.\n" +
                    $"If you <b>Refuse</b>, you keep the disputed salvage but <b>lose</b> <color=#FF0000>{refuseRepPenalty}</color> rep.\n" +
                    $"If you <b>Dispute</b>, you pay <color=#FF0000>{dispute.MRBRepPenalty}</color> MRB rep, <color=#FF0000>{SimGameState.GetCBillString(dispute.MRBFees)}</color>, and have:\n" +
                    $"<line-indent=2px> - {dispute.CriticalChance}% to keep the disputed salvage.\n" +
                    $"<line-indent=2px> - {dispute.SuccessChance}% to keep the disputed salvage, but lose <color=#FF0000>{dispute.SuccessRepPenalty}</color> faction rep.\n" +
                    $"<line-indent=2px> - {dispute.FailChance}% to lose the disputed salvage, <color=#FF0000>{dispute.FailRepPenalty}</color> faction rep, and <color=#FF0000>{SimGameState.GetCBillString(dispute.FailPayout)}</color>.\n" +
                    $"<line-indent=2px> - {dispute.CriticalChance}% to lose <b>ALL</b> salvage, <color=#FF0000>{dispute.CritFailRepPenalty}</color> faction rep, and <color=#FF0000>{SimGameState.GetCBillString(dispute.CritFailPayout)}</color>.\n"
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
            AAR_SalvageChosen salvageChosen = (AAR_SalvageChosen)Traverse.Create(salvageScreen).Field("salvageChosen").GetValue();
            foreach (SalvageDef salvageDef in State.HeldbackItems) {
                RemoveSalvage(contract, salvageScreen, salvageChosen, finalPotentialSalvage, salvageDef);
            }

            State.Reset();
        }

        public static void RefuseAction(SimGameState simGameState, Contract contract, AAR_SalvageScreen salvageScreen, int reputationModifier) {
            int repBefore = simGameState.GetRawReputation(State.Employer);
            simGameState.AddReputation(State.Employer, reputationModifier, false);
            State.EmployerRepRaw = simGameState.GetRawReputation(State.Employer);
            LootMagnet.Logger.Log($"Player refused holdback. {State.Employer} reputation {repBefore} + {reputationModifier} modifier = {State.EmployerRepRaw}.");

            State.Reset();
        }

        public static void DisputeAction(SimGameState simGameState, Contract contract, AAR_SalvageScreen salvageScreen, Dispute dispute) {
            int MRBRepPre = simGameState.GetRawReputation(Faction.MercenaryReviewBoard);
            simGameState.AddReputation(Faction.MercenaryReviewBoard, dispute.MRBRepPenalty, false);
            int MRPRepPost = simGameState.GetRawReputation(Faction.MercenaryReviewBoard);
            LootMagnet.Logger.Log($"Player disputed holdback. {Faction.MercenaryReviewBoard} reputation {MRBRepPre} + {dispute.MRBRepPenalty} modifier = {MRPRepPost}.");

            LootMagnet.Logger.Log($"Dispute legal fees:{dispute.MRBFees}");
            simGameState.AddFunds(dispute.MRBFees, $"MRB Legal Fees re: {contract.Name}", false);

            Dispute.Outcome outcome = dispute.GetOutcome();
            if (outcome == Dispute.Outcome.CRITICAL_SUCCESS) {
                LootMagnet.Logger.Log($"Critical success, player keeps disputed salvage, no further effects.");
                GenericPopup gp = GenericPopupBuilder.Create(
                    "CRITICAL SUCCESS",
                    $"<b>If the glove fits, you must acquit!</b>\n\n" + 
                    $"Your hired laywer dazzled the MRB, who unanimously accepted your claim.  Meanwhile politicing by Darius convinced the {State.Employer} rep to " +
                    $"leave this incident out of the company's employment record. You can't ask for better outcome... for {simGameState.CompanyName} at least. " +
                    $"Maybe Darius really does deserve that raise he's been yammering on about."
                )
                .AddButton("OK") 
                .Render();
            } else if (outcome == Dispute.Outcome.SUCCESS) {
                int factionRepPre = simGameState.GetRawReputation(State.Employer);
                simGameState.AddReputation(State.Employer, dispute.SuccessRepPenalty, false);
                int factionRepPost = simGameState.GetRawReputation(State.Employer);

                LootMagnet.Logger.Log($"Success, player keeps disputed salvage but loses {dispute.SuccessRepPenalty}. Faction rep drops from {factionRepPre} to {factionRepPost}");
                GenericPopup gp = GenericPopupBuilder.Create(
                    "SUCCESS",
                    $"<b>Cause 193 of the standard mercenary contract clearly states...</b>\n\n" +
                    $"Your laywer was able to defend your claim with the MRB. Unfortunately an off-hand remark by Sumire inflamed the already antagonistic {State.Employer} rep." +
                    $"You keep the salvage, but {simGameState.CompanyName} reputation with {State.Employer} has been damaged and a permenant black mark added to the employer's records. "
                )
                .AddButton("OK")
                .Render();
            } else if (outcome == Dispute.Outcome.CRITICAL_FAILURE) {
                int factionRepPre = simGameState.GetRawReputation(State.Employer);
                simGameState.AddReputation(State.Employer, dispute.SuccessRepPenalty, false);
                int factionRepPost = simGameState.GetRawReputation(State.Employer);

                // Remove the disputed items
                RemoveAllSalvage(contract, salvageScreen);

                // Pay the fee
                simGameState.AddFunds(dispute.CritFailPayout, $"Contract {contract.Name} dispute critical failure payout", false);

                LootMagnet.Logger.Log($"Success, player ALL disputed salvage, loses {SimGameState.GetCBillString(dispute.CritFailPayout)}, loses {dispute.CritFailRepPenalty}. Faction rep drops from {factionRepPre} to {factionRepPost}");
                GenericPopup gp = GenericPopupBuilder.Create(
                    "CRITICAL FAILURE",
                    $"<b>I know a guy... who knows a guy.</b>\n\n" +
                    $"When you choose an attorney from a strip mall, you shouldn't expect much. Your lawyer was completely trounched by {State.Employer}'s legal team, with the worst contract penalties being applied." +
                    $"You lose all the battlefield salvage, as well as {SimGameState.GetCBillString(dispute.CritFailPayout)}, but on the bright side your reputation with {State.Employer} barely changes. They're too busy laughing all the way to the bank. "
                )
                .AddButton("OK")
                .Render();
            } else if (outcome == Dispute.Outcome.FAILURE) {
                int factionRepPre = simGameState.GetRawReputation(State.Employer);
                simGameState.AddReputation(State.Employer, dispute.SuccessRepPenalty, false);
                int factionRepPost = simGameState.GetRawReputation(State.Employer);

                // Remove the disputed items
                List<SalvageDef> finalPotentialSalvage = (List<SalvageDef>)Traverse.Create(contract).Field("finalPotentialSalvage").GetValue();
                AAR_SalvageChosen salvageChosen = (AAR_SalvageChosen)Traverse.Create(salvageScreen).Field("salvageChosen").GetValue();
                foreach (SalvageDef salvageDef in State.HeldbackItems) {
                    RemoveSalvage(contract, salvageScreen, salvageChosen, finalPotentialSalvage, salvageDef);
                }

                // Pay the fee
                simGameState.AddFunds(dispute.FailPayout, $"Contract {contract.Name} dispute failure payout", false);

                LootMagnet.Logger.Log($"Success, player loses disputed salvage, loses {SimGameState.GetCBillString(dispute.FailPayout)}, loses {dispute.FailRepPenalty}. Faction rep drops from {factionRepPre} to {factionRepPost}");
                GenericPopup gp = GenericPopupBuilder.Create(
                    "FAILURE",
                    $"<b>This court finds against the claimants...</b>\n\n" +
                    $"{State.Employer}'s lawyer mounted a spirited attack that your counsel never recovered from. You lose the disputed salvage, as well as {SimGameState.GetCBillString(dispute.FailPayout)}." +
                    $"Having the case go their way makes the dispute less bitter for {State.Employer}, and there's little reputation loss with them for {simGameState.CompanyName}."
                )
                .AddButton("OK")
                .Render();
                            }

            State.Reset();
        }

        public static void RemoveSalvage(Contract contract, AAR_SalvageScreen salvageScreen, AAR_SalvageChosen salvageChosen, List<SalvageDef> finalPotentialSalvage, SalvageDef salvageDef) {
            SalvageDef sdef1 = finalPotentialSalvage.Find((SalvageDef x) => x.Description.Id == salvageDef.Description.Id);
            finalPotentialSalvage.Remove(sdef1);

            SalvageDef sdef2 = contract.SalvageResults.Find((SalvageDef x) => x.Description.Id == salvageDef.Description.Id);
            contract.SalvageResults.Remove(sdef2);

            LootMagnet.Logger.Log($"Searching for inventory widget with Id: {salvageDef.Description.Id}");            
            foreach (InventoryItemElement_NotListView iie in salvageChosen.PriorityInventory) {
                if (iie.controller != null && iie.controller.salvageDef != null && iie.controller.salvageDef.Description.Id == salvageDef.Description.Id) {
                    LootMagnet.Logger.Log($"Removing priority salvage: {salvageDef.Description.Id}");
                    iie.gameObject.SetActive(false);
                    break;
                }
            }

            foreach (InventoryItemElement_NotListView iie in salvageChosen.LeftoverInventory) {
                if (iie.controller != null && iie.controller.salvageDef != null && iie.controller.salvageDef.Description.Id == salvageDef.Description.Id) {
                    LootMagnet.Logger.Log($"Removing leftover salvage: {salvageDef.Description.Id}");
                    iie.gameObject.SetActive(false);
                    break;
                }
            }

        }

        public static void RemoveAllSalvage(Contract contract, AAR_SalvageScreen salvageScreen) {
            List<SalvageDef> finalPotentialSalvage = (List<SalvageDef>)Traverse.Create(contract).Field("finalPotentialSalvage").GetValue();
            AAR_SalvageChosen salvageChosen = (AAR_SalvageChosen)Traverse.Create(salvageScreen).Field("salvageChosen").GetValue();

            LootMagnet.Logger.Log($"Removing all player salvage!");
            finalPotentialSalvage.Clear();
            contract.SalvageResults.Clear();

            foreach (InventoryItemElement_NotListView iie in salvageChosen.PriorityInventory) {
                if (iie.controller != null && iie.controller.salvageDef != null) {
                    iie.gameObject.SetActive(false);
                }
            }

            foreach (InventoryItemElement_NotListView iie in salvageChosen.LeftoverInventory) {
                if (iie.controller != null && iie.controller.salvageDef != null) {
                    iie.gameObject.SetActive(false);
                }
            }
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

        public static int FactionCfgIdx() {
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

        public static int MRBCfgIdx() {
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
