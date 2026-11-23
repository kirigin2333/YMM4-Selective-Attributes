# YMM4 Selective Attributes

A WPF tool plugin for Yukkuri Movie Maker 4. The intended workflow is:

1. Select exactly one source item on the YMM4 timeline.
2. Click **复制来源属性** in the tool panel.
3. Select one or more target items.
4. Choose the attribute groups and click **粘贴到选中物品**.

The implementation edits animation tracks and a small set of direct item properties. It does not replace target items or copy their media path, text, audio source, timeline position, layer, or duration.

## Build requirements

- YMM4 4.55.0.0 installed on Windows.
- .NET 10 SDK (Visual Studio is not required).
- The YMM4 installation directory containing `YukkuriMovieMaker.dll`, `YukkuriMovieMaker.Plugin.dll`, and `YukkuriMovieMaker.Controls.dll`.

## Build

Run this from PowerShell in the repository directory (replace the path with the folder containing `YukkuriMovieMaker.exe`):

```powershell
.\build.ps1 -YMM4DirPath "C:\YukkuriMovieMaker4"
```

The script checks the YMM4 DLLs and .NET 10 SDK, writes `Directory.Build.props`, builds the project, and installs the resulting plugin DLL. Visual Studio is not required. If the .NET 10 SDK is missing, install it before running the script.

If the build succeeds and the path is correct, the post-build target copies the plugin DLL into:

```
<YMM4DirPath>\user\plugin\YMM4SelectiveAttributes\
```

Restart YMM4 and open the tool from the Tools menu.

## Current implementation scope

- Animation tracks: X, Y, Z, Opacity, Zoom, Rotation, Volume, and Pan (when present on the selected item type).
- Keyframe/midpoint frame positions are copied; optional fit-to-target-length mapping is included.
- Optional keep-target-start-position offset for X/Y/Z.
- Blend and hidden/visible state are copied through matching public properties when the target type exposes a writable property of the same type.
- Optional common item properties include fade-in/out, playback rate, inversion, always-on-top, and Z-order enablement, where writable on the target.
- Video effect and audio effect collections are taken from a deep-cloned source item and assigned to matching target properties; the cloned item itself is never substituted for the target.
- Multiple selected target items are supported.
- Undo history is recorded through YMM4's UndoRedoManager.

## Known limitations

- The current code has not yet been compiled or run against YMM4 4.55.0.0. Treat this as an implementation-in-progress, not an installable release.
- YMM4's basic `IVideoItem` model exposes one `Zoom` animation and one `Rotation` animation in the motion-copy implementation used as a reference. Independent X/Y/Z rotation or independent X/Y scale are not implemented here.
- Effect collections are copied via YMM4's item-clone mechanism, but the effect-stack assignment and undo behavior still need runtime validation on v4.55.0.0.
- YMM4 keyframes may be shared by multiple animation tracks on an item. The current frame-sync routine is adapted from ToolBoxPlugin and must be tested to ensure changing selected tracks does not alter unrelated animated tracks.
- Preset save/load is not implemented yet.
- The basic blend property is applied only if YMM4 exposes a writable property with a matching type on the target item.
- The plugin copies from one selected source item at a time; paste can target multiple selected items, applying only properties available on each target type.

The copy routine is based on the public YMM4 ToolBoxPlugin motion-copy implementation, adapted to a standalone tool panel. See `docs/IMPLEMENTATION_NOTES.md` for the investigation log and compatibility caveats.
