using UnityEngine;

namespace Kit2
{
    public class AssetFileAttribute : PropertyAttribute
    {
        public readonly string title, fileName, extension;

        public AssetFileAttribute(string title, string fileName, string extension)
        {
            this.title = title;
            this.fileName = fileName;
            this.extension = extension;
        }

        public AssetFileAttribute()
            : this("Select Asset File", "", "File must within current project \"Asset\"")
        { }
    }
}