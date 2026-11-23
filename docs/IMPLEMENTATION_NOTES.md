# Implementation notes

## Target
- Yukkuri Movie Maker 4: 4.55.0.0
- Windows / WPF plugin
- .NET target: `net10.0-windows10.0.19041.0` (YMM4 4.47.0.0+)

## Product behavior
Copy selected attributes from one source timeline item to one or more selected target timeline items. Target media/text/audio content must remain unchanged.

Planned selectable groups:
- Position X/Y/Z
- Scale
- Rotation
- Opacity
- Blend mode
- Visibility
- Effects/filters and their parameters
- Midpoint/keyframe positions
- Other properties only where safe copying can be established

## Verified integration facts
- YMM4 tool-window plugins implement `YukkuriMovieMaker.Plugin.IToolPlugin`.
- A tool plugin supplies a ViewModel type implementing `IToolViewModel` and a WPF UserControl view type.
- YMM4 4.47.0.0+ plugin projects target `net10.0-windows10.0.19041.0`.
- A timeline tool ViewModel can implement `ITimelineToolViewModel`; YMM4 calls `SetTimelineToolInfo(TimelineToolInfo info)`.
- The injected `TimelineToolInfo` exposes `Timeline` and `UndoRedoManager`. A recent public plugin uses `info.Timeline.SelectedItems` to access the current selection, and observes `Timeline.PropertyChanged` for `SelectedItems`.
- The YMM4 project model exposes `IVideoItem` animation tracks X, Y, Z, Opacity, Zoom, Rotation, Blend, and VideoEffects. The public ToolBoxPlugin motion-copy implementation directly uses X/Y/Z/Opacity/Zoom/Rotation and `Animation.CopyFrom`.
- `UndoRedoManager.Record()` is used by that implementation after mutating selected items.

## v4.55 assembly inspection
The uploaded `YukkuriMovieMaker.Controls.dll` is a valid PE/.NET assembly. Its embedded metadata reports:
- Assembly name: `YukkuriMovieMaker.Controls`
- Assembly version: `1.0.0.0`
- Informational version: `1.0.0+200602468f2582254009e11d72c45874f069c32e`
- Target framework: `.NETCoreApp,Version=v10.0`
- Target platform: `Windows10.0.19041.0`
- SHA-256: `3042951871e6a7a2e7199ce666f1170282d2b58b7cb559d779a10ee3a49fb9f3`

The assembly is only 9,216 bytes. Its metadata/string table includes names such as `FastCanvasItemsControl`, `IFastCanvasItemsControlViewModel`, `EffectSelectorItemBase<T>`, and `PropertyEditorAttributeCreatedArgs`. These names are useful clues but do not establish method signatures or public accessibility.

The supplied `YukkuriMovieMaker.Plugin.dll` (8,137,216 bytes; SHA-256 `0efbd5c7932bfe4e90e449d65cbc40ea64dccaaa3a4c5416e91d95fcafa4cc15`) and `YukkuriMovieMaker.dll` (5,641,728 bytes; SHA-256 `b91f3f8b968eab4f497a97dd77893099faed84e5607ca4cf4ee539d211b75945`) are also valid .NET assemblies. Strings in the main assembly confirm names including `TimelineToolInfo`, `SelectedItems`, `KeyFrames`, `Opacity`, `Visibility`, and `Blend`. Full IL/API inspection is still pending because no .NET/IL disassembler is available in the current environment.

## Key implementation reference found
The public `Dolphin-kun/ToolBoxPlugin` repository contains a feature named Motion Copy. Its source:
- obtains selected objects from `IToolBoxContext.SelectedItems` and filters to `IVideoItem`;
- clones the source X/Y/Z/Opacity/Zoom/Rotation `Animation` objects using `Animation.CopyFrom`;
- snapshots source keyframe frames and active values;
- maps keyframe frames to the target length when requested;
- updates target `KeyFrames` and animation values;
- optionally preserves the target's original X/Y/Z start value;
- calls `UndoRedoManager.Record()` after paste.

The companion ViewModel in that repository receives `TimelineToolInfo`, stores `info.Timeline` and `info.UndoRedoManager`, and reads `timeline.SelectedItems`. This establishes a concrete implementation pattern for the core motion-copy feature.

Reference source:
- `https://github.com/Dolphin-kun/ToolBoxPlugin/blob/2642ba1e47e51fa10a355668fd3308e98c7bef65/ToolBoxPlugin/Items/MotionCopy/MotionCopyCommand.cs`
- `https://github.com/Dolphin-kun/ToolBoxPlugin/blob/2642ba1e47e51fa10a355668fd3308e98c7bef65/ToolBoxPlugin/ViewModel/ToolBoxViewModel.cs`

A separate public Object List plugin explicitly warns that it uses non-public YMM4 APIs. That warning is relevant to more advanced timeline UI manipulation, but the current attribute-copy implementation is now based on the concrete public ToolBoxPlugin pattern rather than that plugin's internal API approach.

## Current source implementation in this repository
A first standalone WPF tool-plugin scaffold has been committed:
- `src/YMM4SelectiveAttributes/YMM4SelectiveAttributes.csproj`
- `src/YMM4SelectiveAttributes/SelectiveAttributesPlugin.cs`
- `src/YMM4SelectiveAttributes/SelectiveAttributesViewModel.cs`
- `src/YMM4SelectiveAttributes/SelectiveAttributesView.cs`
- `Directory.Build.props.sample`
- `README.md`

The current ViewModel implements this workflow:
1. Exactly one selected `IItem` is captured as the source.
2. The user selects one or more target `IItem` objects.
3. Selected animation groups are copied when matching Animation properties exist: X/Y/Z, Opacity, Zoom, Rotation, Volume, and Pan.
4. Blend and IsHidden are copied only through matching writable public properties of the same type.
5. VideoEffects and AudioEffects are taken from `source.GetClone()` and assigned to matching target properties. The cloned item itself is never substituted for a target.
6. Optional common direct properties include FadeIn, FadeOut, PlaybackRate, IsInverted, IsAlwaysOnTop, and IsZOrderEnabled, where source and target expose matching writable properties.
7. The operation records one undo step when at least one property was applied.

The code does not replace target items or intentionally write media paths, text content, audio source, frame, layer, or length. Effect-list assignment still needs runtime verification.

## Known limitations / risks
- This source has not yet been compiled or run against YMM4 4.55.0.0. It is not an installable release yet.
- The currently implemented animation groups include one `Zoom` track and one `Rotation` track; independent X/Y scale and XYZ rotation are not implemented.
- Volume and Pan are discovered by public property reflection and copied only when the source and target expose Animation-valued properties with those names.
- VideoEffects and AudioEffects are copied from the source item's `GetClone()` result by assigning only the effect collection. This is based on public plugin code that uses `GetClone()` for item snapshots and on YMM4 interfaces exposing writable effect collections; the actual deep-clone and undo behavior still needs runtime validation.
- Preset save/load is not implemented yet.
- The keyframe synchronization algorithm is adapted from ToolBoxPlugin. It still needs runtime validation against target items that have other animated properties, because keyframe collections may be shared across tracks.
- No CI build or YMM4 runtime test has been completed.

## Next validation steps
1. Compile the project against the exact local v4.55.0.0 installation.
2. Test with two disposable items using distinct media/text/audio contents; verify only selected attributes change.
3. Test Undo/Redo and mixed target types.
4. Inspect effect-copy semantics and the shared-keyframe behavior before treating effects/filters as production-safe.
