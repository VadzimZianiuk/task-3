using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSVisitor
{
    /// <summary>
    /// Represent a file system visitor.
    /// </summary>
    public class FileSystemVisitor
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

        /// <summary>
        /// Start search event.
        /// </summary>
        public event EventHandler<EventArgs> Start;

        /// <summary>
        /// Finish search event.
        /// </summary>
        public event EventHandler<EventArgs> Finish;

        /// <summary>
        /// Directory finded event.
        /// </summary>
        public event EventHandler<FileSystemVisitorEventArgs> DirectoryFinded;

        /// <summary>
        /// File finded event.
        /// </summary>
        public event EventHandler<FileSystemVisitorEventArgs> FileFinded;

        /// <summary>
        /// Filtered directory finded event.
        /// </summary>
        public event EventHandler<FileSystemVisitorEventArgs> FilteredDirectoryFinded;

        /// <summary>
        /// Filtered file finded event.
        /// </summary>
        public event EventHandler<FileSystemVisitorEventArgs> FilteredFileFinded;

        /// <summary>
        /// Gets source directory path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets sequence of catalogs and files in source directory.
        /// </summary>
        /// <returns>Sequence of catalogs and files in source directory.
        /// Method can throw exseptions <seealso cref="Directory.EnumerateDirectories"/>.</returns>
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
        protected virtual IEnumerable<string> EnumerateDirectories(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.EnumerateDirectories(path);
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Enumerate files.
        /// </summary>
        /// <param name="path">Path to search for files.</param>
        /// <returns>Full paths to files.</returns>
        protected virtual IEnumerable<string> EnumerateFiles(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.EnumerateFiles(path);
            }

            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> Search(IEnumerable<string> directories, IEnumerable<string> files)
        {
            bool abort = false;
            var items = Prepare(directories, this.OnDirectoryFinded, this.OnFilteredDirectoryFinded).
                Concat(Prepare(files, this.OnFileFinded, this.OnFilteredFileFinded));

            this.OnStart(EventArgs.Empty);
            foreach (var item in items)
            {
                yield return item;
            }

            this.OnFinish(EventArgs.Empty);

            IEnumerable<string> Prepare(IEnumerable<string> items, Action<FileSystemVisitorEventArgs> onFinded, Action<FileSystemVisitorEventArgs> onFilteredFinded)
            {
                if (abort || items is null)
                {
                    yield break;
                }

                foreach (var path in items)
                {
                    var args = new FileSystemVisitorEventArgs(path);
                    onFinded(args);
                    if (args.Skip)
                    {
                        continue;
                    }
                    else if (args.Abort)
                    {
                        abort = true;
                        yield break;
                    }
                    else if (this.predicate is null)
                    {
                        yield return path;
                    }
                    else if (this.predicate.GetInvocationList().All(x => ((Predicate<string>)x)(path)))
                    {
                        onFilteredFinded?.Invoke(args);
                        if (args.Skip)
                        {
                            continue;
                        }
                        else if (args.Abort)
                        {
                            abort = true;
                            yield break;
                        }

                        yield return path;
                    }
                }
            }
        }
    }
}
