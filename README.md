# Valheim Spawner Cooldown

A BepInEx mod for Valheim that changes how often creature spawners such as body piles, greydwarf nests and bone piles spawn new creatures. Vanilla body piles try to spawn a draugr every 5 seconds; with this mod you choose the interval.

## Features

- A global multiplier and one multiplier per spawner type, both as sliders in the in-game configuration window (F1). Each spawner shows its resulting interval next to the slider, for example `x4.0 = 20 s (vanilla 5 s)`.
- Changes apply immediately, no restart needed.
- Natural first wave: when you arrive at a spawner, its first group still spawns at the vanilla pace (up to the spawner's `maxNear` creatures), then your interval applies to respawns.
- Works in multiplayer for the player running it, with no server-side install (see [Multiplayer](#multiplayer)).

## Requirements

- Valheim with [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) 5.4.22 or later.
- Optional, to edit settings in game: [Official BepInEx ConfigurationManager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/).

## Installation

### 1. Install BepInEx

If you use a mod manager (r2modman, Thunderstore Mod Manager), install `BepInExPack_Valheim` from it.

To install it manually:

1. Download BepInExPack Valheim from the link above and open the archive.
2. Copy the content of its `BepInExPack_Valheim` folder into your Valheim folder, next to `valheim.exe`. With Steam, this folder is usually `C:\Program Files (x86)\Steam\steamapps\common\Valheim`.
3. Start the game once and quit. A `BepInEx` folder with `config` and `plugins` subfolders now exists.

### 2. Build the mod

1. Install the [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0) or later.
2. Clone this repository.
3. From the repository folder, run:

   ```
   dotnet build -c Release
   ```

   If Valheim is not installed in the default Steam folder, pass its location:

   ```
   dotnet build -c Release -p:ValheimDir="D:\SteamLibrary\steamapps\common\Valheim"
   ```

4. The mod is built as `bin\Release\ValheimSpawnerCooldown.dll`.

### 3. Install the mod

Copy `ValheimSpawnerCooldown.dll` into the plugins folder:

- Manual install: `<Valheim folder>\BepInEx\plugins\`
- r2modman: `%APPDATA%\r2modmanPlus-local\Valheim\profiles\<profile name>\BepInEx\plugins\`

Only the `.dll` is needed. Start the game; `BepInEx\LogOutput.log` should contain `Loading [Spawner Cooldown ...]`.

## Configuration

Press **F1** in game (with ConfigurationManager installed) and open **Spawner Cooldown**, or edit `BepInEx\config\alex83690.valheim.spawnercooldown.cfg` while the game is closed.

| Setting | Default | Description |
|---|---|---|
| General / Enabled | true | Apply the configured intervals. |
| General / ClaimOwnership | true | Take control of spawners near you that are run by players without the mod. |
| General / NaturalFirstWave | true | Keep the vanilla interval for the first group after you take control of a spawner. |
| General / DebugLog | false | Log spawner values, ownership claims and the end of each natural first wave. |
| Interval / GlobalMultiplier | 1 | Multiplier applied to every spawner. 1 = vanilla. |
| Spawners / *prefab name* | 1 | Multiplier for one spawner type, combined with the global one. |

The interval of a spawner is: vanilla interval x global multiplier x spawner multiplier. Spawners are checked every 2 seconds, so intervals below 2 seconds behave like 2 seconds.

The spawner list is read from the game when a world loads, then remembered. On the very first launch, the **Spawners** section appears after you load a world; from then on it is available from the main menu.

### Known spawners

| Prefab | Vanilla interval |
|---|---|
| Spawner_DraugrPile (body pile) | 5 s |
| BonePileSpawner, BonePileSpawner_swamp | 5 s |
| BlobMorkBig | 5 s |
| Spawner_GreydwarfNest | 10 s |
| Spawner_Hole, Spawner_Hole_double | 10 s |
| Spawner_CharredStone, _Elite, _event | 10 s |
| EvilHeart_Forest, EvilHeart_Swamp | 20 s |
| BlackIce_Core | 50 s |
| Spawner_Kvastur | 60 s |
| Spawner_CharredCross | 80 s |

## Multiplayer

In Valheim, a spawner is run by a single client: the network owner of that object, usually the first player who came near it. The mod only changes spawners run by a client that has it:

- No install is needed on the server or for other players.
- When a spawner near you is run by a player without the mod, your client takes it over, so your settings apply. Disable `ClaimOwnership` to prevent this.
- When several players have the mod, a spawner already run by one of them is never taken over, and only one modded player claims a free one. Clients do not fight over spawners.
- Creatures are shared by everyone: a spawner has one pace for the whole group, the one set by the player running it.

## Credits

Author: Alexandre Le Borgne (alex83690).

## License

[ValheimSpawnerCooldown](https://github.com/alexandre-le-borgne/ValheimSpawnerCooldown) © 2026 by [Alexandre Le Borgne](https://github.com/alexandre-le-borgne) is licensed under [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/) <img src="https://mirrors.creativecommons.org/presskit/icons/cc.svg" alt="" width="16" height="16"><img src="https://mirrors.creativecommons.org/presskit/icons/by.svg" alt="" width="16" height="16"><img src="https://mirrors.creativecommons.org/presskit/icons/nc.svg" alt="" width="16" height="16"><img src="https://mirrors.creativecommons.org/presskit/icons/sa.svg" alt="" width="16" height="16">

You may use, share and modify this mod for non-commercial purposes, as long as you credit the author and distribute your modified versions under the same license. See [LICENSE](LICENSE) for the full terms.
