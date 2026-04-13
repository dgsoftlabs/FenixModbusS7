using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AvalonDock.Layout;

namespace FenixWPF
{
    /// <summary>
    /// Interaction logic for FrEditorWPF.xaml
    /// </summary>
    public partial class Editor : UserControl
    {
        private CompletionWindow completionWindow;

        private string currentFileName;
        private FoldingManager foldingManager;
        private LayoutAnchorable Win;
        private ProjectContainer PrCon;
        private Guid Pr;
        private ElementKind ElKind;
        private string path_ = "";

        private object foldingStrategy;

        public Editor(ProjectContainer PrCon, Guid Pr, string path, ElementKind ElKind, LayoutAnchorable Win)
        {
            try
            {
                InitializeComponent();

                //Pokaz numery
                textEditor.ShowLineNumbers = true;

                //sciezka
                path_ = path;

                //rosrzerzenie
                string ext = Path.GetExtension(path_);

                if (ext == ".cs")
                {
                    textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                }
                else if (ext == ".html")
                {
                    textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("HTML");
                }
                else if (ext == ".js")
                {
                    textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
                }
                else if (ext == ".css")
                {
                    textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("CSS");
                }
                else
                {
                    textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
                }

                textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
                textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;

                DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
                foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
                foldingUpdateTimer.Tick += delegate { UpdateFoldings(); };
                foldingUpdateTimer.Start();

                //Otwarcie pliku
                textEditor.Text = File.ReadAllText(path_);

                Win.Closing += Win_Closing;

                PrCon.saveProjectEv += new EventHandler<ProjectEventArgs>(saveProjectEvent);
                textEditor.TextChanged += TextEditor_TextChanged;

                this.Win = Win;
                this.PrCon = PrCon;
                this.Pr = Pr;
                this.ElKind = ElKind;
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void TextEditor_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Win.Title.Contains("*"))
                    return;
                else
                    Win.Title = Win.Title + "*";
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(Ex, new ProjectEventArgs(Ex));
            }
        }

        private void saveProjectEvent(object sender, ProjectEventArgs ev)
        {
            try
            {
                if (!string.IsNullOrEmpty(path_))
                    textEditor.Save(path_);

                //Usuń '*'
                if (Win.Title.Contains("*"))
                    Win.Title = Win.Title.Remove(Win.Title.Length - 1);
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Win_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                PrCon.saveProjectEv -= new EventHandler<ProjectEventArgs>(saveProjectEvent);
                this.textEditor.TextChanged -= TextEditor_TextChanged;
                this.Win.Closing -= Win_Closing;
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FrEditorWPFSearch search = new FrEditorWPFSearch(textEditor);
                search.ShowDialog();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void saveFileClick(object sender, EventArgs e)
        {
            try
            {
                if (currentFileName == null)
                {
                    SaveFileDialog dlg = new SaveFileDialog();
                    dlg.DefaultExt = ".txt";
                    if (dlg.ShowDialog() ?? false)
                    {
                        currentFileName = dlg.FileName;
                    }
                    else
                    {
                        return;
                    }
                }

                textEditor.Save(currentFileName);
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            try
            {
                if (e.Text == ".")
                {
                    // open code completion after the user has pressed dot:
                    //completionWindow = new CompletionWindow(textEditor.TextArea);

                    // provide AvalonEdit with the data:
                    //IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                    //data.Add(new FrEditorWPFAux("SetTag"));
                    //data.Add(new FrEditorWPFAux("GetTag"));
                    //data.Add(new FrEditorWPFAux("Write"));
                    //completionWindow.Show();
                    //completionWindow.Closed += delegate
                    //{
                    //completionWindow = null;
                    // };
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            try
            {
                if (e.Text.Length > 0 && completionWindow != null)
                {
                    if (!char.IsLetterOrDigit(e.Text[0]))
                    {
                        // Whenever a non-letter is typed while the completion window is open,
                        // insert the currently selected element.
                        completionWindow.CompletionList.RequestInsertion(e);
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        #region Folding

        private void HighlightingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (textEditor.SyntaxHighlighting == null)
                {
                    foldingStrategy = null;
                }
                else
                {
                    switch (textEditor.SyntaxHighlighting.Name)
                    {
                        case "XML":
                            foldingStrategy = new XmlFoldingStrategy();
                            textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
                            break;

                        case "C#":
                        case "C++":
                        case "PHP":
                        case "Java":
                            textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy(textEditor.Options);
                            foldingStrategy = new BraceFoldingStrategy();
                            break;

                        default:
                            textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
                            foldingStrategy = null;
                            break;
                    }
                }
                if (foldingStrategy != null)
                {
                    if (foldingManager == null)
                        foldingManager = FoldingManager.Install(textEditor.TextArea);
                    UpdateFoldings();
                }
                else
                {
                    if (foldingManager != null)
                    {
                        FoldingManager.Uninstall(foldingManager);
                        foldingManager = null;
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void UpdateFoldings()
        {
            try
            {
                if (foldingStrategy is BraceFoldingStrategy)
                {
                    ((BraceFoldingStrategy)foldingStrategy).UpdateFoldings(foldingManager, textEditor.Document);
                }
                if (foldingStrategy is XmlFoldingStrategy)
                {
                    ((XmlFoldingStrategy)foldingStrategy).UpdateFoldings(foldingManager, textEditor.Document);
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        #endregion Folding

        #region ClipBoard

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextDocument document = new TextDocument(textEditor.SelectedText);
                IHighlightingDefinition highlightDefinition = HighlightingManager.Instance.GetDefinition(highlightingComboBox.Text);
                IHighlighter highlighter = new DocumentHighlighter(document, highlightDefinition);
                string html = HtmlClipboard.CreateHtmlFragment(document, highlighter, null, new HtmlOptions());
                System.Windows.Clipboard.SetText(html);
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        #endregion ClipBoard
    }

    /// <summary>
	/// Implements AvalonEdit ICompletionData interface to provide the entries in the completion drop down.
	/// </summary>
	public class EditorAux : ICompletionData
    {
        public EditorAux(string text)
        {
            this.Text = text;
        }

        public System.Windows.Media.ImageSource Image
        {
            get { return null; }
        }

        public string Text { get; private set; }

        // Use this property if you want to show a fancy UIElement in the drop down list.
        public object Content
        {
            get { return this.Text; }
        }

        public object Description
        {
            get { return "Description for " + this.Text; }
        }

        public double Priority
        { get { return 0; } }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text);
        }
    }

    /// <summary>
    /// Allows producing foldings from a document based on braces.
    /// </summary>
    public class BraceFoldingStrategy
    {
        /// <summary>
        /// Gets/Sets the opening brace. The default value is '{'.
        /// </summary>
        public char OpeningBrace { get; set; }

        /// <summary>
        /// Gets/Sets the closing brace. The default value is '}'.
        /// </summary>
        public char ClosingBrace { get; set; }

        /// <summary>
        /// Creates a new BraceFoldingStrategy.
        /// </summary>
        public BraceFoldingStrategy()
        {
            this.OpeningBrace = '{';
            this.ClosingBrace = '}';
        }

        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            int firstErrorOffset;
            IEnumerable<NewFolding> newFoldings = CreateNewFoldings(document, out firstErrorOffset);
            manager.UpdateFoldings(newFoldings, firstErrorOffset);
        }

        /// <summary>
        /// Create <see cref="NewFolding"/>s for the specified document.
        /// </summary>
        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
        {
            firstErrorOffset = -1;
            return CreateNewFoldings(document);
        }

        /// <summary>
        /// Create <see cref="NewFolding"/>s for the specified document.
        /// </summary>
        public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
        {
            List<NewFolding> newFoldings = new List<NewFolding>();

            Stack<int> startOffsets = new Stack<int>();
            int lastNewLineOffset = 0;
            char openingBrace = this.OpeningBrace;
            char closingBrace = this.ClosingBrace;
            for (int i = 0; i < document.TextLength; i++)
            {
                char c = document.GetCharAt(i);
                if (c == openingBrace)
                {
                    startOffsets.Push(i);
                }
                else if (c == closingBrace && startOffsets.Count > 0)
                {
                    int startOffset = startOffsets.Pop();
                    // don't fold if opening and closing brace are on the same line
                    if (startOffset < lastNewLineOffset)
                    {
                        newFoldings.Add(new NewFolding(startOffset, i + 1));
                    }
                }
                else if (c == '\n' || c == '\r')
                {
                    lastNewLineOffset = i + 1;
                }
            }
            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return newFoldings;
        }
    }
}