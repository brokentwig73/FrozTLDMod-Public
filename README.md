# Froz TLD Mod

**Froz TLD Mod is a configurable collection of quality-of-life improvements for *The Long Dark*.**

It started as a better way to keep useful survival information visible and grew into a broader set of fixes for the small frustrations that repeat throughout a run: checking the weather, starting fires, selecting tools, managing inventory weight, placing gear, and switching weapons.

The goal is not to turn *The Long Dark* into a different game. Froz TLD Mod keeps the original look and behavior wherever possible, then quietly improves the parts that create unnecessary repetition. Every major feature can be enabled or disabled from **Mod Settings**.

![Froz TLD Mod survival HUD shown during outdoor gameplay](images/hud-outdoor-overview.jpg)

## AI Disclaimer

This project was, in part, an experiment in AI.  I have a BS in Computer Science, and have developed a few applications from scratch, including applications in C++, C#, and have years of experience in PHP with a couple Angular apps.  However, times are changing, and Agentic Engineering looks to be the future.  I primarily used OpenAI through Codex 5.5 and 5.6 Sol at Extra High for most of this development.  I was determined to not write any code (or this Readme) directly, but only through the AI agent.  In the end, I have about 200+ hours in this project.  I found this AI to be very competent in actually writing code, if and when it understood what it was doing.

However, after completing this project, I see no way an inexperienced or uneducated person could develop a clean and functional application through "vibe coding".  Chat GPT 5.5 and 5.6 Sol repeatedly circled on itself when attempting to solve issues, or attempted to solve issues through overly complex fixes of fixes of fixes rather than opening and reviewing the games code.  It blindly attempted to write all manner of fall-back methods without regard to which ones actually worked.  It's naming conventions, folder structures, and naming convention adherence was generally horrible.  However, when supervised and guided properly, it was able to get it done in the end.

I understand the use of AI is a hot topic right now, and I clearly see the threat it poses to the I.T. Industry across many fields.  However, it's not going anywhere, it's only getting smarter, and I don't want to be left behind.  However, if it's offensive to you that I used AI in this project, I understand the sentiment.

## Highlights

### A more useful survival HUD

Press **Tab** to show or hide a compact HUD built around the game's time-of-day display. Sticky mode keeps it visible until you toggle it off.

The HUD can include:

- A stick compass
- Wind direction and wind speed
- Current and outdoor feels-like temperatures
- An analog clock
- The game's scent indicator
- Current backpack weight and maximum carry capacity
- Backpack category weights, including carried and worn clothing totals

Each element can be turned on or off independently.

The indoor and outdoor displays adapt to the information available. Indoors, the stick compass is disabled while the HUD continues showing time, scent, indoor and outdoor temperature, and carry weight. Outdoors, the compass and wind dials provide their full directional readouts.

| Indoor HUD | Outdoor HUD |
| --- | --- |
| ![Close-up of the modular survival HUD indoors](images/hud-indoor-closeup.png) | ![Close-up of the modular survival HUD outdoors](images/hud-outdoor-closeup.png) |

Category totals and percentages can also be displayed directly beside the backpack filters, including separate carried and worn clothing weights.

![Backpack category weights and percentages](images/inventory-category-weights.png)

### Less friction around fire

Froz TLD Mod improves several repetitive fire-starting interactions:

- Prefer a lit torch or flare when one is already in hand
- Default to sticks as starting fuel when available
- Continue using optional tinder after Fire Starting Level 3
- Prevent Birch Bark from being selected automatically
- Right-click loose sticks, wood, coal, or other valid fuel and place it directly onto a burning fire

When dragging fuel into a fire, the game-style interaction panel shows the added burn time, temperature increase, and resulting totals before you commit.

![Adding loose fuel directly to a burning fire](images/drag-fuel-into-fire.png)

### Remembers the choices you already made

The game often asks you to select the same tool repeatedly. Froz TLD Mod remembers your last selection for:

- Crafting
- Breaking down objects
- Harvesting and quartering carcasses
- Making and clearing ice-fishing holes

It also remembers the exact weapon you selected, including individual condition variants, for the weapon hotkey and radial menu.

### Weapon reticles

Optional reticles are available for pistols, rifles, and the flare gun. The pistol hip-fire reticle follows the game's actual impact calculation for hip-firing instead of assuming the center of the screen is correct.

![Pistol hip-fire reticle aligned with the game's impact calculation](images/pistol-hip-fire-reticle.png)

### Light-source life warning

Torch, flare, and lantern life indicators turn red when light source approaches empty, making it easier to notice before it goes out.

![Torch life bar and icon turning red near burnout](images/red-light-life-warning.png)

### Item Placement Collision

Items can inherit oversized placement collision boundaries, especially dropped items, forcing unnatural gaps between bottles, cans, firewood, and other loose gear. Froz TLD Mod corrects those excessive boundaries so loose items can be placed closely beside one another while still preventing overlap.

This is not **Place Anywhere** functionality. It does not disable normal placement rules, allow objects to clip through each other, or add unrestricted positioning controls. It specifically fixes the game's overly large item-to-item placement colliders, including the especially noticeable spacing applied to freshly dropped items.

| Oversized placement boundary | Corrected placement footprint |
| --- | --- |
| ![Placement blocked by an oversized loose-item boundary](images/item-placement-oversized-boundary.png) | ![Loose items placed closely without overlapping](images/item-placement-after.png) |

### Small fixes that add up

- Control Aurora sound volume for both ambience and electrical crackling sounds.
- Skip the startup disclaimer sequence through Hinterland's built-in `-skipintro` command line option

## Configuration

Features can be enabled independently from **Options > Mod Settings > FROZ TLD MOD**. The master switch disables the entire mod without removing it.

![Froz TLD Mod configuration options](images/mod-settings.png)

## Requirements

- *The Long Dark: Survival Mode* on Windows
- [MelonLoader](https://github.com/LavaGang/MelonLoader) 0.7.2
- [ModSettings](https://www.tldmods.net/)

Each release will identify the game and MelonLoader versions it was tested with.

## Installation

1. Install MelonLoader and launch the game once.
2. Install ModSettings mod.
3. Download the latest `FrozTLDMod.zip` from the [Releases](../../releases) page.
4. Extract the archive into the game's `Mods` folder.
5. Confirm these files exist:
   - `Mods/FrozTLDMod/FrozTLDMod.dll`
   - `Mods/FrozTLDMod/manifest.json`
6. Launch the game and open **Options > Mod Settings > FROZ TLD MOD**.

## Updating

Download the newest release and replace the existing `Mods/FrozTLDMod` folder. Your Mod Settings are stored separately and should remain intact.

## Reporting problems

Use the [Issues](../../issues) page to report a problem with a published release. Please include:

- The Froz TLD Mod version
- The Long Dark version
- The MelonLoader version
- A short description of what happened
- The relevant section of `MelonLoader/Latest.log`, when available

## Building from source

The production C# source and embedded HUD assets are included in this repository.

1. Install The Long Dark, MelonLoader, and ModSettings.
2. Launch the game once so MelonLoader generates its IL2CPP interop assemblies.
3. Open `FrozTLDMods.slnx` with the .NET 6 SDK installed.
4. If The Long Dark is installed somewhere other than Steam's default folder, override the `TldGameDir` MSBuild property.
5. Build the `src/FrozHud/FrozHud.csproj` project.

The compiled DLL and copied `manifest.json` are placed under `src/FrozHud/bin/<configuration>/net6.0`.

## About this repository

This is the official source, download, documentation, and support repository for Froz TLD Mod. It contains the production mod source and assets used to build published releases.

## Disclaimer

Froz TLD Mod is an unofficial community modification and is not affiliated with or supported by Hinterland Studio. Please direct mod-related support requests [here](../../issues) or to the *The Long Dark* modding community, not to Hinterland.
