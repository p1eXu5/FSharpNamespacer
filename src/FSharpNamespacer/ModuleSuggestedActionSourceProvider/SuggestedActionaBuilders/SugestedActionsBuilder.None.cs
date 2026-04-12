using Microsoft.VisualStudio.Text;

#nullable enable

namespace FSharpNamespacer.ModuleSuggestedActionSourceProvider
{
    internal sealed partial class ModuleSuggestedActionSourceProvider
    {
        private sealed partial class AsyncSuggestedActionSource
        {
            private abstract partial class SuggestedActionsBuilder
            {
                internal sealed class None : SuggestedActionsBuilder
                {
                    public None(Span span, int versionNumber)
                        : base(BuilderType.None, span, versionNumber, -1)
                    { }
                }
            }
        }
    }
}
