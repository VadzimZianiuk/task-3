using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileSystemVisitorTests
{
    internal class TestCasesSource
    {
        private static readonly IEnumerable<string> Directories = Enumerable.Range(0, 10).Select(x => $"Directory #{x}");
        private static readonly IEnumerable<string> Files = Enumerable.Range(0, 10).Select(x => $"File #{x}");

        internal static IEnumerable<TestCaseData> TestCasesWithoutPredicate
        {
            get
            {
                yield return new TestCaseData(
                    Directories,
                    Files,
                    Directories.Concat(Files));
                yield return new TestCaseData(
                    Directories,
                    Enumerable.Empty<string>(),
                    Directories);
                yield return new TestCaseData(
                    Enumerable.Empty<string>(),
                    Files,
                    Files);
            }
        }

        internal static IEnumerable<TestCaseData> TestCasesWithPredicate
        {
            get
            {
                yield return new TestCaseData(
                    Directories,
                    Files,
                    (Predicate<string>)(x => true),
                    Directories.Concat(Files));
                yield return new TestCaseData(
                     Directories,
                     Files,
                     Delegate.Combine((Predicate<string>)(x => x.StartsWith("Directory")), (Predicate<string>)(x => true)),
                     Directories);
                yield return new TestCaseData(
                    Directories,
                    Files,
                    Delegate.Combine((Predicate<string>)(x => x.StartsWith("File")), (Predicate<string>)(x => true)),
                    Files.ToArray());
                yield return new TestCaseData(
                   Directories,
                   Files,
                   Delegate.Combine((Predicate<string>)(x => false), (Predicate<string>)(x => true)),
                   Array.Empty<string>());
            }
        }
    }
}
