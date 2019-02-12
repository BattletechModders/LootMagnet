using BattleTech;
using System;
using System.Collections.Generic;

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
            List<SalvageDef> rolledUpSalvage = new List<SalvageDef>();

            float threshold = GetSalvageThreshold();
            foreach (SalvageDef rawDef in rawSalvage) {
                if (rawDef.Count > 1 && rawDef?.Description?.Cost != 0) {
                    LootMagnet.Logger.Log($"C:GPS - found {rawDef.Count} of salvage:({rawDef?.Description?.Name} / {rawDef?.Description.Id} with rewardId:{rawDef?.RewardID} / GUID:{rawDef?.GUID}");

                    int rollupCount = (int)Math.Ceiling(threshold / rawDef.Description.Cost);
                    LootMagnet.Logger.Log($"threshold:{threshold} / cost:{rawDef?.Description?.Cost} = result:{rollupCount}");

                    if (rollupCount > 1) {
                        int buckets = (int)Math.Floor(rawDef.Count / (double)rollupCount);
                        int remainder = rawDef.Count % rollupCount;
                        LootMagnet.Logger.Log($"count:{rawDef.Count} / limit:{rollupCount} = buckets:{buckets}, remainder:{remainder}");

                        int i = 0;
                        for (i = 0; i < buckets; i++) {
                            SalvageDef bucketDef = CloneToXName(rawDef, rollupCount, i);
                            rolledUpSalvage.Add(bucketDef);
                        }

                        if (remainder != 0) {
                            SalvageDef remainderDef = CloneToXName(rawDef, remainder, i + 1);
                            rolledUpSalvage.Add(remainderDef);
                        }

                    } else {
                        // Add the rawDef, and let the player choose one by one
                        rolledUpSalvage.Add(rawDef);
                    }
                } else {
                    // Only one to pick, so follow the normal logic.
                    rolledUpSalvage.Add(rawDef);
                }
            }

            return rolledUpSalvage;
        }

        public static List<SalvageDef> HoldbackSalvage(List<SalvageDef> salvage) {
            List<SalvageDef> postHoldbackSalvage = new List<SalvageDef>();
            float holdbackChance = Helper.GetHoldbackChance();
            int holdbackPicks = Helper.GetHoldbackPicks();

            return postHoldbackSalvage;
        }

        public class SalvageDefByCostComparer : IComparer<SalvageDef> {
            public int Compare(SalvageDef x, SalvageDef y) {
                if (object.ReferenceEquals(x, y))
                    return 0;
                if (x == null || x.Description == null)
                    return -1;
                if (y == null || y.Description == null)
                    return 1;

                return x.Description.Cost.CompareTo(y.Description.Cost);
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
