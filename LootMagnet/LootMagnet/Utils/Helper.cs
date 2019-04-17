using BattleTech;
using BattleTech.UI;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static List<SalvageDef> HoldbackSalvage(List<SalvageDef> salvage) {
            List<SalvageDef> postHoldbackSalvage = new List<SalvageDef>();
            
            List<SalvageDef> sortedSalvage = new List<SalvageDef>(salvage);
            sortedSalvage.Sort(new SalvageDefByCostDescendingComparer());

            int rawPicks = (int)Math.Ceiling(sortedSalvage.Count * LootMagnet.Config.HoldbackPicksGreed);
            int picksModifier = LootMagnet.Config.HoldbackPicksModifier[FactionCfgIdx()];
            int holdbackPicks = rawPicks + picksModifier;
            LootMagnet.Logger.Log($"Employer is holding back: {holdbackPicks} picks = {rawPicks} (raw picks) + {picksModifier} (modifier)");
            
            List<SalvageDef> heldbackItems = new List<SalvageDef>();
            foreach (SalvageDef sDef in sortedSalvage) {
                if (holdbackPicks > 0) {
                    if (sDef.Type == SalvageDef.SalvageType.COMPONENT) {
                        // If we're not a mechpart, hold back all items in the bundle
                        LootMagnet.Logger.Log($"Employer is holding back {sDef.Count} items of type:{sDef.Description.Name} as one pick.");
                        heldbackItems.Add(sDef);
                        holdbackPicks--;
                    } else {
                        // If we are a mechpart, each part counts as one item for holdback purposes
                        if (sDef.Count >= holdbackPicks) {
                            sDef.Count = sDef.Count - holdbackPicks;
                            LootMagnet.Logger.Log($"Employer is holding back {holdbackPicks} mech parts/chassis of type:{sDef.Description.Name} as {sDef.Count} picks.");
                            if (sDef.Count == 0) {
                                heldbackItems.Add(sDef);
                            }
                            holdbackPicks = 0;
                        } else {
                            LootMagnet.Logger.Log($"Employer is holding back {sDef.Count} mech parts/chassis of type:{sDef.Description.Name} as {sDef.Count} picks.");
                            heldbackItems.Add(sDef);
                            holdbackPicks = holdbackPicks - sDef.Count;
                        }
                    }
                } else {
                    LootMagnet.Logger.LogIfDebug($"Player retains item:{sDef.Description.Name}");
                    postHoldbackSalvage.Add(sDef);
                }
            }

            if (heldbackItems.Count > 0) {
                List<string> heldbackNames = heldbackItems.Select(sd => sd.Description.Name).ToList();
                string names = string.Join("\n<line-indent=2px>", heldbackNames.ToArray());
                GenericPopupBuilder.Create(GenericPopupType.Info, 
                    $"<b>I'm sorry commander, but Section A, Sub-Section 3, Paragraph ii...</b>\n\n" +
                    $"A contract dispute has withheld the following items:\n\n{names}"                    
                    )
                    .AddButton("Accept") // accept holdback, gain slight reputation boost
                    .AddButton("Dispute") // dispute with MSRB, greater chance based upon MSRB rating. Lose less rep, on a failed dispute lose MSRB rating as well 
                    .AddButton("Refuse") // forcibly refuse claims, take reputation hit equal to cost
                    .Render();
            }

            return postHoldbackSalvage;
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
            switch (State.EmployerReputation) {
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
