using Godot;
using System;

public partial class SelectableLabel : Label
{
    public event EventHandler OnSelected;

    private bool pressed;

    private bool selected;
    public bool IsSelected
    {
        get
        {
            return selected;
        }
        
        set
        {
            selected = value;
            ((ColorRect)GetChild(0)).Visible = selected;
            OnSelected?.Invoke(this, EventArgs.Empty);
        }
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    pressed = true;
                }
                else if (!mouseButton.Pressed && pressed)
                {
                    IsSelected = !IsSelected;
                    pressed = false;
                }
            }
        }
    }
}
