using System;
using System.Collections.Generic;

namespace FSharpNamespacer.Actions
{
    internal sealed class FsScopeActionBaseEqualityComparer : IEqualityComparer<FsScopeActionBase>
    {
        private static FsScopeActionBaseEqualityComparer _default;

        public static FsScopeActionBaseEqualityComparer Default
        {
            get
            {
                if (_default is null)
                {
                    _default = new FsScopeActionBaseEqualityComparer();
                }

                return _default;
            }
        }

        public bool Equals(FsScopeActionBase x, FsScopeActionBase y)
            => x.DisplayText.Equals(y.DisplayText, StringComparison.Ordinal);

        public int GetHashCode(FsScopeActionBase obj) => obj.GetHashCode();
    }
}
