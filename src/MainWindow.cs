using Godot;
using SoundBrowzr;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class MainWindow : Control
{
    const string TagDefFilePath = "user://SoundBrowzrTags.dat";
    const string ConfigFilePath = "user://SoundBrowzr.conf";

    private List<string> searchPaths = [];

    [Export]
    ConfigWindow ConfigWindow;

    [Export]
    LineEdit TagSearchText;
    [Export]
    BasicTagList AvailableTags;
    [Export]
    TagEditUi TagEditor;

    [Export]
    BasicTagList FilterIncludeTags;
    [Export]
    BasicTagList FilterExcludeTags;

    [Export]
    Tree FileSystemTree;

    [Export]
    Label FilePathLabel;
    [Export]
    AudioStreamPlayer AudioStreamPlayer;
    [Export]
    Slider PlaybackPosition;
    [Export]
    Slider VolumeSlider;
    [Export]
    BasicTagList AssignedTags;


    public event EventHandler<TagEventArgs> TagAdded;
    public event EventHandler<TagEventArgs> TagDeleted;

    // data that gets persisted
    // TODO?: convert tag def container to its own type that does lookups create/delete etc?
    List<TagDefinition> allTags;
    Dictionary<string, SoundMetadata> metadata;

    // current search state
    TreeItem treeRoot;

    // current selection state
    string selectedPath;
    SoundMetadata selectedSoundInfo;

    // transient state (drags etc)
    // TODO: move into script specific for sound playback control
    bool scrubInProgress;


    public override void _Ready()
    {
        allTags = new List<TagDefinition>();
        metadata = new Dictionary<string, SoundMetadata>();
        LoadTagDefinitions();

        VolumeSlider.SetValueNoSignal(AudioStreamPlayer.VolumeLinear);

        ConfigWindow.OnCancel += _OnConfigChangeCancel;
        ConfigWindow.OnOk += _OnConfigChangeOk;

        string configPath = ProjectSettings.GlobalizePath(ConfigFilePath);
        try
        {
            using (var configStream = new StreamReader(configPath))
            {
                string pathFromConfig;
                while ((pathFromConfig = configStream.ReadLine()) != null)
                {
                    searchPaths.Add(pathFromConfig);
                }
            }
        }
        catch (IOException)
        {
            // TODO: display error in UI if it's anything besides File Not Found
        }

        if (searchPaths.Count > 0)
        {
            ConfigWindow.AllowCancel = true;
            RescanFilesystem();
        }
        else
        {
            ConfigWindow.AllowCancel = false;
            _OnOpenConfig();
        }
    }

    public override void _Process(double delta)
    {
        if (AudioStreamPlayer.Playing)
        {
            float v = AudioStreamPlayer.GetPlaybackPosition();
            if (scrubInProgress)
            {
                AudioStreamPlayer.Seek((float)PlaybackPosition.Value);
            }
            else
            {
                PlaybackPosition.SetValueNoSignal(v);
            }
        }
    }

    private void _OnOpenConfig()
    {
        ConfigWindow.SearchPaths = searchPaths;
        ConfigWindow.Show();
    }

    private void _OnConfigChangeOk(object sender, EventArgs e)
    {
        // !cancel check is a hack until config sequence is coded
        if (searchPaths != ConfigWindow.SearchPaths || !ConfigWindow.AllowCancel)
        {
            searchPaths = ConfigWindow.SearchPaths.ToList();
            RescanFilesystem();
            SaveConfigFile();
        }

        ConfigWindow.AllowCancel = true;
    }

    private void _OnConfigChangeCancel(object sender, EventArgs e)
    {
    }

    private void _OnFileTreeItemSelected()
    {
        TreeItem item = FileSystemTree.GetSelected();
        if (item.HasMeta("file"))
        {
            string soundFilePath = item.GetMeta("file").ToString();
            selectedSoundInfo = metadata[soundFilePath];

            FilePathLabel.Text = item.GetText(0);
            AssignedTags.AssignTags(selectedSoundInfo.Tags);
            LoadSound(soundFilePath);
            _OnPlay();
        }
    }

    private void _OnCreateTagClicked()
    {
        TagEditor.OnOk += _CreateTagOnOk;
        TagEditor.OnCancel += _CreateTagOnCancel;

        TagDefinition newTag = new TagDefinition();
        if (TagSearchText.Text.Length > 0)
        {
            newTag.Name = TagSearchText.Text;
        }

        TagEditor.AssignTag(newTag);
        TagEditor.Show();
    }

    private void _CreateTagOnOk(object sender, EventArgs e)
    {
        TagEditor.OnOk -= _CreateTagOnOk;
        TagEditor.OnCancel -= _CreateTagOnCancel;

        var newTag = TagEditor.ModifiedTag;
        if (allTags.Contains(newTag))
        {
            GD.Print("show error message here! tag is dupe");
            return;
        }

        // TODO: sort tags by name on UI
        AddNewTag(newTag);
        SaveTags();
    }

    private void _CreateTagOnCancel(object sender, EventArgs e)
    {
        TagEditor.OnOk -= _CreateTagOnOk;
        TagEditor.OnCancel -= _CreateTagOnCancel;
    }

    private void _OnEditTagClicked()
    {
        TagEditor.OnOk += _EditTagOnOk;
        TagEditor.OnCancel += _EditTagOnCancel;
        TagEditor.AssignTag(AvailableTags.SelectedTags.First());
        TagEditor.Show();
    }

    private void _EditTagOnOk(object sender, EventArgs e)
    {
        TagEditor.OnOk -= _EditTagOnOk;
        TagEditor.OnCancel -= _EditTagOnCancel;
        SaveTags();
    }

    private void _EditTagOnCancel(object sender, EventArgs e)
    {
        TagEditor.OnOk -= _EditTagOnOk;
        TagEditor.OnCancel -= _EditTagOnCancel;
    }

    private void _OnIncludeAdd()
    {
        foreach (var tag in AvailableTags.SelectedTags)
        {
            FilterIncludeTags.AddTag(tag);
        }

        AvailableTags.ClearSelection();
        UpdateTreeFilter(treeRoot);
    }

    private void _OnIncludeRemove()
    {
        foreach (var tag in FilterIncludeTags.SelectedTags)
        {
            FilterIncludeTags.RemoveTag(tag);
        }

        UpdateTreeFilter(treeRoot);
    }

    private void _OnExcludeAdd()
    {
        foreach (var tag in AvailableTags.SelectedTags)
        {
            FilterExcludeTags.AddTag(tag);
        }

        AvailableTags.ClearSelection();
        UpdateTreeFilter(treeRoot);
    }

    private void _OnExcludeRemove()
    {
        foreach (var tag in FilterExcludeTags.SelectedTags)
        {
            FilterExcludeTags.RemoveTag(tag);
        }

        UpdateTreeFilter(treeRoot);
    }

    private void _OnFilterReset()
    {
        FilterIncludeTags.AssignTags([]);
        FilterExcludeTags.AssignTags([]);
        UpdateTreeFilter(treeRoot);
    }

    private void _OnAssignedTagsAdd()
    {
        if (selectedSoundInfo != null)
        {
            foreach (var tag in AvailableTags.SelectedTags)
            {
                AssignedTags.AddTag(tag);
            }

            selectedSoundInfo.SetTags(AssignedTags.Tags);
            selectedSoundInfo.SaveMetaFile();
            AvailableTags.ClearSelection();
        }
    }

    private void _OnAssignedTagsRemove()
    {
        if (selectedSoundInfo != null)
        {
            foreach (var tag in AssignedTags.SelectedTags)
            {
                AssignedTags.RemoveTag(tag);
            }

            selectedSoundInfo.SetTags(AssignedTags.Tags);
            selectedSoundInfo.SaveMetaFile();
        }
    }

    private void _OnPlay()
    {
        AudioStreamPlayer.Play();
    }

    private void _OnStop()
    {
        AudioStreamPlayer.Stop();
    }

    private void _OnPlaybackPosDragStart()
    {
        scrubInProgress = true;
    }

    private void _OnPlaybackPosDragEnd(bool valChanged)
    {
        scrubInProgress = false;
    }

    private void _OnVolumeChanged(float value)
    {
        AudioStreamPlayer.VolumeLinear = value;
    }

    private void SaveConfigFile()
    {
        try
        {
            string configPath = ProjectSettings.GlobalizePath(ConfigFilePath);
            using (var configWriter = new StreamWriter(configPath))
            {
                foreach(var searchPath in searchPaths)
                {
                    configWriter.WriteLine(searchPath);
                }
            }
        }
        catch(IOException)
        { 
            // TODO: display error message
        }
    }

    private void LoadTagDefinitions()
    {
        // TODO: on failure other than non-existent database show error dialog
        ConfigFile tagDefinitionFile = new ConfigFile();
        string actualFilePath = ProjectSettings.GlobalizePath(TagDefFilePath);
        GD.Print(actualFilePath);
        var result = tagDefinitionFile.Load(actualFilePath);
        if (result == Error.FileNotFound)
        {
            return;
        }

        var tagNames = tagDefinitionFile.GetSections();
        foreach (var tagName in tagNames)
        {
            string colorStr = tagDefinitionFile.GetValue(tagName, "color", "ffffff").ToString();
            TagDefinition tag = new TagDefinition();

            string cleanTagName = tagName.Trim();
            if(cleanTagName == string.Empty)
            {
                cleanTagName = "Unnamed";
                GD.Print("Warning - tag definition file has blank tag (replaced)");
            }

            tag.Name = cleanTagName;
            tag.Color = new Color(colorStr);

            if (allTags.Contains(tag)) GD.Print("Warning - tag definition file has duplicated tag");
            AddNewTag(tag);
        }
    }

    private void SaveTags()
    {
        ConfigFile tagDefinitionFile = new ConfigFile();
        foreach(TagDefinition tag in allTags)
        {
            tagDefinitionFile.SetValue(tag.Name, "color", tag.Color.ToHtml(false));
        }
        
        // TODO: show error message if save fails
        string actualFilePath = ProjectSettings.GlobalizePath(TagDefFilePath);
        tagDefinitionFile.Save(actualFilePath);
    }

    private void RescanFilesystem()
    {
        FileSystemTree.Clear();
        metadata.Clear();
        treeRoot = FileSystemTree.CreateItem();
        treeRoot.SetText(0, "Sounds");

        // TODO: this entire process should be in _Process or a thread instead of stalling the program
        foreach (var dirPath in searchPaths)
        {
            DirectoryInfo walk = new DirectoryInfo(dirPath);
            AddSounds(walk, treeRoot);
        }
    }

    private void AddSounds(DirectoryInfo dir, TreeItem treeItem)
    {
        // TODO: this processing should happen in _Process and be broken up
        //       so that the program doesn't freeze on load
        foreach (var d in dir.EnumerateDirectories())
        {
            // TODO: if no files are to be added in the branch do not add directory
            var item = treeItem.CreateChild();
            item.SetText(0, d.Name);
            item.SetMeta("dir", true);
            AddSounds(d, item);

            foreach (FileInfo file in d.EnumerateFiles())
            {
                // TODO: use 3rd party sound library so more formats are supported
                string ext = StringExtensions.GetExtension(file.Name);
                if (ext == "wav" || ext == "mp3" || ext == "ogg")
                {
                    var fileItem = item.CreateChild();
                    fileItem.SetText(0, file.Name);
                    fileItem.SetMeta("file", file.FullName);
                
                    var fileMetadata = new SoundMetadata(file.FullName);
                    fileMetadata.TryLoadMetaFile(GetOrCreateTag);
                    metadata[file.FullName] = fileMetadata;

                    fileItem.SetSelectable(0, true);
                }
            }
        }
    }

    private TagDefinition GetOrCreateTag(string tag)
    {
        var foundTag = allTags.Find((t) => t.Name == tag);
        if (foundTag != null)
        {
            return foundTag;
        }

        string cleanTagName = tag.Trim();
        if (cleanTagName == string.Empty)
        {
            cleanTagName = "Unnamed";
            GD.Print("Warning - tag definition has blank tag (replaced)");
        }

        TagDefinition newTag = new TagDefinition();
        newTag.Name = cleanTagName;
        AddNewTag(newTag);
        return newTag;
    }

    private void AddNewTag(TagDefinition newTag)
    {
        GD.Print("'", newTag.Name, "'");
        allTags.Add(newTag);
        AvailableTags.AddTag(newTag);

        // TODO: convert above to use this event
        TagAdded?.Invoke(this, new TagEventArgs(newTag));
    }

    private bool UpdateTreeFilter(TreeItem directoryItem)
    {
        bool anyChildVisible = false;
        foreach(var child in directoryItem.GetChildren())
        {
            if(child.HasMeta("file"))
            {
                bool shouldInclude = CheckFilter(child.GetMeta("file").ToString());
                child.Visible = shouldInclude;
                anyChildVisible |= shouldInclude;
            }
            else
            {
                anyChildVisible |= UpdateTreeFilter(child);
            }
        }

        directoryItem.Visible = anyChildVisible;
        return anyChildVisible;
    }

    private bool CheckFilter(string soundFile)
    {
        SoundMetadata soundInfo = metadata[soundFile];
        bool shouldInclude = true;
        foreach (var includedTag in FilterIncludeTags.Tags)
        {
            shouldInclude &= soundInfo.Tags.Contains(includedTag);
            if (!shouldInclude) break;
        }

        foreach (var excludedTag in FilterExcludeTags.Tags)
        {
            shouldInclude &= !soundInfo.Tags.Contains(excludedTag);
        }

        return shouldInclude;
    }

    private void LoadSound(string path)
    {
        // TODO: async load
        AudioStream stream = null;
        if (path.EndsWith(".wav"))
        {
            stream = AudioStreamWav.LoadFromFile(path);
        }
        else if (path.EndsWith(".ogg"))
        {
            stream = AudioStreamOggVorbis.LoadFromFile(path);
        }
        else if (path.EndsWith(".mp3"))
        {
            stream = AudioStreamMP3.LoadFromFile(path);
        }

        AudioStreamPlayer.Stream = stream;
        PlaybackPosition.MaxValue = stream.GetLength();
        PlaybackPosition.SetValueNoSignal(0);
    }
}
