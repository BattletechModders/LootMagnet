using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LootMagnet {

    class Helper {

        public static float GetSalvageThreshold() {
            float rollup = LootMagnet.Config.RollupMRBValue[MRBCfgIdx()];
            float multi = LootMagnet.Config.RollupFactionMulti[FactionCfgIdx()];
            float result = (float)Math.Floor(rollup * multi);
            LootMagnet.Logger.Log($"rollup:{rollup} x multi:{multi} = result:{result}");
            return result;
        }

        public static float GetHoldbackChance() {
            float holdback = LootMagnet.Config.HoldbackFactionValue[FactionCfgIdx()];
            float multi = LootMagnet.Config.HoldbackMRBMulti[MRBCfgIdx()];
            float result = (float)Math.Floor(holdback * multi);
            LootMagnet.Logger.Log($"holdback:{holdback} x multi:{multi} = result:{result}");
            return result;
        }

        public static int GetHoldbackPicks() {
            return LootMagnet.Config.HoldbackPicks[FactionCfgIdx()];
        }

        // Rollup the salvage into buckets
        public static List<SalvageDef> RollupSalvage(List<SalvageDef> rawSalvage) {
            
            // Rollup items with more than one instance, and that aren't mech chassis
            List<SalvageDef> toRollup = rawSalvage.Where(sd => sd.Count > 1 && sd?.Description?.Cost != 0 && sd.Type != SalvageDef.SalvageType.CHASSIS).ToList();
            List<SalvageDef> rolledUpSalvage = rawSalvage.Except(toRollup).ToList();

            float baseThreshold = LootMagnet.Config.DeveloperMode ? 999999999f : GetSalvageThreshold();
            float mechThreshold = LootMagnet.Config.DeveloperMode ? 999999999f : baseThreshold * LootMagnet.Config.RollupAlliedMultiForMechs;
            foreach (SalvageDef rawDef in toRollup) {
                LootMagnet.Logger.Log($"Found {rawDef.Count} of salvage:'{rawDef?.Description?.Name}' / '{rawDef?.Description.Id}' with rewardId:'{rawDef?.RewardID}'");

                if (rawDef.Type == SalvageDef.SalvageType.COMPONENT) {
                    LootMagnet.Logger.Log($"Rolling up {rawDef.Count} of component salvage:'{rawDef?.Description?.Name}' with threshold:{baseThreshold}");
                    RollupSalvage(rawDef, baseThreshold, rolledUpSalvage);
                } else if (rawDef.Type == SalvageDef.SalvageType.MECH_PART && LootMagnet.Config.RollupMechsAtAllied || LootMagnet.Config.DeveloperMode) {
                    LootMagnet.Logger.Log($"Rolling up {rawDef.Count} of mech part salvage:'{rawDef?.Description?.Name}' with threshold:{mechThreshold}");
                    RollupSalvage(rawDef, mechThreshold, rolledUpSalvage);
                }
            } 

            return rolledUpSalvage;
        }

        private static void RollupSalvage(SalvageDef salvageDef, float threshold, List<SalvageDef> salvage) {
            int rollupCount = (int)Math.Ceiling(threshold / salvageDef.Description.Cost);
            LootMagnet.Logger.Log($"threshold:{threshold} / cost:{salvageDef?.Description?.Cost} = result:{rollupCount}");

            if (rollupCount > 1) {
                int buckets = (int)Math.Floor(salvageDef.Count / (double)rollupCount);
                int remainder = salvageDef.Count % rollupCount;
                LootMagnet.Logger.Log($"count:{salvageDef.Count} / limit:{rollupCount} = buckets:{buckets}, remainder:{remainder}");

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

            float holdbackChance = Helper.GetHoldbackChance();
            int holdbackPicks = Helper.GetHoldbackPicks();
            
            foreach (SalvageDef sDef in sortedSalvage) {
                if ((sDef.Type != SalvageDef.SalvageType.COMPONENT && LootMagnet.Config.HoldbackAlwaysForMechs) || holdbackPicks > 0) {
                    int roll = LootMagnet.Random.Next(100);
                    if (roll <= holdbackChance) {
                        LootMagnet.Logger.Log($"Roll:{roll} <= holdback%:{holdbackChance}. Employer is holding back item:{sDef.Description.Name}.");
                        holdbackPicks--;
                    } else {
                        LootMagnet.Logger.Log($"Roll:{roll} > holdback%:{holdbackChance}. Player retains item:{sDef.Description.Name}");
                        postHoldbackSalvage.Add(sDef);
                    }
                } else {
                    LootMagnet.Logger.Log($"Employer has no holdback picks. Player retains item:{sDef.Description.Name}");
                    postHoldbackSalvage.Add(sDef);
                }
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

            string uiNameWithQuantity = $"{salvageDef.Description.UIName} ({quantity}ct.)";
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
            switch (State.EmployerReputation) {
                case SimGameReputation.LOATHED:
                    return 0;
                case SimGameReputation.HATED:
                    return 1;
                case SimGameReputation.DISLIKED:
                    return 2;
                case SimGameReputation.INDIFFERENT:
                    return 3;
                case SimGameReputation.LIKED:
                    return 4;
                case SimGameReputation.FRIENDLY:
                    return 5;
                case SimGameReputation.HONORED:
                default:
                    return 6;
            }
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
