# Game Design Document — Rush Champions

## Game Overview

- **Title:** Rush Champions
- **Genre:** Multiplayer Obstacle Race
- **Target Audience:** Casual to mid-core mobile players
- **Platform:** Mobile (Android, iOS)
- **Multiplayer:** Up to 4 players via Unity Relay (internet play)

## Core Gameplay

Race through 50+ obstacle-filled levels to finish 1st. Pick up powerups along the way, use your character's unique ability, and survive the obstacles that slow everyone down. First across the finish line wins.

## Characters & Abilities

Each character has one unique ability via PlayerStats tuning. No custom code — all abilities use PLAYER TWO mechanics.

| # | Character | Ability |
|---|---|---|
| 0 | **Gale** | Glider |
| 1 | **Blaze** | Dash |
| 2 | **Bolt** | Sprinter |
| 3 | **Bruno** | Roller |
| 4 | **Spike** | Air Dive |

See `CharacterAbilities.md` for full stat tables.

## In-Game Powerups

Pickups scattered across levels — collected mid-race:

- **Shield** — blocks one hit from an obstacle or rival
- **Rocket** — forward speed burst, launches past rivals
- *(more to be added)*

## Obstacles

Environmental hazards placed throughout levels to slow players down. Variety increases with level progression.

## Progression

- **50+ levels** across multiple worlds
- **Character upgrades** — improve ability strength/duration
- **Skins & Cosmetics** — character skins, outfits, effects
- **Events** — limited-time seasonal challenges and rewards

## UI/UX Guidelines

- Large touch controls optimised for mobile (Virtual Gamepad MobileRig)
- CanvasScaler reference resolution: 960×540 for 2× effective UI size
- Lobby: Host/Join via Unity Relay join code
- In-game HUD: player count, race position, ability cooldown indicator
