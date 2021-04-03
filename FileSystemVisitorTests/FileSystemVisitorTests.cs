using FSVisitor;
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
        private readonly string ValidPath = Directory.GetCurrentDirectory();

        [Test]
        public void Ctor_OneParam() => Assert.DoesNotThrow(() => new FileSystemVisitor(ValidPath));

        [Test]
        public void Ctor_TwoParams() => Assert.DoesNotThrow(() => new FileSystemVisitor(ValidPath, x => true));

        [TestCase(null)]
        [TestCase("")]
        [TestCase("    ")]
        [TestCase("6")]
        public void Ctor_PathIsNullOrEmptyOrWhiteSpaceOrNotExist_ThrowArgumentException(string path) =>
            Assert.Throws<ArgumentException>(() => new FileSystemVisitor(path), "Path is null or empty, or white space, or not exist.");

        [Test]
        public void Ctor_PredicateIsNull_ArgumentNullException() =>
            Assert.Throws<ArgumentNullException>(() => new FileSystemVisitor(ValidPath, null), "Predicate is null.");

        [TestCaseSource(typeof(TestCasesSource), nameof(TestCasesSource.TestCasesWithoutPredicate))]
        public void Search_Events_WithoutPredicate(IEnumerable<string> directories, IEnumerable<string> files, IEnumerable<string> expected)
        {
            var mock = new Mock<FileSystemVisitor>(ValidPath);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateDirectories", ItExpr.IsAny<string>()).
                Returns(directories);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateFiles", ItExpr.IsAny<string>()).
                Returns(files);

            int start = 0;
            int finish = 0;
            int directoryFinded = 0;
            int fileFinded = 0;
            int filteredDirectoryFinded = 0;
            int filteredFileFinded = 0;
            mock.Protected().Setup("OnStart", ItExpr.IsAny<EventArgs>()).Callback(() => start++);
            mock.Protected().Setup("OnFinish", ItExpr.IsAny<EventArgs>()).Callback(() => finish++);
            mock.Protected().Setup("OnDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => directoryFinded++);
            mock.Protected().Setup("OnFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => fileFinded++);
            mock.Protected().Setup("OnFilteredDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => filteredDirectoryFinded++);
            mock.Protected().Setup("OnFilteredFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => filteredFileFinded++);

            var actual = mock.Object.Search();

            Assert.AreEqual(0, start, "events start count before run");
            Assert.AreEqual(0, finish, "events finish count before run");
            Assert.AreEqual(0, directoryFinded, "events directoryFinded count before run");
            Assert.AreEqual(0, fileFinded, "events fileFinded count before run");
            Assert.AreEqual(0, filteredDirectoryFinded, "events filteredDirectoryFinded count before run");
            Assert.AreEqual(0, filteredFileFinded, "events filteredFileFinded count before run");
            CollectionAssert.AreEquivalent(expected, actual);
            Assert.AreEqual(1, start, "events start count after run");
            Assert.AreEqual(1, finish, "events finish count after run");
            Assert.AreEqual(directories.Count(), directoryFinded, "events directoryFinded count after run");
            Assert.AreEqual(files.Count(), fileFinded, "events fileFinded count after run");
            Assert.AreEqual(0, filteredDirectoryFinded, "events filteredDirectoryFinded count after run");
            Assert.AreEqual(0, filteredFileFinded, "events filteredFileFinded count after run");
        }

        [TestCaseSource(typeof(TestCasesSource), nameof(TestCasesSource.TestCasesWithPredicate))]
        public void Search_Events_WithPredicate(IEnumerable<string> directories, IEnumerable<string> files, Predicate<string> predicate, IEnumerable<string> expected)
        {
            var mock = new Mock<FileSystemVisitor>(ValidPath, predicate);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateDirectories", ItExpr.IsAny<string>()).
                Returns(directories);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateFiles", ItExpr.IsAny<string>()).
                Returns(files);

            int start = 0;
            int finish = 0;
            int directoryFinded = 0;
            int fileFinded = 0;
            int filteredDirectoryFinded = 0;
            int filteredFileFinded = 0;
            mock.Protected().Setup("OnStart", ItExpr.IsAny<EventArgs>()).Callback(() => start++);
            mock.Protected().Setup("OnFinish", ItExpr.IsAny<EventArgs>()).Callback(() => finish++);
            mock.Protected().Setup("OnDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => directoryFinded++);
            mock.Protected().Setup("OnFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => fileFinded++);
            mock.Protected().Setup("OnFilteredDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => filteredDirectoryFinded++);
            mock.Protected().Setup("OnFilteredFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => filteredFileFinded++);

            var actual = mock.Object.Search();

            Assert.AreEqual(0, start, "events start count before run");
            Assert.AreEqual(0, finish, "events finish count before run");
            Assert.AreEqual(0, directoryFinded, "events directoryFinded count before run");
            Assert.AreEqual(0, fileFinded, "events fileFinded count before run");
            Assert.AreEqual(0, filteredDirectoryFinded, "events filteredDirectoryFinded count before run");
            Assert.AreEqual(0, filteredFileFinded, "events filteredFileFinded count before run");
            CollectionAssert.AreEquivalent(expected, actual);
            Assert.AreEqual(1, start, "events start count after run");
            Assert.AreEqual(1, finish, "events finish count after run");
            Assert.AreEqual(directories.Count(), directoryFinded, "events directoryFinded count after run");
            Assert.AreEqual(files.Count(), fileFinded, "events fileFinded count after run");
            Assert.AreEqual(expected.Count(), filteredDirectoryFinded + filteredFileFinded, $"events filteredDirectoryFinded + filteredFileFinded count after run");
        }

        [TestCaseSource(typeof(TestCasesSource), nameof(TestCasesSource.TestCasesWithoutPredicate))]
        public void Search_Events_Skip_WithoutPredicate(IEnumerable<string> directories, IEnumerable<string> files, IEnumerable<string> expected)
        {
            expected = Enumerable.Empty<string>();
            var mock = new Mock<FileSystemVisitor>(ValidPath);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateDirectories", ItExpr.IsAny<string>()).
                Returns(directories);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateFiles", ItExpr.IsAny<string>()).
                Returns(files);

            int start = 0;
            int finish = 0;
            int directoryFinded = 0;
            int fileFinded = 0;
            int filteredDirectoryFinded = 0;
            int filteredFileFinded = 0;
            mock.Protected().Setup("OnStart", ItExpr.IsAny<EventArgs>()).Callback(() => start++);
            mock.Protected().Setup("OnFinish", ItExpr.IsAny<EventArgs>()).Callback(() => finish++);
            mock.Protected().Setup("OnFilteredDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => filteredDirectoryFinded++);
            mock.Protected().Setup("OnFilteredFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => filteredFileFinded++);
            mock.Protected().Setup("OnDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).
                Callback((FileSystemVisitorEventArgs e) =>
                {
                    directoryFinded++;
                    e.Skip = true;
                });
            mock.Protected().Setup("OnFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).
                Callback((FileSystemVisitorEventArgs e) =>
                {
                    fileFinded++;
                    e.Skip = true;
                });

            var actual = mock.Object.Search();

            Assert.AreEqual(0, start, "events start count before run");
            Assert.AreEqual(0, finish, "events finish count before run");
            Assert.AreEqual(0, directoryFinded, "events directoryFinded count before run");
            Assert.AreEqual(0, fileFinded, "events fileFinded count before run");
            Assert.AreEqual(0, filteredDirectoryFinded, "events filteredDirectoryFinded count before run");
            Assert.AreEqual(0, filteredFileFinded, "events filteredFileFinded count before run");
            CollectionAssert.AreEquivalent(expected, actual);
            Assert.AreEqual(1, start, "events start count after run");
            Assert.AreEqual(1, finish, "events finish count after run");
            Assert.AreEqual(directories.Count(), directoryFinded, $"events directoryFinded count after run");
            Assert.AreEqual(files.Count(), fileFinded, $"events fileFinded count after run");
            Assert.AreEqual(0, filteredDirectoryFinded, "events filteredDirectoryFinded count after run");
            Assert.AreEqual(0, filteredFileFinded, "events filteredFileFinded count after run");
        }

        [TestCaseSource(typeof(TestCasesSource), nameof(TestCasesSource.TestCasesWithPredicate))]
        public void Search_Events_Skip_WithPredicate(IEnumerable<string> directories, IEnumerable<string> files, Predicate<string> predicate, IEnumerable<string> expected)
        {
            int expectedCount = expected.Count();
            expected = Enumerable.Empty<string>();
            var mock = new Mock<FileSystemVisitor>(ValidPath, predicate);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateDirectories", ItExpr.IsAny<string>()).
                Returns(directories);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateFiles", ItExpr.IsAny<string>()).
                Returns(files);

            int start = 0;
            int finish = 0;
            int directoryFinded = 0;
            int fileFinded = 0;
            int filteredDirectoryFinded = 0;
            int filteredFileFinded = 0;
            mock.Protected().Setup("OnStart", ItExpr.IsAny<EventArgs>()).Callback(() => start++);
            mock.Protected().Setup("OnFinish", ItExpr.IsAny<EventArgs>()).Callback(() => finish++);
            mock.Protected().Setup("OnDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => directoryFinded++);
            mock.Protected().Setup("OnFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => fileFinded++);
            mock.Protected().Setup("OnFilteredDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).
                Callback((FileSystemVisitorEventArgs e) =>
                {
                    filteredDirectoryFinded++;
                    e.Skip = true;
                });
            mock.Protected().Setup("OnFilteredFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).
                Callback((FileSystemVisitorEventArgs e) =>
                {
                    filteredFileFinded++;
                    e.Skip = true;
                });


            var actual = mock.Object.Search();

            Assert.AreEqual(0, start, "events start count before run");
            Assert.AreEqual(0, finish, "events finish count before run");
            Assert.AreEqual(0, directoryFinded, "events directoryFinded count before run");
            Assert.AreEqual(0, fileFinded, "events fileFinded count before run");
            Assert.AreEqual(0, filteredDirectoryFinded, "events filteredDirectoryFinded count before run");
            Assert.AreEqual(0, filteredFileFinded, "events filteredFileFinded count before run");
            CollectionAssert.AreEquivalent(expected, actual);
            Assert.AreEqual(1, start, "events start count after run");
            Assert.AreEqual(1, finish, "events finish count after run");
            Assert.AreEqual(directories.Count(), directoryFinded, $"events directoryFinded count after run");
            Assert.AreEqual(files.Count(), fileFinded, $"events fileFinded count after run");
            Assert.AreEqual(expectedCount, filteredDirectoryFinded + filteredFileFinded, $"events filteredDirectoryFinded + filteredFileFinded count after run");
        }

        [TestCaseSource(typeof(TestCasesSource), nameof(TestCasesSource.TestCasesWithoutPredicate))]
        public void Search_Events_Abort_WithoutPredicate(IEnumerable<string> directories, IEnumerable<string> files, IEnumerable<string> _)
        {
            var mock = new Mock<FileSystemVisitor>(ValidPath);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateDirectories", ItExpr.IsAny<string>()).
                Returns(directories);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateFiles", ItExpr.IsAny<string>()).
                Returns(files);

            int start = 0;
            int finish = 0;
            int directoryFinded = 0;
            int fileFinded = 0;
            int filteredDirectoryFinded = 0;
            int filteredFileFinded = 0;
            mock.Protected().Setup("OnStart", ItExpr.IsAny<EventArgs>()).Callback(() => start++);
            mock.Protected().Setup("OnFinish", ItExpr.IsAny<EventArgs>()).Callback(() => finish++);
            mock.Protected().Setup("OnFilteredDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => filteredDirectoryFinded++);
            mock.Protected().Setup("OnFilteredFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).Callback(() => filteredFileFinded++);
            mock.Protected().Setup("OnDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).
                Callback((FileSystemVisitorEventArgs e) =>
                {
                    directoryFinded++;
                    e.Abort = true;
                });
            mock.Protected().Setup("OnFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).
                Callback((FileSystemVisitorEventArgs e) =>
                {
                    fileFinded++;
                    e.Abort = true;
                });

            var actual = mock.Object.Search();

            Assert.AreEqual(0, start, "events start count before run");
            Assert.AreEqual(0, finish, "events finish count before run");
            Assert.AreEqual(0, directoryFinded, "events directoryFinded count before run");
            Assert.AreEqual(0, fileFinded, "events fileFinded count before run");
            Assert.AreEqual(0, filteredDirectoryFinded, "events filteredDirectoryFinded count before run");
            Assert.AreEqual(0, filteredFileFinded, "events filteredFileFinded count before run");
            CollectionAssert.AreEquivalent(Enumerable.Empty<string>(), actual);
            Assert.AreEqual(1, start, "events start count after run");
            Assert.AreEqual(1, finish, "events finish count after run");
            int expectedDirectoryFinded = directoryFinded > 0 ? 1 : 0;
            int expectedfilesFinded = directoryFinded > 0 ? 0 : 1;
            Assert.AreEqual(expectedDirectoryFinded, directoryFinded, $"events directoryFinded count after run");
            Assert.AreEqual(expectedfilesFinded, fileFinded, $"events fileFinded count after run");
            Assert.AreEqual(0, filteredDirectoryFinded, "events filteredDirectoryFinded count after run");
            Assert.AreEqual(0, filteredFileFinded, "events filteredFileFinded count after run");
        }

        [TestCaseSource(typeof(TestCasesSource), nameof(TestCasesSource.TestCasesWithPredicate))]
        public void Search_Events_Abort_WithPredicate(IEnumerable<string> directories, IEnumerable<string> files, Predicate<string> predicate, IEnumerable<string> _)
        {
            var mock = new Mock<FileSystemVisitor>(ValidPath, predicate);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateDirectories", ItExpr.IsAny<string>()).
                Returns(directories);
            mock.Protected().
                SetupSequence<IEnumerable<string>>("EnumerateFiles", ItExpr.IsAny<string>()).
                Returns(files);

            int start = 0;
            int finish = 0;
            int filteredDirectoryFinded = 0;
            int filteredFileFinded = 0;
            mock.Protected().Setup("OnStart", ItExpr.IsAny<EventArgs>()).Callback(() => start++);
            mock.Protected().Setup("OnFinish", ItExpr.IsAny<EventArgs>()).Callback(() => finish++);
            mock.Protected().Setup("OnFilteredDirectoryFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).
                Callback((FileSystemVisitorEventArgs e) =>
                {
                    filteredDirectoryFinded++;
                    e.Abort = true;
                });
            mock.Protected().Setup("OnFilteredFileFinded", ItExpr.IsAny<FileSystemVisitorEventArgs>()).
                Callback((FileSystemVisitorEventArgs e) =>
                {
                    filteredFileFinded++;
                    e.Abort = true;
                });


            var actual = mock.Object.Search();

            Assert.AreEqual(0, start, "events start count before run");
            Assert.AreEqual(0, finish, "events finish count before run");
            Assert.AreEqual(0, filteredDirectoryFinded, "events filteredDirectoryFinded count before run");
            Assert.AreEqual(0, filteredFileFinded, "events filteredFileFinded count before run");
            CollectionAssert.AreEquivalent(Enumerable.Empty<string>(), actual);
            Assert.AreEqual(1, start, "events start count after run");
            Assert.AreEqual(1, finish, "events finish count after run");
            int expectedDirectoryFinded = filteredDirectoryFinded > 0 ? 1 : 0;
            int expectedfilesFinded = filteredDirectoryFinded > 0 ? 0 : filteredFileFinded;
            Assert.AreEqual(expectedDirectoryFinded, filteredDirectoryFinded, "events filteredDirectoryFinded count after run");
            Assert.IsTrue(expectedfilesFinded <= 1, "events filteredFileFinded count after run");
        }
    }
}