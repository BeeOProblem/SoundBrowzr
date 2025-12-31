using System;
using System.IO;

namespace SoundBrowzr
{
    internal class ScannedFile
    {
        public DirectoryInfo dir;
        public string Name;
        public string RelativePath;
        public string FullName;
        public SoundMetadata Metadata;
    }
}
