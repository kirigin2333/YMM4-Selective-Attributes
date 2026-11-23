# Manual validation plan for YMM4 4.55.0.0

Do not test on the only copy of an important project. Make a disposable copy first.

## 1. Basic safety test
1. Create two different video/image/text items with visibly different media or text content.
2. Give the source different X/Y/Z, opacity, zoom, rotation, and keyframes from the target.
3. Copy the source attributes, select the target, and paste only position.
4. Confirm the target's media path/text/audio source, frame, layer, and length are unchanged.
5. Repeat for opacity, zoom, rotation, blend, visibility, and extra properties.

## 2. Batch target test
1. Copy one source item.
2. Select two or more targets of different item types.
3. Paste position and opacity.
4. Confirm compatible properties are copied and incompatible properties are skipped without changing target content.

## 3. Effects test
1. Add two different video effects with distinct parameter values to the source.
2. Copy video effects to a target that has a different media source.
3. Confirm the target keeps its media source and receives independent effect instances with the source parameters.
4. Repeat with audio effects on audio-capable items.
5. Edit one copied effect on the target and verify the source effect does not change.

## 4. Keyframe isolation test
1. Give the target an animated property that is not selected for copying.
2. Copy a different animated property from the source, including multiple midpoint/keyframe positions.
3. Verify the unselected animation is unchanged before and after paste.
4. Repeat with different source and target lengths and both FitLength settings.

This test is particularly important because YMM4 keyframe collections may be shared across multiple animation tracks.

## 5. Undo/redo test
1. Paste several selected property groups.
2. Press Undo once and confirm all pasted changes revert.
3. Press Redo once and confirm the pasted state returns.
4. Confirm the media/text/audio source remains unchanged through both operations.

## Expected current limits
- Basic animation groups are X/Y/Z, Opacity, Zoom, Rotation, Volume, and Pan when those Animation properties exist.
- Independent X/Y scale and independent XYZ rotation are not implemented as separate base tracks.
- Effect collections, blend, and extra scalar flags need runtime confirmation on the exact YMM4 build.
