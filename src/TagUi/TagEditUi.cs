using Godot;
using System;

public partial class TagEditUi : Window
{
    [Export]
    LineEdit TagName;
    [Export]
    ColorPickerButton TagColor;

    public event EventHandler OnCancel;
    public event EventHandler OnOk;

    public TagDefinition ModifiedTag
    {
        get;
        private set;
    } = new TagDefinition();

    public void AssignTag(TagDefinition tag)
    {
        ModifiedTag = tag;

        TagName.Text = tag.Name;
        TagColor.Color = tag.Color;
    }

    public override void _Ready()
    {
        TagName.Text = ModifiedTag.Name;
        TagColor.Color = ModifiedTag.Color;

        TagName.TextChanged += _TagNameChanged;
        TagColor.ColorChanged += _TagColorChanged;
    }

    private void _TagNameChanged(string newText)
    {
        ModifiedTag.Name = newText;
    }

    private void _TagColorChanged(Color color)
    {
        ModifiedTag.Color = color;
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
