using BattleTech;
using BattleTech.UI;
using CustomComponents;
using Harmony;
using LootMagnet.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using static LootMagnet.LootMagnet;

namespace LootMagnet {

    public class Dispute {

        public enum Outcome {
            SUCCESS,
            FAILURE,
        }

        public int MRBFees;
        public float SuccessChance;
        public int Picks;

        public Dispute(int initialContractValue, string contractName) {
            int maxMoney = initialContractValue;
            this.MRBFees = (int)Math.Ceiling(maxMoney * Mod.Config.Holdback.DisputeMRBFeeFactor);
            Mod.Log.Info($"Disputing contract: ({contractName}) - maxMoney:{maxMoney} MRB Fees:{MRBFees}");

            float rawSuccess = Mod.Config.Holdback.DisputeSuccessBase + Mod.Config.Holdback.DisputeMRBSuccessFactor * Helper.MRBCfgIdx();
            int randBound = (int)Math.Ceiling(rawSuccess * Mod.Config.Holdback.DisputeSuccessRandomBound);
            float successRand = LootMagnet.Random.Next(randBound);
            this.SuccessChance = rawSuccess - successRand;
            Mod.Log.Info($"  rawSuccess:{rawSuccess} randBound:{randBound} rand:{successRand} finalSuccess:{this.SuccessChance}%");

            this.Picks = LootMagnet.Random.Next(Mod.Config.Holdback.DisputePicks[0], Mod.Config.Holdback.DisputePicks[1]);
            Mod.Log.Info($"  picks: {this.Picks}");
        }

        public Outcome GetOutcome() {
            float roll = LootMagnet.Random.Next(100);
            if (roll > SuccessChance) {
                Mod.Log.Info($"Roll {roll} vs. {SuccessChance} is a failure.");
                return Outcome.FAILURE;
            } else { 
                Mod.Log.Info($"Roll {roll} vs. {SuccessChance} is a success.");
                return Outcome.SUCCESS;
            } 
        }
    }

    public class Helper {

        public static float GetSalvageThreshold(bool forMech=false) {
            RepCfg repCfg = Mod.Config.Reputation.Find(r => r.Reputation == (Rep)FactionCfgIdx());
            float multi = forMech ? repCfg.RollupMultiMech : repCfg.RollupMultiComponent;

            float rollup = Mod.Config.RollupMRBValue[MRBCfgIdx()];
            float result = (float)Math.Floor(rollup * multi);

            Mod.Log.Debug($"rollup:{rollup} x multi:{multi} = result:{result}");
            return result;
        }

        public static float GetHoldbackTriggerChance() {
            RepCfg repCfg = Mod.Config.Reputation.Find(r => r.Reputation == (Rep)FactionCfgIdx());
            return repCfg.HoldbackTrigger;
        }

        // Rollup the salvage into buckets
        public static List<SalvageDef> RollupSalvage(List<SalvageDef> rawSalvage) {
            
            // Rollup items with more than one instance, and that aren't mech chassis
            List<SalvageDef> toRollup = rawSalvage.Where(sd => sd.Count > 1 && sd?.Description?.Cost != 0 && sd.Type != SalvageDef.SalvageType.CHASSIS).ToList();
            List<SalvageDef> rolledUpSalvage = rawSalvage.Except(toRollup).ToList();

            float componentThreshold = Mod.Config.DeveloperMode ? 999999999f : GetSalvageThreshold(false);
            float mechThreshold = Mod.Config.DeveloperMode ? 999999999f : GetSalvageThreshold(true);
            foreach (SalvageDef rawDef in toRollup) {
                Mod.Log.Debug($"Found {rawDef.Count} of salvage:'{rawDef?.Description?.Name}' / '{rawDef?.Description.Id}' with rewardId:'{rawDef?.RewardID}'");

                if (rawDef.Type == SalvageDef.SalvageType.COMPONENT && componentThreshold > 0 ) {
                    Mod.Log.Info($"  Rolling up {rawDef.Count} of component salvage:'{rawDef?.Description?.Name}' with value:{rawDef.Description.Cost} threshold:{componentThreshold.ToString("0")}");
                    RollupSalvageDef(rawDef, componentThreshold, rolledUpSalvage);
                } else if (rawDef.Type == SalvageDef.SalvageType.MECH_PART && mechThreshold > 0) {
                    Mod.Log.Info($"  Rolling up {rawDef.Count} of mech part salvage:'{rawDef?.Description?.Name}' with value:{rawDef.Description.Cost} threshold:{mechThreshold.ToString("0")}");
                    RollupSalvageDef(rawDef, mechThreshold, rolledUpSalvage);
                } else {
                    rolledUpSalvage.Add(rawDef);
                }
            } 

            return rolledUpSalvage;
        }
        
        private static void RollupSalvageDef(SalvageDef salvageDef, float threshold, List<SalvageDef> salvage) {
            try {
                int sDefCost = 0;
                if (salvageDef != null && salvageDef.Description != null) {
                    sDefCost = salvageDef.Description.Cost;
                } else {
                    Mod.Log.Info($"WARNING: salvageDef.rewardID:({salvageDef?.RewardID}) is null or has null description: {salvageDef?.Description?.Id}");
                }

                int rollupCount = (int)Math.Ceiling(threshold / sDefCost);
                Mod.Log.Debug($"  threshold:{threshold.ToString("0")} / cost:{salvageDef?.Description?.Cost} = result:{rollupCount}");

                bool isBlacklisted = IsBlacklisted(salvageDef);
                if (isBlacklisted) {
                    Mod.Log.Info($"  BLACKLISTED: {salvageDef?.MechComponentDef?.Description?.Id} cannot be rolled up. Skipping. ");
                    salvage.Add(salvageDef);
                } else if (rollupCount > 1) {
                    int buckets = (int)Math.Floor(salvageDef.Count / (double)rollupCount);
                    int remainder = salvageDef.Count % rollupCount;
                    Mod.Log.Debug($"count:{salvageDef.Count} / limit:{rollupCount} = buckets:{buckets}, remainder:{remainder}");

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

            } catch (Exception e) {
                Mod.Log.Info($"ERROR: Failed to rollup salvageDef due to: {e.Message}");
            }
        }

        // Checks to determine if a mech part is blacklisted
        public static bool IsBlacklisted(SalvageDef salvageDef) {

            // If the blacklist config contains the componentDef
            if (Mod.Config.RollupBlacklist.Contains(salvageDef?.MechComponentDef?.Description?.Id))
            {
                Mod.Log.Debug($"  Component blacklisted by id: {salvageDef?.MechComponentDef?.Description?.Id}");
                return true;
            }

            // If the component is a LootComponent and is blacklisted
            if (salvageDef.MechComponentDef.Is<LootMagnetComp>(out LootMagnetComp lootComponent) && lootComponent.Blacklisted)
            {
                Mod.Log.Debug($"  Component blacklisted by CC:LootMagnetComp.");
                return true;
            }

            foreach (string componentTag in salvageDef?.MechComponentDef?.ComponentTags)
            {
                // If the blacklist config contains the componentDef
                if (Mod.Config.RollupBlacklistTags.Contains(componentTag))
                {
                    Mod.Log.Debug($"  Component blacklisted by component tag: {componentTag}");
                    return true;
                }
            }

            return false;
        }

        public static void CalculateHoldback(List<SalvageDef> potentialSalvage) {
            if (potentialSalvage == null || potentialSalvage.Count == 0) { return; }

            // Filter to mech parts only
            List<SalvageDef> allMechParts = potentialSalvage.Where(sd => sd.Type != SalvageDef.SalvageType.COMPONENT).ToList();
            int mechPartsToHoldback = LootMagnet.Random.Next(Mod.Config.Holdback.MechParts[0], Mod.Config.Holdback.MechParts[1]);
            Mod.Log.Info($"Holding back up to {mechPartsToHoldback} mech parts.");

            foreach (SalvageDef salvageDef in allMechParts) {
                Mod.Log.Debug($"Evaluating mech:({salvageDef.Description.Name}) with parts:{salvageDef.Count} for holdback.");
                if (mechPartsToHoldback == 0) {
                    Mod.Log.Info($"No more parts to holdback, skipping.");
                    break;
                } else if (mechPartsToHoldback >= salvageDef.Count) {
                    ModState.HeldbackParts.Add(salvageDef);
                    mechPartsToHoldback -= salvageDef.Count;
                    Mod.Log.Debug($"Holding back all {mechPartsToHoldback} parts of mech:({salvageDef.Description.Name}).");
                } else if (mechPartsToHoldback < salvageDef.Count) {
                    SalvageDef partialDef = new SalvageDef(salvageDef) {
                        Count = mechPartsToHoldback
                    };
                    ModState.HeldbackParts.Add(partialDef);
                    Mod.Log.Debug($"Holding back {mechPartsToHoldback} parts of mech:({salvageDef.Description.Name}), leaving {salvageDef.Count} parts.");
                    mechPartsToHoldback = 0;
                    break;
                }
            }
        }

        public static void CalculateCompensation(List<SalvageDef> potentialSalvage) {
            if (ModState.HeldbackParts == null || ModState.HeldbackParts.Count == 0) { return; }

            double compensation = 0;
            int valueCap = 0;
            int mechPartsForAssembly = UnityGameInstance.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax;
            foreach (SalvageDef mechPart in ModState.HeldbackParts) {
                int adjustedCost = (int)Math.Ceiling(mechPart.Description.Cost / (double)mechPartsForAssembly);
                if (adjustedCost >= valueCap) { valueCap = adjustedCost; }
                compensation += adjustedCost;
                Mod.Log.Info($"Mech part:({mechPart.Description.Id}::{mechPart.Description.Name}) has " +
                    $"raw cost:{mechPart.Description.Cost} / {mechPartsForAssembly} = {adjustedCost}");
            }

            RepCfg repCfg = Mod.Config.Reputation.Find(r => r.Reputation == (Rep)FactionCfgIdx());
            double adjValueCap = Math.Ceiling(valueCap * repCfg.HoldbackValueCapMulti);
            Mod.Log.Info($"Total compensation: {compensation} valueCap:{valueCap} adjValueCap:{adjValueCap}");

            // Filter to components only
            List<SalvageDef> allComponents = potentialSalvage.Where(sd => sd.Type == SalvageDef.SalvageType.COMPONENT).ToList();
            foreach (SalvageDef compSDef in allComponents) {
                Mod.Log.Info($"   Component:{compSDef.Description.Id}::{compSDef.Description.Name}");
                bool isBlacklisted = IsBlacklisted(compSDef);
                if (isBlacklisted)
                {
                    Mod.Log.Info($"   Blacklisted: skipping.");
                }
                else if (compSDef.Description.Cost > adjValueCap) 
                {
                    Mod.Log.Info($"   cost:{compSDef.Description.Cost} greater than cap, skipping.");
                } 
                else if (compSDef.Description.Cost > compensation) 
                {
                    Mod.Log.Info($"   remaining compensation:{compensation} less than cost:{compSDef.Description.Cost}, skipping.");
                } 
                else 
                {
                    int available = (int)Math.Floor(compensation / compSDef.Description.Cost);
                    Mod.Log.Info($" - remaining compensation:{compensation} / cost: {compSDef.Description.Cost} = available:{available}");

                    // TODO: Test for too large a stack here / do div by 3 to reduce large stacks
                    int adjAvailable = (available > 10) ? (int)Math.Ceiling(available / 3.0f) : available;

                    SalvageDef equivDef = new SalvageDef(compSDef) {
                        Count = compSDef.Count + adjAvailable
                    };
                    ModState.CompensationParts.Add(equivDef);
                    Mod.Log.Info($" - rawCount:{compSDef.Count} to adjCost:{equivDef.Count}");

                    // Reduce the remaining compensation
                    compensation = compensation - (compSDef.Description.Cost * adjAvailable);
                }
            }

            if (compensation > 0) {
                // TODO: Should this come back as cbills?
                Mod.Log.Info($" Compensation of {compensation} remaining and unpaid!");
            }

        }

        public static void RemoveSalvage(SalvageDef holdbackDef) {
            // TODO: Could cause an NRE, but shouldn't if the holdback logic is safe
            SalvageDef spSDef = ModState.PotentialSalvage.Find((SalvageDef x) => x.Description.Id == holdbackDef.Description.Id && x.RewardID == holdbackDef.RewardID);
            if (holdbackDef.Count == spSDef.Count) {
                Mod.Log.Debug($"  Removing salvageDef:({spSDef.Description.Id}_{spSDef.Description.Name}_{spSDef.RewardID}) with count:{spSDef.Count} ");
                ModState.PotentialSalvage.Remove(spSDef);
            } else {
                spSDef.Count = spSDef.Count - holdbackDef.Count;
                Mod.Log.Debug($"  reducing salvageDef:({spSDef.Description.Id}_{spSDef.Description.Name}_{spSDef.RewardID}) to count:{spSDef.Count} ");
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

            // don't create QTY:1 strings
            string uiNameWithQuantity = quantity > 1 ?
                $"{salvageDef.Description.UIName} <lowercase>[QTY:{quantity}]</lowercase>" :
                salvageDef.Description.UIName;
            
            // increase the value of the def
            DescriptionDef newDescDef = new DescriptionDef(
                salvageDef.Description.Id,
                salvageDef.Description.Name,
                salvageDef.Description.Details,
                salvageDef.Description.Icon,
                salvageDef.Description.Cost * quantity,
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

            Mod.Log.Debug($"Incoming for {quantity}, Cost in {salvageDef.Description.Cost}, out {newDef.Description.Cost}");
            return newDef;
        }

        public static int FactionCfgIdx() {
            int cfgIdx = 0;
            switch (ModState.EmployerRep) {
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
            if (ModState.IsEmployerAlly) {
                cfgIdx = 7;
            }

            return cfgIdx;
        }

        public static int MRBCfgIdx() {
            if (ModState.MRBRating <= 0) {
                return 0;
            } else if (ModState.MRBRating >= 5) {
                return 5;
            } else {
                return ModState.MRBRating;
            }
        }

        // Replica of HBS code used once holdback has been decided
        public static void CalculateAndAddAvailableSalvage(AAR_SalvageScreen salvageScreen, List<SalvageDef> potentialSalvage) {

            Traverse salvageScreenT = Traverse.Create(salvageScreen);
            Mod.Log.Debug("CAAAS - created base traverse.");

            Traverse salvageSelectT = salvageScreenT.Field("salvageSelection");
            AAR_SalvageSelection salvageSelection = salvageSelectT.GetValue<AAR_SalvageSelection>();
            Mod.Log.Debug("CAAAS - found salvage selection.");

            foreach (SalvageDef item in potentialSalvage) {
                salvageScreen.AddNewSalvageEntryToWidget(item, salvageSelection.GetInventoryWidget());
            }
            Mod.Log.Debug("CAAAS - added all salvage entries.");

            Traverse allSalvageContT = salvageScreenT.Field("AllSalvageControllers");
            List<ListElementController_BASE_NotListView> allSalvageControllers = 
                allSalvageContT.GetValue<List<ListElementController_BASE_NotListView>>();
            Mod.Log.Debug("CAAAS - found salvage controllers");

            Traverse totalSalvageT = salvageScreenT.Field("totalSalvageMadeAvailable");
            totalSalvageT.SetValue(allSalvageControllers.Count);
            Mod.Log.Debug("CAAAS - updated salvageController count");

            salvageSelection.ApplySalvageSorting();

            // Update the contract potential salvage
            Contract contract = salvageScreenT.Field("contract").GetValue<Contract>();
            Traverse finalPotentialSalvageT = Traverse.Create(contract).Field("finalPotentialSalvage");
            List<SalvageDef> finalPotentialSalvage = finalPotentialSalvageT.GetValue<List<SalvageDef>>();

            finalPotentialSalvage.Clear();
            finalPotentialSalvage.AddRange(potentialSalvage);
        }
    }
}
