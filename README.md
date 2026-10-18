# Advanced Resource Counter

A [Timberborn](https://mechanistry.com/) mod that adds a **Resource Counter with built-in memory
(hysteresis)** — one building that replaces the usual *2× Resource Counter + Memory (Set/Reset)*
automation setup.

## What it does

Pick a good and set two thresholds:

| Stock level                     | Signal                                  |
| ------------------------------- | --------------------------------------- |
| Drops **below the low** threshold  | turns **ON** ("Refilling")           |
| Between low and high            | **holds** its previous state            |
| Reaches **the high** threshold  | turns **OFF** ("Stocked")               |

Typical use: switch a producer on when logs fall below 50 and keep it running until the
warehouse is back up to 200, instead of flickering on and off around a single value.

- Works in both **Quantity** and **Percent (fill rate)** modes
- Supports **Include inputs**, just like the vanilla counter
- Shows the selected good's icon on the building's banner
- Latched state is saved with the game; settings copy with the duplicate tool
- Setting Low = High makes it behave like a plain "below X" counter
- Available to **Folktails** and **Iron Teeth**, next to the vanilla Resource Counter in the
  Automation tool group (same model, cost and science unlock)

## Requirements

- Timberborn **1.1.2.0** or later
- No other mods required

## Installation

**Steam Workshop / mod.io:** subscribe and enable it in the in-game mod manager.

**Manual:** download the zip from the [latest release](https://github.com/Biztactix-Ryan/Timberborn-AdvancedResourceCounter/releases/latest) and extract the `AdvancedResourceCounter` folder into your
Timberborn mods folder:

- Windows: `Documents\Timberborn\Mods\`
- Linux (Proton): `~/.steam/steam/steamapps/compatdata/1062090/pfx/drive_c/users/steamuser/Documents/Timberborn/Mods/`

Then enable it in the mod manager on the main menu.

## Building from source

Requires the .NET SDK (8+) and a local Timberborn install — the game's DLLs are referenced
directly, no Unity project needed.

```sh
dotnet build                      # assembles dist/AdvancedResourceCounter
dotnet build -p:Deploy=true       # also copies it into the game's Mods folder
dotnet build -p:GamePath=/path/to/Timberborn -p:ModsPath=/path/to/Documents/Timberborn/Mods
```

Defaults assume Steam on Linux/Proton; on Windows, pass `GamePath` and `ModsPath`.

### Layout

- `src/` – C# component, entity panel fragment, banner setter, goods dropdown, configurator
- `Mod/` – manifest, blueprints (cloned from the vanilla Resource Counter, reusing its model),
  template-collection appends, localization

### Releasing

GitHub Actions builds every push against reference-only copies of the game assemblies
(kept in a private repo, `Biztactix-Ryan/Timberborn-Stubs`, pinned by `TIMBERBORN_VERSION`
in `.github/workflows/build.yml`), and publishes a release when a `v*` tag is pushed.

1. Bump `Version` in `Mod/manifest.json` and add a section to `CHANGELOG.md`; commit.
2. Run `scripts/tag-release.sh`. It pushes `vX.Y.Z`, and CI attaches
   `AdvancedResourceCounter-vX.Y.Z.zip` to a release with that version's changelog notes.
3. Upload the same zip to [mod.io](https://mod.io/g/timberborn), and use the in-game
   Mod Manager's upload panel for the Steam Workshop.

`scripts/package.sh` builds the same zip locally. After a game update, regenerate the stubs
(see that repo's README) and bump `TIMBERBORN_VERSION`.

After the first Workshop upload, copy `workshop_data.json` from the deployed mod folder into
`Mod/` and commit it. It holds the Workshop item ID, so later uploads from any machine
update the same item. (`-p:Deploy=true` never deletes it from the Mods folder.)

## Localization

The mod ships all 15 languages the game supports. English is the source (`Mod/Localizations/enUS.csv`);
the others reuse the game's own terms but were written by a non-native speaker, so corrections are
very welcome — edit the CSV for your language and open a pull request, or file an issue.

## License

[MIT](LICENSE). Timberborn and its assets belong to Mechanistry; none are redistributed here.
