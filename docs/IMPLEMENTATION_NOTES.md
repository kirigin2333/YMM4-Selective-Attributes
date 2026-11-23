# Implementation notes

## Target
- Yukkuri Movie Maker 4: 4.55.0.0
- Windows / WPF plugin
- .NET target: `net10.0-windows10.0.19041.0` (YMM4 4.47.0.0+)

## Product behavior
The tool should copy only selected attributes from a source timeline item to one or more target timeline items. Target media/text/audio content must remain unchanged.

Planned selectable groups:
- Position X/Y/Z
- Scale X/Y/Z
- Rotation X/Y/Z
- Opacity
- Blend mode
- Visibility
- Effects/filters and their parameters
- Midpoint
- Other properties only where safe copying can be established

## Verified integration facts
- YMM4 tool-window plugins implement `YukkuriMovieMaker.Plugin.IToolPlugin`.
- A tool plugin supplies a ViewModel type implementing `IToolViewModel` and a WPF UserControl view type.
- YMM4 4.47.0.0+ plugin projects target `net10.0-windows10.0.19041.0`.
- The plugin API assembly is supplied by the YMM4 installation; build references should match the target YMM4 installation.

## v4.55 assembly inspection so far
The uploaded `YukkuriMovieMaker.Controls.dll` is a valid PE/.NET assembly. Its embedded metadata reports:
- Assembly name: `YukkuriMovieMaker.Controls`
- Assembly version: `1.0.0.0`
- Informational version: `1.0.0+200602468f2582254009e11d72c45874f069c32e`
- Target framework: `.NETCoreApp,Version=v10.0`
- Target platform: `Windows10.0.19041.0`
- SHA-256: `3042951871e6a7a2e7199ce666f1170282d2b58b7cb559d779a10ee3a49fb9f3`

Names visible in the assembly metadata/string table include `FastCanvasItemsControl`, `IFastCanvasItemsControlViewModel`, `EffectSelectorItemBase<T>`, `PropertyEditorAttributeCreatedArgs`, and several effect-selector/editor controls. These names suggest relevant UI/control infrastructure exists in this assembly, but strings alone do not establish public accessibility, method signatures, or a supported route to selected timeline items. No API-level conclusion is drawn from these names yet.

## Timeline integration discovery (public sample code)
A public YMM4 timeline-tool sample shows a tool ViewModel implementing both `IToolViewModel` and `ITimelineToolViewModel`. The host calls `SetTimelineToolInfo(TimelineToolInfo info)`; the sample then uses `info.Timeline`, `info.Scenes`, `info.UndoRedoManager`, and `info.AsyncAwaitStatus` to create a `TimelineViewModel` for the active scene. This is useful evidence that YMM4 can inject the active timeline and undo/redo manager into a tool ViewModel through this extension interface.

Important limitation: the sample uses the injected timeline to display another timeline view and observe the current frame. It does not demonstrate how to obtain the current selection, how to change selected items, or how to copy individual properties. Therefore it advances the timeline/undo integration question but does not yet solve safe attribute transfer.

A separate public Object List plugin advertises item enumeration and multi-selection support, but its README explicitly warns that it uses YMM4 non-public APIs and may break after YMM4 updates. That is evidence a non-public route exists in the ecosystem, not evidence that it is safe or compatible with v4.55.0.0. If the required selection API is internal, the implementation may need version-gated reflection and should be treated as compatibility-sensitive.

## Critical implementation constraint
The public tool-plugin interface establishes how to add a tool window. The timeline-tool sample establishes a route to the active timeline and undo/redo manager. Neither source establishes a supported API for enumerating selected timeline items or mutating every item property. The implementation must not assume that names found in assembly strings are callable public APIs.

Before implementing copy/paste, inspect the exact v4.55.0.0 assemblies and sample code to identify selection access, property models, effect serialization/copy semantics, and undo/redo integration. If a property cannot be safely copied without changing target media/content, leave it unsupported and document that limitation.

## Current status
- Repository access confirmed.
- Three YMM4 assemblies have been supplied across the conversation: `YukkuriMovieMaker.Plugin.dll`, `YukkuriMovieMaker.dll`, and `YukkuriMovieMaker.Controls.dll`. The latest uploaded Controls DLL is available for direct inspection in this turn.
- Basic PE/.NET metadata and strings have been inspected; full IL/API inspection is still pending because no .NET/IL disassembler is available in the current environment.
- A public timeline-tool sample has been inspected and confirms active timeline / undo-redo injection into an `ITimelineToolViewModel`.
- Current selection access and safe per-property mutation remain unverified.
- Project implementation and CI build are not yet complete.
- No plugin binary has been built or tested in YMM4 yet.
