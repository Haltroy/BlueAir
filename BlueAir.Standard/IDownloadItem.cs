using System.Data.SqlTypes;
using System.IO;

namespace BlueAir
{
    /// <summary>
    ///     Base class used by Folders and Files.
    /// </summary>
    public abstract class DownloadObject : INullable
    {
        protected DownloadObject(bool isNull)
        {
            IsNull = isNull;
        }

        /// <summary>
        ///     Parent of the item.
        /// </summary>
        public DownloadFolder Parent { get; set; }


        public bool IsNull { get; }
    }

    /// <summary>
    ///     A folder class.
    /// </summary>
    public abstract class DownloadFolder : DownloadObject
    {
        protected DownloadFolder(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        ///     Items inside this folder.
        /// </summary>
        public DownloadObject[] Content { get; set; }

        /// <summary>
        ///     Name of the folder.
        /// </summary>
        public string Name { get; set; }

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
    public abstract class DownloadFile : DownloadObject
    {
        protected DownloadFile(bool isNull) : base(isNull)
        {
        }

        /// <summary>
        ///     Place on the Internet to download this file from.
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        ///     File name to write on, set to empty for auto.
        /// </summary>
        public string FileName { get; set; }
    }
}