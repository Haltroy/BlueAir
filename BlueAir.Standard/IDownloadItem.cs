using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;

namespace BlueAir
{
    /// <summary>
    ///     Base class used by Folders and Files.
    /// </summary>
    public abstract class DownloadObject : INullable
    {
        /// <summary>
        ///     We don't recommend creating download items this way, please use <see cref="DownloadFile" /> for files and
        ///     <see cref="DownloadFolder" /> for folders.
        /// </summary>
        public DownloadObject()
        {
        }

        protected DownloadObject(bool isNull)
        {
            IsNull = isNull;
        }

        /// <summary>
        ///     Parent of the item.
        /// </summary>
        public DownloadFolder Parent { get; set; } = null;

        /// <summary>
        ///     Gets or sets the associated object (like TreeViewItem) of this download object.
        /// </summary>
        public object AssociatedObject { get; set; }

        public bool IsNull { get; }
    }

    /// <summary>
    ///     A folder class.
    /// </summary>
    public class DownloadFolder : DownloadObject
    {
        /// <summary>
        ///     Creates a new download folder.
        /// </summary>
        public DownloadFolder()
        {
        }

        protected DownloadFolder(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        ///     Items inside this folder.
        /// </summary>
        public List<DownloadObject> Content { get; set; } = new List<DownloadObject>();

        /// <summary>
        ///     Name of the folder.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        ///     Gets the path of the folder.
        /// </summary>
        /// <param name="MainFolder">The root folder.</param>
        /// <returns>
        ///     <see cref="string" />
        /// </returns>
        public string GetPath(string MainFolder)
        {
            return Path.Combine(Parent != null ? Parent.GetPath(MainFolder) : MainFolder, Name);
        }
    }

    /// <summary>
    ///     Represents a download file.
    /// </summary>
    public class DownloadFile : DownloadObject
    {
        /// <summary>
        ///     Creates a new download file.
        /// </summary>
        public DownloadFile()
        {
        }

        protected DownloadFile(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        ///     Place on the Internet to download this file from.
        /// </summary>
        public string Link { get; set; } = string.Empty;

        /// <summary>
        ///     File name to write on, set to empty for auto.
        /// </summary>
        public string FileName { get; set; } = string.Empty;
    }
}