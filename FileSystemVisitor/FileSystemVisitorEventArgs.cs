using System;

namespace FileSystemVisitor
{
    /// <summary>
    /// Contain event data.
    /// </summary>
    public class FileSystemVisitorEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemVisitorEventArgs"/> class.
        /// </summary>
        /// <param name="path">catalog or file path.</param>
        public FileSystemVisitorEventArgs(string path) => this.Path = path;

        /// <summary>
        /// Gets catalog or file path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip the found element.
        /// </summary>
        public bool Skip { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether stop search flag.
        /// </summary>
        public bool Abort { get; set; }
    }
}
