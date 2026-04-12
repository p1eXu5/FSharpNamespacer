using System;
using System.ComponentModel.Composition;
using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace FSharpNamespacer.ModuleSuggestedActionSourceProvider
{
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Export(typeof(ModuleSuggestedActionSourceProvider))]
    [Name("Module Suggested Actions")]
    [ContentType("F#")]
    internal sealed partial class ModuleSuggestedActionSourceProvider : ISuggestedActionsSourceProvider
    {
        private ITextStructureNavigatorSelectorService NavigatorService { get; }

        private ITextDocumentFactoryService TextDocumentFactoryService { get; }

        private ISuggestedActionCategoryRegistryService SuggestedActionCategoryRegistryService { get; }

        private IClassifierAggregatorService ClassifierAggregatorService { get; }

        private IServiceProvider ServiceProvider { get; }

        [ImportingConstructor]
        public ModuleSuggestedActionSourceProvider(
            ITextStructureNavigatorSelectorService navigatorService,
            ITextDocumentFactoryService textDocumentFactoryService,
            ISuggestedActionCategoryRegistryService suggestedActionCategoryRegistryService,
            IClassifierAggregatorService classifierAggregatorService,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider
        )
        {
            NavigatorService = navigatorService;
            TextDocumentFactoryService = textDocumentFactoryService;
            SuggestedActionCategoryRegistryService = suggestedActionCategoryRegistryService;
            ClassifierAggregatorService = classifierAggregatorService;
            ServiceProvider = serviceProvider;
        }

#nullable enable

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Calls every time when solution is loading, file is opened but bot switched.
        /// </para>
        /// </summary>
        /// <param name="textView"></param>
        /// <param name="textBuffer"></param>
        /// <returns></returns>
        public ISuggestedActionsSource? CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            Requires.NotNull(textView, nameof(textView));
            Requires.NotNull(textBuffer, nameof(textView));

            if (!TextDocumentFactoryService.TryGetTextDocument(textBuffer, out ITextDocument? textDocument))
            {
                return null;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            DTE? dte = ServiceProvider.GetService(typeof(DTE)) as DTE;
            Assumes.Present(dte);

            if (dte.ActiveDocument is null)
            {
                return null;
            }

            string? projectFileFullPath = dte.ActiveDocument.ProjectItem?.ContainingProject.FileName;
            // FileName
            // var sourceFile = dte.ActiveDocument?.Name;
            //var sourceFileFullPath = textDocument.FilePath;

            Document doc = dte.ActiveDocument;

            int tabSize = doc.TabSize;
            int indentSize = doc.IndentSize;

            return new AsyncSuggestedActionSource(
                this,
                textView,
                textBuffer,
                textDocument,
                projectFileFullPath,
                indentSize);
        }

#nullable restore
    }
}
