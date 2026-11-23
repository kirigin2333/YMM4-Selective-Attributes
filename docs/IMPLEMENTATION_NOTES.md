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

## Critical implementation constraint
The public tool-plugin interface establishes how to add a tool window, but does not by itself establish a supported API for enumerating selected timeline items or mutating every item property. The implementation must not assume that names found in assembly strings are callable public APIs.

Before implementing copy/paste, inspect the exact v4.55.0.0 assemblies and sample code to identify supported selection access, property models, effect serialization/copy semantics, and undo/redo integration. If a property cannot be safely copied without changing the target media/content, leave it unsupported and document that limitation.

## Current status
- Repository access confirmed.
- Project implementation and CI build are not yet complete.
- No plugin binary has been built or tested in YMM4 yet.
