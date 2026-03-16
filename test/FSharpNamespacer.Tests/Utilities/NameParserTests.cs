using System;
using System.Collections.Generic;
using FluentAssertions;
using FSharpNamespacer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using NSubstitute;

namespace FSharpNamespacer.Tests.Utilities
{
    [TestClass]
    public sealed class NameParserTests
    {
        [TestMethod]
        [DataRow("foo", new string[] { "foo" }, new string[] { "foo" }, false)]
        [DataRow("``foo bar``", new string[] { "``", "foo", " ", "bar", "``" }, new string[] { "``foo bar``" }, false)]
        [DataRow("``foo bar``", new string[] { "``", "foo", " ", "bar", "``" }, new string[] { "``foo bar``" }, false)]
        [DataRow("foo =", new string[] { "foo", " ", "=" }, new string[] { "foo" }, true)]
        [DataRow("foo.bar", new string[] { "foo", ".", "bar" }, new string[] { "foo", "bar" }, false)]
        [DataRow("foo.bar.baz", new string[] { "foo", ".", "bar", ".", "baz" }, new string[] { "foo", "bar", "baz" }, false)]
        [DataRow("foo.bar.baz = ", new string[] { "foo", ".", "bar", ".", "baz", " ", "=", " " }, new string[] { "foo", "bar", "baz" }, true)]
        public void TryGetNameSegments_Succeeded_ReturnsExpected(string text, string[] textExtents, string[] nameSegments, bool isEqualSign)
        {
            ITextSnapshot snapshot = CreateTextSnapshotMock(text);
            ITextStructureNavigator navigator = CreateNavigatorMock(snapshot, textExtents, text);
            SnapshotSpan rangeSpan = new SnapshotSpan(snapshot, 0, text.Length);

            // Start with empty initial extent to trigger parsing
            var initialExtent = new TextExtent(new SnapshotSpan(snapshot, 0, 0), isSignificant: false);

            // Act:
            bool result = NameParser.TryGetNameSegments(navigator, rangeSpan, initialExtent, out var output);

            // Assert:
            result.Should().BeTrue();
            output.hasEqualSign.Should().Be(isEqualSign);

            var segments = new List<string>();
            while (output.nameSegments.Count > 0)
            {
                segments.Add(output.nameSegments.Dequeue());
            }
            segments.Should().Equal(nameSegments);
        }

        [TestMethod]
        [DataRow("`foo`", (object)new string[] { "`", "foo", "`" })]
        [DataRow("// `foo`", (object)new string[] { "//", " ", "`", "foo", "`" })]
        public void TryGetNameSegments_Failed_ReturnsExpected(string text, string[] textExtents)
        {
            ITextSnapshot snapshot = CreateTextSnapshotMock(text);
            ITextStructureNavigator navigator = CreateNavigatorMock(snapshot, textExtents, text);
            SnapshotSpan rangeSpan = new SnapshotSpan(snapshot, 0, text.Length);

            // Start with empty initial extent to trigger parsing
            var initialExtent = new TextExtent(new SnapshotSpan(snapshot, 0, 0), isSignificant: false);

            // Act:
            bool result = NameParser.TryGetNameSegments(navigator, rangeSpan, initialExtent, out var output);

            // Assert:
            result.Should().BeFalse();
        }

        private static ITextSnapshot CreateTextSnapshotMock(string text)
        {
            // Create a snapshot mock that returns the text content
            var snapshot = Substitute.For<ITextSnapshot>();
            snapshot.Length.Returns(text.Length);
            snapshot.GetText().Returns(text);
            snapshot.GetText(Arg.Any<Span>()).Returns(x =>
            {
                var span = (Span)x[0];
                if (span.Start >= text.Length) return string.Empty;
                int length = Math.Min(span.Length, text.Length - span.Start);
                return text.Substring(span.Start, length);
            });
            return snapshot;
        }

        private ITextStructureNavigator CreateNavigatorMock(ITextSnapshot snapshot, string[] textExtents, string fullText)
        {
            var navigator = Substitute.For<ITextStructureNavigator>();
            var extentList = new List<TextExtent>();

            // Build extents from the text extents array
            int position = 0;
            foreach (var extent in textExtents)
            {
                if (position + extent.Length <= fullText.Length)
                {
                    var span = new SnapshotSpan(snapshot, position, extent.Length);
                    bool isSignificant = !string.IsNullOrEmpty(extent) && !char.IsWhiteSpace(extent[0]);
                    var textExtent = new TextExtent(span, isSignificant);
                    extentList.Add(textExtent);
                    position += extent.Length;
                }
            }

            int callCount = 0;
            // Setup mock to return extents in sequence, then return an extent at the end
            navigator.GetExtentOfWord(Arg.Any<SnapshotPoint>())
                .Returns(_ =>
                {
                    if (callCount < extentList.Count)
                    {
                        return extentList[callCount++];
                    }
                    // Return an empty extent at current position to stop iteration
                    int pos = Math.Min(position, fullText.Length);
                    if (pos < fullText.Length)
                    {
                        return new TextExtent(new SnapshotSpan(snapshot, pos, 0), false);
                    }
                    else
                    {
                        // Position is at the end, return empty span at end
                        return new TextExtent(new SnapshotSpan(snapshot, fullText.Length, 0), false);
                    }
                });

            return navigator;
        }
    }
}
