using Godot;
using SoundBrowzr;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public partial class MainWindow : Control
{
    const string TagDefFilePath = "user://SoundBrowzrTags.dat";
    const string ConfigFilePath = "user://SoundBrowzr.conf";

    const long ScanProcessTimeboxMs = 100;

    [Export]
    ConfigWindow ConfigWindow;
    [Export]
    Control ScanOverlay;

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
    Button OpenButton;
    [Export]
    BasicTagList AssignedTags;

    public event EventHandler<TagEventArgs> TagAdded;
    public event EventHandler<TagEventArgs> TagDeleted;

    // persistent config (separate file same path as tag defs)
    private List<string> searchPaths = [];
    private string openCommand;
    private bool openMultiple;

    // persisted metadata (in tags file and sound meta files)
    // TODO?: convert tag def container to its own type that does lookups create/delete etc?
    List<TagDefinition> allTags;
    Dictionary<string, SoundMetadata> metadata;

    // current search state
    TreeItem treeRoot;

    // current selection state
    string selectedPath;
    SoundMetadata selectedSoundInfo;

    // transient state (drags etc)
    // TODO: move into script specific for sound playback control etc
    bool scrubInProgress;
    bool scanInProgress;
    Queue<PathToScan> scanQueue;
    List<ScannedFile> scanResults;

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
                string line;
                while ((line = configStream.ReadLine()) != null)
                {
                    // TODO: this is dumb, parse out section then section name then parse content
                    if(line == "[search]")
                    {
                        string pathFromConfig;
                        while ((line = pathFromConfig = configStream.ReadLine()) != null)
                        {
                            if(line == "[commands]") { break; }
                            searchPaths.Add(pathFromConfig);
                        }
                    }

                    // not an else on purpose (cuz fallthrough from loop above, again this is dumb)
                    if (line == "[commands]")
                    {
                        line = configStream.ReadLine();
                        string[] split = line.Split('=', StringSplitOptions.TrimEntries);
                        switch (split[0])
                        {
                            case "path":
                                openCommand = split[1];
                                break;

                            case "allow_multiple":
                                openMultiple = false;
                                bool.TryParse(split[1], out openMultiple);
                                break;
                        }
                    }
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

        OpenButton.Disabled = false;
        if (string.IsNullOrEmpty(openCommand))
        {
            openCommand = "";
            OpenButton.Disabled = true;
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

        if(scanInProgress)
        {
            // scan directories continually until timebox has ended
            Stopwatch timer = new Stopwatch();
            do
            {
                timer.Start();
                var dir = scanQueue.Dequeue();
                ScanDirectoryForSounds(dir.root, dir.dir, scanResults);
                if (scanQueue.Count == 0)
                {
                    FileSystemTree.Clear();
                    treeRoot = FileSystemTree.CreateItem();
                    treeRoot.SetText(0, "Sounds");
                    PopulateFileTree(treeRoot, scanResults);
                    scanInProgress = false;
                    scanResults.Clear();
                    ScanOverlay.Visible = false;
                }
                timer.Stop();
            } while (timer.ElapsedMilliseconds < ScanProcessTimeboxMs);
        }
    }

    private void PopulateFileTree(TreeItem treeRoot, List<ScannedFile> scanResults)
    {
        var treeItem = treeRoot;
        string currentPath = "";
        foreach (var foundFile in scanResults)
        {
            // move to path containing file in UI tree
            // if it doesn't exist, create it
            if (foundFile.RelativePath != currentPath)
            {
                var curPathSplit = currentPath.Split(Path.DirectorySeparatorChar);
                var newPathSplit = foundFile.RelativePath.Split(Path.DirectorySeparatorChar);
                int firstMiss = 0;
                for (; firstMiss < newPathSplit.Length; firstMiss++)
                {
                    if (firstMiss >= curPathSplit.Length || curPathSplit[firstMiss] != newPathSplit[firstMiss])
                    {
                        break;
                    }
                }

                int back = curPathSplit.Length - firstMiss;
                while (back > 0)
                {
                    treeItem = treeItem.GetParent();
                    back--;
                }

                var fwd = newPathSplit.Skip(firstMiss).ToList();
                while (fwd.Count > 0)
                {
                    int i = 0;
                    int c = treeItem.GetChildCount();
                    for (i = 0; i < c; i++)
                    {
                        var checkItem = treeItem.GetChild(i);
                        if (checkItem.GetText(0) == fwd[0])
                        {
                            treeItem = checkItem;
                        }
                    }

                    if (i == c)
                    {
                        treeItem = treeItem.CreateChild();
                        treeItem.SetText(0, fwd[0]);
                        treeItem.SetMeta("dir", true);
                    }

                    fwd.RemoveAt(0);
                }

                currentPath = foundFile.RelativePath;
            }

            // add entry to UI, meta value used to get metadata out of metadata dictionary
            // and also to load and play the sound file
            var fileItem = treeItem.CreateChild();
            fileItem.SetText(0, foundFile.Name);
            fileItem.SetMeta("file", foundFile.FullName);
            fileItem.SetSelectable(0, true);
            metadata[foundFile.FullName] = foundFile.Metadata;
        }
    }

    private void _OnOpenConfig()
    {
        ConfigWindow.SearchPaths = searchPaths;
        ConfigWindow.Show();
    }

    private void _OnConfigChangeOk(object sender, EventArgs e)
    {
        bool saveChanges = false;
        if (searchPaths != ConfigWindow.SearchPaths || !ConfigWindow.AllowCancel)
        {
            searchPaths = ConfigWindow.SearchPaths.ToList();
            RescanFilesystem();
            saveChanges = true;
        }

        if(openCommand != ConfigWindow.OpenCommand || openMultiple != ConfigWindow.AllowOpenMultiple)
        {
            openMultiple = ConfigWindow.AllowOpenMultiple;
            openCommand = ConfigWindow.OpenCommand;

            OpenButton.Disabled = false;
            if (string.IsNullOrEmpty(openCommand))
            {
                openCommand = "";
                OpenButton.Disabled = true;
            }
            saveChanges = true;
        }

        if (saveChanges)
        {
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
            selectedPath = soundFilePath;
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

    private void _OnOpen()
    {
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = openCommand;
        startInfo.RedirectStandardError = false;
        startInfo.RedirectStandardError = false;
        startInfo.RedirectStandardOutput = false;
        startInfo.UseShellExecute = false;
        startInfo.ArgumentList.Add(selectedPath);
        
        System.Diagnostics.Process.Start(startInfo);
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
                configWriter.WriteLine("[commands]");
                configWriter.WriteLine(string.Format("path = {0}", openCommand));
                configWriter.WriteLine(string.Format("allow_multiple = ", openMultiple));
                configWriter.WriteLine();
                configWriter.WriteLine("[search]");
                foreach (var searchPath in searchPaths)
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
            if (allTags.Contains(tag))
            {
                GD.Print("Warning - tag definition file has duplicated tag, skipped");
            }
            else
            {
                AddNewTag(tag);
            }
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

        ScanOverlay.Visible = true;
        scanQueue = new Queue<PathToScan>();
        scanResults = new List<ScannedFile>();
        scanInProgress = true;
        foreach (var dirPath in searchPaths)
        {
            DirectoryInfo walk = new DirectoryInfo(dirPath);
            AddToScanQueue(walk.FullName, walk, scanQueue);
        }
    }

    private void AddToScanQueue(string scanRoot, DirectoryInfo dir, Queue<PathToScan> scanQueue)
    {
        foreach (var d in dir.EnumerateDirectories())
        {
            PathToScan sc = new PathToScan();
            sc.dir = d;
            sc.root = scanRoot;
            scanQueue.Enqueue(sc);
            AddToScanQueue(scanRoot, d, scanQueue);
        }
    }

    private void ScanDirectoryForSounds(string scanRoot, DirectoryInfo dir, List<ScannedFile> scanResults)
    {
        foreach (FileInfo file in dir.EnumerateFiles())
        {
            // TODO: use 3rd party sound library so more formats are supported
            string ext = StringExtensions.GetExtension(file.Name);
            if (ext == "wav" || ext == "mp3" || ext == "ogg")
            {
                ScannedFile newFile = new ScannedFile();
                newFile.dir = dir;
                newFile.Name = file.Name;
                newFile.FullName = file.FullName;
                newFile.RelativePath = dir.FullName.Replace(scanRoot, "");
                newFile.Metadata = new SoundMetadata(file.FullName);
                newFile.Metadata.TryLoadMetaFile(GetOrCreateTag);

                scanResults.Add(newFile);
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
        GD.Print("Create tag '", newTag.Name, "'");
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
