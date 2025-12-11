using Godot;

public class TagDefinition
{
    public string Name;
    public Color Color;

    public TagDefinition()
    {
        Name = "Unnamed";
        Color = new Color(0xFFFFFFFF);
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
}
