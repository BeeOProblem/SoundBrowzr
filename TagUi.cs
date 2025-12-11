using Godot;
using System;

public partial class TagUi : Control
{
    [Export]
    Label TagName;
    [Export]
    Control Background;
    [Export]
    AnimationPlayer AnimationPlayer;
    [Export]
    public bool Selectable
    {
        get; set; 
    }

    public TagDefinition Tag
    {
        get
        {
            return tagDefinition;
        }

        set
        {
            tagDefinition = value;
            TagName.Text = value.Name;
            Background.SelfModulate = value.Color;
        }
    }

    public bool IsSelected { get { return selected; } }

    public event EventHandler<EventArgs> OnRemoveClicked;
    public event EventHandler<EventArgs> OnSelected;
    public event EventHandler<EventArgs> OnDeselected;

    private TagDefinition tagDefinition;
    private bool pressed;
    private bool selected;

    internal void SetSelectedNoSignal(bool selected)
    {
        this.selected = selected;
        if (selected)
        {
            AnimationPlayer.Play("Selected", 0);
        }
        else
        {
            AnimationPlayer.Play("Default", 0);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (Selectable)
        {
            if(@event is InputEventMouseButton buttonEvent)
            {
                if (buttonEvent.IsPressed() && buttonEvent.ButtonIndex == MouseButton.Left)
                {
                    GD.Print("mouse pressed ", Tag.Name);
                    pressed = true;
                }
                else if (buttonEvent.IsReleased() && buttonEvent.ButtonIndex == MouseButton.Left)
                {
                    GD.Print("mouse released ", Tag.Name);
                    if (pressed && buttonEvent.Position.X > 0 && buttonEvent.Position.Y > 0 && buttonEvent.Position.X < Size.X && buttonEvent.Position.Y < Size.Y)
                    {
                        GD.Print("selected toggled ", Tag.Name);
                        selected = !selected;

                        if (selected)
                        {
                            OnSelected?.Invoke(this, EventArgs.Empty);
                            AnimationPlayer.Play("Selected", 0.25);
                        }
                        else
                        {
                            OnDeselected?.Invoke(this, EventArgs.Empty);
                            AnimationPlayer.Play("Default", 0.25);
                        }
                    }

                    pressed = false;
                }
            }
        }
    }
}
