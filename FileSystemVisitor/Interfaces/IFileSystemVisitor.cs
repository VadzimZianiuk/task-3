using System;
using System.Collections.Generic;

namespace FileSystemVisitor.Interfaces
{
    /// <summary>
    /// Represent a file system visitor.
    /// </summary>
    public interface IFileSystemVisitor
    {
        /// <summary>
        /// Start search event.
        /// </summary>
        public event EventHandler<EventArgs> Start;

        /// <summary>
        /// Finish search event.
        /// </summary>
        public event EventHandler<EventArgs> Finish;

        /// <summary>
        /// Directory find event.
        /// </summary>
        public event EventHandler<FileSystemVisitorEventArgs> DirectoryFinded;

        /// <summary>
        /// File find event.
        /// </summary>
        public event EventHandler<FileSystemVisitorEventArgs> FileFinded;

        /// <summary>
        /// Filtered directory find event.
        /// </summary>
        public event EventHandler<FileSystemVisitorEventArgs> FilteredDirectoryFinded;

        /// <summary>
        /// Filtered file find event.
        /// </summary>
        public event EventHandler<FileSystemVisitorEventArgs> FilteredFileFinded;

        /// <summary>
        /// Gets source directory path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets sequence of catalogs and files in source directory.
        /// </summary>
        /// <returns>Sequence of catalogs and files in source directory.</returns>
        public IEnumerable<string> Search();
    }
}
