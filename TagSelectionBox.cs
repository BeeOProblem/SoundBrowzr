using Godot;
using System;
using System.Collections.Generic;

public partial class TagSelectionBox : Panel
{
    [Export]
    LineEdit TagSearch;
    [Export]
    Control TagContainer;

    [Export]
    PackedScene TagUiScene;

    List<TagUi> tags;
    List<TagDefinition> selectedTags;

    public event EventHandler OnSelectionChanged;
    public IEnumerable<TagDefinition> SelectedTags
    {
        get
        {
            return selectedTags;
        }
    }

    public void ChangeSelection(IEnumerable<TagDefinition> newSelectedTags)
    {
        selectedTags.Clear();
        foreach (var tag in newSelectedTags)
        {
            selectedTags.Add(tag);
        }

        foreach (var tag in tags)
        {
            tag.SetSelectedNoSignal(selectedTags.Contains(tag.Tag));
        }
    }

    public void TagCreated(TagDefinition tag)
    {
        TagUi tagUi = TagUiScene.Instantiate<TagUi>();
        tagUi.Tag = tag;
        tagUi.Selectable = true;

        tagUi.OnSelected += _OnAvailableTagSelected;
        tagUi.OnDeselected += _OnAvailableTagDeselected;
        TagContainer.AddChild(tagUi);
        tags.Add(tagUi);
    }

    public void TagDeleted(TagDefinition tag) 
    {
        TagUi uiForTag = null;
        foreach (TagUi tagUi in tags)
        {
            if (tagUi.Tag == tag)
            {
                uiForTag = tagUi;
                break;
            }
        }

        if (uiForTag != null && uiForTag.IsSelected)
        {
            selectedTags.Remove(uiForTag.Tag);
            OnSelectionChanged.Invoke(this, EventArgs.Empty);
        }

        tags.Remove(uiForTag);
        uiForTag.QueueFree();
    }

    public override void _Ready()
    {
        tags = new List<TagUi>();
        selectedTags = new List<TagDefinition>();
    }

    private void _OnAvailableTagSelected(object sender, EventArgs e)
    {
        var tagUi = sender as TagUi;
        selectedTags.Add(tagUi.Tag);
        OnSelectionChanged.Invoke(this, EventArgs.Empty);
    }

    private void _OnAvailableTagDeselected(object sender, EventArgs e)
    {
        var tagUi = sender as TagUi;
        selectedTags.Remove(tagUi.Tag);
        OnSelectionChanged.Invoke(this, EventArgs.Empty);
    }
}
