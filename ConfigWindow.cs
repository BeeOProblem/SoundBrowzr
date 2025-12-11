using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ConfigWindow : Window
{
    private enum FileDialogAction
    {
        None,
        Add,
        Change
    }

    [Export]
    Button OkButton;
    [Export]
    Button CancelButton;
    [Export]
    Control ExistingPathsContainer;
    [Export]
    FileDialog DirBrowseDialog;

    [Export]
    PackedScene SelectableLabel;

    private FileDialogAction dirBrowseAction;

    private List<string> modifiedSearchPaths;
    private SelectableLabel selectedPath;

    public bool AllowCancel = true;
    public IEnumerable<string> SearchPaths
    {
        get
        {
            return modifiedSearchPaths.ToArray().AsReadOnly();
        }

        set
        {
            modifiedSearchPaths = value.ToList();
            foreach(var child in ExistingPathsContainer.GetChildren())
            {
                child.QueueFree();
            }

            foreach(string path in modifiedSearchPaths)
            {
                AddPathItem(path);
            }
        }
    }

    public event EventHandler OnCancel;
    public event EventHandler OnOk;

    private void _OnOpen()
    {
        if (!Visible) return;

        CancelButton.Disabled = !AllowCancel;
        if (modifiedSearchPaths.Count == 0)
        {
            OkButton.Disabled = true;
        }
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

    private void _AddPathClicked()
    {
        dirBrowseAction = FileDialogAction.Add;
        DirBrowseDialog.Show();
    }

    private void _ChangePathClicked()
    {
        // TODO: toggle enable of change and delete on select/deselect
        if (selectedPath == null) return;
        dirBrowseAction = FileDialogAction.Change;
        DirBrowseDialog.CurrentPath = selectedPath.Text;
        DirBrowseDialog.Show();
    }

    private void _RemovePathClicked()
    {
        // TODO: prompt for confirmation?
        // TODO: disallow saving config with no scan targes
        if (selectedPath == null) return;

        modifiedSearchPaths.RemoveAt(selectedPath.GetIndex());
        selectedPath.QueueFree();
        selectedPath = null;

        if (modifiedSearchPaths.Count == 0)
        {
            OkButton.Disabled = true;
        }
    }

    private void _DirBrowseSelect(string path)
    {
        switch(dirBrowseAction)
        {
            case FileDialogAction.Change:
                // TODO: make sure dupes can't be added here either
                int i = selectedPath.GetIndex();
                modifiedSearchPaths[i] = path;
                selectedPath.Text = path;
                break;

            case FileDialogAction.Add:
                // TODO: ensure path is canonical before checking and adding
                if (!modifiedSearchPaths.Contains(path))
                {
                    modifiedSearchPaths.Add(path);
                    AddPathItem(path);
                    OkButton.Disabled = false;
                }
                break;
        }

        dirBrowseAction = FileDialogAction.None;
    }

    private void _DirBrowseCancel()
    {
        dirBrowseAction = FileDialogAction.None;
    }

    private void AddPathItem(string path)
    {
        SelectableLabel pathLabel = SelectableLabel.Instantiate<SelectableLabel>();
        pathLabel.Text = path;
        pathLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        pathLabel.OnSelected += _SelectedChanged; 

        ExistingPathsContainer.AddChild(pathLabel);
    }

    private void _SelectedChanged(object sender, EventArgs e)
    {
        SelectableLabel l = (SelectableLabel)sender;
        if (l.IsSelected)
        {
            if(selectedPath != null) selectedPath.IsSelected = false;
            selectedPath = l;
        }
        else
        {
            if(l == selectedPath) selectedPath = null;
        }
    }
}
