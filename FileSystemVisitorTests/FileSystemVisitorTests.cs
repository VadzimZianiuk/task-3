using FileSystemVisitor;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileSystemVisitorTests
{
    [TestFixture]
    internal class FileSystemVisitorTests
    {
        private const int DirectoriesCount = 15;
        private const int FilesCount = 15;
        private static readonly IEnumerable<string> Directories = Enumerable.Range(0, DirectoriesCount).Select(x => $"Directory #{x}");
        private static readonly IEnumerable<string> Files = Enumerable.Range(0, FilesCount).Select(x => $"File #{x}");
        private readonly string validPath = Directory.GetCurrentDirectory();
        private readonly EventCounters actualEventCounters = new EventCounters();

        private static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                yield return new TestCaseData(Directories, Files, Directories.Concat(Files), new EventCounters(Directories, Files));
                yield return new TestCaseData(Directories, Enumerable.Empty<string>(), Directories, new EventCounters(Directories, null));
                yield return new TestCaseData(Enumerable.Empty<string>(), Files, Files, new EventCounters(null, Files));
                yield return new TestCaseData(null, null, Enumerable.Empty<string>(), new EventCounters { Start = 1, Finish = 1 });
            }
        }

        private static IEnumerable<TestCaseData> TestCasesWithPredicate
        {
            get
            {
                yield return new TestCaseData(
                    Directories,
                    Files,
                    (Predicate<string>)(x => true),
                    Directories.Concat(Files),
                    new EventCounters(Directories, Files) { FilteredDirectoriesFind = DirectoriesCount, FilteredFilesFind = FilesCount });
                yield return new TestCaseData(
                     Directories,
                     Files,
                     Delegate.Combine((Predicate<string>)(x => x.StartsWith("Directory")), (Predicate<string>)(x => true)),
                     Directories,
                     new EventCounters(Directories, Files) { FilteredDirectoriesFind = DirectoriesCount });
                yield return new TestCaseData(
                    Directories,
                    Files,
                    Delegate.Combine((Predicate<string>)(x => x.StartsWith("File")), (Predicate<string>)(x => true)),
                    Files,
                    new EventCounters(Directories, Files) { FilteredFilesFind = FilesCount });
                yield return new TestCaseData(
                   Directories,
                   Files,
                   Delegate.Combine((Predicate<string>)(x => false), (Predicate<string>)(x => true)),
                   Array.Empty<string>(),
                   new EventCounters(Directories, Files));
            }
        }

        [SetUp]
        protected void Setup() => actualEventCounters.Clear();

        [Test]
        public void Factory_OneParam() => Assert.DoesNotThrow(() => FileSystemVisitor.FileSystemVisitor.CreateInstance(this.validPath), "Create instance with valid path.");

        [Test]
        public void Factory_TwoParams() => Assert.DoesNotThrow(() => FileSystemVisitor.FileSystemVisitor.CreateInstance(this.validPath, _ => true),
            "Create instance with valid path and predicate.");

        [Test]
        public void Ctor_OneParam() => Assert.DoesNotThrow(() => new FileSystemVisitor.FileSystemVisitor(this.validPath), "Create instance with valid path.");

        [Test]
        public void Ctor_TwoParams() => Assert.DoesNotThrow(() => new FileSystemVisitor.FileSystemVisitor(this.validPath, _ => true),
            "Create instance with valid path and predicate.");

        [TestCase(null)]
        [TestCase("")]
        [TestCase("    ")]
        [TestCase("6")]
        public void Ctor_PathIsNullOrEmptyOrWhiteSpaceOrNotExist_ThrowArgumentException(string path) =>
            Assert.Throws<ArgumentException>(() => new FileSystemVisitor.FileSystemVisitor(path), "Path is null or empty, or white space, or not exist.");

        [Test]
        public void Ctor_PredicateIsNull_ArgumentNullException() =>
            Assert.Throws<ArgumentNullException>(() => new FileSystemVisitor.FileSystemVisitor(this.validPath, null), "Predicate is null.");

        [TestCaseSource(nameof(TestCases))]
        public void Search_Events(IEnumerable<string> directories, IEnumerable<string> files, IEnumerable<string> expected, EventCounters expectedEventCounters)
        {
            var mock = CreateMock(directories, files);
            var actual = mock.Object.Search();

            AssertActualEventCountersIsZero();
            CollectionAssert.AreEqual(expected, actual);
            AssertEventCounters(expectedEventCounters);
        }

        [TestCaseSource(nameof(TestCasesWithPredicate))]
        public void Search_Events_WithPredicate(IEnumerable<string> directories, IEnumerable<string> files, Predicate<string> predicate, IEnumerable<string> expected, EventCounters expectedEventCounters)
        {
            var mock = CreateMock(directories, files, predicate);
            var actual = mock.Object.Search();

            AssertActualEventCountersIsZero();
            CollectionAssert.AreEqual(expected, actual);
            AssertEventCounters(expectedEventCounters);
        }

        [TestCaseSource(nameof(TestCases))]
        public void Search_Events_Skip_5(IEnumerable<string> directories, IEnumerable<string> files, IEnumerable<string> expected, EventCounters expectedEventCounters)
        {
            int count = 5;
            expected = expected.Skip(count);
            var mock = CreateMock(directories, files);
            mock.Protected().Setup("OnDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback((FileSystemVisitorEventArgs e) => 
                {
                    actualEventCounters.DirectoriesFind++;
                    if (count-- > 0)
                        e.Skip = true;
                });
            mock.Protected().Setup("OnFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback((FileSystemVisitorEventArgs e) =>
                {
                    actualEventCounters.FilesFind++;
                    if (count-- > 0)
                        e.Skip = true;
                });

            var actual = mock.Object.Search();

            AssertActualEventCountersIsZero();
            CollectionAssert.AreEqual(expected, actual);
            AssertEventCounters(expectedEventCounters);
        }

        [TestCaseSource(nameof(TestCasesWithPredicate))]
        public void Search_Events_Skip_3_WithPredicate(IEnumerable<string> directories, IEnumerable<string> files, Predicate<string> predicate, IEnumerable<string> expected, EventCounters expectedEventCounters)
        {
            int count = 3;
            expected = expected.Skip(count);
            var mock = CreateMock(directories, files, predicate);
            mock.Protected().Setup("OnFilteredDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback((FileSystemVisitorEventArgs e) =>
                {
                    actualEventCounters.FilteredDirectoriesFind++;
                    if (count-- > 0)
                        e.Skip = true;
                });
            mock.Protected().Setup("OnFilteredFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback((FileSystemVisitorEventArgs e) =>
                {
                    actualEventCounters.FilteredFilesFind++;
                    if (count-- > 0)
                        e.Skip = true;
                });

            var actual = mock.Object.Search();

            AssertActualEventCountersIsZero();
            CollectionAssert.AreEqual(expected, actual);
            AssertEventCounters(expectedEventCounters);
        }

        [TestCaseSource(nameof(TestCases))]
        public void Search_Events_Abort_5(IEnumerable<string> directories, IEnumerable<string> files, IEnumerable<string> expected, EventCounters expectedEventCounters)
        {
            int count = 5;
            int eventsCount = count + 1;
            expected = expected.Take(count);
            expectedEventCounters.DirectoriesFind = Math.Min(expectedEventCounters.DirectoriesFind, eventsCount);
            expectedEventCounters.FilesFind = Math.Min(expectedEventCounters.FilesFind, eventsCount - expectedEventCounters.DirectoriesFind);

            var mock = CreateMock(directories, files);
            mock.Protected().Setup("OnDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback((FileSystemVisitorEventArgs e) =>
                {
                    actualEventCounters.DirectoriesFind++;
                    if (--count < 0)
                        e.Abort = true;
                });
            mock.Protected().Setup("OnFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback((FileSystemVisitorEventArgs e) =>
                {
                    actualEventCounters.FilesFind++;
                    if (--count < 0)
                        e.Abort = true;
                });

            var actual = mock.Object.Search();

            AssertActualEventCountersIsZero();
            CollectionAssert.AreEqual(expected, actual);
            AssertEventCounters(expectedEventCounters);
        }

        [TestCaseSource(nameof(TestCasesWithPredicate))]
        public void Search_Events_Abort_3_WithPredicate(IEnumerable<string> directories, IEnumerable<string> files, Predicate<string> predicate, IEnumerable<string> expected, EventCounters expectedEventCounters)
        {
            int count = 3;
            int eventsCount = count + 1;
            expected = expected.Take(count);
            expectedEventCounters.FilteredDirectoriesFind = Math.Min(expectedEventCounters.FilteredDirectoriesFind, eventsCount);
            expectedEventCounters.FilteredFilesFind = Math.Min(expectedEventCounters.FilteredFilesFind, eventsCount - expectedEventCounters.FilteredDirectoriesFind);

            var mock = CreateMock(directories, files, predicate);
            mock.Protected().Setup("OnFilteredDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback((FileSystemVisitorEventArgs e) =>
                {
                    actualEventCounters.FilteredDirectoriesFind++;
                    if (--count < 0)
                        e.Abort = true;
                });
            mock.Protected().Setup("OnFilteredFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback((FileSystemVisitorEventArgs e) =>
                {
                    actualEventCounters.FilteredFilesFind++;
                    if (--count < 0)
                        e.Abort = true;
                });

            var actual = mock.Object.Search();

            AssertActualEventCountersIsZero();
            CollectionAssert.AreEqual(expected, actual);

            expectedEventCounters.DirectoriesFind = actualEventCounters.DirectoriesFind;
            expectedEventCounters.FilesFind = actualEventCounters.FilesFind;
            AssertEventCounters(expectedEventCounters);
        }

        private void AssertActualEventCountersIsZero()
        {
            Assert.AreEqual(0, actualEventCounters.Start, "Start event counter before iteration. Search should be lazy.");
            Assert.AreEqual(0, actualEventCounters.Finish, "Finish event counter before iteration. Search should be lazy.");
            Assert.AreEqual(0, actualEventCounters.DirectoriesFind, "DirectoryFinded event counter before iteration. Search should be lazy.");
            Assert.AreEqual(0, actualEventCounters.FilesFind, "FileFinded event counter before iteration. Search should be lazy.");
            Assert.AreEqual(0, actualEventCounters.FilteredDirectoriesFind, "FilteredDirectoryFinded event counter before iteration. Search should be lazy.");
            Assert.AreEqual(0, actualEventCounters.FilteredFilesFind, "FilteredFileFinded event counter before iteration. Search should be lazy.");
        }

        private void AssertEventCounters(EventCounters expected)
        {
            Assert.AreEqual(expected.Start, actualEventCounters.Start, "Start event counter after iteration.");
            Assert.AreEqual(expected.Finish, actualEventCounters.Finish, "Finish event counter after iteration.");
            Assert.AreEqual(expected.DirectoriesFind, actualEventCounters.DirectoriesFind, "DirectoryFinded event counter after iteration.");
            Assert.AreEqual(expected.FilesFind, actualEventCounters.FilesFind, "FileFinded event counter after iteration.");
            Assert.AreEqual(expected.FilteredDirectoriesFind, actualEventCounters.FilteredDirectoriesFind, "FilteredDirectoryFinded event counter after iteration.");
            Assert.AreEqual(expected.FilteredFilesFind, actualEventCounters.FilteredFilesFind, "FilteredFileFinded event counter after iteration.");
        }

        private Mock<FileSystemVisitor.FileSystemVisitor> CreateMock(IEnumerable<string> directories, IEnumerable<string> files, Predicate<string> predicate = null)
        {
            var mock = predicate is null ? new Mock<FileSystemVisitor.FileSystemVisitor>(validPath) : new Mock<FileSystemVisitor.FileSystemVisitor>(validPath, predicate);
            mock.Protected().SetupSequence<IEnumerable<string>>("EnumerateDirectories", ItExpr.IsAny<string>())
                .Returns(directories);
            mock.Protected().SetupSequence<IEnumerable<string>>("EnumerateFiles", ItExpr.IsAny<string>())
                .Returns(files);

            mock.Protected().Setup("OnStart", ItExpr.IsAny<EventArgs>())
                .Callback(() => actualEventCounters.Start++);
            mock.Protected().Setup("OnFinish", ItExpr.IsAny<EventArgs>())
                .Callback(() => actualEventCounters.Finish++);
            mock.Protected().Setup("OnDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback(() => actualEventCounters.DirectoriesFind++);
            mock.Protected().Setup("OnFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback(() => actualEventCounters.FilesFind++);
            mock.Protected().Setup("OnFilteredDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback(() => actualEventCounters.FilteredDirectoriesFind++);
            mock.Protected().Setup("OnFilteredFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>())
                .Callback(() => actualEventCounters.FilteredFilesFind++);
            return mock;
        }

        internal class EventCounters
        {
            internal int Start;
            internal int Finish;
            internal int DirectoriesFind;
            internal int FilesFind;
            internal int FilteredDirectoriesFind;
            internal int FilteredFilesFind;

            public EventCounters()
            {
            }

            public EventCounters(IEnumerable<string> directories, IEnumerable<string> files)
                : this()
            {
                this.Start = 1;
                this.Finish = 1;
                this.DirectoriesFind = directories?.Count() ?? 0;
                this.FilesFind = files?.Count() ?? 0;
                this.FilteredDirectoriesFind = 0;
                this.FilteredFilesFind = 0;
            }

            public void Clear()
            {
                Start = 0;
                Finish = 0;
                DirectoriesFind = 0;
                FilesFind = 0;
                FilteredDirectoriesFind = 0;
                FilteredFilesFind = 0;
            }
        }
    }
}