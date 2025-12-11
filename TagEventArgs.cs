using System;

public class TagEventArgs : EventArgs
{
    public TagDefinition Tag;

    public TagEventArgs(TagDefinition tag)
    {
        Tag = tag;
    }
}
