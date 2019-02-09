# Loot Magnet
This mod for the [HBS BattleTech](http://battletechgame.com/) game strives to build a better salvage experience. In the vanilla game, you can only select so many pieces of salvage, regardless of their value. The same weight is given to a _Box of SRM Ammo_ as to a piece of a _BattleMech_. Only a completely inept negotiator would agree to such terms, but I suppose we have Darius to thank so perhaps this makes sense!

In the BattleTech lore salvage was a hotly negotiated topic for mercenaries. Most employers want to retain as much of the salvage as possible, and only the shrewdest, most respected companies could negotiate on anything like even terms. Young companies have to deal with like-in-kind equipment trades, or cash-equivalents, or outright theft from the employer.

To simulate that negotiation process, this mod modifies your salvage choices based upon your MSRB rating and your faction allegiance. The vanilla experience becomes the default for low MSRB and faction contracts. As your MSRB or faction allegiance increases, your employer will be more willing to negotiate bundles of equipment of equivalent value. At the highest levels, you'll find all similar equipment bundled together as a single salvage pick.

## Overview

Your rating with a particular faction determines your companies negotiating power come salvage time. Your rating^3 sets a c-bill limit
- Strong MSRB & friendly = good outcome
- Strong MSRB & neutral = good outcome
- Moderate MSRB & friendly =
- Moderate MSRB & neutral =
- Low MSRB & friendly = good outcome
- Low MSRB & neutral = vanilla

Allied is where you start rolling up mech parts into a single bundle?
MSRB & faction influences how much gets rolled up?


## Lore Mode
If you prefer a grittier play-style that's closer to the lore, enabled `LoreMode : true` in the __LootMagnet/mod.json__ file. In this mode, BattleTechs and rare equipment is jealously guarded by the employer and only the savviest of companies can claw it from their hands. In addition your faction rank matters much more, and with unfriendly factions you may find them replacing your hard-earned loot with worse alternatives. After all, in every contract somebody gets screwed over - why not the mercs?

Changes in Lore Mode include:

BattleTech salvage is rarer; at a faction rating of 50 or less you will only receive a single BattleTech part included in the salvage table. If there are multiple chassis types in the salvage table, the lowest tonnage BattleMech will be chosen.

Rare equipment is considered more valuable; any equipment over FIXME C-Bills is considered rare and will be replaced

## WIP NOTES

from mpstark - Yeah, you could hook on to the end of GenerateSalvage, grab the items out, add the "fake" items back in, adjust the UI, and then hook FinalizeSalvage to add the extra items in. Balancing it would be a challenge though. Because the progression is really build around how much salvage you get. 
Really like the idea though.



* there is Salvage.VictorySalvageChance, Salvage.VictorySalvageLostPerMechDestroyed, plus analogs for Failed and Retreated.
* There is something called PercentageContractSalve from the contract
* There is also constants.Finances.ContractFloorSalvageBonus

- Employer will hide even regular equipment at very low relation levels
