using UnityEngine;

namespace Kit2
{
    public class AssetFolderAttribute : PropertyAttribute
    {
        public readonly string title, defaultName, helpMessage;
        public AssetFolderAttribute(string title, string defaultName, string helpMessage)
        {
            this.title = title;
            this.defaultName = defaultName;
            this.helpMessage = helpMessage;
        }

        public AssetFolderAttribute()
            : this("Select Asset Folder", "", "Folder must within current project \"Asset\"")
        { }
    }
}
