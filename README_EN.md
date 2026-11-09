<h1 align="center">ChaAlphaMask</h1>

<p align="center"><a href="README.md">简体中文</a> | <b>English</b></p>

A plugin that gives clothing "body masks / alpha masks". A mask can come from a zipmod (`manifest.xml`) or be imported straight into the character card from the in-game maker (**no XML needed**).

This repository is a decompiled and re-adapted source tree that builds both the Koikatu and KoikatsuSunshine plugins from a single set of sources.

> ## ⚠️ Read this first
>
> - **Koikatu repacks (HF Patch, BetterRepack, …) already ship this plugin** (HF Patch bundles `Character Alpha Mask v1.0.0`). The original features already work there, so the **KK build is not recommended for repacks** — the identical file name and plugin GUID easily cause conflicts / incompatibilities.
> - **This plugin mainly exists to fill the missing plugin on KoikatsuSunshine**: if your KKS install has no ChaAlphaMask, use `KKS_ChaAlphaMask.dll`.
> - If you really need it on Koikatu: **overwrite** `BepInEx\plugins\KK_ChaAlphaMask.dll` with `KK_ChaAlphaMask.dll`. **Do not rename it and keep both**, and do not mix it with same-named plugins from other sources.
> - Details: [Coexisting with HF Patch / repacks](#coexisting-with-hf-patch--repacks).

## Features

- **Masks stored inside cards (no XML)**: open the "Character Alpha Mask" window in the maker and import / export / delete PNG masks per clothing slot. Masks are saved into the character card (all outfits) and coordinate cards, and keep working across machines, characters and H scenes.
- A `ボディマスク` (body mask) checkbox is added to every clothing slot in the maker.
- An `Alpha mask` dropdown is added to every clothing slot, letting one part's mask follow another part's clothing state.
- zipmods can provide mask textures through `manifest.xml`, either as a plain PNG or as an asset inside an AssetBundle.
- Compatible with masks stored by Madevil `PlusPack - ChaAlphaMask` (`madevil.kk.PPCAM`); old cards do not need to be redone.
- Integrates with Material Editor / Clothes Overlay (KCOX): mask changes follow material and overlay updates.

## Using card-stored masks

1. In the character maker, tick **`Alpha遮罩窗口`** (Alpha Mask Window) at the bottom of the right-hand Control Panel, or press `F5` (the hotkey is configurable).
2. Click **Import** on a clothing slot and pick a PNG:
   - `512×512` is recommended (matches the body UV); other sizes work too and a log message will remind you.
   - Black background; paint the body parts to hide (mask out) in **pure red**, leave the rest black.
3. A thumbnail appears in the slot once it took effect. **Export** writes the mask back to a PNG, **Delete** removes it from that slot.
4. Save the character card (or coordinate card) — the mask travels with the card.

Notes:

- Masks are stored per "outfit (coordinate) × clothing slot": a mask only applies to the current outfit, other outfits need their own import.
- Rows are only shown for equipped slots; tick **Show all** to see empty slots and pre-import masks for them.
- The window can be toggled from the Control Panel checkbox, the `F5` hotkey, or the `×` button in the window title.
- While the mouse is over the window the plugin temporarily blocks the game's mouse input, so dragging the window does not rotate the character / camera and does not click through to the UI behind it.

Supported slots: bottoms, bra, shorts, gloves, pantyhose, socks, indoor shoes, outdoor shoes (the top slot has no mask, same as the original plugin).

## Configuration

`BepInEx/config/nakay.kk.ChaAlphaMask.cfg`:

| Section | Key | Default | Description |
|---|---|---|---|
| Maker | `Toggle Mask Window` | `F5` | Hotkey that toggles the mask window |
| Maker | `Window X` / `Window Y` | `560` / `100` | Window position, remembered after dragging |
| General | `Export Path` | `UserData\Overlays` | Where masks are exported; also the starting folder of the import dialog |
| General | `Import PlusPack Mask Data` | `true` | Read mask data stored by Madevil PlusPack |

## Tutorial

**Full illustrated tutorial (draw a mask → put it into unity3d → fill in the manifest → verify in game):**

**Local file: [docs/独立遮罩使用方法.md](docs/独立遮罩使用方法.md)** (Chinese)

## Directory layout

```
src/                                       Shared sources
  ChaAlphaMask.cs                          Plugin entry point / configuration
  AlphaMaskController.cs                   Character behaviour / card data / mask application
  MaskWindow.cs                            In-maker "Character Alpha Mask" window (import/export/delete)
  InputBlock.cs                            Mouse input blocking (dragging the window no longer moves the character)
  MakerGui.cs                              Maker UI controls
  MaskProperty.cs                          zipmod manifest parsing
  MultiplyShader.cs                        Shader property IDs for the multiply pass
  Patch.cs                                 Harmony patches
  Util.cs                                  Helpers
  KK_ChaAlphaMask.resources.alphamaskmultiply   Embedded AssetBundle with the multiply shader
KK_ChaAlphaMask/KK_ChaAlphaMask.csproj     Koikatu build (net35)
KKS_ChaAlphaMask/KKS_ChaAlphaMask.csproj   KoikatsuSunshine build (net46)
docs/独立遮罩使用方法.md                    Illustrated mask tutorial (Chinese)
docs/images/                               Tutorial images
```

Both projects share `..\src\*.cs`; only the referenced game assemblies and the target framework differ, so all code changes go into `src/`.

## Building

Requires the .NET SDK (or Visual Studio 2022 with the .NET Framework targeting pack):

```powershell
dotnet build KK_ChaAlphaMask\KK_ChaAlphaMask.csproj -c Release -p:GameDir="E:\game\GDAkk\Koikatu"
dotnet build KKS_ChaAlphaMask\KKS_ChaAlphaMask.csproj -c Release -p:GameDir="E:\game\GDAkk\KoikatsuSunshine"
```

`-p:GameDir=` points at the game root (the folder containing `Koikatu_Data\Managed` and `BepInEx`). If omitted, the default path from the csproj is used.

## Dependencies

- BepInEx 5
- [Illusion Modding API](https://github.com/IllusionMods/IllusionModdingAPI) (KKAPI / KKSAPI)
- [Material Editor](https://github.com/IllusionMods/IllusionMaterialEditor)
- [Overlay Mods](https://github.com/ManlyMarco/Illusion-Overlay-Mods) (KCOX)
- Sideloader, ExtensibleSaveFormat (BepisPlugins)
- Force High Poly (optional; only used when installed — read via reflection, so the plugin also runs without it)

## zipmod mask configuration

Add a `ChaAlphaMask` block to the zipmod's `manifest.xml`; each `<mask>` maps to one clothing item:

```xml
<manifest schema-ver="1">
  <guid>your-mod-guid</guid>
  <name>Some Mod</name>
  <version>1.0</version>

  <ChaAlphaMask>
    <mask>
      <category>109</category>            <!-- clothing kind, see table below -->
      <id>58809</id>                      <!-- clothing ID from the csv -->
      <abPath>chara/xxx.unity3d</abPath>  <!-- bundle with the mask texture (relative to abdata) -->
      <abBodyMaskName>alpnamark_gloves</abBodyMaskName>  <!-- texture asset name inside the bundle -->
    </mask>

    <mask>
      <category>109</category>
      <id>58811</id>
      <pngBodyMaskPath>chara/xxx/mask.png</pngBodyMaskPath> <!-- or use a plain PNG -->
    </mask>
  </ChaAlphaMask>
</manifest>
```

| Field | Description |
|---|---|
| `category` | Clothing kind number (e.g. 109 = gloves), used to match the item |
| `id` | Clothing ID; Sideloader converts it to the local slot automatically |
| `abPath` | AssetBundle containing the mask texture, relative to `abdata/` |
| `abBodyMaskName` | Texture asset name inside the bundle |
| `pngBodyMaskPath` | Path to a plain PNG, relative to `abdata/` |
| `botMask` | Marks the mask as belonging to the lower body (bottoms / shorts) |
| `objOpt01` / `objOpt02` | Tied to the clothing option toggles; the mask follows them |
| `guid` | Usually omitted, defaults to the manifest `<guid>` |

Use either `abPath` or `pngBodyMaskPath` — both are optional; without them the item simply has no mask texture.

The full illustrated workflow (draw a mask → put it into unity3d → fill in the manifest → verify in game) is in [docs/独立遮罩使用方法.md](docs/独立遮罩使用方法.md) (Chinese).

## Card data

Masks live in ExtensibleSaveFormat plugin data under the `CustomMasks` key (`List<CustomMask>`, each item has `coordinateType` / `clothesKind` / `png` raw bytes):

- Character card: masks of every outfit
- Coordinate card: masks of the current outfit only
- When both a card mask and a zipmod manifest mask exist, **the card mask wins**, so you can tweak someone else's mod clothing without editing the mod itself

## Coexisting with HF Patch / repacks

**Koikatu repacks already ship this plugin** — that is the main source of "conflict / incompatible" reports, so handle it like this.

### What is bundled

| Repack | Component | Version | Installed to |
|---|---|---|---|
| [KK-HF_Patch](https://github.com/ManlyMarco/KK-HF_Patch) | `Content\KK_ChaAlphaMask` (enabled by the `full` / `extra` presets) | `Character Alpha Mask v1.0.0` (by nakay) | `BepInEx\plugins\KK_ChaAlphaMask.dll` |
| BetterRepack (RX series) and similar | Bundled plugin | usually 1.0.0 | `BepInEx\plugins\KK_ChaAlphaMask.dll` |

The file name is **identical** to this repository's release, and the plugin GUID is the same (`nakay.kk.ChaAlphaMask`).

### Do

- To use the new features: **overwrite** `BepInEx\plugins\KK_ChaAlphaMask.dll` with `KK_ChaAlphaMask.dll`
- Reinstalling, repairing or updating HF Patch / the repack **restores 1.0.0** (the window disappears, new card data is not read), so overwrite again afterwards
- If you only need the original features, you do not need this KK build at all — the bundled one is enough

### Don't (this is what causes "incompatibilities")

- **Renaming and keeping both**: putting the DLL under another name (e.g. `KK_ChaAlphaMask111.dll`) into `plugins` gives two plugins with the same GUID → BepInEx skips one of them, and KKAPI's `RegisterExtraBehaviour` throws because the controller GUID is already registered. Result: the plugin does not work and the log fills with errors.
- **Mixing with same-named plugins from other sources** (e.g. a Madevil PlusPack equivalent installed at the same time)

### Card data caveat

This plugin adds a `CustomMasks` key to the plugin data. **1.0.0 does not know about it**: if a card is saved once while 1.0.0 is active, KKAPI replaces the whole plugin data block for that GUID and the card-stored masks are lost. The other way around works fine — this plugin reads cards made with 1.0.0 (same GUID, old fields are parsed as before).

### Recommendation

- **KoikatsuSunshine users**: KKS installs usually lack this plugin, just use `KKS_ChaAlphaMask.dll` — none of the above applies
- **Koikatu users**: prefer the version bundled with your repack; only overwrite when you actually want the new features (the card-mask window), and add the overwrite step to your repack update routine

## Differences from the KoikatuSunshine build

- The plugin GUID stays `nakay.kk.ChaAlphaMask`, so mask settings already stored in KK cards are read directly in KKS
- KK deserializes `MaskProperty` with `XmlSerializer`, which requires public property setters; the old Mono tolerated private setters, Mono in Unity 2019 throws
- The KKS clothing UI has no `tglInnerShoes` sub-category (indoor shoes were merged into the shoes page), so the indoor shoes slot has no body mask checkbox; the other 8 slots behave normally
- APIs such as `CvsClothes.UpdateSelectClothes` and `ChaControl.SetClothesState` have extra parameters in KKS; the Harmony patches bind by parameter name, so the patch logic is unchanged

## Changelog

### 1.1.1

- Fixed mask thumbnails in the window rendering a fully opaque black background as white (visibility is now computed from RGB luminance only, so black stays transparent)
- Default export folder changed to `UserData\Overlays` under the game root; the old `UserData\AlphaMask` value in existing configs is migrated automatically

### 1.1

- Added the in-maker "Character Alpha Mask" window: import / export / delete PNG masks stored in the card, no manifest needed
- Added the right-hand Control Panel toggle, the `F5` hotkey and window position memory
- Mouse input is suppressed while the window is open, so dragging it neither moves the character / camera nor clicks through to the UI below
- Reads old Madevil `PlusPack - ChaAlphaMask` card data (`madevil.kk.PPCAM`)
- Force High Poly is now read through reflection, so the plugin builds and runs without this optional plugin

### 1.0

- First release: zipmod manifest masks, maker body mask checkbox and mask syncing

## Notes

The sources in this repository come from decompiling and re-adapting the original plugin, for maintenance and porting purposes only. Original copyright belongs to the original author.
