# Poison Ivy

A [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) mod for *Pathfinder: Wrath of the Righteous* that adds **Poison Ivy**, a Druid archetype built on [Nature Fang](https://github.com/Vek17/TabletopTweaks-Base) from Tabletop Tweaks.

Druids who dwell in underground caverns learn to command the venom of plants. Through their hands they work magical infusions, drawing extra power from poisons and acids.

## Requirements

- *Pathfinder: Wrath of the Righteous* 2.7.0
- Unity Mod Manager 0.23.0 or newer
- [TabletopTweaks-Base](https://github.com/Vek17/TabletopTweaks-Base), loaded before this mod

Poison Ivy clones Nature Fang at runtime. Without Tabletop Tweaks the archetype is not created.

## How to play

Create a new Druid and choose **Poison Ivy**. The toggle is on by default and stays on through rest.

## What the archetype does

Poison Ivy keeps Nature Fang’s progression, then replaces the slayer talent picks:

| Level | Change |
| --- | --- |
| 4 | Slayer talent is replaced by **Painful Strike**. Its save DC uses Druid level. |
| 6 | Slayer talent is replaced by the **Poison Ivy** toggle. |
| 10, 14, 18 | Slayer talent picks are removed. |

**Poison Ivy** (level 6, toggle) infuses every weapon you wield, including empty-hand and extra limbs, with plant venom:

- Extra acid damage: 1d3, 1d6 at 14th level, 2d6 at 20th level.
- While the infusion is on: +1 alchemical bonus to Wisdom and Intelligence at 10th level, +2 at 16th, +3 at 20th.
- The weapon uses the corrosive glow.

Studied Target counts Druid levels and this archetype. A few existing talents also become available to Poison Ivy at levels 4, 6, 10, and 12.

## Building

The project targets .NET Framework 4.8 and references the game assemblies plus Unity Mod Manager. `Directory.Build.props` points at a local install:

```
D:\SteamLibrary\steamapps\common\Pathfinder Second Adventure
```

Change `GamePath` there if your copy lives somewhere else. A successful build copies `PoisonIvy.dll` and `Info.json` into the game’s `Mods\PoisonIvy` folder.
