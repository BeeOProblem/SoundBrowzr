using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoundBrowzr
{
    internal class SoundMetadata
    {
        const string MetaFileExtension = ".sbzm";

        string soundFilePath;
        List<TagDefinition> tags;

        public delegate TagDefinition TagByNameFunction(string tag);

        public IEnumerable<TagDefinition> Tags
        {
            get
            {
                return tags;
            }
        }

        public SoundMetadata(string path)
        {
            tags = new List<TagDefinition>();
            soundFilePath = path;
        }

        public void TryLoadMetaFile(TagByNameFunction tagProvider)
        {
            try
            {
                using (StreamReader sr = new StreamReader(soundFilePath + MetaFileExtension))
                {
                    string tagsLine = sr.ReadLine();
                    string[] tagsFromFile = tagsLine.Split(',');
                    foreach (string tag in tagsFromFile)
                    {
                        TagDefinition foundTag = tagProvider(tag);
                        if (tag != null)
                        {
                            tags.Add(foundTag);
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // ignore file not found since meta files are only created when metadata assignment is done
            }
        }

        public void SaveMetaFile()
        {
            using (StreamWriter sw = new StreamWriter(soundFilePath + MetaFileExtension))
            {
                sw.WriteLine(String.Join(",", tags.Select((tag) => tag.Name)));
            }
        }

        public void SetTags(IEnumerable<TagDefinition> newTags)
        {
            tags.Clear();
            foreach (var tag in newTags)
            {
                tags.Add(tag);
            }
        }

        public void AddTag(TagDefinition tag)
        {
            if (tags.Contains(tag)) return;
            tags.Add(tag);
        }

        public void RemoveTag(TagDefinition tag)
        {
            if (!tags.Contains(tag)) return;
            tags.Remove(tag);
        }
    }
}
