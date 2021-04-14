using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileSystemVisitor.Interfaces;

namespace FileSystemVisitor
{
    /// <summary>
    /// Represent a file system visitor.
    /// </summary>
    public class FileSystemVisitor : IFileSystemVisitor
    {
        private readonly Predicate<string> predicate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemVisitor"/> class.
        /// </summary>
        /// <param name="path">Source directory path.</param>
        /// <exception cref="ArgumentException">Throw when <paramref name="path"/> is invalid or not exist.</exception>
        public FileSystemVisitor(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new ArgumentException("Directory path is invalid or not exist.", nameof(path));
            }

            this.Path = path;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemVisitor"/> class.
        /// </summary>
        /// <param name="path">Source directory path.</param>
        /// <param name="predicate">Search filter predicate.</param>
        /// <exception cref="ArgumentException">Throw when <paramref name="path"/> is invalid or not exist.</exception>
        /// <exception cref="ArgumentNullException">Throw when <paramref name="predicate"/> is null.</exception>
        public FileSystemVisitor(string path, Predicate<string> predicate)
            : this(path)
        {
            this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Start;

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Finish;

        /// <inheritdoc/>
        public event EventHandler<FileSystemVisitorEventArgs> DirectoryFinded;

        /// <inheritdoc/>
        public event EventHandler<FileSystemVisitorEventArgs> FileFinded;

        /// <inheritdoc/>
        public event EventHandler<FileSystemVisitorEventArgs> FilteredDirectoryFinded;

        /// <inheritdoc/>
        public event EventHandler<FileSystemVisitorEventArgs> FilteredFileFinded;

        /// <inheritdoc/>
        public string Path { get; }

        /// <summary>
        /// Create a new instance of the <see cref="IFileSystemVisitor"/>.
        /// </summary>
        /// <param name="path">Source directory path.</param>
        /// <returns>A new instance of the <see cref="IFileSystemVisitor"/>.</returns>
        /// <exception cref="ArgumentException">Throw when <paramref name="path"/> is invalid or not exist.</exception>
        public static IFileSystemVisitor CreateInstance(string path) => new FileSystemVisitor(path);

        /// <summary>
        /// Create a new instance of the <see cref="IFileSystemVisitor"/>.
        /// </summary>
        /// <param name="path">Source directory path.</param>
        /// <param name="predicate">Search filter predicate.</param>
        /// <returns>A new instance of the <see cref="IFileSystemVisitor"/>.</returns>
        /// <exception cref="ArgumentException">Throw when <paramref name="path"/> is invalid or not exist.</exception>
        /// <exception cref="ArgumentNullException">Throw when <paramref name="predicate"/> is null.</exception>
        public static IFileSystemVisitor CreateInstance(string path, Predicate<string> predicate) => new FileSystemVisitor(path, predicate);

        /// <inheritdoc/>
        public IEnumerable<string> Search() => this.Search(this.EnumerateDirectories(this.Path), this.EnumerateFiles(this.Path));

        /// <summary>
        /// On start.
        /// </summary>
        /// <param name="eventArgs">Source args.</param>
        protected virtual void OnStart(EventArgs eventArgs) => this.Start?.Invoke(this, eventArgs);

        /// <summary>
        /// On finish.
        /// </summary>
        /// <param name="eventArgs">Source args.</param>
        protected virtual void OnFinish(EventArgs eventArgs) => this.Finish?.Invoke(this, eventArgs);

        /// <summary>
        /// On directory finded.
        /// </summary>
        /// <param name="eventArgs">Source args.</param>
        protected virtual void OnDirectoryFinded(FileSystemVisitorEventArgs eventArgs) => this.DirectoryFinded?.Invoke(this, eventArgs);

        /// <summary>
        /// On file finded.
        /// </summary>
        /// <param name="eventArgs">Source args.</param>
        protected virtual void OnFileFinded(FileSystemVisitorEventArgs eventArgs) => this.FileFinded?.Invoke(this, eventArgs);

        /// <summary>
        /// On filtered directory finded.
        /// </summary>
        /// <param name="eventArgs">Source args.</param>
        protected virtual void OnFilteredDirectoryFinded(FileSystemVisitorEventArgs eventArgs) => this.FilteredDirectoryFinded?.Invoke(this, eventArgs);

        /// <summary>
        /// On filtered file finded.
        /// </summary>
        /// <param name="eventArgs">Source args.</param>
        protected virtual void OnFilteredFileFinded(FileSystemVisitorEventArgs eventArgs) => this.FilteredFileFinded?.Invoke(this, eventArgs);

        /// <summary>
        /// Enumerate directories.
        /// </summary>
        /// <param name="path">Path to search for directories.</param>
        /// <returns>Full paths to directories.</returns>
        protected virtual IEnumerable<string> EnumerateDirectories(string path) =>
            Directory.Exists(path) ? Directory.EnumerateDirectories(path) : Enumerable.Empty<string>();

        /// <summary>
        /// Enumerate files.
        /// </summary>
        /// <param name="path">Path to search for files.</param>
        /// <returns>Full paths to files.</returns>
        protected virtual IEnumerable<string> EnumerateFiles(string path) =>
            Directory.Exists(path) ? Directory.EnumerateFiles(path) : Enumerable.Empty<string>();

        private IEnumerable<string> Search(IEnumerable<string> directories, IEnumerable<string> files)
        {
            bool abort = false;
            this.OnStart(EventArgs.Empty);
            var sequence = Prepare(directories, this.OnDirectoryFinded, this.OnFilteredDirectoryFinded)
                .Concat(Prepare(files, this.OnFileFinded, this.OnFilteredFileFinded));

            foreach (var path in sequence)
            {
                yield return path;
            }

            this.OnFinish(EventArgs.Empty);

            IEnumerable<string> Prepare(IEnumerable<string> source, Action<FileSystemVisitorEventArgs> findAction, Action<FileSystemVisitorEventArgs> filterAction)
            {
                if (source is null)
                {
                    return Enumerable.Empty<string>();
                }

                var enumerable = source
                    .TakeWhile(_ => !abort)
                    .Select(x => new FileSystemVisitorEventArgs(x))
                    .TakeWhile(x => DoActionWithoutAbort(x, findAction))
                    .Where(x => !x.Skip);

                if (this.predicate != null)
                {
                    enumerable = enumerable
                        .Where(x => this.predicate.GetInvocationList()
                            .All(p => ((Predicate<string>)p)(x.Path)))
                        .TakeWhile(x => DoActionWithoutAbort(x, filterAction))
                        .Where(x => !x.Skip);
                }

                return enumerable.Select(x => x.Path);

                bool DoActionWithoutAbort(FileSystemVisitorEventArgs args, Action<FileSystemVisitorEventArgs> action)
                {
                    action(args);
                    if (args.Abort)
                    {
                        abort = true;
                    }

                    return !abort;
                }
            }
        }
    }
}
