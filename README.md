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

1. Copy `Directory.Build.props.sample` to `Directory.Build.props` in the repository root.
2. Edit `YMM4DirPath` in `Directory.Build.props` to the YMM4 installation directory. Keep the trailing backslash.
3. Run:

```powershell
dotnet build .\src\YMM4SelectiveAttributes\YMM4SelectiveAttributes.csproj -c Release
```

If the build succeeds and the path is correct, the post-build target copies the plugin DLL into:

```
<YMM4DirPath>\user\plugin\YMM4SelectiveAttributes\
```

Restart YMM4 and open the tool from the Tools menu.

## Current implementation scope

- Animation tracks: X, Y, Z, Opacity, Zoom, Rotation.
- Keyframe/midpoint frame positions are copied; optional fit-to-target-length mapping is included.
- Optional keep-target-start-position offset for X/Y/Z.
- Blend and hidden/visible state are copied through matching public properties when the target type exposes a writable property of the same type.
- Multiple selected target items are supported.
- Undo history is recorded through YMM4's UndoRedoManager.

## Known limitations

- The current code has not yet been compiled or run against YMM4 4.55.0.0. Treat this as an implementation-in-progress, not an installable release.
- YMM4's basic `IVideoItem` model exposes one `Zoom` animation and one `Rotation` animation in the motion-copy implementation used as a reference. Independent X/Y/Z rotation or independent X/Y scale are not implemented here.
- Video/audio effects and their parameters are not copied yet. Their cloning and undo semantics still need to be verified.
- Preset save/load is not implemented yet.
- The plugin currently copies from one selected source item at a time; paste can target multiple selected video items.

The copy routine is based on the public YMM4 ToolBoxPlugin motion-copy implementation, adapted to a standalone tool panel. See `docs/IMPLEMENTATION_NOTES.md` for the investigation log and compatibility caveats.
