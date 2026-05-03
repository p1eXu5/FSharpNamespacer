using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Utilities;

namespace FSharpNamespacer.Utilities
{
    internal static class LogUtilities
    {
        internal static void LogDebug(
                string message,
                [CallerFilePath] string filePath = "",
                [CallerLineNumber] int lineNumber = 0,
                [CallerMemberName] string memberName = "")
        {
#if DEBUG
            var sb = PooledStringBuilder.GetInstance();
            string log = sb.Builder
                .Append('[').Append(filePath).Append(']').Append(' ')
                .Append(memberName).Append(' ')
                .Append('(').Append('#').Append(lineNumber).Append(')').Append(':').AppendLine()
                .AppendLine(message)
                .ToString();

            Debug.WriteLine(log);

            sb.Free();
#endif
        }
    }
}
