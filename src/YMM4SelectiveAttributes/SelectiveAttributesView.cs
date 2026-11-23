using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Controls.Primitives;

namespace YMM4SelectiveAttributes;

public sealed class SelectiveAttributesView : UserControl
{
    public SelectiveAttributesView()
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(12)
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Selective Attributes",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(new TextBlock
        {
            Text = "1. 只选中一个来源物品并复制。 2. 选择一个或多个目标物品并粘贴。",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var copyButton = new Button
        {
            Content = "复制来源属性",
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 0, 6)
        };
        copyButton.SetBinding(Button.CommandProperty, new Binding(nameof(SelectiveAttributesViewModel.CopySourceCommand)));
        panel.Children.Add(copyButton);

        var pasteButton = new Button
        {
            Content = "粘贴到选中物品",
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 0, 10)
        };
        pasteButton.SetBinding(Button.CommandProperty, new Binding(nameof(SelectiveAttributesViewModel.PasteTargetsCommand)));
        panel.Children.Add(pasteButton);

        panel.Children.Add(new TextBlock
        {
            Text = "属性预设",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 2, 0, 4)
        });

        var presetNameBox = new TextBox
        {
            MinWidth = 140,
            Margin = new Thickness(0, 0, 0, 6),
            ToolTip = "输入预设名称，例如：人物位置"
        };
        presetNameBox.SetBinding(TextBox.TextProperty, new Binding(nameof(SelectiveAttributesViewModel.PresetName))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        panel.Children.Add(presetNameBox);

        var presetButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 6)
        };

        var savePresetButton = new Button { Content = "保存当前选项", Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 5, 0) };
        savePresetButton.SetBinding(Button.CommandProperty, new Binding(nameof(SelectiveAttributesViewModel.SavePresetCommand)));
        presetButtons.Children.Add(savePresetButton);

        var presetSelector = new ComboBox
        {
            MinWidth = 110,
            Margin = new Thickness(0, 0, 5, 0),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        presetSelector.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(SelectiveAttributesViewModel.PresetNames)));
        presetSelector.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(SelectiveAttributesViewModel.SelectedPreset))
        {
            Mode = BindingMode.TwoWay
        });
        presetButtons.Children.Add(presetSelector);

        var loadPresetButton = new Button { Content = "载入", Padding = new Thickness(7, 4, 7, 4), Margin = new Thickness(0, 0, 5, 0) };
        loadPresetButton.SetBinding(Button.CommandProperty, new Binding(nameof(SelectiveAttributesViewModel.LoadPresetCommand)));
        presetButtons.Children.Add(loadPresetButton);

        var deletePresetButton = new Button { Content = "删除", Padding = new Thickness(7, 4, 7, 4) };
        deletePresetButton.SetBinding(Button.CommandProperty, new Binding(nameof(SelectiveAttributesViewModel.DeletePresetCommand)));
        presetButtons.Children.Add(deletePresetButton);

        panel.Children.Add(presetButtons);
        panel.Children.Add(new TextBlock
        {
            Text = "预设只保存下方勾选项，不保存来源物品或素材。",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(new TextBlock
        {
            Text = "要复制的属性",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 2, 0, 4)
        });

        panel.Children.Add(CreateCheckBox("位置 X / Y / Z", nameof(SelectiveAttributesViewModel.CopyPosition)));
        panel.Children.Add(CreateCheckBox("不透明度", nameof(SelectiveAttributesViewModel.CopyOpacity)));
        panel.Children.Add(CreateCheckBox("缩放率", nameof(SelectiveAttributesViewModel.CopyZoom)));
        panel.Children.Add(CreateCheckBox("旋转", nameof(SelectiveAttributesViewModel.CopyRotation)));
        panel.Children.Add(CreateCheckBox("混合模式", nameof(SelectiveAttributesViewModel.CopyBlend)));
        panel.Children.Add(CreateCheckBox("可见性（隐藏状态）", nameof(SelectiveAttributesViewModel.CopyVisibility)));
        panel.Children.Add(CreateCheckBox("视频效果 / 滤镜及参数", nameof(SelectiveAttributesViewModel.CopyVideoEffects)));
        panel.Children.Add(CreateCheckBox("音频效果及参数", nameof(SelectiveAttributesViewModel.CopyAudioEffects)));
        panel.Children.Add(CreateCheckBox("音量动画", nameof(SelectiveAttributesViewModel.CopyVolume)));
        panel.Children.Add(CreateCheckBox("声像 / Pan 动画", nameof(SelectiveAttributesViewModel.CopyPan)));
        panel.Children.Add(CreateCheckBox("其他基础属性（淡入淡出 / 播放速度 / 反转 / 置顶）", nameof(SelectiveAttributesViewModel.CopyExtraProperties)));

        panel.Children.Add(new Separator { Margin = new Thickness(0, 8, 0, 8) });
        panel.Children.Add(CreateCheckBox("按目标物品长度缩放关键帧位置", nameof(SelectiveAttributesViewModel.FitLength)));
        panel.Children.Add(CreateCheckBox("保持目标物品原本的起始位置", nameof(SelectiveAttributesViewModel.KeepPosition)));

        var status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0)
        };
        status.SetBinding(TextBlock.TextProperty, new Binding(nameof(SelectiveAttributesViewModel.Status)));
        panel.Children.Add(status);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = panel
        };
    }

    private static CheckBox CreateCheckBox(string label, string propertyName)
    {
        var checkBox = new CheckBox
        {
            Content = label,
            Margin = new Thickness(0, 3, 0, 3),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        checkBox.SetBinding(ToggleButton.IsCheckedProperty, new Binding(propertyName)
        {
            Mode = BindingMode.TwoWay
        });
        return checkBox;
    }
}
