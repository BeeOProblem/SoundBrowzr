using Godot;
using System;

public class TagDefinition
{
    private string name;
    public string Name
    {
        get
        {
            return name;
        }

        set
        {
            if (value != name)
            {
                name = value;
                NameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private Color color;
    public Color Color
    {
        get
        {
            return color;
        }

        set
        {
            if (value != color)
            {
                color = value;
                ColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler ColorChanged;
    public event EventHandler NameChanged;

    public TagDefinition()
    {
        Name = "Unnamed";
        Color = new Color(0xFFFFFFFF);
    }

    public TagDefinition(TagDefinition cloneFrom)
    {
        Name = cloneFrom.Name;
        Color = cloneFrom.Color;
    }

    public override bool Equals(object obj)
    {
        if (obj == this) return true;
        if (obj is TagDefinition otherTag)
        {
            return otherTag.Name.ToLowerInvariant().Equals(Name.ToLowerInvariant());
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public bool ContainsText(string newText)
    {
        string l = Name.ToLowerInvariant();
        string s = newText.ToLowerInvariant();
        return l.Contains(s);
    }

    public void CopyFrom(TagDefinition modifiedTag)
    {
        Name = modifiedTag.Name;
        Color = modifiedTag.Color;
    }
}
