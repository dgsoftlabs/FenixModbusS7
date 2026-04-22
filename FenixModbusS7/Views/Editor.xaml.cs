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
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AvalonDock.Layout;

namespace Fenix
{
    public partial class Editor : UserControl
    {
        private CompletionWindow completionWindow;
        private FoldingManager foldingManager;
        private LayoutAnchorable Win;
        private ProjectContainer PrCon;
        private Guid Pr;
        private ElementKind ElKind;
        private string path_ = "";
        private object foldingStrategy;
        private List<ScriptFunctionCategory> allCategories_;
        private double fontSizePt_ = 10;

        public Editor(ProjectContainer PrCon, Guid Pr, string path, ElementKind ElKind, LayoutAnchorable Win)
        {
            try
            {
                InitializeComponent();

                textEditor.ShowLineNumbers = true;
                path_ = path;

                fontSizePt_ = Math.Round(textEditor.FontSize * 72.0 / 96.0);
                UpdateFontSizeLabel();

                string ext = Path.GetExtension(path_);
                string langName = ext switch
                {
                    ".cs"   => "C#",
                    ".html" => "HTML",
                    ".js"   => "JavaScript",
                    ".css"  => "CSS",
                    ".xml"  => "XML",
                    _       => "C#"
                };

                textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(langName);
                InitializeFolding(langName);

                textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
                textEditor.TextArea.TextEntered  += textEditor_TextArea_TextEntered;
                textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
                textEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;

                DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
                foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
                foldingUpdateTimer.Tick += delegate { UpdateFoldings(); };
                foldingUpdateTimer.Start();

                textEditor.Text = File.ReadAllText(path_);

                Win.Closing += Win_Closing;
                PrCon.saveProjectEv += new EventHandler<ProjectEventArgs>(saveProjectEvent);
                textEditor.TextChanged += TextEditor_TextChanged;

                this.Win = Win;
                this.PrCon = PrCon;
                this.Pr = Pr;
                this.ElKind = ElKind;

                InitializeCatalog();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        #region Keyboard shortcuts

        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseSearchPanel();
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.S) { SaveFile(); e.Handled = true; }
                else if (e.Key == Key.F) { OpenSearchPanel(replaceMode: false); e.Handled = true; }
                else if (e.Key == Key.H) { OpenSearchPanel(replaceMode: true); e.Handled = true; }
            }

            if (e.Key == Key.F2) { ToggleCatalog(); e.Handled = true; }
        }

        #endregion

        #region Status bar

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            var caret = textEditor.TextArea.Caret;
            tbCaretInfo.Text = $"Ln {caret.Line}  Col {caret.Column}";
        }

        #endregion

        #region File operations

        private void TextEditor_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (!Win.Title.Contains("*"))
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
                textEditor.TextChanged -= TextEditor_TextChanged;
                Win.Closing -= Win_Closing;
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void saveFileClick(object sender, RoutedEventArgs e) => SaveFile();

        private void SaveFile()
        {
            try
            {
                if (!string.IsNullOrEmpty(path_))
                {
                    textEditor.Save(path_);
                    if (Win.Title.Contains("*"))
                        Win.Title = Win.Title.Remove(Win.Title.Length - 1);
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        #endregion

        #region Script API Catalog

        private void InitializeCatalog()
        {
            allCategories_ = new List<ScriptFunctionCategory>
            {
                new ScriptFunctionCategory { Name = "Lifecycle", Functions = new List<ScriptFunctionEntry>
                {
                    new ScriptFunctionEntry { Name = "Start",  Signature = "Start()",  Description = "Called once when the driver starts. Override to initialize resources.",                InsertSnippet = "public override void Start()\n{\n    \n}" },
                    new ScriptFunctionEntry { Name = "Stop",   Signature = "Stop()",   Description = "Called once when the driver stops. Override to release resources.",                   InsertSnippet = "public override void Stop()\n{\n    \n}" },
                    new ScriptFunctionEntry { Name = "Cycle",  Signature = "Cycle()",  Description = "Called periodically by the assigned timer. Override to implement cyclic logic.",       InsertSnippet = "public override void Cycle()\n{\n    \n}" },
                }},
                new ScriptFunctionCategory { Name = "Tags", Functions = new List<ScriptFunctionEntry>
                {
                    new ScriptFunctionEntry { Name = "GetTag", Signature = "GetTag(name)",         Description = "Returns the current value of a tag by name. Returns null if not found.", InsertSnippet = "GetTag(\"tagName\")" },
                    new ScriptFunctionEntry { Name = "SetTag", Signature = "SetTag(name, value)",   Description = "Sets the value of a tag by name.",                                       InsertSnippet = "SetTag(\"tagName\", value)" },
                }},
                new ScriptFunctionCategory { Name = "Logging", Functions = new List<ScriptFunctionEntry>
                {
                    new ScriptFunctionEntry { Name = "Write",  Signature = "Write(message)",        Description = "Sends a message to the application event/log system.",                   InsertSnippet = "Write(\"message\")" },
                }},
                new ScriptFunctionCategory { Name = "Dialogs", Functions = new List<ScriptFunctionEntry>
                {
                    new ScriptFunctionEntry { Name = "MessageBox.Show",               Signature = "MessageBox.Show(text)",                       Description = "Displays a message dialog. Requires: using System.Windows.Forms;",              InsertSnippet = "MessageBox.Show(\"message\")" },
                    new ScriptFunctionEntry { Name = "MessageBox.Show (caption+btn)", Signature = "MessageBox.Show(text, caption, buttons)",     Description = "Message dialog with title and buttons. Requires: using System.Windows.Forms;", InsertSnippet = "MessageBox.Show(\"message\", \"Title\", MessageBoxButtons.OK)" },
                }},
                new ScriptFunctionCategory { Name = "Conversion", Functions = new List<ScriptFunctionEntry>
                {
                    new ScriptFunctionEntry { Name = "Convert.ToString",  Signature = "Convert.ToString(value)",  Description = "Converts a value to string.",  InsertSnippet = "Convert.ToString(value)" },
                    new ScriptFunctionEntry { Name = "Convert.ToInt32",   Signature = "Convert.ToInt32(value)",   Description = "Converts a value to int.",     InsertSnippet = "Convert.ToInt32(value)" },
                    new ScriptFunctionEntry { Name = "Convert.ToDouble",  Signature = "Convert.ToDouble(value)",  Description = "Converts a value to double.",  InsertSnippet = "Convert.ToDouble(value)" },
                    new ScriptFunctionEntry { Name = "Convert.ToBoolean", Signature = "Convert.ToBoolean(value)", Description = "Converts a value to bool.",    InsertSnippet = "Convert.ToBoolean(value)" },
                }},
                new ScriptFunctionCategory { Name = "Math", Functions = new List<ScriptFunctionEntry>
                {
                    new ScriptFunctionEntry { Name = "Math.Round",  Signature = "Math.Round(value, digits)", Description = "Rounds a value to the given number of decimal places.", InsertSnippet = "Math.Round(value, 2)" },
                    new ScriptFunctionEntry { Name = "Math.Abs",    Signature = "Math.Abs(value)",           Description = "Returns the absolute value.",                           InsertSnippet = "Math.Abs(value)" },
                    new ScriptFunctionEntry { Name = "Math.Min",    Signature = "Math.Min(a, b)",            Description = "Returns the smaller of two values.",                    InsertSnippet = "Math.Min(a, b)" },
                    new ScriptFunctionEntry { Name = "Math.Max",    Signature = "Math.Max(a, b)",            Description = "Returns the larger of two values.",                     InsertSnippet = "Math.Max(a, b)" },
                }},
                new ScriptFunctionCategory { Name = "DateTime", Functions = new List<ScriptFunctionEntry>
                {
                    new ScriptFunctionEntry { Name = "DateTime.Now",    Signature = "DateTime.Now",                          Description = "Gets the current local date and time.",        InsertSnippet = "DateTime.Now" },
                    new ScriptFunctionEntry { Name = "DateTime.Format", Signature = "DateTime.Now.ToString(format)",         Description = "Formats the current date/time as string.",    InsertSnippet = "DateTime.Now.ToString(\"yyyy-MM-dd HH:mm:ss\")" },
                }},
                new ScriptFunctionCategory { Name = "Collections / LINQ", Functions = new List<ScriptFunctionEntry>
                {
                    new ScriptFunctionEntry { Name = "new List<T>",      Signature = "new List<T>()",              Description = "Creates a new generic list.",                                InsertSnippet = "new List<object>()" },
                    new ScriptFunctionEntry { Name = ".Where",           Signature = ".Where(predicate)",          Description = "Filters a sequence based on a predicate.",                  InsertSnippet = ".Where(x => x != null)" },
                    new ScriptFunctionEntry { Name = ".FirstOrDefault",  Signature = ".FirstOrDefault()",          Description = "Returns the first element, or default if none found.",      InsertSnippet = ".FirstOrDefault()" },
                    new ScriptFunctionEntry { Name = ".Select",          Signature = ".Select(selector)",          Description = "Projects each element into a new form.",                    InsertSnippet = ".Select(x => x.ToString())" },
                    new ScriptFunctionEntry { Name = ".ToList",          Signature = ".ToList()",                  Description = "Converts an enumerable to List<T>.",                        InsertSnippet = ".ToList()" },
                    new ScriptFunctionEntry { Name = ".Count",           Signature = ".Count()",                   Description = "Returns the number of elements.",                           InsertSnippet = ".Count()" },
                    new ScriptFunctionEntry { Name = ".OrderBy",         Signature = ".OrderBy(keySelector)",      Description = "Sorts elements in ascending order.",                        InsertSnippet = ".OrderBy(x => x)" },
                }},
            };

            tvCatalog.ItemsSource = allCategories_;
        }

        private void ToggleCatalog(bool? show = null)
        {
            bool open = show ?? CatalogCol.Width.Value == 0;
            if (open)
            {
                CatalogSplitterCol.Width = new GridLength(4);
                CatalogCol.Width = new GridLength(240);
                btnShowCatalog.IsChecked = true;
                txtCatalogFilter.Focus();
            }
            else
            {
                CatalogSplitterCol.Width = new GridLength(0);
                CatalogCol.Width = new GridLength(0);
                btnShowCatalog.IsChecked = false;
            }
        }

        private void BtnCatalog_Click(object sender, RoutedEventArgs e) => ToggleCatalog();
        private void BtnCloseCatalog_Click(object sender, RoutedEventArgs e) => ToggleCatalog(show: false);

        private void CatalogFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = txtCatalogFilter.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(filter))
            {
                tvCatalog.ItemsSource = allCategories_;
                return;
            }

            var filtered = allCategories_
                .Select(cat => new ScriptFunctionCategory
                {
                    Name = cat.Name,
                    Functions = cat.Functions
                        .Where(f => f.Name.ToLower().Contains(filter)
                                 || f.Signature.ToLower().Contains(filter)
                                 || f.Description.ToLower().Contains(filter))
                        .ToList()
                })
                .Where(cat => cat.Functions.Count > 0)
                .ToList();

            tvCatalog.ItemsSource = filtered;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                foreach (object item in tvCatalog.Items)
                {
                    if (tvCatalog.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem tvi)
                        tvi.IsExpanded = true;
                }
            }));
        }

        private void tvCatalog_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ScriptFunctionEntry entry)
                tbCatalogDesc.Text = entry.Description;
            else
                tbCatalogDesc.Text = "Select a function to see details.";
        }

        private void tvCatalog_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (tvCatalog.SelectedItem is ScriptFunctionEntry entry)
            {
                int offset = textEditor.CaretOffset;
                textEditor.Document.Insert(offset, entry.InsertSnippet);
                textEditor.Focus();
                e.Handled = true;
            }
        }

        #endregion

        #region Inline Find / Replace panel

        private void Button_FindClick(object sender, RoutedEventArgs e)    => OpenSearchPanel(replaceMode: false);
        private void Button_ReplaceClick(object sender, RoutedEventArgs e) => OpenSearchPanel(replaceMode: true);

        private void OpenSearchPanel(bool replaceMode)
        {
            var visibility = replaceMode ? Visibility.Visible : Visibility.Collapsed;
            lblReplace.Visibility   = visibility;
            txtReplace.Visibility   = visibility;
            btnReplace.Visibility   = visibility;
            btnReplaceAll.Visibility = visibility;

            SearchPanel.Visibility = Visibility.Visible;

            string sel = !textEditor.TextArea.Selection.IsMultiline
                ? textEditor.TextArea.Selection.GetText()
                : string.Empty;

            if (!string.IsNullOrEmpty(sel))
            {
                txtFind.Text = sel;
                txtFind2_Sync();
            }

            (replaceMode ? (TextBox)txtReplace : txtFind).Focus();
            txtFind.SelectAll();
        }

        private void CloseSearchPanel()
        {
            SearchPanel.Visibility = Visibility.Collapsed;
            textEditor.Focus();
        }

        private void CloseSearch_Click(object sender, RoutedEventArgs e) => CloseSearchPanel();

        private void TxtFind_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift) FindPrev();
                else FindNext();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CloseSearchPanel();
                e.Handled = true;
            }
        }

        private void TxtReplace_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)  { DoReplace(); e.Handled = true; }
            else if (e.Key == Key.Escape) { CloseSearchPanel(); e.Handled = true; }
        }

        private void TxtFind_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtFind2_Sync();
            UpdateSearchStatus();
        }

        private void txtFind2_Sync()
        {
            // keep both textboxes in sync (Find tab uses txtFind, Replace tab uses txtFind)
        }

        private void FindNextClick(object sender, RoutedEventArgs e) => FindNext();
        private void FindPrevClick(object sender, RoutedEventArgs e) => FindPrev();
        private void ReplaceClick(object sender, RoutedEventArgs e)  => DoReplace();

        private void ReplaceAllClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFind.Text)) return;

            try
            {
                Regex regex = BuildRegex(leftToRight: true);
                int offset = 0;
                int count = 0;
                string replaceWith = txtReplace.Text;

                textEditor.BeginChange();
                foreach (Match match in regex.Matches(textEditor.Text))
                {
                    textEditor.Document.Replace(offset + match.Index, match.Length, replaceWith);
                    offset += replaceWith.Length - match.Length;
                    count++;
                }
                textEditor.EndChange();

                tbSearchStatus.Text = count > 0 ? $"{count} replaced" : "No matches";
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void FindNext()
        {
            if (!SearchForward()) SystemSounds.Beep.Play();
        }

        private void FindPrev()
        {
            if (!SearchBackward()) SystemSounds.Beep.Play();
        }

        private void DoReplace()
        {
            if (string.IsNullOrEmpty(txtFind.Text)) return;
            try
            {
                Regex regex = BuildRegex();
                string input = textEditor.Text.Substring(textEditor.SelectionStart, textEditor.SelectionLength);
                Match match = regex.Match(input);
                if (match.Success && match.Index == 0 && match.Length == input.Length)
                    textEditor.Document.Replace(textEditor.SelectionStart, textEditor.SelectionLength, txtReplace.Text);

                if (!SearchForward()) SystemSounds.Beep.Play();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private bool SearchForward()
        {
            if (string.IsNullOrEmpty(txtFind.Text)) return false;
            try
            {
                Regex regex = BuildRegex();
                int start = textEditor.SelectionStart + textEditor.SelectionLength;
                Match match = regex.Match(textEditor.Text, start);
                if (!match.Success) match = regex.Match(textEditor.Text, 0);
                if (match.Success) SelectMatch(match);
                UpdateSearchStatus();
                return match.Success;
            }
            catch { return false; }
        }

        private bool SearchBackward()
        {
            if (string.IsNullOrEmpty(txtFind.Text)) return false;
            try
            {
                Regex regex = BuildRegex(rightToLeft: true);
                int start = textEditor.SelectionStart;
                Match match = regex.Match(textEditor.Text, start);
                if (!match.Success) match = regex.Match(textEditor.Text, textEditor.Text.Length);
                if (match.Success) SelectMatch(match);
                UpdateSearchStatus();
                return match.Success;
            }
            catch { return false; }
        }

        private void SelectMatch(Match match)
        {
            textEditor.Select(match.Index, match.Length);
            TextLocation loc = textEditor.Document.GetLocation(match.Index);
            textEditor.ScrollTo(loc.Line, loc.Column);
        }

        private void UpdateSearchStatus()
        {
            if (string.IsNullOrEmpty(txtFind.Text))
            {
                tbSearchStatus.Text = string.Empty;
                return;
            }
            try
            {
                int count = BuildRegex(leftToRight: true).Matches(textEditor.Text).Count;
                tbSearchStatus.Text = count == 0 ? "No results" : $"{count} match(es)";
                tbSearchStatus.Foreground = count == 0
                    ? System.Windows.Media.Brushes.OrangeRed
                    : FindResource("Th.TextSecBrush") as System.Windows.Media.Brush;
            }
            catch
            {
                tbSearchStatus.Text = string.Empty;
            }
        }

        private Regex BuildRegex(bool rightToLeft = false, bool leftToRight = false)
        {
            RegexOptions options = RegexOptions.None;

            if (rightToLeft || (cbSearchUp.IsChecked == true && !leftToRight))
                options |= RegexOptions.RightToLeft;
            if (cbCaseSensitive.IsChecked != true)
                options |= RegexOptions.IgnoreCase;

            if (cbRegex.IsChecked == true)
                return new Regex(txtFind.Text, options);

            string pattern = Regex.Escape(txtFind.Text);
            if (cbWholeWord.IsChecked == true)
                pattern = $@"\b{pattern}\b";

            return new Regex(pattern, options);
        }

        #endregion

        #region Text completion

        private void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            try
            {
                if (e.Text.Length > 0 && completionWindow != null)
                    completionWindow.CompletionList.RequestInsertion(e);
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
                        completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        #endregion

        #region Folding

        private void InitializeFolding(string langName)
        {
            switch (langName)
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

            if (foldingStrategy != null)
            {
                if (foldingManager == null)
                    foldingManager = FoldingManager.Install(textEditor.TextArea);
            }
            else if (foldingManager != null)
            {
                FoldingManager.Uninstall(foldingManager);
                foldingManager = null;
            }
        }

        private void HighlightingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string langName = textEditor.SyntaxHighlighting?.Name ?? string.Empty;
                InitializeFolding(langName);
                UpdateFoldings();
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
                if (foldingStrategy is BraceFoldingStrategy bfs)
                    bfs.UpdateFoldings(foldingManager, textEditor.Document);
                else if (foldingStrategy is XmlFoldingStrategy xfs)
                    xfs.UpdateFoldings(foldingManager, textEditor.Document);
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        #endregion

        #region Clipboard

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

        #endregion

        #region Font size

        private void UpdateFontSizeLabel()
        {
            tbFontSize.Text = $"{fontSizePt_:F0} pt";
        }

        private void FontSizeDec_Click(object sender, RoutedEventArgs e)
        {
            fontSizePt_ = Math.Max(6, fontSizePt_ - 1);
            textEditor.FontSize = fontSizePt_ * 96.0 / 72.0;
            UpdateFontSizeLabel();
        }

        private void FontSizeInc_Click(object sender, RoutedEventArgs e)
        {
            fontSizePt_ = Math.Min(32, fontSizePt_ + 1);
            textEditor.FontSize = fontSizePt_ * 96.0 / 72.0;
            UpdateFontSizeLabel();
        }

        #endregion
    }

    public class EditorAux : ICompletionData
    {
        public EditorAux(string text) { this.Text = text; }
        public System.Windows.Media.ImageSource Image => null;
        public string Text { get; private set; }
        public object Content => Text;
        public object Description => "Description for " + Text;
        public double Priority => 0;
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            => textArea.Document.Replace(completionSegment, Text);
    }

    public class BraceFoldingStrategy
    {
        public char OpeningBrace { get; set; } = '{';
        public char ClosingBrace { get; set; } = '}';

        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            int firstErrorOffset;
            IEnumerable<NewFolding> newFoldings = CreateNewFoldings(document, out firstErrorOffset);
            manager.UpdateFoldings(newFoldings, firstErrorOffset);
        }

        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
        {
            firstErrorOffset = -1;
            return CreateNewFoldings(document);
        }

        public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
        {
            List<NewFolding> newFoldings = new List<NewFolding>();
            Stack<int> startOffsets = new Stack<int>();
            int lastNewLineOffset = 0;

            for (int i = 0; i < document.TextLength; i++)
            {
                char c = document.GetCharAt(i);
                if (c == OpeningBrace)
                {
                    startOffsets.Push(i);
                }
                else if (c == ClosingBrace && startOffsets.Count > 0)
                {
                    int startOffset = startOffsets.Pop();
                    if (startOffset < lastNewLineOffset)
                        newFoldings.Add(new NewFolding(startOffset, i + 1));
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

    public class ScriptFunctionEntry
    {
        public string Name { get; set; }
        public string Signature { get; set; }
        public string Description { get; set; }
        public string InsertSnippet { get; set; }
    }

    public class ScriptFunctionCategory
    {
        public string Name { get; set; }
        public List<ScriptFunctionEntry> Functions { get; set; } = new();
    }
}
