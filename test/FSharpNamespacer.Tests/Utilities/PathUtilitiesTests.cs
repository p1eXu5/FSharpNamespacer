using System;
using System.Collections.Generic;
using FluentAssertions;
using FSharpNamespacer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using NSubstitute;

#nullable enable

namespace FSharpNamespacer.Tests.Utilities
{
    [TestClass]
    public sealed class PathUtilitiesTests
    {
        [TestMethod]
        [DataRow(null, "C:\\Project\\src\\Foo\\Bar.fs", new[] { "Bar" })]
        [DataRow(null, "C:\\Project\\src\\Foo\\Bar.Baz.fs", new[] { "Bar", "Baz" })]
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project\\src\\Foo\\Bar.fs", new[] { "Bar" })]
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project\\src\\Foo\\Bar\\Baz.fs", new[] { "Bar", "Baz" })]
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project\\src\\Foo\\Bar\\Bar.Baz.fs", new[] { "Bar", "Baz" })]
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project\\src\\Foo\\Bar\\Bar1.Baz.fs", new[] { "Bar", "Bar1", "Baz" })]
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project2\\src\\Foo\\Zoo\\Bar.fs", new[] { "Bar" })]
        [DataRow("C:\\Project\\src\\Foo\\Foo.fsproj", "C:\\Project2\\src\\Foo\\Zoo\\Zoo.Bar.fs", new[] { "Zoo", "Bar" })]
        public void GetRelativePathSegments_Tests(string? rootPath, string childItemPath, string[] expectedSegments)
        {
            Queue<string> segments = PathUtilities.GetRelativePathSegments(rootPath, childItemPath);
            segments.Should().BeEquivalentTo(expectedSegments);
        }
    }
}
