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

## Critical implementation constraint
The public tool-plugin interface establishes how to add a tool window, but does not by itself establish a supported API for enumerating selected timeline items or mutating every item property. The implementation must not assume that names found in assembly strings are callable public APIs.

Before implementing copy/paste, inspect the exact v4.55.0.0 assemblies and sample code to identify supported selection access, property models, effect serialization/copy semantics, and undo/redo integration. If a property cannot be safely copied without changing the target media/content, leave it unsupported and document that limitation.

## Current status
- Repository access confirmed.
- Three YMM4 assemblies have been supplied for inspection: `YukkuriMovieMaker.Plugin.dll`, `YukkuriMovieMaker.dll`, and `YukkuriMovieMaker.Controls.dll`.
- Basic PE/.NET metadata and strings have been inspected; full IL/API inspection is still pending because no .NET/IL disassembler is available in the current environment.
- Project implementation and CI build are not yet complete.
- No plugin binary has been built or tested in YMM4 yet.
