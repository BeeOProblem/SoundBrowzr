# SoundBrowzr
A tool for categorizing and searching sound effects. I created this because I have thousands of sounds
and needed something to make searching them easier.

This is a very early version of the tool, if you have any suggestions or run into any bugs feel free
to log an issue.

# How to Use
When you first open SoundBrowzr you will be shown the config dialog. Here you can choose which folders
to scan for sound effects. You must choose at least one folder for SoundBrowzr to be able to function.

Optionally, you can also choose a program to open sound effects with.

## Setup
![SoundBrowzr configuration window](/screenshots/config.png)

After you choose the folders to scan SoundBrowzr will scan them. Once this is done all sounds will appear
in the file tree on the bottom left section of the main window.

In addition you may optionally choose a program to open and edit sound effects. This makes it faster to
make use of the sounds you've selected in your project. If the program you choose can open multiple sound
files at once turn on "Command Accepts Multiple Files" to take advantage. Otherwise SoundBrowzr will open
one sound file at a time with the program you've chosen.

## The Main Window
![SoundBrowzr main view](/screenshots/main.png)

On the top left are tools for creating and managing tags. And, on the bottom left is a list of all sound files
found during scanning. Options for filtering the sound list by tags are also found on the left side of the main window.
More on that later. You may create edit or delete tags by clicking the buttons under Manage Tags.
Create and Edit both open the tag editor.

## Editing Tags
![SoundBrowzr tag editor](/screenshots/tag.png)

Tags are used to categorize sounds so they're easier to find. Each tag has a unique name and a color. Create as many
or as few tags as you want.

## Filtering the Sound List
![An animation of filtering by tags](/screenshots/filter.gif)

Tags are used to make searching for specific sound effects easier. Once you've assigned tags to some sounds
you can set a filter on the listed sound files. Use the Include Tags filter to only display sounds that have
every tag listed. Use the Exclude Tags filter to hide sounds that have any of the tags listed.

## Playing and Tagging Sounds
On the left section, below Manage Tags, is the sound file list. This is where you can browse sounds found
in the folders you selected when configuring SoundBrowzr. Currently SoundBrowzr can only play MP3, WAV
and OGG files. Other formats such as AAC, AIF, FLAC etc. cannot be played and are unlisted. This will also be
fixed in a future version.

Clicking on a sound file will play it and show what tags are assigned to it, if any. All properties of
the sound that is selected appear on the right side of SoundBrowzr. You may assign tags to the selected
sound effect by clicking the Tag button above the list of tags. All tags that are selected in the top left
will be assigned to the sound effect. To remove one or more tags from a sound select the tag on the list
of assigned tags and click Untag.

If you have configured SoundBrowzr with an exteral sound editor you may use it to open the sound you selected
by clicking the Open button.

## Tagging and Opening Multiple Sounds at once
![An animation of taggin multiple sounds at once](/screenshots/multitag.gif)

On the right side of each sound listed int the sound file list is a checkbox. This allows you to work with
multiple sound files at once. When one or more sounds are checked the Tag and Untag buttons become Tag All and
Untag All and the list of assigned tags will only show tags that are common to all the sounds you have checked.

In addition the Open button will change to Open All when you have checked sounds on the list. Clicking the
Open All button with multiple sounds checked will open all of them at once.
