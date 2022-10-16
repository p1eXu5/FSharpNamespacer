using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using FSharpNamespacer.Models;
using FluentAssertions;

namespace FSharpNamespacer.Tests.Models
{
    [TestClass]
    public class FsScopeTests
    {
        [TestMethod]
        public void TryCreate_RangeIsDefault_ReturnsTrue()
        {
            // Arrange:
            SnapshotSpan range = new SnapshotSpan();

            // Action:
            // Assert:
            FsScope.TryCreate(range, out var scope).Should().BeTrue();
        }

        [TestMethod]
        public void TrySetSuggestedFsModuleName_FileNameStartsWithFolderName_ReturnsTrue()
        {
            // Arrange:
            SnapshotSpan range = new SnapshotSpan();
            FsScope.TryCreate(range, out var fsScope);

            string fsProjectFilePath = "Z:\\Foo\\Foo.fsproj";
            string fsFilePath        = "Z:\\Foo\\Baz\\Baz.fs";

            // Action:
            // Assert:
            fsScope.TrySetSuggestedFsModuleName(fsProjectFilePath, fsFilePath).Should().BeTrue();
        }

        [TestMethod]
        public void TrySetSuggestedFsModuleName_FileNameStartsWithFolderName_SetsSuggestedFsModuleNameProperty()
        {
            // Arrange:
            SnapshotSpan range = new SnapshotSpan();
            FsScope.TryCreate(range, out var fsScope);

            string fsProjectFilePath = "Z:\\Foo\\Foo.fsproj";
            string fsFilePath        = "Z:\\Foo\\Bar\\Bar.Baz.fs";

            // Action:
            fsScope.TrySetSuggestedFsModuleName(fsProjectFilePath, fsFilePath);

            // Assert:
            fsScope.SuggestedFsModuleName.Should().BeEquivalentTo(new[] {"Foo", "Bar", "Baz"});
        }

        [TestMethod]
        [DataRow("Z:\\Foo\\Foo.fsproj", "Z:\\Foo\\Bar\\Baz.Tango.fs", new[] { "Foo", "Bar", "Baz", "Tango"  })]
        [DataRow("Z:\\Foo\\Foo.fsproj", "Z:\\Foo\\Bar\\Tango.fs", new[] { "Foo", "Bar", "Tango"  })]
        public void TrySetSuggestedFsModuleName_FileNameDoesNotStartWithFolderName_SetsSuggestedFsModuleNameProperty(
            string fsProjectFilePath,
            string fsFilePath,
            string[] expected
            )
        {
            // Arrange:
            SnapshotSpan range = new SnapshotSpan();
            FsScope.TryCreate(range, out var fsScope);

            // Action:
            fsScope.TrySetSuggestedFsModuleName(fsProjectFilePath, fsFilePath);

            // Assert:
            fsScope.SuggestedFsModuleName.Should().BeEquivalentTo(expected);
        }
    }
}
