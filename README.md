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

| MRB Rating | Indifferent | Liked | Friendly | Honored | Allied |
| -- | -- | -- | -- | -- | -- |
| 0 | 20,000 | 100,000 | 180,000 | 260,000 | 420,000 |
| 1 | 30,000 | 150,000 | 270,000 | 390,000 | 630,000 |
| 2 | 45,000 | 225,000 | 405,000 | 585,000 | 945,000 |
| 3 | 65,000 | 320,000 | 585,000 | 845,000 | 1,365,000 |
| 4 | 90,000 | 450,000 | 810,000 | 1,170,000 | 1,890,000 |
| 5 | 120,000 | 600,000 | 1,080,000 | 1,560,000 | 2,520,000 |

> Example: A company with MRB rating 2 is Liked by a faction. Their  threshold is $25,000 * 6 = 125,000k. They destroy multiple mechs and the salvage pool includes 10 standard heat sinks. Each heat sink is worth 30,000. 125,000 / 30,000 = 4.166, so the player will see 2 salvage picks of 4 Heat Sinks, and one salvage pick of 2 Heat Sinks.

These values are controlled through the **mod.json** values **RollupMRBValue** and **RollupMultiComponent**. Default values have been set to reflect a vanilla BattleTech experience, but you should feel free to customize them as you see fit.

###  Mech Rollup
Mech parts may also be rolled up, if your faction rating is good enough. The **RollupMultiMech** value in `mod.json` defines the multipliers to the base MRB threshold used for mech parts. Mech parts are priced at the **full market value** of the Mech, typically making each of them valued at 3-12 milliion c-bills. The default value applies a 20x multiplier at friendly, 30x at honored, and 180x at allied.

| MRB Rating | Indifferent | Liked | Friendly | Honored | Allied |
| -- | -- | -- | -- | -- | -- |
| 0 | 0 | 0 | 400,000 | 600,000 | 3,600,000 |
| 1 | 0 | 0 | 600,000 | 900,000 | 5,400,000 |
| 2 | 0 | 0 | 900,000 | 1,350,000 | 8,100,000 |
| 3 | 0 | 0 | 1,300,000 | 1,950,000 | 11,700,000 |
| 4 | 0 | 0 | 1,800,000 | 2,700,000 | 16,200,000 |
| 5 | 0 | 0 | 2,400,000 | 3,600,000 | 21,600,000 |

### Blacklist

Some items are too powerful or rare for employers to offer multiples. To mark any items as being ineligible for rollup, add them to the **RollupBlacklist** array in *mod.json*. If you are using the [CustomComponents](https://github.com/battletechmodders/customcomponents/) mod, you can blacklist items by including the following in the item JSON:

```json
    "Custom" : { 
        "LootMagnetComp" : { "Blacklisted" : true }
    }
```



## Salvage Holdback

Mercenaries are contract workers. Even friendly employers hesitate when they notice LosTech or rare Battlemechs being carted off the field by a temporary 'friend'.  Hidden clauses and fine print are the weapons Inner Sphere lawyers use to **hold back** items they desperately desire. In lore, this was very common when salvage included Star League or Clan technology.

> Employer: I'm sorry commander, but Section A, Sub-Section 3, Paragraph ii clearly covers the exemption clauses for material deemed 'critical to the war effort'. That bit of salvage falls under the clause, and thus we'll be retaining it.

This mod gives contract employers an opportunity to prevent the player from claiming one or more pieces from the salvage pool. Once the player chooses  their priority salvage and random salvage is assigned, the employer has an opportunity to demand some of the player's salvage.

The employer makes a random roll to determine if they attempt to holdback an item. This threshold is determined by the player's rating with the employing faction. Factions that dislike the player are more likely to trigger a holdback, but even friendly factions have a small chance as well. The chance for a holdback is given by the table below, and defined by  **HoldbackTrigger** (in *mod.json*).

##### Employer Holdback 

| Value | Loathed | Hated | Disliked | Indifferent | Liked | Friendly | Honored | Allied |
| -- | -- | -- | -- | -- | -- | -- | -- | -- |
| Trigger | 72% | 48% | 32% | 16% | 8% | 4% | 2% | 1% |
| Value Cap | 0.2 | 0.3 | 0.4 | 0.6 | 0.8 | 1 | 1.25 | 2 |

Once holdback has been triggered, the employer determines how greedy they will be. A random roll is made against the bounds defined as **Holdback.MechParts** (in *mod.json*), and determines how many pieces of *partial mech salvage* will be heldback. The employer will take the most expensive items first. 

### Compensation

For each piece of partial mech salvage the employer keeps, they will offer items in compensation. These items are added to the salvage pool as normal salvage, and won't be rolled-up unless your reputation allows them to be. Compensation is generally an insincere gesture on your employer's part; they expect the parts to come right back to them because of insufficient salvage rights on your part.

The employer calculates what they are willing to offer you based upon raw mech part cost (`MechDef.Description.Cost`) for each part they heldback. If a mech part was worth 750,000 c-bills, and they kept 3 of them, the employer would start with compensation of 2,250,000 c-bills. This value is then multiplied by the **Value Cap**, defined as `HoldbackValueCapMulti` in *mod.json*. This value cap varies by reputation, and is a applied as a straight multiplier to the compensation sum. See the table above for default value caps. Using the value of 2,250,000 c-bills and the INDIFFERENT value cap of 0.6, the employer would offer you `2,250,000 * 0.6 = 1,350,00` worth of items.

Once the compensation sum has been determined, we iterate through all components in the salvage pool. Components are ordered by value, and if their raw cost (`MechComponentDef.Description.Cost`) is less than the compensation value, one or more of that item are added as compensation.  If the component cost is low, and the compensation is high, multiples of that item can be added to the salvage pool. 

Any compensation remainder left after iterating through salvage pool is lost. Consider it graft from unreliable employers.

If a component would normally add more than 10 of itself to the pool, the compensation quantity is divided by 3, and this value is added instead. This prevents any single item from dominating the pool, which provides more verisimilitude to the process.

> Example: The salvage pool has a SRM-2, and the compensation sum is high enough that we'd normally add 26 copies of the SRM-2 to the pool. Because 26 > 10, we divide 26 / 3 = 8.66. We round up to 9, and add 9 copies of the SRM-2 to the salvage pool instead.

### Negotiation

> Every day's a negotiation and sometimes it's done with guns. - Joss Whedon

Not every employer has an army of attorneys and a fleet of warships ready to enforce their will. Angry mercenaries in BattleMechs have significant negotiating power, though this can sour future dealings with that faction. The player can make one of three choices in the face of unreasonable demands:

You can **Accept** the employer's terms, and let them keep the disputed salvage. The employer rewards you with a small bonus to faction reputation, but you're unlikely to recover your self-respect.

You can **Refuse** the employer's terms, power up the PPCs, and renegotiate with extreme prejudice. You keep the items, but your faction reputation will take a big hit. Nobody likes having a gun pointed at them!

You can **Dispute** the claims with the Mercenary Review Board. You pay a small fee to the MRB and put your trust in the lawyers. A random check is made that determines how successful your lawyer is:

   * On a **success**, you retain the items and your employer gets nothing. You also get a few items from the offered compensation pool.
   * On a **failure**, you lose the items **and** a few additional items as legal fees. Maybe you should find a better lawyer next time!

#### Configuration

The negotiation process has a few configuration values that influence the outcome. These are described below:

* **DisputePicks** defines how many items are used for the success and failure cases.
* **DisputeSuccessBase** defines the base success chance as a random chance to succeed in the dispute.
* **DisputeMRBSuccessFactor** is added to your success chance once for MRB rating level you have.
* **DisputeSuccessRandomBound** is a modifier that reduces your total success by a random amount. If you have a success chance of 80% (base 50% + MRB 3 * SuccessFactor: 10%), then 80 * SuccessRandomBound (0.2 by default) = 16 defines the range by which your success will be randomly reduced. It could be 0, it could be 7, or could be 16. Once the random value is determined, your success rate is reduced by that amount.
* **DisputeMRBFeeFactor** defines the cost to dispute the holdback. It's a multiplier to the total contract cost, and should be negative.

