namespace FSharpNamespacer.Models
{
    internal readonly struct RangeLine
    {
        public RangeLine(int start, int end, int versionNumber)
        {
            Start = start;
            End = end;
            VersionNumber = versionNumber;
        }

        public int Start { get; }
        public int End { get; }
        public int VersionNumber { get; }

        public static bool operator ==(RangeLine left, RangeLine right) =>
            left.Start == right.Start
            && left.End == right.End
            && left.VersionNumber == right.VersionNumber;

        public static bool operator !=(RangeLine left, RangeLine right) => !(left == right);

        public override bool Equals(object obj) => obj is RangeLine other && this == other;

        public override int GetHashCode() => (31 * (31 * VersionNumber + End) + Start);
    }
}
