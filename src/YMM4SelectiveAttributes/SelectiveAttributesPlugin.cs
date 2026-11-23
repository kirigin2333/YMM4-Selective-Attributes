using YukkuriMovieMaker.Plugin;

namespace YMM4SelectiveAttributes;

public sealed class SelectiveAttributesPlugin : IToolPlugin
{
    public string Name => "Selective Attributes";
    public Type ViewModelType => typeof(SelectiveAttributesViewModel);
    public Type ViewType => typeof(SelectiveAttributesView);
    public bool AllowMultipleInstances => false;
}