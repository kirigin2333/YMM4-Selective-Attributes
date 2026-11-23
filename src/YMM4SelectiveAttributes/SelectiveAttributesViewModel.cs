using System.Reflection;
using System.Windows.Input;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.Project.Items;
using YukkuriMovieMaker.UndoRedo;

namespace YMM4SelectiveAttributes;

public sealed class SelectiveAttributesViewModel : Bindable, IToolViewModel, ITimelineToolViewModel, IDisposable
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
        bool HasVideoEffects,
        object? AudioEffects,
        bool HasAudioEffects);

    private static readonly string[] AnimationPropertyNames =
    [
        "X", "Y", "Z", "Opacity", "Zoom", "Rotation", "Volume", "Pan"
    ];

    private static readonly string[] PositionNames = ["X", "Y", "Z"];

    private Timeline? timeline;
    private UndoRedoManager? undoRedoManager;
    private IItem? sourceItem;
    private Snapshot? snapshot;

    private bool copyPosition = true;
    public bool CopyPosition { get => copyPosition; set => Set(ref copyPosition, value); }

    private bool copyOpacity = true;
    public bool CopyOpacity { get => copyOpacity; set => Set(ref copyOpacity, value); }

    private bool copyZoom = true;
    public bool CopyZoom { get => copyZoom; set => Set(ref copyZoom, value); }

    private bool copyRotation = true;
    public bool CopyRotation { get => copyRotation; set => Set(ref copyRotation, value); }

    private bool copyVolume;
    public bool CopyVolume { get => copyVolume; set => Set(ref copyVolume, value); }

    private bool copyPan;
    public bool CopyPan { get => copyPan; set => Set(ref copyPan, value); }

    private bool copyBlend;
    public bool CopyBlend { get => copyBlend; set => Set(ref copyBlend, value); }

    private bool copyVisibility;
    public bool CopyVisibility { get => copyVisibility; set => Set(ref copyVisibility, value); }

    private bool copyVideoEffects;
    public bool CopyVideoEffects { get => copyVideoEffects; set => Set(ref copyVideoEffects, value); }

    private bool copyAudioEffects;
    public bool CopyAudioEffects { get => copyAudioEffects; set => Set(ref copyAudioEffects, value); }

    private bool fitLength = true;
    public bool FitLength { get => fitLength; set => Set(ref fitLength, value); }

    private bool keepPosition;
    public bool KeepPosition { get => keepPosition; set => Set(ref keepPosition, value); }

    private string status = "先选中一个来源物品，点击“复制来源属性”。";
    public string Status { get => status; private set => Set(ref status, value); }

    private readonly ActionCommand copySourceCommand;
    private readonly ActionCommand pasteTargetsCommand;
    public ICommand CopySourceCommand => copySourceCommand;
    public ICommand PasteTargetsCommand => pasteTargetsCommand;

    public string Title => "Selective Attributes";
    public event EventHandler<CreateNewToolViewRequestedEventArgs>? CreateNewToolViewRequested;

    public SelectiveAttributesViewModel()
    {
        copySourceCommand = new ActionCommand(_ => CanCopySource(), _ => CopySource());
        pasteTargetsCommand = new ActionCommand(_ => CanPasteTargets(), _ => PasteTargets());
    }

    public void SetTimelineToolInfo(TimelineToolInfo info)
    {
        if (timeline is not null)
            timeline.PropertyChanged -= Timeline_PropertyChanged;

        timeline = info.Timeline;
        undoRedoManager = info.UndoRedoManager;

        if (timeline is not null)
            timeline.PropertyChanged += Timeline_PropertyChanged;

        Status = timeline is null
            ? "当前没有可用的时间轴。"
            : "已连接时间轴。先选中一个来源物品，再复制属性。";
        RaiseCommandCanExecuteChanged();
    }

    private void Timeline_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Timeline.SelectedItems))
            RaiseCommandCanExecuteChanged();
    }

    private void RaiseCommandCanExecuteChanged()
    {
        copySourceCommand.RaiseCanExecuteChanged();
        pasteTargetsCommand.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        if (timeline is not null)
            timeline.PropertyChanged -= Timeline_PropertyChanged;
    }

    private List<IItem> GetSelectedItems() =>
        timeline?.SelectedItems?.ToList() ?? [];

    private bool CanCopySource() => GetSelectedItems().Count == 1;

    private bool CanPasteTargets() =>
        snapshot is not null &&
        undoRedoManager is not null &&
        GetSelectedItems().Any(item => !ReferenceEquals(item, sourceItem));

    private void CopySource()
    {
        var selected = GetSelectedItems();
        if (selected.Count != 1)
        {
            Status = "复制来源时请只选中一个物品。";
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

        // GetClone() deep-clones the item model. We retain only the effect collections
        // from that clone, never the cloned item itself or its media/text/audio content.
        var clonedSource = source.GetClone();
        var videoEffectsProperty = FindPublicProperty(clonedSource.GetType(), "VideoEffects");
        var audioEffectsProperty = FindPublicProperty(clonedSource.GetType(), "AudioEffects");
        var videoEffects = videoEffectsProperty?.CanRead == true ? videoEffectsProperty.GetValue(clonedSource) : null;
        var audioEffects = audioEffectsProperty?.CanRead == true ? audioEffectsProperty.GetValue(clonedSource) : null;

        snapshot = new Snapshot(
            GetIntProperty(source, "Length") ?? 1,
            GetKeyFrames(source)?.Frames.ToArray() ?? [],
            tracks,
            blend,
            hasBlend,
            isHidden,
            videoEffects,
            videoEffectsProperty?.CanRead == true,
            audioEffects,
            audioEffectsProperty?.CanRead == true);

        sourceItem = source;
        Status = "来源属性已复制。现在选择一个或多个目标物品，再点击“粘贴到选中物品”。";
        RaiseCommandCanExecuteChanged();
    }

    private void PasteTargets()
    {
        if (snapshot is not { } sourceSnapshot || undoRedoManager is null)
            return;

        var targets = GetSelectedItems()
            .Where(item => !ReferenceEquals(item, sourceItem))
            .ToList();

        if (targets.Count == 0)
        {
            Status = "请先选择至少一个目标物品。";
            return;
        }

        var selectedNames = GetSelectedAnimationNames().ToHashSet();
        if (selectedNames.Count == 0 &&
            !CopyBlend && !CopyVisibility && !CopyVideoEffects && !CopyAudioEffects)
        {
            Status = "请至少勾选一个要复制的属性。";
            return;
        }

        var changed = false;
        var skippedSomewhere = false;

        foreach (var target in targets)
        {
            changed |= PasteAnimations(target, sourceSnapshot, selectedNames);

            if (CopyBlend)
            {
                var copied = sourceSnapshot.HasBlend && TryCopyProperty(sourceSnapshot.Blend, target, "Blend");
                changed |= copied;
                skippedSomewhere |= !copied;
            }

            if (CopyVisibility)
            {
                var copied = sourceSnapshot.IsHidden is bool hidden && TryCopyProperty(hidden, target, "IsHidden");
                changed |= copied;
                skippedSomewhere |= !copied;
            }

            if (CopyVideoEffects)
            {
                var copied = sourceSnapshot.HasVideoEffects && TryCopyProperty(sourceSnapshot.VideoEffects, target, "VideoEffects");
                changed |= copied;
                skippedSomewhere |= !copied;
            }

            if (CopyAudioEffects)
            {
                var copied = sourceSnapshot.HasAudioEffects && TryCopyProperty(sourceSnapshot.AudioEffects, target, "AudioEffects");
                changed |= copied;
                skippedSomewhere |= !copied;
            }
        }

        if (!changed)
        {
            Status = "所选属性没有可应用到这些目标物品的匹配属性。";
            return;
        }

        undoRedoManager.Record();
        Status = $"已将选中的属性粘贴到 {targets.Count} 个目标物品。素材路径、文字内容和音频源内容不会被替换。"
            + (skippedSomewhere ? " 部分属性因目标物品类型不支持而跳过。" : "");
        RaiseCommandCanExecuteChanged();
    }

    private IEnumerable<string> GetSelectedAnimationNames()
    {
        if (CopyPosition)
        {
            foreach (var name in PositionNames)
                yield return name;
        }

        if (CopyOpacity)
            yield return "Opacity";
        if (CopyZoom)
            yield return "Zoom";
        if (CopyRotation)
            yield return "Rotation";
        if (CopyVolume)
            yield return "Volume";
        if (CopyPan)
            yield return "Pan";
    }

    private bool PasteAnimations(IItem item, Snapshot sourceSnapshot, HashSet<string> names)
    {
        var targets = GetAnimations(item)
            .Where(entry => names.Contains(entry.Name) && sourceSnapshot.Tracks.ContainsKey(entry.Name))
            .ToList();

        if (targets.Count == 0)
            return false;

        var usesKeyFrames = targets.Any(entry =>
            sourceSnapshot.Tracks[entry.Name].Animation.AnimationType.IsKeyFrameSupported());

        var targetLength = GetIntProperty(item, "Length") ?? 1;
        var mappedFrames = MapFrames(sourceSnapshot, targetLength, FitLength);
        var targetKeyFrames = GetKeyFrames(item);

        if (usesKeyFrames && targetKeyFrames is not null)
            SyncKeyFrames(targetKeyFrames, mappedFrames.Select(entry => entry.Frame).ToArray());

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

        return true;
    }

    private static IEnumerable<(string Name, Animation Animation)> GetAnimations(IItem item)
    {
        foreach (var name in AnimationPropertyNames)
        {
            var property = FindPublicProperty(item.GetType(), name);
            if (property?.CanRead == true && property.GetValue(item) is Animation animation)
                yield return (name, animation);
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

    private static KeyFrames? GetKeyFrames(IItem item)
    {
        var property = FindPublicProperty(item.GetType(), "KeyFrames");
        return property?.CanRead == true ? property.GetValue(item) as KeyFrames : null;
    }

    private static int? GetIntProperty(object item, string name)
    {
        var property = FindPublicProperty(item.GetType(), name);
        return property?.PropertyType == typeof(int) && property.CanRead
            ? (int?)property.GetValue(item)
            : null;
    }

    private static PropertyInfo? FindPublicProperty(Type type, string name) =>
        type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

    private static bool TryCopyProperty(object? value, object target, string propertyName)
    {
        if (value is null)
            return false;

        var property = FindPublicProperty(target.GetType(), propertyName);
        if (property is null || !property.CanWrite || property.PropertyType != value.GetType())
            return false;

        try
        {
            property.SetValue(target, value);
            return true;
        }
        catch
        {
            return false;
        }
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
