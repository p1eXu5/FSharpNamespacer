using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace FSharpNamespacer
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Module Suggested Actions")]
    [ContentType("F#")]
    public class ModuleSuggestedActionSourceProvider : ISuggestedActionsSourceProvider
    {
        [Import(typeof(ITextStructureNavigatorSelectorService))]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import(typeof(ITextDocumentFactoryService))]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import(typeof(SVsServiceProvider))]
		private IServiceProvider serviceProvider = null;


        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null || textView == null)
            {
                return null;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = serviceProvider.GetService(typeof(DTE)) as DTE;
            Assumes.Present(dte);

            var projectFileName = dte.ActiveDocument?.ProjectItem?.ContainingProject.FileName;

            if (string.IsNullOrWhiteSpace(projectFileName))
            {
                return null;
            }

            return new ModuleSuggestedActionSource(this, textView, textBuffer, projectFileName);
        }
    }
}
