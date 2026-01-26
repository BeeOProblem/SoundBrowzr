using Godot;
using Microsoft.VisualBasic;
using System;

public partial class TagEditUi : Window
{
    [Export]
    LineEdit TagName;
    [Export]
    ColorRect CurrentColor;
    [Export]
    Button ColorPickerButton;
    [Export]
    PopupPanel ColorPickerPopup;
    [Export]
    ColorPicker ColorPicker;

    public event EventHandler OnCancel;
    public event EventHandler OnOk;

    public TagDefinition ModifiedTag
    {
        get;
        private set;
    } = new TagDefinition();

    public void AssignTag(TagDefinition tag)
    {
        ModifiedTag = new TagDefinition(tag);

        TagName.Text = tag.Name;
        CurrentColor.Color = tag.Color;
        ColorPicker.Color = tag.Color;
    }

    public override void _Ready()
    {
        TagName.Text = ModifiedTag.Name;
        CurrentColor.Color = ModifiedTag.Color;

        TagName.TextChanged += _TagNameChanged;
        ColorPicker.ColorChanged += _TagColorChanged;
        ColorPickerPopup.VisibilityChanged += _ColorPickerVisibilityChanged;
    }

    private void _TagNameChanged(string newText)
    {
        ModifiedTag.Name = newText;
    }

    private void _TagColorChanged(Color color)
    {
        ModifiedTag.Color = color;
        CurrentColor.Color = ModifiedTag.Color;
    }

    private void _ColorButtonClicked()
    {
        ColorPickerPopup.Show();
    }

    private void _ColorPickerVisibilityChanged()
    {
        ColorPickerButton.Disabled = ColorPickerPopup.Visible;
    }

    private void _OkClicked()
    {
        OnOk?.Invoke(this, EventArgs.Empty);
        Hide();
    }

    private void _CancelClicked()
    {
        OnCancel?.Invoke(this, EventArgs.Empty);
        Hide();
    }
}
