using System.Reflection;
using System.Windows.Input;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace YMM4SelectiveAttributes;

public sealed class SelectiveAttributesViewModel : Bindable, IToolViewModel, ITimelineToolViewModel
{
    private sealed record Track(Animation Animation, double[] Values);
    private sealed record Snapshot(
        int Length,
        IReadOnlyList<int> Frames,
        IReadOnlyDictionary<string, Track> Tracks,
        object? Blend,
        bool HasBlend,
        bool? IsHidden,
        object? VideoEffects,
        bool HasVideoEffects);

    private static readonly string[] PositionNames =
    [
        nameof(IVideoItem.X),
        nameof(IVideoItem.Y),
        nameof(IVideoItem.Z)
    ];

    private static IEnumerable<(string Name, Animation Animation)> GetAnimations(IVideoItem item) =>
    [
        (nameof(IVideoItem.X), item.X),
        (nameof(IVideoItem.Y), item.Y),
        (nameof(IVideoItem.Z), item.Z),
        (nameof(IVideoItem.Opacity), item.Opacity),
        (nameof(IVideoItem.Zoom), item.Zoom),
        (nameof(IVideoItem.Rotation), item.Rotation)
    ];

    private Timeline? timeline;
    private UndoRedoManager? undoRedoManager;
    private IVideoItem? sourceItem;
    private Snapshot? snapshot;

    private bool copyPosition = true;
    public bool CopyPosition { get => copyPosition; set => Set(ref copyPosition, value); }

    private bool copyOpacity = true;
    public bool CopyOpacity { get => copyOpacity; set => Set(ref copyOpacity, value); }

    private bool copyZoom = true;
    public bool CopyZoom { get => copyZoom; set => Set(ref copyZoom, value); }

    private bool copyRotation = true;
    public bool CopyRotation { get => copyRotation; set => Set(ref copyRotation, value); }

    private bool copyBlend;
    public bool CopyBlend { get => copyBlend; set => Set(ref copyBlend, value); }

    private bool copyVisibility;
    public bool CopyVisibility { get => copyVisibility; set => Set(ref copyVisibility, value); }

    private bool copyEffects;
    public bool CopyEffects { get => copyEffects; set => Set(ref copyEffects, value); }

    private bool fitLength = true;
    public bool FitLength { get => fitLength; set => Set(ref fitLength, value); }

    private bool keepPosition;
    public bool KeepPosition { get => keepPosition; set => Set(ref keepPosition, value); }

    private string status = "先选中一个来源物品，点击“复制来源属性”。";
    public string Status { get => status; private set => Set(ref status, value); }

    public ICommand CopySourceCommand { get; }
    public ICommand PasteTargetsCommand { get; }

    public string Title => "Selective Attributes";
    public event EventHandler<CreateNewToolViewRequestedEventArgs>? CreateNewToolViewRequested;

    public SelectiveAttributesViewModel()
    {
        CopySourceCommand = new ActionCommand(_ => CanCopySource(), _ => CopySource());
        PasteTargetsCommand = new ActionCommand(_ => CanPasteTargets(), _ => PasteTargets());
    }

    public void SetTimelineToolInfo(TimelineToolInfo info)
    {
        timeline = info.Timeline;
        undoRedoManager = info.UndoRedoManager;
        Status = timeline is null
            ? "当前没有可用的时间轴。"
            : "已连接时间轴。先选中一个来源物品，再复制属性。";
    }

    private List<IVideoItem> GetSelectedVideoItems() =>
        timeline?.SelectedItems?.OfType<IVideoItem>().ToList() ?? [];

    private bool CanCopySource() => GetSelectedVideoItems().Count == 1;

    private bool CanPasteTargets() =>
        snapshot is not null &&
        undoRedoManager is not null &&
        GetSelectedVideoItems().Any(item => !ReferenceEquals(item, sourceItem));

    private void CopySource()
    {
        var selected = GetSelectedVideoItems();
        if (selected.Count != 1)
        {
            Status = "复制来源时请只选中一个视频类物品。";
            return;
        }

        var source = selected[0];
        var tracks = GetAnimations(source).ToDictionary(
            entry => entry.Name,
            entry =>
            {
                var clone = new Animation();
                clone.CopyFrom(entry.Animation);
                return new Track(clone, entry.Animation.ActiveValues.Select(value => value.Value).ToArray());
            });

        var blendProperty = FindPublicProperty(source.GetType(), "Blend");
        var blend = blendProperty?.CanRead == true ? blendProperty.GetValue(source) : null;
        var hasBlend = blendProperty?.CanRead == true;
        var hiddenProperty = FindPublicProperty(source.GetType(), "IsHidden");
        bool? isHidden = hiddenProperty?.PropertyType == typeof(bool) && hiddenProperty.CanRead
            ? (bool?)hiddenProperty.GetValue(source)
            : null;

        // GetClone() gives us an independent copy of the source item's effect collection.
        // Only the cloned VideoEffects property is retained; the target item itself is never replaced.
        var clonedSource = source.GetClone();
        var effectsProperty = FindPublicProperty(clonedSource.GetType(), "VideoEffects");
        var videoEffects = effectsProperty?.CanRead == true ? effectsProperty.GetValue(clonedSource) : null;
        var hasVideoEffects = effectsProperty?.CanRead == true;

        snapshot = new Snapshot(
            source.Length,
            source.KeyFrames.Frames.ToArray(),
            tracks,
            blend,
            hasBlend,
            isHidden,
            videoEffects,
            hasVideoEffects);
        sourceItem = source;
        Status = "来源属性已复制。现在选择一个或多个目标物品，再点击“粘贴到选中物品”。";
    }

    private void PasteTargets()
    {
        if (snapshot is not { } sourceSnapshot || undoRedoManager is null)
            return;

        var targets = GetSelectedVideoItems()
            .Where(item => !ReferenceEquals(item, sourceItem))
            .ToList();

        if (targets.Count == 0)
        {
            Status = "请先选择至少一个目标物品。";
            return;
        }

        var selectedNames = GetSelectedAnimationNames().ToHashSet();
        if (selectedNames.Count == 0 && !CopyBlend && !CopyVisibility && !CopyEffects)
        {
            Status = "请至少勾选一个要复制的属性。";
            return;
        }

        foreach (var target in targets)
        {
            PasteAnimations(target, sourceSnapshot, selectedNames);
            if (CopyBlend && sourceSnapshot.HasBlend)
                TryCopyProperty(sourceSnapshot.Blend, target, "Blend");
            if (CopyVisibility && sourceSnapshot.IsHidden is bool hidden)
                TryCopyProperty(hidden, target, "IsHidden");
            if (CopyEffects && sourceSnapshot.HasVideoEffects)
                TryCopyProperty(sourceSnapshot.VideoEffects, target, "VideoEffects");
        }

        undoRedoManager.Record();
        Status = $"已将选中的属性粘贴到 {targets.Count} 个目标物品。素材路径、文字内容和音频内容未被复制。";
    }

    private IEnumerable<string> GetSelectedAnimationNames()
    {
        if (CopyPosition)
        {
            foreach (var name in PositionNames)
                yield return name;
        }

        if (CopyOpacity)
            yield return nameof(IVideoItem.Opacity);
        if (CopyZoom)
            yield return nameof(IVideoItem.Zoom);
        if (CopyRotation)
            yield return nameof(IVideoItem.Rotation);
    }

    private void PasteAnimations(IVideoItem item, Snapshot sourceSnapshot, HashSet<string> names)
    {
        var targets = GetAnimations(item)
            .Where(entry => names.Contains(entry.Name) && sourceSnapshot.Tracks.ContainsKey(entry.Name))
            .ToList();

        if (targets.Count == 0)
            return;

        var usesKeyFrames = targets.Any(entry =>
            sourceSnapshot.Tracks[entry.Name].Animation.AnimationType.IsKeyFrameSupported());

        var mappedFrames = MapFrames(sourceSnapshot, item.Length, FitLength);
        if (usesKeyFrames)
            SyncKeyFrames(item.KeyFrames, mappedFrames.Select(entry => entry.Frame).ToArray());

        foreach (var (name, animation) in targets)
        {
            var track = sourceSnapshot.Tracks[name];
            var originalFirstValue = animation.GetFirstValue();

            animation.CopyFrom(track.Animation);

            if (track.Animation.AnimationType.IsKeyFrameSupported() && track.Values.Length >= 2)
            {
                var values = new List<double> { track.Values[0] };
                values.AddRange(mappedFrames.Select(entry => track.Values[entry.Index + 1]));
                values.Add(track.Values[^1]);

                for (var i = 0; i < values.Count && i < animation.Values.Count; i++)
                    animation.Values[i].Value = values[i];
            }

            if (KeepPosition && PositionNames.Contains(name) && track.Values.Length > 0)
                animation.AddToEachValues(originalFirstValue - track.Values[0]);
        }
    }

    private static List<(int Index, int Frame)> MapFrames(Snapshot sourceSnapshot, int targetLength, bool fitLength)
    {
        var scale = fitLength && sourceSnapshot.Length > 1
            ? (targetLength - 1.0) / (sourceSnapshot.Length - 1)
            : 1.0;

        var result = new List<(int Index, int Frame)>();
        for (var i = 0; i < sourceSnapshot.Frames.Count; i++)
        {
            var frame = (int)Math.Round(sourceSnapshot.Frames[i] * scale);
            if (frame <= 0 || frame >= targetLength - 1 || result.Any(entry => entry.Frame == frame))
                continue;

            result.Add((i, frame));
        }

        return result;
    }

    private static void SyncKeyFrames(KeyFrames keyFrames, IReadOnlyCollection<int> frames)
    {
        foreach (var frame in frames.Where(frame => !keyFrames.Frames.Contains(frame)))
            keyFrames.Insert(frame);

        for (var i = keyFrames.Frames.Count - 1; i >= 0; i--)
        {
            if (!frames.Contains(keyFrames.Frames[i]))
                keyFrames.RemoveAt(i);
        }
    }

    private static PropertyInfo? FindPublicProperty(Type type, string name) =>
        type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

    private static void TryCopyProperty(object? value, object target, string propertyName)
    {
        if (value is null)
            return;

        var property = FindPublicProperty(target.GetType(), propertyName);
        if (property is null || !property.CanWrite || property.PropertyType != value.GetType())
            return;

        property.SetValue(target, value);
    }

    public ToolState SaveState() => new()
    {
        Title = Title,
        SavedState = string.Empty
    };

    public void LoadState(ToolState stateData)
    {
        // This tool has no persistent per-instance state yet.
    }
}
