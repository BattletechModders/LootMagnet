# Loot Magnet
This mod for the [HBS BattleTech](http://battletechgame.com/) game strives to build a better salvage experience. In the vanilla game, you can only select so many pieces of salvage, regardless of their value. The same weight is given to a _Box of SRM Ammo_ as to a piece of a _BattleMech_. Only a completely inept negotiator would agree to such terms, but I suppose we have Darius to thank so perhaps this makes sense!

> Employer: Excellent work commander. You've exceeded my admittedly low expectations for mercenaries, and completed the contract as requested. Let's discuss the matter of battlefield salvage, shall we?
>
> Commander: What's there to discuss? We've already hauled away that Hunchback we put down and we're rounding up some of the armor plates.
>
> Employer: Unfortunately that's not your contract terms. We'll need you to choose which pieces of that unfortunate pirate you want to retain, and turn the rest over to our salvage units. Your contract only grants you the right to pick three items, after all.
>
> Commander: Three items of equivalent value, correct? So we took two mech parts and a bundle of medium lasers. What's the problem?
>
> Employer: Your contract doesn't stipulate equivalent value, only 'individual parts'. Your negotiator was quite insistent on that point. Given the terms of the contract each of those medium lasers counts as a single item.
>
> Commander: .... DARIUS!

In the BattleTech lore salvage was a hotly negotiated topic for mercenaries. Most employers want to retain as much of the salvage as possible, and only the shrewdest, most respected companies could negotiate on anything like even terms. Young companies have to deal with like-in-kind equipment trades, or cash-equivalents, or outright theft from the employer.

To simulate that negotiation process, this mod modifies your salvage choices based upon your MSRB rating and your faction allegiance. The vanilla experience becomes the default for low MSRB and faction contracts. As your MSRB or faction allegiance increases, your employer will be more willing to negotiate bundles of equipment of equivalent value. At the highest levels, you'll find all similar equipment bundled together as a single salvage pick.

## Salvage Rollup

If a faction trusts you and wants to continue working with you, they will negotiate more favorable terms. One of the most common 'little favors' is to allow salvage to be driven by equivalent cash amounts, instead of a strict item by item accounting. We refer to this as **Salvage Rollup**.

> Employer: Commander, we're used to working with you and would like to continue the relationship. We don't normally offer this, but given the nature of our relationship we'll negotiate salvage on an equivalent c-bill value rate. How does that sound?

Your rating with a particular faction determines your companies negotiating power come salvage time. Your MRB rating sets a C-Bill threshold, which determines how many items will be rolled up into a single salvage pick. This rating is multiplied by your faction rating. When you're honored with a faction you'll find they allow you to cart off more salvage, especially if you are a well-known merc company.

| MRB Rating | Indifferent | Liked | Friendly | Honored |
| -- | -- | -- | -- | -- |
| 0 | 15,000 | 90,000 | 180,000 | 300,000 |
| 1 | 20,000 | 120,000 | 240,000 | 400,000 |
| 2 | 25,000 | 150,000 | 300,000 | 500,000 |
| 3 | 30,000 | 180,000 | 360,000 | 600,000 |
| 4 | 35,000 | 210,000 | 420,000 | 700,000 |
| 5 | 40,000 | 240,000 | 480,000 | 800,000 |

> Example: A company with MRB rating 2 is Liked by a faction. Their  threshold is $25,000 * 6 = 125,000k. They destroy multiple mechs and the salvage pool includes 10 standard heat sinks. Each heat sink is worth 30,000. 125,000 / 30,000 = 4.166, so the player will see 2 salvage picks of 4 Heat Sinks, and one salvage pick of 2 Heat Sinks.

### Optional Mech Rollup
By default, only components are rolled up into salvage picks. This reflects the background where mechs (in whole or part) are extremely valuable pieces of salvage.

If the option *RollupMechsForAllies* in `mod.json` is set to true, mech parts will also be rolled up into buckets if the player is **Allied** with the employing faction.

## Salvage Holdback

Players are mercenaries, and mercenaries are contract workers. Even friendly employers that see you getting ready to cart LosTech off the field may invoke hidden contractual clauses and hold back items they really want. This was extremely common in the early days of the Clan Invasion and anytime rare technology comes into play.

> Employer: I'm sorry commander, but Section A, Sub-Section 3, Paragraph ii clearly covers the exemption clauses for material deemed 'critical to the war effort'. That bit of salvage falls under the clause, and thus out of your grubby hands. We'll be retaining it. You should be thankful we're honoring the terms of agreement in the first place, mercenary.

This mod replicates this effect by giving employers an opportunity to remove one or more pieces of salvage from the salvage pool before the player ever sees it. The salvage pool is rolled up and sorted by cost (which closely tracks rarity and value), then a check if made made for each item in the list. If the check succeeds, the item is removed from the salvage pool before the player ever notices it.

The employer faction rating determines how strong this effect is, and how many items the employer will try to remove. The employer will try to remove up to their Picks count of items from the salvage list, but no more.

The chance for a holdback, and the number of holdback picks an employer receives, are given by the table below.

| Faction Rating | Picks | MRB 0 | MRB 1 | MRB 2 | MRB 3 | MRB 4 | MRB 5 |
| -- | -- | -- | -- | -- | -- | -- | -- |
| Loathed | 4 | 60.00% |52.50% | 45.00% | 37.50% | 30.00% | 22.50% |
| Hated | 3 | 40.00% | 35.00% | 30.00% | 25.00% | 20.00% | 15.00% |
| Disliked | 2 | 20.00% | 17.50% | 15.00% | 12.50% | 10.00% | 7.50% |
| Indifferent | 1 | 10.00% |8.75% | 7.50%| 6.25% | 5.00% | 3.75% |
| Liked | 1 | 5.00% | 4.38% | 3.75% | 3.13% | 2.50% | 1.88% |
| Friendly | 1 | 2.50% | 2.19% | 1.88% | 1.56% | 1.25% | 0.94% |
| Honored | 0 | 0.00% | 0.00% | 0.00% | 0.00% | 0.00% | 0.00% |


## WIP NOTES
* there is Salvage.VictorySalvageChance, Salvage.VictorySalvageLostPerMechDestroyed, plus analogs for Failed and Retreated.
* There is something called PercentageContractSalve from the contract
* There is also constants.Finances.ContractFloorSalvageBonus

* add a salvage bonus boost, that you get some items for free without picking them, just for completing the mission? Tie into victory chance, faction loyalty, etc
* Show holdback items on the salvage screen, but prevent them from being selected?
* Show bonus items on the salvage screen, but prevent them from being removed?

### Lore Mode
If you prefer a grittier play-style that's closer to the lore, enabled `LoreMode : true` in the __LootMagnet/mod.json__ file. In this mode, BattleTechs and rare equipment is jealously guarded by the employer and only the savviest of companies can claw it from their hands. In addition your faction rank matters much more, and with unfriendly factions you may find them replacing your hard-earned loot with worse alternatives. After all, in every contract somebody gets screwed over - why not the mercs?

Changes in Lore Mode include:

BattleTech salvage is rarer; at a faction rating of 50 or less you will only receive a single BattleTech part included in the salvage table. If there are multiple chassis types in the salvage table, the lowest tonnage BattleMech will be chosen.

Rare equipment is considered more valuable; any equipment over FIXME C-Bills is considered rare and will be replaced
