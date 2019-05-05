using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using static LootMagnet.LootMagnet;

namespace LootMagnet {


    public class UIHelper {

        public static void ShowHoldbackDialog(Contract contract, AAR_SalvageScreen salvageScreen) {

            List<string> heldbackItemsDesc = new List<string>();
            foreach (SalvageDef sDef in State.HeldbackParts) {
                heldbackItemsDesc.Add($"{sDef.Description.Name} [QTY:{sDef.Count}]");
            }
            string heldbackDescs = " -" + string.Join("\n -", heldbackItemsDesc.ToArray());

            List<string> compItemsDesc = new List<string>();
            foreach (SalvageDef sDef in State.CompensationParts) {
                compItemsDesc.Add($"{sDef.Description.Name} [QTY:{sDef.Count}]");
            }
            string compDescs = " -" + string.Join("\n -", compItemsDesc.ToArray());

            int acceptRepMod = LootMagnet.Random.Next(Mod.Config.Holdback.ReputationRange[0], Mod.Config.Holdback.ReputationRange[1]);
            int refuseRepMod = LootMagnet.Random.Next(Mod.Config.Holdback.ReputationRange[0], Mod.Config.Holdback.ReputationRange[1]);
            int disputeRepMod = LootMagnet.Random.Next(Mod.Config.Holdback.ReputationRange[0], Mod.Config.Holdback.ReputationRange[1]);
            Mod.Logger.Debug($"Reputation modifiers - accept:{acceptRepMod} refuse:{refuseRepMod} dispute:{disputeRepMod}");

            Dispute dispute = new Dispute(contract);
            void acceptAction() { AcceptAction(contract, salvageScreen, acceptRepMod); }
            void refuseAction() { RefuseAction(contract, salvageScreen, refuseRepMod); }
            void disputeAction() { DisputeAction(contract, salvageScreen, dispute); }

            GenericPopup gp = GenericPopupBuilder.Create(
                "DISPUTED SALVAGE",
                $"<b>I'm sorry commander, but Section A, Sub-Section 3, Paragraph ii...</b>\n\n" +
                $"As the salvage crew picks over the battlefield, you are contacted by the {State.Employer} representative. " + 
                $"They insist the contract allows them first pick on the following items:" +
                $"\n\n{heldbackDescs}\n\n" +
                $"They offer additional quantities of the following in compensation:" +
                $"\n\n{compDescs}\n\n" +
                $"You may choose to:\n" +
                $"<b>Refuse</b>: the disputed salvage is retained but you <b>lose</b> <color=#FF0000>{refuseRepMod}</color> rep.\n" +
                $"<b>Accept</b>: the disputed salvage is lost, but the exchanged items will be added and " +
                  $"you gain <b>gain</b> <color=#00FF00>{acceptRepMod:+0}</color> rep.\n" +
                $"<b>Dispute</b>: you pay <color=#FF0000>{SimGameState.GetCBillString(dispute.MRBFees)}</color> in legal fees, and have:\n" +
                    $"<line-indent=2px> - {dispute.SuccessChance}% to keep the disputed salvage, and " +
                      $"gain {Mod.Config.Holdback.DisputePicks[0]}-{Mod.Config.Holdback.DisputePicks[1]} from the compensation offer.\n" +
                    $"<line-indent=2px> - {100 - dispute.SuccessChance}% to lose the disputed salvage, but also " +
                      $"lose an additional {Mod.Config.Holdback.DisputePicks[0]}-{Mod.Config.Holdback.DisputePicks[1]} components.\n"
                )
                .AddButton("Refuse", refuseAction, true, null)
                .AddButton("Accept", acceptAction, true, null)
                .AddButton("Dispute", disputeAction, true, null)
                .Render();

            TextMeshProUGUI contentText = (TextMeshProUGUI)Traverse.Create(gp).Field("_contentText").GetValue();
            contentText.alignment = TextAlignmentOptions.Left;
        }
        

        public static void AcceptAction(Contract contract, AAR_SalvageScreen salvageScreen, int reputationModifier) {

            SimGameState sgs = UnityGameInstance.BattleTechGame.Simulation;
            int repBefore = sgs.GetRawReputation(State.Employer);
            sgs.AddReputation(State.Employer, reputationModifier, false);
            State.EmployerRepRaw = sgs.GetRawReputation(State.Employer);
            Mod.Logger.Info($"Player accepted holdback. {State.Employer} reputation {repBefore} + {reputationModifier} modifier = {State.EmployerRepRaw}.");

            // Remove the disputed items
            List<SalvageDef> finalPotentialSalvage = (List<SalvageDef>)Traverse.Create(contract).Field("finalPotentialSalvage").GetValue();
            AAR_SalvageSelection salvageSelection = (AAR_SalvageSelection)Traverse.Create(salvageScreen).Field("salvageSelection").GetValue();
            foreach (SalvageDef sDef in State.HeldbackParts) {
                Helper.RemoveSalvage(sDef, contract, salvageScreen, salvageSelection, finalPotentialSalvage);
                Mod.Logger.Info($"Removing salvageDef:{sDef.Description.Name} with quantity:{sDef.Count}");
            }

            // Update quantities of compensation parts
            Mod.Logger.Info("Updating quantities on compensation parts.");
            foreach (SalvageDef sDef in contract.SalvageResults) {
                Mod.Logger.Info($" result salvageDef:{sDef.Description.Name} with quantity:{sDef.Count}");
                foreach (SalvageDef compSDef in State.CompensationParts) {
                    Mod.Logger.Info($" compensation salvageDef:{compSDef.Description.Name} with quantity:{compSDef.Count}");
                    if (compSDef.RewardID == sDef.RewardID) {
                        Mod.Logger.Info($" Matched results, updating quantity to: {compSDef.Count + sDef.Count}");
                        sDef.Count = sDef.Count + compSDef.Count;
                        break;
                    }
                }
            }

            State.Reset();
        }

        public static void RefuseAction(Contract contract, AAR_SalvageScreen salvageScreen, int reputationModifier) {

            SimGameState sgs = UnityGameInstance.BattleTechGame.Simulation;
            int repBefore = sgs.GetRawReputation(State.Employer);
            sgs.AddReputation(State.Employer, reputationModifier, false);
            State.EmployerRepRaw = sgs.GetRawReputation(State.Employer);
            Mod.Logger.Info($"Player refused holdback. {State.Employer} reputation {repBefore} + {reputationModifier} modifier = {State.EmployerRepRaw}.");

            State.Reset();
        }

        public static void DisputeAction(Contract contract, AAR_SalvageScreen salvageScreen, Dispute dispute) {
            Mod.Logger.Info($"Player disputed holdback.");

            SimGameState sgs = UnityGameInstance.BattleTechGame.Simulation;
            Mod.Logger.Info($"Dispute legal fees:{dispute.MRBFees}");
            sgs.AddFunds(dispute.MRBFees, $"MRB Legal Fees re: {contract.Name}", false);

            Dispute.Outcome outcome = dispute.GetOutcome();
            if (outcome == Dispute.Outcome.SUCCESS) {
                Mod.Logger.Info($"DISPUTE SUCCESS: Player keeps disputed salvage and gains {dispute.Picks} items from compensation pool.");
                GenericPopup gp = GenericPopupBuilder.Create(
                    "SUCCESSFUL DISPUTE",
                    $"<b>Cause 193 of the standard mercenary contract clearly states...</b>\n\n" +
                    $"Your laywer deftly defend your claim with the MRB. You keep your salvage, and gain the following compensation items:" +
                    $"\n\nTODO\n\n"
                )
                .AddButton("OK")
                .Render();
            } else {
                Mod.Logger.Info($"DISPUTE FAILURE: Player loses disputed items, but {dispute.Picks} items from the salvage pool.");
                GenericPopup gp = GenericPopupBuilder.Create(
                    "FAILED DISPUTE",
                    $"<b>I know a guy... who knows a guy.</b>\n\n" +
                    $"{State.Employer}'s legal team completely ran away with the proceeding, painting {sgs.CompanyName} in the worst possible light." +
                    $"You lose salvage rights to all of the following:" +
                    $"\n\nTODO\n\n"
                )
                .AddButton("OK")
                .Render();
            }

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
