using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using FSharpNamespacer.Models;
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
        public static IEnumerable<object[]> GetData()
        {
            yield return new object[] { "foo", new string[] { "foo" }, new object[] { CodeCommentType.Code, "foo" }, false };
            yield return new object[] { "``foo bar``", new string[] { "``", "foo", " ", "bar", "``" }, new object[] { CodeCommentType.Code, "``foo bar``" }, false };
            yield return new object[] { "``foo bar``", new string[] { "``", "foo", " ", "bar", "``" }, new object[] { CodeCommentType.Code, "``foo bar``" }, false };
            yield return new object[] { "foo =", new string[] { "foo", " ", "=" }, new object[] { CodeCommentType.Code, "foo" }, true };
            yield return new object[] { "foo.bar", new string[] { "foo", ".", "bar" }, new object[] { CodeCommentType.Code, "foo", CodeCommentType.Code, "bar" }, false };
            yield return new object[] { "foo.bar.baz", new string[] { "foo", ".", "bar", ".", "baz" }, new object[] { CodeCommentType.Code, "foo", CodeCommentType.Code, "bar", CodeCommentType.Code, "baz" }, false };
            yield return new object[] { "foo.bar.baz = ", new string[] { "foo", ".", "bar", ".", "baz", " ", "=", " " }, new object[] { CodeCommentType.Code, "foo", CodeCommentType.Code, "bar", CodeCommentType.Code, "baz" }, true };
            yield return new object[] { "foo // some comment", new string[] { "foo", " ", "//", " ", "some", " ", "comment" }, new object[] { CodeCommentType.Code, "foo", CodeCommentType.TerminateComment, "// some comment" }, false };
            yield return new object[] { "foo (* some comment *)", new string[] { "foo", " ", "(*", " ", "some", " ", "comment", " ", "*)" }, new object[] { CodeCommentType.Code, "foo", CodeCommentType.InlineComment, "(* some comment *)" }, false };
            yield return new object[] { "(* some comment *) foo", new string[] { "(*", " ", "some", " ", "comment", " ", "*)", " ", "foo" }, new object[] { CodeCommentType.InlineComment, "(* some comment *)", CodeCommentType.Code, "foo" }, false };
        }

        public static string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            return $"{methodInfo.Name} with value '{data[0]}'";
        }

        [TestMethod]
        [DynamicData(nameof(GetData), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetDisplayName))]
        public void TryGetNameSegments_Succeeded_ReturnsExpected(string text, string[] textExtents, object[] nameSegments, bool isEqualSign)
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

            Queue<(CodeCommentType, string)> expectedOutput = new Queue<(CodeCommentType, string)>();
            for (var i = 0; i < nameSegments.Length; i++)
            {
                expectedOutput.Enqueue(((CodeCommentType)nameSegments[i], (string)nameSegments[++i]));
            }

            output.nameSegments.Should().BeEquivalentTo(expectedOutput);
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
