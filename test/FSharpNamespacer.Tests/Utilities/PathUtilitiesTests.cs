using System.Collections.Generic;
using FluentAssertions;
using FSharpNamespacer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable enable

namespace FSharpNamespacer.Tests.Utilities
{
    [TestClass]
    public sealed class PathUtilitiesTests
    {
        [TestMethod]
        // no project
        [DataRow(null, "C:\\Project\\src\\Foo\\Bar.fs", new[] { "Bar" })]
        [DataRow(null, "C:\\Project\\src\\Foo\\Bar.Baz.fs", new[] { "Bar", "Baz" })]
        // within project
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project\\src\\Foo\\Bar.fs", new[] { "Foo", "Bar" })]
        [DataRow("C:\\Project\\src\\Foo\\Foo.Baz.fsproj", "C:\\Project\\src\\Foo\\Bar.fs", new[] { "Foo", "Baz", "Bar" })]
        [DataRow("C:\\Project\\src\\Foo.Qux\\Foo.fsproj", "C:\\Project\\src\\Foo\\Bar\\Baz.fs", new[] { "Foo", "Bar", "Baz" })]
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project\\src\\Foo\\Bar\\Bar.Baz.fs", new[] { "Foo", "Bar", "Baz" })]
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project\\src\\Foo\\Bar\\Bar1.Baz.fs", new[] { "Foo", "Bar", "Bar1", "Baz" })]
        // different directory
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project2\\src\\Foo\\Zoo\\Bar.fs", new[] { "Bar" })]
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project2\\src\\Foo\\Zoo\\Zoo.Bar.fs", new[] { "Zoo", "Bar" })]
        public void GetRelativePathSegments_Tests(string? rootPath, string childItemPath, string[] expectedSegments)
        {
            Queue<string> segments = PathUtilities.GetRelativePathSegments(rootPath, childItemPath);
            segments.Should().BeEquivalentTo(expectedSegments);
        }
    }
}
