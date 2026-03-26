using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSharpNamespacer.Models;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace FSharpNamespacer.Utilities
{
    internal static class NameParser
    {
        internal static bool TryGetNameSegments(
            ITextStructureNavigator navigator,
            SnapshotSpan range,
            TextExtent running,
            out (Queue<(CodeCommentType, string)> nameSegments, bool hasEqualSign) result)
        {
            var nameSegments = new Queue<(CodeCommentType, string)>();
            bool isComment = false;
            bool isSpacedName = false;
            string runningText;

            StringBuilder sb = new StringBuilder();

            while (running.Span.End < range.End)
            {
                running = navigator.GetExtentOfWord(running.Span.End);
                bool isSignificant = running.IsSignificant;

                if (!isComment)
                {
                    if (!isSignificant)
                    {
                        if (!isSpacedName)
                        {
                            continue;
                        }

                        sb.Append(running.Span.GetText());
                        continue;
                    }
                }
                else
                {
                    continue;
                }

                runningText = running.Span.GetText();

                if (running.Span.Length == 1)
                {
                    if (!isSpacedName)
                    {
                        if (runningText[0] == '=')
                        {
                            // Add any accumulated name before returning
                            if (sb.Length > 0)
                            {
                                nameSegments.Enqueue((CodeCommentType.Code, sb.ToString()));
                            }
                            result = (nameSegments, true);
                            return true;
                        }

                        if (runningText[0] == '.')
                        {
                            if (sb.Length == 0)
                            {
                                result = (nameSegments, false);
                                return false;
                            }

                            nameSegments.Enqueue((CodeCommentType.Code, sb.ToString()));
                            sb.Clear();
                            continue;
                        }

                        if (runningText[0] == '`')
                        {
                            result = (nameSegments, default);
                            return false;
                        }
                    }

                    sb.Append(runningText);
                    continue;
                }

                if (running.Span.Length == 2)
                {
                    switch ((runningText[0], runningText[1]))
                    {
                        case ('*', ')') when !isSpacedName:
                            sb.Append('*').Append(')');
                            nameSegments.Enqueue((CodeCommentType.InlineComment, sb.ToString()));
                            sb.Clear();

                            isComment = false;
                            
                            continue;

                        case ('/', '/') when !isSpacedName:
                            // Add any accumulated name before returning
                            if (sb.Length > 0)
                            {
                                nameSegments.Enqueue((CodeCommentType.Code, sb.ToString()));
                                sb.Clear();
                            }

                            if (nameSegments.Count > 0)
                            {
                                sb.Append('/').Append('/');
                                while (running.Span.End < range.End)
                                {
                                    running = navigator.GetExtentOfWord(running.Span.End);
                                    sb.Append(running.Span.GetText());
                                }

                                nameSegments.Enqueue((CodeCommentType.TerminateComment, sb.ToString()));
                                result = (nameSegments, false);
                                return true;
                            }

                            result = (nameSegments, default);
                            return false;

                        case ('(', '*') when !isSpacedName:
                            if (sb.Length > 0)
                            {
                                nameSegments.Enqueue((CodeCommentType.Code, sb.ToString()));
                                sb.Clear();
                            }

                            sb.Append('(').Append('*');
                            running = navigator.GetExtentOfWord(running.Span.End);
                            var text = running.Span.GetText();

                            while (!(text.Length >= 2 && text[text.Length - 2] == '*' && text[text.Length - 1] == ')') && running.Span.End < range.End)
                            {
                                sb.Append(text);
                                running = navigator.GetExtentOfWord(running.Span.End);
                                text = running.Span.GetText();
                            }

                            if (text.Length >= 2 && text[text.Length - 2] == '*' && text[text.Length - 1] == ')')
                            {
                                sb.Append(text);
                                nameSegments.Enqueue((CodeCommentType.InlineComment, sb.ToString()));
                                sb.Clear();

                                continue;
                            }

                            sb.Append('*').Append(')');
                            nameSegments.Enqueue((CodeCommentType.InlineComment, sb.ToString()));
                            sb.Clear();

                            if (nameSegments.Any(t => t.Item1 == CodeCommentType.Code))
                            {
                                result = (nameSegments, false);
                                return true;
                            }

                            result = (nameSegments, false);
                            return false;

                        case ('`', '`'):
                            isSpacedName = !isSpacedName;
                            sb.Append(runningText);
                            continue;

                        default:
                            sb.Append(runningText);
                            continue;
                    }
                }

                if (running.Span.Length == 3)
                {
                    switch ((runningText[0], runningText[1], runningText[2]))
                    {
                        case ('`', '`', '.') when isSpacedName:
                            isSpacedName = !isSpacedName;
                            sb.Append('`').Append('`');
                            nameSegments.Enqueue((CodeCommentType.Code, sb.ToString()));
                            sb.Clear();

                            continue;

                        case ('.', '`', '`') when !isSpacedName:
                            isSpacedName = !isSpacedName;
                            sb.Append('`').Append('`');

                            continue;

                        case ('`', '`', '=') when isSpacedName:
                            sb.Append('`').Append('`');
                            nameSegments.Enqueue((CodeCommentType.Code, sb.ToString()));
                            sb.Clear();
                            result = (nameSegments, true);

                            return true;

                        default:
                            sb.Append(running.Span.GetText());
                            continue;
                    }
                }

                if (running.Span.Length == 4)
                {
                    switch ((runningText[0], runningText[1], runningText[2], runningText[3]))
                    {
                        case ('`', '`', '/', '/') when isSpacedName:
                            sb.Append('`').Append('`');

                            if (sb.Length > 4)
                            {
                                nameSegments.Enqueue((CodeCommentType.Code, sb.ToString()));
                                sb.Clear();

                                sb.Append('/').Append('/');
                                while (running.Span.End < range.End)
                                {
                                    running = navigator.GetExtentOfWord(running.Span.End);
                                    sb.Append(running.Span.GetText());
                                }

                                nameSegments.Enqueue((CodeCommentType.TerminateComment, sb.ToString()));
                                result = (nameSegments, false);

                                return true;
                            }

                            result = (nameSegments, false);
                            return false;

                        default:
                            sb.Append(running.Span.GetText());
                            continue;
                    }
                }

                if (running.Span.Length == 5)
                {
                    switch ((runningText[0], runningText[1], runningText[2], runningText[3], runningText[4]))
                    {
                        case ('`', '`', '.', '`', '`') when isSpacedName:
                            isSpacedName = !isSpacedName;
                            sb.Append('`').Append('`');
                            nameSegments.Enqueue((CodeCommentType.Code, sb.ToString()));
                            sb.Clear();
                            sb.Append('`').Append('`');

                            continue;

                        default:
                            sb.Append(running.Span.GetText());
                            continue;
                    }
                }

                sb.Append(runningText);
            }

            // Add any remaining accumulated name to the queue
            if (sb.Length > 0)
            {
                nameSegments.Enqueue((CodeCommentType.Code, sb.ToString()));
            }

            result = (nameSegments, false);
            return true;
        }
    }
}
