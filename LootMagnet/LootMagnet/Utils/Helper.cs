using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LootMagnet {

    public class Dispute {

        public enum Outcome {
            SUCCESS,
            FAILURE,
        }

        public int MRBFees;
        public float SuccessChance;
        public int Picks;

        public Dispute(Contract contract) {
            int maxMoney = contract.InitialContractValue;
            this.MRBFees = (int)Math.Ceiling(maxMoney * LootMagnet.Config.Holdback.DisputeMRBFeeFactor);
            LootMagnet.Logger.Log($"Disputing contract: ({contract.Name}) - maxMoney:{maxMoney} MRB Fees:{MRBFees}");

            float rawSuccess = LootMagnet.Config.Holdback.DisputeSuccessBase + LootMagnet.Config.Holdback.DisputeMRBSuccessFactor * Helper.MRBCfgIdx();
            int randBound = (int)Math.Ceiling(rawSuccess * LootMagnet.Config.Holdback.DisputeSuccessRandomBound);
            float successRand = LootMagnet.Random.Next(randBound);
            this.SuccessChance = rawSuccess - successRand;
            LootMagnet.Logger.Log($"  rawSuccess:{rawSuccess} randBound:{randBound} rand:{successRand} finalSuccess:{this.SuccessChance}%");

            this.Picks = LootMagnet.Random.Next(LootMagnet.Config.Holdback.DisputePicks[0], LootMagnet.Config.Holdback.DisputePicks[1]);
            LootMagnet.Logger.Log($"  picks: {this.Picks}");
        }

        public Outcome GetOutcome() {
            float roll = LootMagnet.Random.Next(100);
            if (roll < SuccessChance) {
                LootMagnet.Logger.Log($"Roll {roll} vs. {SuccessChance} is a failure.");
                return Outcome.FAILURE;
            } else { 
                LootMagnet.Logger.Log($"Roll {roll} vs. {SuccessChance} is a success.");
                return Outcome.SUCCESS;
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
                    LootMagnet.Logger.Log($"  Rolling up {rawDef.Count} of component salvage:'{rawDef?.Description?.Name}' with value:{rawDef.Description.Cost} threshold:{componentThreshold.ToString("0")}");
                    RollupSalvageDef(rawDef, componentThreshold, rolledUpSalvage);
                } else if (rawDef.Type == SalvageDef.SalvageType.MECH_PART && mechThreshold > 0) {
                    LootMagnet.Logger.Log($"  Rolling up {rawDef.Count} of mech part salvage:'{rawDef?.Description?.Name}' with value:{rawDef.Description.Cost} threshold:{mechThreshold.ToString("0")}");
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

        public static void CalculateHoldback(List<SalvageDef> potentialSalvage) {
            if (potentialSalvage == null || potentialSalvage.Count == 0) { return; }

            // Filter to mech parts only
            List<SalvageDef> allMechParts = potentialSalvage.Where(sd => sd.Type != SalvageDef.SalvageType.COMPONENT).ToList();
            int mechPartsToHoldback = LootMagnet.Random.Next(LootMagnet.Config.Holdback.MechParts[0], LootMagnet.Config.Holdback.MechParts[1]);
            LootMagnet.Logger.Log($"Holding back up to {mechPartsToHoldback} mech parts.");

            foreach (SalvageDef salvageDef in allMechParts) {
                if (mechPartsToHoldback == 0) {
                    break;
                } else if (mechPartsToHoldback >= salvageDef.Count) {
                    State.HeldbackParts.Add(salvageDef);
                    mechPartsToHoldback -= salvageDef.Count;
                    LootMagnet.Logger.Log($"Holding back all {mechPartsToHoldback} parts of mech:({salvageDef.Description.Name}).");
                } else if (mechPartsToHoldback < salvageDef.Count) {
                    SalvageDef partialDef = new SalvageDef(salvageDef) {
                        Count = mechPartsToHoldback
                    };
                    State.HeldbackParts.Add(partialDef);
                    mechPartsToHoldback = 0;
                    LootMagnet.Logger.Log($"Holding back {mechPartsToHoldback} parts of mech:({salvageDef.Description.Name}), leaving {salvageDef.Count} parts.");
                }
            }
        }

        public static void CalculateCompensation(List<SalvageDef> potentialSalvage) {
            if (State.HeldbackParts == null || State.HeldbackParts.Count == 0) { return; }

            double compensation = 0;
            int valueCap = 0;
            int mechPartsForAssembly = UnityGameInstance.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax;
            foreach (SalvageDef mechPart in State.HeldbackParts) {
                int adjustedCost = (int)Math.Ceiling(mechPart.Description.Cost / (double)mechPartsForAssembly);
                if (adjustedCost >= valueCap) { valueCap = adjustedCost; }
                compensation += adjustedCost;
                LootMagnet.Logger.Log($"Mech part:({mechPart.Description.Id}::{mechPart.Description.Name}) has " +
                    $"raw cost:{mechPart.Description.Cost} / {mechPartsForAssembly} = {adjustedCost}");
            }

            RepCfg repCfg = LootMagnet.Config.Reputation.Find(r => r.Reputation == (Rep)FactionCfgIdx());
            double adjValueCap = Math.Ceiling(valueCap * repCfg.HoldbackValueCapMulti);
            LootMagnet.Logger.Log($"Total compensation: {compensation} valueCap:{valueCap} adjValueCap:{adjValueCap}");

            // Filter to components only
            List<SalvageDef> allComponents = potentialSalvage.Where(sd => sd.Type == SalvageDef.SalvageType.COMPONENT).ToList();
            foreach (SalvageDef compSDef in allComponents) {
                LootMagnet.Logger.Log($"   Component:{compSDef.Description.Id}::{compSDef.Description.Name}");
                if (compSDef.Description.Cost > adjValueCap) {
                    LootMagnet.Logger.Log($"   cost:{compSDef.Description.Cost} greater than cap, skipping.");
                } else if (compSDef.Description.Cost > compensation) {
                    LootMagnet.Logger.Log($"   remaining compensation:{compensation} less than cost:{compSDef.Description.Cost}, skipping.");
                } else {
                    int available = (int)Math.Floor(compensation / compSDef.Description.Cost);
                    LootMagnet.Logger.Log($" - remaining compensation:{compensation} / cost: {compSDef.Description.Cost} = available:{available}");

                    // TODO: Test for too large a stack here / do div by 3 to reduce large stacks
                    int adjAvailable = (available > 10) ? (int)Math.Ceiling(available / 3.0f) : available;

                    SalvageDef equivDef = new SalvageDef(compSDef) {
                        Count = compSDef.Count + adjAvailable
                    };
                    State.CompensationParts.Add(equivDef);
                    LootMagnet.Logger.Log($" - rawCount:{compSDef.Count} to adjCost:{equivDef.Count}");

                    // Reduce the remaining compensation
                    compensation = compensation - (compSDef.Description.Cost * adjAvailable);
                }
            }

            if (compensation != 0) {
                // TODO: Should this come back as cbills?
                LootMagnet.Logger.Log($" Compensation of {compensation} remaining and unpaid!");
            }

        }

        public static void RemoveSalvage(SalvageDef holdbackDef, Contract contract, AAR_SalvageScreen salvageScreen, AAR_SalvageSelection salvageSelection, List<SalvageDef> finalPotentialSalvage) {
            SalvageDef spSDef = finalPotentialSalvage.Find((SalvageDef x) => x.Description.Id == holdbackDef.Description.Id && x.RewardID == holdbackDef.RewardID);
            if (holdbackDef.Count == spSDef.Count) {
                finalPotentialSalvage.Remove(spSDef);
            } else {
                spSDef.Count = spSDef.Count - holdbackDef.Count;
            }

            LootMagnet.Logger.Log($"Searching for inventory widget with Id: {holdbackDef.Description.Id}");
            foreach (InventoryItemElement_NotListView iie in salvageSelection.GetSalvageInventory()) {
                if (iie.controller != null && iie.controller.salvageDef != null &&
                    iie.controller.salvageDef.Description.Id == holdbackDef.Description.Id &&
                    iie.controller.salvageDef.RewardID == holdbackDef.RewardID) {
                    SalvageDef iieSDef = iie.controller.salvageDef;
                    LootMagnet.Logger.Log($"Removing salvage: {iieSDef.Description.Id}_{iieSDef.Description.Name}_{iieSDef.RewardID}");
                    if (holdbackDef.Count == iieSDef.Count) {
                        iie.gameObject.SetActive(false);
                    } else {
                        // TODO: Update description here?
                        iieSDef.Count = iieSDef.Count - holdbackDef.Count;
                    }

                    break;
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
