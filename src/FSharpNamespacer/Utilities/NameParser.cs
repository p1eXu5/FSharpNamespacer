using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace FSharpNamespacer.Utilities
{
    internal static class NameParser
    {
        internal static bool TryGetNameSegments(
            ITextStructureNavigator navigator,
            SnapshotSpan rangeEnd,
            TextExtent running,
            out (Queue<string> nameSegments, bool hasEqualSign) result)
        {
            var nameSegments = new Queue<string>();
            bool isComment = false;
            bool isSpacedName = false;
            string runningText;

            StringBuilder sb = new StringBuilder();

            while (running.Span.End < rangeEnd.End)
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
                                nameSegments.Enqueue(sb.ToString());
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

                            nameSegments.Enqueue(sb.ToString());
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
                            isComment = false;
                            continue;

                        case ('/', '/') when !isSpacedName:
                            // Add any accumulated name before returning
                            if (sb.Length > 0)
                            {
                                nameSegments.Enqueue(sb.ToString());
                                sb.Clear();
                            }

                            if (nameSegments.Count > 0)
                            {
                                result = (nameSegments, false);
                                return true;
                            }

                            result = (nameSegments, default);
                            return false;

                        case ('(', '*') when !isSpacedName:
                            isComment = true;
                            continue;

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
                            nameSegments.Enqueue(sb.ToString());
                            sb.Clear();

                            continue;

                        case ('.', '`', '`') when !isSpacedName:
                            isSpacedName = !isSpacedName;
                            sb.Append('`').Append('`');

                            continue;

                        case ('`', '`', '=') when isSpacedName:
                            sb.Append('`').Append('`');
                            nameSegments.Enqueue(sb.ToString());
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
                                nameSegments.Enqueue(sb.ToString());
                                sb.Clear();
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
                            nameSegments.Enqueue(sb.ToString());
                            sb.Clear();
                            sb.Append('`').Append('`');

                            continue;

                        default:
                            sb.Append(running.Span.GetText());
                            continue;
                    }
                }
            }

            // Add any remaining accumulated name to the queue
            if (sb.Length > 0)
            {
                nameSegments.Enqueue(sb.ToString());
            }

            result = (nameSegments, false);
            return true;
        }
    }
}
