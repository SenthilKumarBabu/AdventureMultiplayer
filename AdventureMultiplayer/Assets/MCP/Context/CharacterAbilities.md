# Character Abilities Design Document

## Approach

Each character has one unique ability defined entirely through a dedicated **PlayerStats ScriptableObject**.  
No custom ability code needed — all abilities use existing PLAYER TWO mechanics, toggled and tuned per character.

**Create:** Assets → Create → PLAYER TWO → Platformer Project → Player → New Player Stats  
**Wire:** Prefab root → `PlayerStatsManager` component → **Stats** array element 0

---

## Ability Roster

| # | Character | Skin | Source | Ability | Core Mechanic Used |
|---|---|---|---|---|---|
| 0 | **Gale** | LilyPlayer | Existing | Glider | `canGlide`, gliding stats |
| 1 | **Blaze** | Racer | Mixamo | Dash | `canAirDash`, `canGroundDash`, dash stats |
| 2 | **Bolt** | Sporty Granny | Existing | Sprinter | `topSpeed`, `runningTopSpeed`, acceleration stats |
| 3 | **Bruno** | Big Vegas | Existing | Roller | `canRoll`, `canRollOnAir`, rolling stats |
| 4 | **Spike** | Vanguard By T. Choonyung | Mixamo | Air Dive | `canAirDive`, air dive stats |

---

## Character Stat Cards

> Bars are normalised across all 5 characters. █ = strength, ░ = weakness.

| Stat | What it means |
|---|---|
| **Speed** | How fast the character moves on the ground at full sprint. Higher = covers distance faster. |
| **Acceleration** | How quickly they reach top speed from a standstill. Higher = responsive, lower = sluggish startup. |
| **Jump** | How high and how long they stay airborne. Affected by jump height and fall gravity. |
| **Air Control** | How precisely they can steer mid-air. Higher = sharp aerial turns, lower = committed trajectory. |
| **Ability Power** | How strong and useful their unique ability is in a race. Covers force, cooldown, and opportunity. |

---

### 0 · Gale — Glider
> Soars over gaps. Near-hover glide, floaty movement even without gliding. Slow on the ground.

| Stat          | Bar          | Score |
|---------------|--------------|-------|
| Speed         | `████░░░░░░` | 4/10  |
| Acceleration  | `██████░░░░` | 6/10  |
| Jump          | `████████░░` | 8/10  |
| Air Control   | `█████████░` | 9/10  |
| Ability Power | `████████░░` | 8/10  |

**Ability — Glider:** Hold jump in air to enter glide. Near-zero fall speed (`glidingMaxFallSpeed 0.5`), gentle gravity (`glidingGravity 3`). Floaty fall even without gliding (`fallGravity 45`, `gravityTopSpeed 35`).  
**Strong at:** Long gaps, vertical drops, aerial navigation.  
**Weak at:** Straight-line ground races.

---

### 1 · Blaze — Dash
> Explosive burst in any direction. Weakest base speed — built entirely around chaining dashes.

| Stat          | Bar          | Score |
|---------------|--------------|-------|
| Speed         | `████░░░░░░` | 4/10  |
| Acceleration  | `████░░░░░░` | 4/10  |
| Jump          | `████████░░` | 8/10  |
| Air Control   | `██████████` | 10/10 |
| Ability Power | `█████████░` | 9/10  |

**Ability — Dash:** Ground + air dash with boosted force (`dashForce 35` vs default 25). 1 air dash per jump (`allowedAirDashes 1`), ground cooldown `1.0s`. Sharp turning (`turningDrag 22`) for corner-dash chains.  
**Cooldown:** 1.0s ground · 1 air dash per jump (resets on landing).  
**Strong at:** Crossing gaps instantly, obstacle recovery, burst speed.  
**Weak at:** Sustained speed on open straights.

---

### 2 · Bolt — Sprinter
> Untouchable on flat ground. Terrible in the air — once airborne, barely steerable.

| Stat          | Bar          | Score |
|---------------|--------------|-------|
| Speed         | `██████████` | 10/10 |
| Acceleration  | `█████████░` | 9/10  |
| Jump          | `███░░░░░░░` | 3/10  |
| Air Control   | `█░░░░░░░░░` | 1/10  |
| Ability Power | `████████░░` | 8/10  |

**Ability — Sprinter:** Passive. `topSpeed 9`, `runningTopSpeed 12` (default: 6 / 7.5). Heavy air penalty — `airTurningDrag 90`, `fallGravity 78` — punishes any time off the ground.  
**Strong at:** Straight sections, flat courses, dominating open ground.  
**Weak at:** Corners at speed, aerial sections, precision jumps.

---

### 3 · Bruno — Roller
> Momentum master. Devastating downhill. Agonisingly slow off the line without a roll.

| Stat          | Bar          | Score |
|---------------|--------------|-------|
| Speed         | `███░░░░░░░` | 3/10  |
| Acceleration  | `████░░░░░░` | 4/10  |
| Jump          | `█████░░░░░` | 5/10  |
| Air Control   | `████░░░░░░` | 4/10  |
| Ability Power | `█████████░` | 9/10  |

**Ability — Roller:** Enter roll at just `minSpeedToRoll 3` (default: 10). Near-frictionless momentum (`rollingFriction 0.2`), massive downhill force (`rollingSlopeDownwardForce 100`), poor uphill (`rollingSlopeUpwardForce 10`). Roll charge: 0.8s charge for `maxChargeForce 55` launch.  
**Cooldown:** 2.0s after roll ends (rarely triggers — Bruno stays rolling as long as he has speed).  
**Strong at:** Slopes, downhill sections, sustained momentum on flat.  
**Weak at:** Uphill, air sections, standing starts.

---

### 4 · Spike — Air Diver
> Highest jump in the roster. Dives into the ground and launches back up — punishing aerial sections.

| Stat          | Bar          | Score |
|---------------|--------------|-------|
| Speed         | `██████░░░░` | 6/10  |
| Acceleration  | `█████░░░░░` | 5/10  |
| Jump          | `██████████` | 10/10 |
| Air Control   | `█████████░` | 9/10  |
| Ability Power | `█████████░` | 9/10  |

**Ability — Air Dive:** Highest jump (`maxJumpHeight 20`) sets up dives. Powerful dive force (`airDiveForwardForce 28` vs default 16), low friction on landing (`airDiveFriction 20`), massive bounce recovery (`airDiveGroundLeapHeight 18` vs default 10).  
**Cooldown:** 1.5s after each dive (bounce + fall time makes this feel natural, not punishing).  
**Strong at:** Aerial sections, slope dives, bounce chaining.  
**Weak at:** Horizontal gap crossing, no burst tool on flat ground.

---

## Implementation Steps

1. Create folder `Assets/RC/Stats/`
2. Right-click → **Create → PLAYER TWO → Platformer Project → Player → New Player Stats**
3. Name: `GaleStats`, `BlazeStats`, `BoltStats`, `BrunoStats`, `SpikeStats`
4. Set each stat from the tables above — leave everything else at the PLAYER TWO defaults
5. Open each character prefab → root GameObject → `PlayerStatsManager` component → drag the matching stats asset into **Stats** array element 0

---

## Tradeoffs Summary

| Character | Ability | Strong At | Weak At |
|---|---|---|---|
| **Gale** | Glider | Long gaps, vertical drops | Ground speed |
| **Blaze** | Dash | Crossing gaps instantly, burst speed | Sustained acceleration |
| **Bolt** | Sprinter | Straight sections | Corners, air, precision jumps |
| **Bruno** | Roller | Slopes, downhill, momentum | Uphill, flat starts, air |
| **Spike** | Air Dive | Aerial sections, bounce recovery | Horizontal gaps, flat ground |
