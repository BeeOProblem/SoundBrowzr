using Godot;
using System;
using System.Collections.Generic;

public partial class BasicTagList : ScrollContainer
{
    [Export]
    bool AllTagsSelected;
    [Export]
    Control TagContainer;

    [Export]
    public PackedScene TagUiScene;

    public event EventHandler OnSelectionChanged;

    List<TagDefinition> tags;
    List<TagUi> tagControls;
    List<TagDefinition> selectedTags;

    public IEnumerable<TagDefinition> Tags
    {
        get
        {
            return tags.ToArray().AsReadOnly();
        }
    }

    public IEnumerable<TagDefinition> SelectedTags
    {
        get
        {
            return selectedTags.ToArray().AsReadOnly();
        }
    }

    public void AddTag(TagDefinition tag)
    {
        if (tags.Contains(tag)) return;

        TagUi tagUi = TagUiScene.Instantiate<TagUi>();
        tagUi.Tag = tag;
        tagUi.Selectable = true;

        tagUi.OnSelected += _OnAvailableTagSelected;
        tagUi.OnDeselected += _OnAvailableTagDeselected;
        TagContainer.AddChild(tagUi);
        tagControls.Add(tagUi);
        tags.Add(tag);

        if (AllTagsSelected)
        {
            tagUi.SetSelectedNoSignal(true);
            selectedTags.Add(tagUi.Tag);
        }
    }

    public void RemoveTag(TagDefinition tag)
    {
        if(!tags.Contains(tag)) return;

        TagUi uiForTag = null;
        foreach (TagUi tagUi in tagControls)
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
            if (!AllTagsSelected)
            {
                OnSelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        tags.Remove(tag);
        tagControls.Remove(uiForTag);
        uiForTag.QueueFree();
    }

    public void AssignTags(IEnumerable<TagDefinition> newTags)
    {
        foreach (var tagUi in tagControls)
        {
            tagUi.QueueFree();
        }

        tags.Clear();
        tagControls.Clear();
        selectedTags.Clear();

        foreach(var tag in newTags)
        {
            AddTag(tag);
        }
    }

    public void ClearSelection()
    {
        foreach(var tag in tagControls)
        {
            tag.SetSelectedNoSignal(false);
        }

        selectedTags.Clear();
    }

    public override void _Ready()
    {
        tags = new List<TagDefinition>();
        tagControls = new List<TagUi>();
        selectedTags = new List<TagDefinition>();
    }

    private void _OnAvailableTagSelected(object sender, EventArgs e)
    {
        if (AllTagsSelected) return;
        var tagUi = sender as TagUi;
        selectedTags.Add(tagUi.Tag);
        OnSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void _OnAvailableTagDeselected(object sender, EventArgs e)
    {
        if (AllTagsSelected) return;
        var tagUi = sender as TagUi;
        selectedTags.Remove(tagUi.Tag);
        OnSelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
