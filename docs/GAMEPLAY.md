# Zumbi Blocks: how the game actually plays

A plain-language guide for players, with the real numbers from the 2013 game. Everything here was read from the decompiled scripts (`game/Assets/Scripts/Assembly-UnityScript/`) and from the values saved in the prefabs and scenes. The saved prefab/scene value always wins over the default written in the script, so each table says where its numbers come from.

Short names used below:

* **prefab** = a value stored in a file under `game/Assets/GameObject/`.
* **scene** = a value stored in `game/Assets/Meus assets/Cenas/menu.unity` (shop, starting kit) or `city.unity` (the map).
* **code** = a value written in the script itself, with no prefab override.
* "unit" = one Unity world unit (roughly one block / one metre).

Playtest options (Timed waves, more players = more zombies, Loot More) change some of this. They are described in `docs/MULTIPLAYER.md`, "Playtest features", and are marked "(Timed/More)" below. Everything else is the original game.

---

## 1. Zombies

There is only **one zombie type** (prefab `ssumbii`). No runners, tanks or spitters. The only variation is a random skin colour and shirt colour, and whether it carries a backpack. They get tougher every wave (section 2).

### 1.1 Base stats (wave 1)

| Stat | Value | Source |
|---|---|---|
| Body health | 100 | prefab `ssumbii`, `sc_life.quantia` |
| Head health | 40 | prefab `ssumbii`, `sc_zumbi.headlife` (code default is 60, the prefab overrides it) |
| Walk speed | 3.5 units/s (the only speed it has; there is no run mode) | prefab `sc_zumbi.walkspeed` (code default 20) |
| Attack damage | 5 to 10 per hit (random in between) | prefab `dmgmin` / `dmgmax` |
| Attack reach | 1.5 units | prefab `atackreach` |
| Starts an attack when | within 1.7 units of its target | code |
| Attack rate | about one swing per second (the attack animation lasts 1.0 s; the hit lands between 0.4 s and 0.75 s into it) | code |
| Sight, in front | 50 units | prefab `frontsight` |
| Sight, behind | 20 units | prefab `backsight` |
| Spawn "birth" time | about 3.5 s of rising from the ground; it cannot be hurt by bullets or melee during this | code (`state -1`) |
| Chance to bleed you | 5 % per hit that lands on a player | code |

The zombie dies when **either** its body health or its head health reaches 0.

### 1.2 How zombies spawn

* The host picks a **random zombie spawn zone** for every zombie. The city map has **111 zones** tagged `zombiespawn` (counted in `city.unity`). Each zone is a box; the zombie appears at a random spot inside it, 1.28 units above the floor.
* At the start of each wave the whole wave appears at once (see section 2 for how many).
* Cap on zombies alive at the same time: **50** (prefab `zsmax`). In the original, zombies that would exceed the cap are simply dropped. In Timed mode they are queued and appear later with the stats of their wave (Timed).
* **2 % of zombies carry a backpack** (prefab `backpackprobmin 1` of `backpackprobmax 50`). In Loot More mode it is 10 % (Loot More).
* A zombie that has just spawned stands still ("born" animation) and then idles.

### 1.3 How zombies find you

A zombie that has no target just stands idle (it sometimes plays a "look around" animation: 5 % chance every half second). It does not wander. It starts hunting you in one of two ways.

**Seeing you.** Every frame it checks one player (it cycles through all players). It casts a line from its eyes toward the player. If nothing solid (walls, buildings) is in the way and the player is inside the sight distance, it locks on.

| Situation | How far it sees you |
|---|---|
| Player standing, in front of the zombie | 50 units |
| Player standing, behind the zombie | 20 units |
| Player crouching (for the first 0.3 s after you stand up it still counts as crouching) | 15 units in front (0.3 x 50), 0 units behind |
| At night | all distances halved (time-of-day factor `0.5 + timecoef/2`, where `timecoef` is 0 at night and 1 at noon) |
| Holding a lit glowstick or road flare | back to 1.0 x (full distance in front and behind), regardless of crouching or night |

Source: `sc_zumbi.LateUpdate` (the sight rays), `sc_player` (the exposure values 1 / 0.3 / 0, and the light override), `sc_timecontrol` (day/night factor).

**Hearing you.** Everything you do makes "noise". Any zombie that has no target and is closer than **noise / 4** units turns toward you. A noise is only sent when it gets louder than before, or once per second while it stays loud.

| What you do | Noise number | Heard within |
|---|---|---|
| Crouch-walking, standing still | 0 | nobody |
| Walking | 30 (prefab `walknoise`) | 7.5 units |
| Sprinting | 60 (prefab `sprintnoise`) | 15 units |
| Jumping or landing (a fixed noise in the code) | 80 | 20 units |
| Firing a gun | gun's `noisemaker` x 2 (prefab `noisemultiplier`) | gun's noisemaker / 2 units. Glock 45, Mac-10 45, AK-47 70, Enfield sniper 100, silenced USP only 5, RPG 30 |

A zombie that already has a target keeps chasing it and ignores new noises. While chasing, if another player gets clearly closer (the current target is more than 1.5 times as far as the new one) and the zombie sees them, it switches, at most once per second.

### 1.4 How zombies chase

The game has no navigation mesh. The map is cut into **areas** that are joined by **doors/gaps** (`AI_area`, `AI_porta`, `AAA_PATHFINDING`). When a zombie has a target it finds the shortest chain of doors from its area to the player's area, walks through each door in turn, and at the last door goes straight at the player.

* Speed while chasing is always `walkspeed` (3.5 at wave 1). There is no speed difference between "hunting" and "chasing".
* It gives up (goes back to idle) if there is no path to you, or if you stand somewhere the pathfinder does not know (it waits 4 s first). Breaking line of sight does **not** by itself make it forget you: it remembers your area and keeps following.
* A zombie has a 1-unit forward probe. If it bumps into an object that is a **barricade or sandbag** (anything with `sc_punchme`), it stops and attacks it as if it were a player. It damages it with the same 5 to 10 hits, so barricades are destroyed by hitting them over time (see 3.8 for their health).
* There is no jump or climb code for zombies; they only use the gaps in the area graph. If they are not standing on the ground they play a falling animation.

### 1.5 What a zombie attack does

* Damage is a random number between `dmgmin` and `dmgmax` (floats; it goes up every wave).
* The hit is a short line (reach 1.5) from the zombie's arm. It damages the first player body part, barricade or construction it touches. One hit per attack swing.
* A hit on a player: damage, a blood splash, and a **5 % chance of starting a bleed** (see section 4).
* A zombie hit by a **blunt** melee weapon, or shoved by a player, is knocked back for about one second (it cannot attack during that time).

### 1.6 Taking damage (and the "sneak attack" bonus)

* **A zombie that has not noticed anyone takes triple damage** from everything (bullets, melee, explosions, traps). The code subtracts the damage once normally and a second and third time when the zombie has no target. A headshot on an unaware zombie counts triple on the head health too.
* **Bullets:** a bullet that hits any body part does its full damage to the body health. If the part is the **head**, the same damage is also taken off the head health. So a headshot does the damage twice (once to each health bar), and the zombie dies as soon as either hits 0. Practical effect: with head health 40 and body health 100 at wave 1, **headshots kill 2.5 times faster than body shots**.

  | Gun (damage per bullet) | Body shots to kill (wave 1) | Headshots to kill (wave 1) |
  |---|---|---|
  | Glock 10 | 10 | 4 |
  | AK-47 27 | 4 | 2 |
  | M4A1 25 | 4 | 2 |
  | Desert Eagle 35 | 3 | 2 |
  | Enfield sniper 70 | 2 | 1 |

* **Melee** hits the body only (no head bonus), and melee cannot damage a zombie during its 3.5 s birth.
* There is **no damage falloff with distance** for bullets (see section 3.2).

### 1.7 What happens when a zombie dies

* **Money:** the player who got the last hit (bullet, melee or explosion; traps count for their owner) gets `int(7 x 1.11^wave)` dollars, where `wave` is the wave number **at that moment**. This is in match money; it is added to the shop wallet at the end of the match. A zombie killed by barbwire (no owner) gives **nothing**. Table in section 4.
* **Kill count** goes up by one for the killer.
* **Ragdoll:** the zombie turns into a physics ragdoll that is shoved by the bullet or blast. On a headshot kill the head blows off with a blood spray (the death effect, no gameplay change).
* **Backpack:** if it carried one (2 %), a random item drops on the floor (see 3.1). The item stays on the ground for 30 s.
* Zombies do not drop money, ammo or health by themselves.

---

## 2. Waves

### 2.1 When a wave starts

The host runs `sc_matchcontroller`. The match begins at **wave 0**, and the first wave starts the moment the match is running (fewer than 3 zombies alive is already true).

A new wave starts as soon as **either**:

1. **3 or fewer zombies are left alive** (code constant), or
2. the **120 s long-wave timer** runs out (code constant).

The original has a catch: the 120 s timer only runs if the previous wave was "cleared" (`canlongwave`). If a wave ended by timeout, the next one is only triggered by killing the zombies down to 3 or fewer. **Timed mode (default in our version)** arms the 120 s timer for every wave, and a wave only counts as cleared when the waiting queue is empty too. The HUD shows "Wave N" and the countdown. Classic keeps the original rule exactly.

### 2.2 How many zombies

| Rule | Value |
|---|---|
| Zombies per wave (1 player, original) | 15 + (wave - 1). Wave 1 = 15, wave 10 = 24, wave 30 = 44 (prefab `zminforwave` 15) |
| Cap on zombies alive | 50 (prefab `zsmax`) |
| More players (Timed) | round((15 + wave - 1) x (1 + 0.5 x (players - 1))). Wave 1: 2 players 22 to 23, 3 players 30, 4 players 38. Cap grows by the same factor up to 80 (Timed) |

### 2.3 How zombies get stronger

Every zombie is spawned with the base stats plus a bonus based on the wave it belongs to (code, `spawnzombie`):

| Per wave after wave 1 | Added |
|---|---|
| Body health | +10 |
| Head health | +4 |
| Damage min and max | +0.5 each |
| Walk speed | +0.06, but never above 6 |

| Wave | Body health | Head health | Damage (min to max) | Walk speed | Money per kill |
|---|---|---|---|---|---|
| 1 | 100 | 40 | 5.0 to 10.0 | 3.50 | 7 |
| 5 | 140 | 56 | 7.0 to 12.0 | 3.74 | 11 |
| 10 | 190 | 76 | 9.5 to 14.5 | 4.04 | 19 |
| 20 | 290 | 116 | 14.5 to 19.5 | 4.64 | 56 |
| 30 | 390 | 156 | 19.5 to 24.5 | 5.24 | 160 |
| 43 (speed cap reached) | 520 | 208 | 26.0 to 31.0 | 6.00 (capped) | 622 |
| 50 | 590 | 236 | 29.5 to 34.5 | 6.00 (capped) | 1291 |

* Speed would pass the cap of 6 at wave 43 (3.5 + 0.06 x 42 = 6.02). Health, head health and damage have no cap.
* Player walking speed is 5 and sprint is 7, so zombies are faster than a **walking** player from wave 27 on (5.06 > 5), and a sprinting player outruns them at every wave, but sprint runs out (section 4).
* Money per kill grows 11 % per wave, so late waves pay very well compared with the shop prices (a 1000-unit shop item is two kills at wave 30).
* A zombie keeps the stats of the wave it was created in, even if it is still alive when the next wave starts.

### 2.4 Other things the match does over time

* **Floor items** appear on random item spawn zones (45 zones in the city): one item every 20 s, at most 20 on the map (prefab `itemspawnrate 0.05`, `ismax 20`). 30 % are "real" items, 70 % are ammo boxes (prefab `probmin 30` of `probmax 100`). See 3.8 for what the real items are. Loot More mode changes these numbers (see `MULTIPLAYER.md`).
* The match ends when all players are dead (the game waits 3 s, then shows the score screen).
* `zspawnrate` and `rateincrease` exist in the code (the spawn rate was meant to go up every 30 s) but nothing reads them, so they have **no effect**.

---

## 3. Weapons and items

### 3.1 How you get weapons

* **Start of every match:** you spawn with what is in your **loadout** (10 slots, saved in the browser/PlayerPrefs). A new player starts with a **Glock and a Pipe Wrench** (code `sc_gamecontrol`: `invent[0]=0`, `invent[1]=8`). Each gun starts full plus one spare magazine's worth of ammo (loaded = `maxammo`, reserve = `maxammo`). Grenades, glowsticks and flares start with 1.
* **Shop (main menu):** you buy loadout items with the money you earned in earlier matches. Selling gives back 25 % (`sellprice`). The shop sells 31 items (list below). Items in your loadout are **not** lost when you die. Source for prices: scene `menu.unity`, `sc_item` entries; list of shop items: `sc_gamecontrol.itemtosell` in the same scene.
* **Zombie backpacks and supply bags:** can drop **any shop item** at random (each of the 31 equally likely), with a full magazine of ammo for guns. This is the only way to find guns during a match.
* **Floor loot** (the item spawns): only medkits, bandages, the radio, constructions, mines, and ammo boxes. No guns.
* **Dead players** drop their whole inventory when their corpse times out.

### 3.2 How shooting works

* **Bullets are fast projectiles, not instant hitscan.** Pressing fire creates a small bullet object at the muzzle. Each frame it moves forward (speed = the gun's `force`, 500 units/s for every gun except the rocket launchers) and checks a short ray ahead of itself. It lives 3.5 s (that is a range of about 1750 units, longer than anything on the map).
* **One bullet hits one thing and disappears.** No piercing. No damage falloff with distance: damage is the same at 2 units and at 500.
* **Spread** is an angle in degrees. Each bullet is rotated by a random amount between minus and plus `spread` around where you aim. The spread shown in the table is the zoomed (aiming down sights) value; hip-fire adds `addspread` (only the Enfield sniper has any: +8 degrees). **Crouching halves spread** (x 0.5).
* **Shotguns** fire 10 pellets per shot, each with its own random spread and its own damage.
* **Recoil:** each shot kicks the view up by `V_rec` degrees and sideways by up to `H_rec`.
* **Fire rate:** `rate` is shots per second. Guns marked "semi" fire once per click; "auto" keep firing while held.
* **Reload:** takes `puttime / 100` seconds (so 0.9 to 1.4 s). The reload moves up to `reloadamount` rounds from your reserve into the magazine. If you shoot with an empty magazine and auto-reload is on, it reloads by itself. Ammo is a **per-gun reserve**, not a shared pool by caliber.
* **Ammo box** (grey crate on the floor): adds `receiverammo x random(0.5 to 1.5)` rounds to the reserve of the gun you currently have selected.
* **Noise:** every shot is heard by zombies within `noisemaker / 2` units (see 1.3).
* **Friendly fire:** bullets never hurt other players (they pass the splash effect only). Explosions **do** hurt everyone, including you.

### 3.3 Guns

All numbers are from the weapon prefabs (`sc_gunscript`) in `game/Assets/GameObject/`; price from the scene shop entries. "Hip spread" adds `addspread`. Max DPS = damage x pellets x rate (auto guns; semi guns are limited by how fast you click). Reload = `puttime` / 100.

| Weapon | Damage per bullet | Shots/s | Mode | Magazine | Reload | Spread (deg) | Recoil up / side | Noise heard within | Shop price | Sell |
|---|---|---|---|---|---|---|---|---|---|---|
| Glock | 10 | 10 | semi | 18 | 0.9 s | 0.5 | 0.5 / 0.5 | 45 | $150 | $35 |
| Ingram Mac-10 | 10 | 15 | auto | 35 | 0.9 s | 2.5 | 0.5 / 1.0 | 45 | $200 | $50 |
| MP5 | 15 | 12 | auto | 30 | 1.2 s | 1.0 | 0.5 / 0.5 | 50 | $1,000 | $250 |
| Winchester .12 (double-barrel) | 12 x 10 pellets | 6 | semi | 2 | 1.2 s per cycle, loads 1 shell | 2.0 | 0.5 / 0.5 | 85 | $2,250 | $560 |
| Desert Eagle | 35 | 5 | semi | 9 | 0.9 s | 1.5 | 1.5 / 0.5 | 60 | $1,500 | $375 |
| SIG SG (rifle) | 20 | 10 | auto | 30 | 1.2 s | 0.9 | 0.8 / 0.8 | 60 | $2,500 | $600 |
| Tommy Gun | 15 | 14 | auto | 50 | 1.2 s | 1.0 | 0.5 / 0.8 | 45 | $5,000 | $1,250 |
| USP-45 Silenced | 17 | 10 | semi | 15 | 0.9 s | 0.4 | 0.5 / 0.5 | 5 | $4,000 | $1,000 |
| AK-47 | 27 | 12 | auto | 30 | 1.2 s | 1.0 | 1.0 / 1.0 | 70 | $6,000 | $1,500 |
| M16 | 26 | 13 | auto | 30 | 1.2 s | 0.5 | 0.8 / 0.8 | 70 | $8,000 | $2,000 |
| M4A1 | 25 | 14 | auto | 30 | 1.2 s | 0.9 | 0.8 / 0.8 | 65 | $8,000 | $2,000 |
| Enfield (sniper) | 70 | 4 | semi | 10 | 1.2 s | 0.3 (hip 8.3) | 0.5 / 0.5 | 100 | $9,000 | $2,250 |
| XM8 | 30 | 17 | auto | 35 | 1.2 s | 0.7 | 0.3 / 0.6 | 50 | $10,000 | $2,500 |
| G36 | 35 | 13 | auto | 30 | 1.2 s | 0.3 | 0.6 / 0.4 | 65 | $10,000 | $2,500 |
| AA-12 (auto shotgun) | 10 x 10 pellets | 5 | auto | 20 | 1.2 s | 2.0 | 1.0 / 1.0 | 50 | $15,000 | $3,750 |
| FN SCAR | 45 | 10 | auto | 30 | 1.2 s | 0.3 | 0.4 / 0.5 | 65 | $15,000 | $3,750 |
| RPG-7 | 300 blast | (1 rocket) | semi | 1 | 1.4 s | 0.3 | 0 / 0 | 30 | $10,000 | $2,500 |
| M202-A1 | 450 blast | (4 rockets) | semi | 4 | 1.4 s | 0.3 | 1.0 / 1.0 | 30 | $30,000 | $7,500 |

Where each is obtained: all are in the shop (from the 31-item list) and can drop from backpacks and supply bags at random. None appear as ordinary floor loot. Notes:

* The "hold fire" rate of the rocket launchers (RPG 4/s, M202 8/s) cannot be used in practice: they hold 1 and 4 rockets and reload 1.4 s.
* Per-hit damage in the table is before any wave bonus: **guns never get stronger or weaker with the wave**. Only zombies do.
* Magazine items (Glock Mag, Ingram Mag, MP5 Mag, Winchester Mag, Desert Eagle Mag, SIG Mag) exist as items but are not in the shop, not in the random loot list and not dropped by zombies. They only appear if a player drops one. Grabbing one adds 18 / 35 / 30 / 2 / 9 / 30 rounds of reserve for that gun.
* The "SG" prefab is the SIG Sauer rifle; "win2barrel" is the Winchester .12 double-barrel; "sniperenfield" is the Enfield. Names matched using the shop list (`itemlist`) and the prefab names.
* Starting ammo: Glock 18 loaded + 18 spare.

### 3.4 Melee weapons

Hit = damage + random(0 to `dmgrnd` minus 1), whole number. The ray starts at the camera and reaches `range` units. Melee **cannot headshot** and only damages the body. Blunt weapons knock a zombie back for about a second. A swing takes 0.45 to 0.9 s (the number below).

| Weapon | Damage | Range | Swing time | Blunt (knocks back) | Shop price | Sell |
|---|---|---|---|---|---|---|
| Pipe Wrench (starting kit) | 20 to 24 | 1.5 | 0.50 s | yes | $30 | $30 |
| Flashlight (weak) | 15 to 24 | 1.5 | 0.45 s | yes | $500 | $125 |
| Kitchen Knife | 25 to 29 | 1.5 | 0.45 s | no | $750 | $180 |
| Baseball Bat | 25 to 29 | 1.7 | 0.60 s | yes | $1,000 | $250 |
| Aluminium Bat | 30 to 39 | 1.7 | 0.60 s | yes | $2,500 | $625 |
| Crowbar | 45 to 54 | 1.7 | 0.62 s | yes | $7,000 | $1,750 |
| Cleaver | 55 to 64 | 1.5 | 0.75 s | no | $750 | $250 |
| Machete | 60 to 69 | 1.8 | 0.60 s | no | $3,500 | $875 |
| Fire Axe | 95 to 114 | 2.0 | 0.90 s | no | $7,000 | $1,750 |

Source: prefabs `pipewrench`, `flashlight`, `kitchenknife`, `baseball_bat`, `aluminium_bat`, `crowbar`, `cleaver`, `machete`, `fireaxe` (`sc_meelegun`). Damage shown is `dmg` to `dmg + dmgrnd - 1`. The Cleaver is oddly cheap for its damage. A melee hit on an unaware zombie does triple damage (1.6). When a zombie is within 1.5 units, pressing Shoot or Aim shoves it instead (a gun-butt bash if your gun is a `basher` type); damage of the shove is unknown.

### 3.5 Grenades, rockets and mines (explosives)

Blast damage is highest at the centre and falls off in a straight line to **0 at the blast radius**: damage = `dano` x (radius - distance) / radius. A wall between you and the blast stops the damage. Everyone in range is hit, **including you and your friends**, and players near a blast get a screen shake (stun) out to 2.5 times the radius.

| Item | Damage at centre | Radius | Fuse / trigger | Price | Source |
|---|---|---|---|---|---|
| He-Grenade | 350 | 10 | explodes 3.0 s after throw | $5,000 | prefab `Granada_out` (`sc_granada`) |
| Stick Grenade | 200 | 8 | explodes 3.0 s after throw | $2,500 | prefab `StickG_out` |
| RPG-7 rocket | 300 | 6 | on impact, flies at 50 units/s, max 5 s | $10,000 | gun `rpg7` (`dano`, `rangeofexp`, `force`) |
| M202-A1 rocket | 450 | 10 | on impact, flies at 100 units/s, max 5 s | $30,000 | gun `m202a1` |
| VS-50 Landmine | 200 | 7 | goes off when anything (a player or a zombie) steps on it | not sold; floor loot and supply bags | prefab `vs50_mine_0` |

* A grenade is thrown with a force of 1000 (charge for distance); you start the match with 1 if it is in your loadout.
* A zombie hit by a blast is flung away from the centre.
* Glowsticks (shop $750) and road flares (shop $750) are throwables that make light. They do not hurt zombies. A lit one in your belt slot makes you fully visible to zombies (1.3).
* Frag damage against a barricade or sandbag also counts: those have `sc_life` and a blast uses the same formula.

### 3.6 Constructions (placed defences)

You find these as floor loot or in supply bags (never in the shop). You pick one from your belt, a ghost shows where it will go, you place it, and it belongs to you (kills by it give you the money).

| Item | Health / effect | Details | Source |
|---|---|---|---|
| Sandbag barricade | 2,000 health | zombies stop and chew on it | prefab `sandbagbarricade` (`sc_life`) |
| Ground barricade | 1,000 health | same | prefab `groundbarricade` |
| Barbwire | 40 uses. Each zombie that stands in it takes **25 damage every 0.25 s** (that costs one use); a player who walks in takes **10** and uses one too. Gone after the 40th use | slows nothing, kills slowly; kills give no money (owner id -1) | prefab `barbwire` (`lifetime 40`; the script default is 25) |
| Bear trap | **Instant kill** of the first zombie or player that touches it (damage 999999). It closes and vanishes 20 s later | the kill pays the owner | prefab `beartrap`, code |
| Landmine | see 3.5 | | |

Unknown: whether zombies can destroy barbwire or traps (only barricades and sandbags carry the `sc_punchme` tag that makes zombies attack them).

### 3.7 Healing, radio and airdrops

| Item | Effect | Use time | Source |
|---|---|---|---|
| Medkit (MedShot) | +30 health (max 100) | 1.0 s | prefab `MedShot` (`sc_consumable.life 30`) |
| Bandages | +10 health and **stops bleeding** | 1.0 s | prefab `Bandages` (`life 10`, `stopbleeding`) |
| Radio (military) | calls an **airdrop** (see below); heals nothing | 1.0 s | prefab `radiomilitar` (`callsuply`) |

* Each use takes one from the stack (`ammo` count). Medkits, bandages and the radio come in stacks of 1 from the floor.
* **Airdrop:** using the radio makes a supply bag appear at a random one of the **27 bag drop points** on the map (`bagspawn`), 75 units up. It falls at 4 units/s for 17 s and lands as a bag. Anyone can open it. Opening it makes **7 rolls**. Each roll has a 1 in 8 chance of a random **shop item** (any of the 31, so it can be a rifle, an RPG or a grenade) and a 7 in 8 chance of a common supply item:

  | Supply roll | Weight (out of 60) |
  |---|---|
  | Ammo box | 10 |
  | Bandages | 10 |
  | Medkit | 10 |
  | Glowstick | 10 |
  | Road flare | 10 |
  | Barbwire | 2 |
  | Bear trap | 2 |
  | Ground barricade | 2 |
  | Sandbag | 2 |
  | Landmine | 2 |

  Source: prefab `supplybaground` (`myprobmin 1` of `myprobmax 8`, `myitemsrand`, `myitensprob`).

### 3.8 What the random floor items are

The regular floor spawn (a "real" item, 30 % of spawns; the other 70 % are ammo boxes) picks from this list (prefab `matchcontroller`, `itemsrand` / `itensprob`):

| Item | Weight (out of 130 in the original) |
|---|---|
| Medkit | 30 |
| Radio | 30 |
| Bandages | 20 |
| Barbwire | 10 |
| Bear trap | 10 |
| Sandbag | 10 |
| Ground barricade | 10 |
| Landmine | 10 |

Loot More mode adds hand grenades and stick grenades (20 each) and raises the real-item share to 50 % (Loot More).

---

## 4. The player

### 4.1 Health, bleeding, falling

| Thing | Value | Source |
|---|---|---|
| Health | 100 (maximum 100) | prefab `mightyplayer`, `sc_life.quantia` |
| Natural regeneration | none found: health only comes back from medkits and bandages | code (no regen code in `sc_player`) |
| Bleeding | each bleed hit adds a stack of **1 health per second** (it stacks if you get hit again). It lasts 30 s from the last bleed; **bandages** stop it at once | code (`addbleed`, `bleedingconter 30`) |
| What starts a bleed | a 5 % chance on every zombie hit | code (`sc_zumbi`) |
| Falling | falls of more than 4.6 units hurt: 10 health for each extra unit | code (`sc_player`) |
| Explosions near you | damage as in 3.5, plus a screen shake | code |
| Bear trap | instant death | code |

### 4.2 Lives and being revived

* You start with **3 lives** (prefab `lifes 3`). When your health hits 0 you fall as a **body** (a "dying" player). Every death costs one life, shown in the code as `lifes - 1`.
* For **20 s** (`playerdoll` `lifetime 2000` = 20 s) a teammate can **revive** you. Revive: stand at the body, hold Interact, then **mash the fire button**: the bar drains 150 per second and each click adds 45, you need 500. The reviver earns **$50**.
* If nobody revives you in 20 s, the body dies for good and the body drops your whole inventory (spread around it). It stays on the ground for another 30 s.
* If you were revived you come back with your **inventory exactly as you had it** (inventory, magazine and reserve ammo are copied from the body) and with the lives you had left.
* When your lives are used up (`lifes <= 0`, which is the fourth death, since lives go 3, 2, 1, 0), you die with no chance of revive. Counting it as the code does, a player can be revived **3 times** per match.
* If you are alone and go down with no teammate, you cannot revive yourself; the match ends when every player is dead.
* Score: kills are counted per player, the match shows your money and time survived at the end.

### 4.3 Movement and stamina

| Thing | Value | Source |
|---|---|---|
| Walk | 5 units/s | prefab `walkspeed` |
| Sprint | 7 units/s | prefab `sprintspeed` |
| Backward | x 0.7 | prefab `walkbackmulti` |
| Crouch | x 0.5 speed, spread x 0.5, zombies see you much less (1.3), you make no walk noise | code |
| Jump | `jumpspeed` 5, with gravity 14; air control 0.8 | prefab |
| Sprint stamina | 100. It drains **10 per second** while sprinting (10 s of sprint from full), and you need **20** to start a sprint. It refills at 10 per second when standing, but only a third of that (3.3 per second) while walking | prefab `sprintdecay 10`, `sprintregen 10`, `mintosrpint 20`; code |
| Shove (Shoot or Aim when a zombie is within range) | range 1.5, speed 1.5 | prefab `pushrange`, `pushspeed` |
| Use range | 3 units | prefab `userange` |

### 4.4 Money

| Source | Amount |
|---|---|
| Zombie kill (bullet, melee, explosion, trap you placed) | `int(7 x 1.11^wave)`: 7 at wave 1, 19 at wave 10, 56 at wave 20, 160 at wave 30 |
| Reviving a teammate | $50 |
| Zombie killed by barbwire | nothing |

Your match money is added to your **shop wallet** when the match ends (or when you leave). Prices of everything are in 3.3 to 3.5. There is no shop inside a match.

### 4.5 Inventory

* **10 item slots** (`item`, `quant`, `ammo` arrays of 10, prefab `mightyplayer`; the loadout in the main menu is also 10 slots).
* Each slot holds one item type: a gun with its loaded rounds and spare rounds, or a stack of consumables (`ammo` = count).
* Pick items up from the floor with Interact. What happens when you grab an item with all 10 slots full is unknown (the code destroys the floor item and only adds it if a slot is free). Dropping puts an item on the ground. Dead players' items and your own dropped items stay on the floor for 30 s when they come from a body or a backpack.
* Guns you pick up from the floor keep the rounds in them; a backpack gun comes with one full magazine (`quant = maxammo`).

---

## Not determined

* Exact melee hit timing inside the swing (`hittime` list) and the damage of the unarmed push.
* Whether zombies can destroy barbwire, bear traps and mines.
* Vehicle and helicopter scripts (`sc_carro`, `sc_vehicle`, `sc_helicopter`, `sc_plane`) were not read: they look like map scenery and are not part of normal play.
* How the original game limited wave size above 50 zombies on screen beyond dropping them (read from code only, not tested).
* Anything that depends on animation tick rates beyond what the code states (reload and swing times assume the code's 100 ticks per second clock).
