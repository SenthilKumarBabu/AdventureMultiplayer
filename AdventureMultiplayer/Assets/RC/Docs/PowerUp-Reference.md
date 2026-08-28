# Power-Up & Trap Reference

Last updated: 2026-08-03

---

## Inventory System

Every player carries **3 slots**. Slots are filled when a player walks into a `NetworkedPowerUpBox`. The HUD taps `UseSlotServerRpc` to activate. All effect logic runs on the **server**; results are pushed to clients via targeted `ClientRpc` calls.

---

## Self-Use Power-Ups

### Speed Boost
**File:** `PlayerPowerUpInventory.cs` → `DispatchSpeedBoost`

- Multiplies **topSpeed × 1.4** and **acceleration/airAcceleration × 1.2** for all stats entries.
- Duration: **5 seconds**.
- Effect is applied only on the **owner client** (`BeginSpeedBoostClientRpc`).
- Stats are divided back to original values when the timer expires.
- No shield/invincibility interaction (self-buff).

---

### Shield
**File:** `PlayerPowerUpInventory.cs` → `DispatchShieldAsync`

- Activates `NetworkedHealth.ShieldActive = true`.
- Lasts up to **30 seconds** or until it absorbs **one** incoming hit.
- **Absorbs (shield consumed):** StunBolt, Rocket, Freeze, Swap.
- **Does NOT block:** Banana Peel (BananaPeel.cs has no shield check), DecoyBox (intentionally bypasses both).
- If Invincibility is active when a hit arrives, the shield is not consumed.

---

### Invisible
**File:** `PlayerPowerUpInventory.cs` → `DispatchInvisibleAsync`

Merges the old **Invincibility** (damage immunity) and **Invisibility** (physical
pass-through) power-ups into one.

- Activates `NetworkedHealth.InvisibleActive = true`.
- Duration: **5 seconds**.
- **Blocks/phases through (not consumed):** Rocket, StunBolt, Freeze, Swap, Banana Peel, Decoy Box, Treadmill push, ObstacleKnockback.
- Also ignores player-vs-player collision (`Physics.IgnoreCollision` between the invisible player's `CharacterController` and everyone else's) and applies a semi-transparent glass look via `InvisibleEffect`.
- Invisible is never consumed; it simply ignores/passes through everything until it expires.

---

## Offensive Power-Ups

### Rocket
**Files:** `PlayerPowerUpInventory.cs` → `DispatchRocket`, `RocketProjectile.cs`

**Targeting:**
- Always targets the **1st-place player**.
- If the caster **is** in 1st place, the power-up has **no effect** (wasted).

**Projectile behaviour:**
- Homing — moves at **20 units/sec**, steers at **180°/sec**.
- Auto-despawns after **10 seconds** if it never hits.
- Uses a kinematic Rigidbody (`MovePosition`/`MoveRotation`) replicated via `NetworkTransform`.

**On hit:**
- **Shield present:** shield absorbed (consumed), rocket despawns — no damage.
- **Invisible active:** rocket blocked (Invisible not consumed), rocket despawns — no damage.
- **Otherwise:** deals **full MaxHealth** damage (kill shot).
  - Server calls `ApplyDamageFromPowerUp(MaxHealth)` → `Health.Value = 0`.
  - `ForceSetHealthClientRpc` sets `health.Set(0)` on **all** clients (HUD update).
  - `TriggerOwnerDeathClientRpc` sent **only** to the target's owner client:
    - `player.Die()` → fires `playerEvents.OnDie` → `NetworkRespawner` starts respawn timer.
    - `player.states.Change<DiePlayerState>()` → freezes player visually during the delay.
  - After **2 seconds** (`NetworkRespawner.respawnDelay`) the player respawns at their **last checkpoint**, offset by `(clientId % 4) × 90°` arc at 1.5 units to avoid stacking.
  - `SyncRespawnHealthServerRpc` is called after respawn: restores `Health.Value` to max on the server and broadcasts `ForceSetHealthClientRpc` to all clients so the HUD shows full health.

---

### StunBolt
**File:** `PlayerPowerUpInventory.cs` → `DispatchStunBolt`

**Targeting:**
- Targets the **player directly ahead** of the caster in race position.
- Fallback: if caster is **in 1st place**, targets the player **directly behind** instead.
- If no other player is found, does nothing.

**Shield/Invisible on target:**
- Shield: absorbs hit (consumed), no stun applied.
- Invisible: blocks hit (not consumed), no stun applied.

**Effect (`StunEffect.Apply`, 3 seconds):**
- Zeros `player.lateralVelocity` and `player.verticalVelocity` every `LateUpdate`.
- Sets `animator.speed = 0` — character pose freezes in place; movement animation stops.
- Clears all AI input (`desiredMoveDirection`, `jumpQueued`, `dashQueued`, etc.).
- On expiry: `animator.speed` restored to `1`.

---

### Freeze
**File:** `PlayerPowerUpInventory.cs` → `DispatchFreeze`

**Targeting:**
- Stuns **all players behind** the caster simultaneously.
- Each target is checked independently for shield/invincibility.

**Shield/Invisible per target:**
- Shield: absorbs (consumed per target). Invisible: blocks (not consumed per target).

**Effect:**
- Same as StunBolt (`StunEffect`) but duration is **configurable in Inspector** (`freezeDuration`, default **10 seconds**).

---

### Swap
**File:** `PlayerPowerUpInventory.cs` → `DispatchSwap`

**Targeting:**
- Swaps positions with the **player directly ahead** of the caster.
- **No effect** if caster is in 1st place.

**Shield/Invisible on target:**
- Shield: absorbs (consumed), swap cancelled.
- Invisible: blocks (not consumed), swap cancelled.

**Effect:**
- `TeleportClientRpc` sent to both caster and target — moves each to the other's position.
- Both players' `lateralVelocity` and `verticalVelocity` are zeroed on arrival.
- `RaceManager.SwapCheckpoints(caster, target)` — swaps their checkpoint indices so race ranking reflects the new physical positions immediately.

---

## Trap Power-Ups

### Banana Peel
**Files:** `PlayerPowerUpInventory.cs` → `DispatchBanana`, `BananaPeel.cs`, `SlipEffect.cs`

**Placement:**
- Dropped **1.2 units behind** the caster at ground level.
- Persists for **15 seconds** or until one player triggers it (single-use trap).

**On contact with non-owner player:**
- **Invisible:** slip blocked (not consumed, phases through). Trap remains.
- **Shield:** does NOT block the slip.
- **Otherwise:** trap despawns and `ApplySlipClientRpc` is sent to the victim's owner client.

**SlipEffect (duration configurable in Inspector, `bananaSlipDuration`, default 2 seconds):**
1. Sets `player.lateralVelocity = forward × 10 u/s` (ensures `minSpeedToSlide` is met).
2. Forces `player.states.Change<CrouchPlayerState>()` — shrinks collider, ground-snap active.
3. Sets `animator.SetBool("Is Sliding", true)` → animator transitions to the **Slide state** (Saphy|RailGrind clip for MixamoPlayer, Lily|Grind clip for Lily).
4. Every `Update` while on ground: `lateralVelocity` is locked to `forward × 10 u/s` to fight CrouchPlayerState's deceleration, so the player glides at constant speed.
5. Airborne: upward velocity is suppressed (no jumping out of the slide).
6. On expiry: `Is Sliding = false`, state reverts naturally.

> **Setup requirement:** Run `Tools > Adventure Multiplayer > Setup Slide Animation` once to wire the Slide state and `Is Sliding` parameter into all animator controllers.

---

### Decoy Box
**Files:** `PlayerPowerUpInventory.cs` → `DispatchDecoyBox`, `DecoyBoxPickup.cs`

**Placement:**
- Dropped **3 units ahead** of the caster + **0.5 units up** (lands in the path ahead).
- Persists for **20 seconds** or until one player triggers it (single-use trap).

**On contact with non-owner player:**
- **Intentionally bypasses Shield** — no protection check.
- **Invisible** still phases through it (pass-through half of the merged effect).
- Otherwise: trap despawns and `ApplyStunClientRpc` is sent to the victim.
- Stun duration: **2 seconds** (same StunEffect as StunBolt/Freeze).

---

## Shield & Invisible Interaction Matrix

| Power-up    | Shield            | Invisible           |
|-------------|-------------------|--------------------|
| Rocket      | Absorbs (consumed) | Blocks (kept)     |
| StunBolt    | Absorbs (consumed) | Blocks (kept)     |
| Freeze      | Absorbs (consumed) | Blocks (kept)     |
| Swap        | Absorbs (consumed) | Blocks (kept)     |
| Banana Peel | No check — hits   | Blocks (kept)     |
| Decoy Box   | No check — hits   | Phases through (kept) |

---

## Respawn Flow (Rocket kill)

```
RocketProjectile.OnTriggerEnter (server)
  └─ NetworkedHealth.ApplyDamageFromPowerUp(maxHealth)
       ├─ Health.Value = 0
       ├─ ForceSetHealthClientRpc(0)          → all clients: m_health.Set(0), HUD update
       └─ TriggerOwnerDeathClientRpc          → owner client only:
            ├─ player.Die()                   → playerEvents.OnDie fires
            │    └─ NetworkRespawner.OnPlayerDied
            │         └─ RespawnAfterDelay (2s UniTask)
            │              └─ ApplyRespawn()
            │                   ├─ player.SetRespawn(checkpoint + offset)
            │                   ├─ player.Respawn()               → health restored locally
            │                   └─ SyncRespawnHealthServerRpc()   → server: m_health.Set(max)
            │                        └─ ForceSetHealthClientRpc(max) → all clients HUD update
            └─ player.states.Change<DiePlayerState>()  → freeze pose during 2s wait
```

---

## Inspector-Configurable Values

| Field                      | Component                | Default | Notes                                |
|----------------------------|--------------------------|---------|--------------------------------------|
| `respawnDelay`             | NetworkRespawner         | 2 s     | Delay before respawn after death     |
| `freezeDuration`           | PlayerPowerUpInventory   | 10 s    | Freeze power-up stun duration        |
| `bananaSlipDuration`       | PlayerPowerUpInventory   | 2 s     | Banana peel slip duration            |
| `slideSpeed`               | SlipEffect               | 10 u/s  | Constant speed during banana slide   |
| `ShieldMaxDur` (const)     | PlayerPowerUpInventory   | 30 s    | Max shield duration before expiry    |
| `InvisibleDur` (const)      | PlayerPowerUpInventory   | 5 s     | Invisible duration                    |
| `StunBoltDur` (const)      | PlayerPowerUpInventory   | 3 s     | StunBolt stun duration               |
| `DecoyStunDur` (const)     | PlayerPowerUpInventory   | 2 s     | DecoyBox stun duration               |
| `moveSpeed`                | RocketProjectile         | 20 u/s  | Rocket travel speed                  |
| `turnSpeed`                | RocketProjectile         | 180°/s  | Rocket steering rate                 |
| `maxLifetime`              | RocketProjectile         | 10 s    | Rocket auto-despawn if no hit        |
| `lifetime` (BananaPeel)    | BananaPeel               | 15 s    | Peel auto-despawn                    |
| `lifetime` (DecoyBoxPickup)| DecoyBoxPickup           | 20 s    | Decoy auto-despawn                   |
