# SoundBrowzr
A tool for categorizing and searching sound effects

# How to Use
When you first open SoundBrowzr you will be shown the config dialog. Here you can choose which folders
to scan for sound effects. You must choose at least one folder for SoundBrowzr to be able to function.

## Setup

![SoundBrowzr configuration window](/screenshots/config.png)

After you choose the folders to scan SoundBrowzr will scan them. If a lot of folders are being scanned
this may cause SoundBrowzr to freeze briefly. This should be fixed in a future release. Once this
is done you should see the main SoundBrowzr window.

## The Main Window
![SoundBrowzr main view](/screenshots/main.png)

On the left are tools for creating and managing tags and a list of all sound files found during scanning.
Options for filtering the sound list by tags are also found here, more on that later. You may create edit
or delete tags by clicking the buttons under Manage Tags. Create and Edit both open the tag editor.

## Editing Tags
~[SoundBrowzr tag editor](/screenshots/tag.png)

Tags are used to categorize sounds so they're easier to find. Each tag has a unique name and a color.

## Playing and Tagging Sounds
On the left section, below Manage Tags, is the sound file list. This is where you can browse sounds found
in the folders you selected when configuring SoundBrowzr. Currently SoundBrowzr can only play with MP3, WAV
and OGG files. Other formats such as AAC, AIF, FLAC cannot be played and are unlisted. This will also be
fixed in a future version.

Clicking on a sound file will play it and show what tags are assigned to it, if any. All properties of
the sound that is selected appear on the right side of SoundBrowzr. You may assign tags to the selected
sound effect by clicking the + button on the list of tags. All tags that are selected in the top left
will be assigned to the sound effect.

## Filtering the Sound List
Tags are used to make searching for specific sound effects easier. Once you've assigned tags to some sounds
you can set a filter on the listed sound files. Use the Include Tags filter to only display sounds that have
every tag listed. Use the Exclude Tags filter to hide sounds that have any of the tags listed.