namespace Fb2Editor
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Vurdalakov;

    public partial class MainForm : Form
    {
        private const String Fb2ImageKey = "fb2";
        private const String ZipImageKey = "zip";
        private const String ErrorImageKey = "error";
        private const String NotRenamedMessage = "Saved, but the file was not renamed.\n";

        private readonly AppSettings _settings = AppSettings.Load();
        private DirectoryInfo _rootFolder;
        private CancellationTokenSource _checkCancellation; // stops the background tests of the previous list
        private Boolean _updatingFields; // the text fields are being filled by the program, not edited by the user
        private (String First, String Last) _rememberedAuthor = (String.Empty, String.Empty); // the author that an Anthology check hides
        private List<(FileInfo File, String RelativePath)> _files = new List<(FileInfo File, String RelativePath)>(); // all files found
        private Dictionary<String, Int32> _fileIndex = new Dictionary<String, Int32>(); // full path -> index in _files
        private readonly Dictionary<String, List<String>> _problems = new Dictionary<String, List<String>>(StringComparer.OrdinalIgnoreCase); // full path -> result of the tests, for the files tested so far
        private List<Int32> _shown = new List<Int32>(); // indexes in _files of the rows of the virtual list, ascending
        private Boolean _keepSelectedFile; // the selection is only moved to follow its file, so the file is not reloaded
        private readonly Dictionary<String, String> _texts = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase); // full path -> author, title, ... of the files read for the filter
        private String _filter = String.Empty; // the text in the filter box
        private CancellationTokenSource _indexCancellation; // stops reading the book data for the filter
        private Int32 _errorCount; // files of _files that are known to have problems
        private readonly Image _checkedImage;
        private readonly Image _uncheckedImage;
        private Boolean _modified; // the user changed a text field
        private Boolean _hasProblems; // the selected file does not pass the tests

        public MainForm()
        {
            this.InitializeComponent();

            this.listViewFiles.SizeChanged += (sender, e) => this.FitNameColumn();
            this.listViewFiles.VirtualMode = true; // only the visible rows are created, so a huge library is no problem
            this.listViewFiles.RetrieveVirtualItem += (sender, e) => e.Item = this.CreateItem(e.ItemIndex);
            this.listViewFiles.SelectedIndexChanged += (sender, e) =>
            {
                if (!this._keepSelectedFile)
                {
                    this.ShowSelectedItem();
                }
            };
            this.FormClosed += (sender, e) =>
            {
                this._checkCancellation?.Cancel();
                this._indexCancellation?.Cancel();
            };
            this.toolStripTextBoxFilter.TextChanged += (sender, e) => this.OnFilterChanged();

            foreach (var textBox in new[] { this.textBoxAuthorFirst, this.textBoxAuthorLast, this.textBoxTranslatorFirst, this.textBoxTranslatorLast, this.textBoxTitle, this.textBoxSeriesTitle, this.textBoxSeriesNumber })
            {
                textBox.TextChanged += (sender, e) => this.OnFieldChanged();
            }

            this.checkBoxAnthology.CheckedChanged += (sender, e) => this.OnAnthologyCheckedChanged();
            this.buttonReRead.Click += (sender, e) => this.ReReadSelectedFile();
            this.buttonSave.Click += (sender, e) => this.SaveSelectedFile();
            this.buttonPack.Click += (sender, e) => this.PackSelectedFile();

            this.components ??= new System.ComponentModel.Container();
            var imageList = new ImageList(this.components)
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = SystemInformation.SmallIconSize,
            };

            // File icons are drawn from the SVG files at the size of a small icon, so they are sharp at any DPI
            imageList.Images.Add(MainForm.Fb2ImageKey, SvgIcons.Render("fb2file.svg", imageList.ImageSize.Width));
            imageList.Images.Add(MainForm.ZipImageKey, SvgIcons.Render("zipfile.svg", imageList.ImageSize.Width));
            imageList.Images.Add(MainForm.ErrorImageKey, SvgIcons.Render("error.svg", imageList.ImageSize.Width));

            this.listViewFiles.SmallImageList = imageList;

            // The "Show errors only" button is a check box: it is drawn with an unchecked or a checked image
            var buttonImageSize = this.toolStrip1.ImageScalingSize.Width;
            this._checkedImage = SvgIcons.Render("checkbox-checked.svg", buttonImageSize);
            this._uncheckedImage = SvgIcons.Render("checkbox-unchecked.svg", buttonImageSize);
            this.toolStripButtonShowErrorsOnly.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            this.toolStripButtonShowErrorsOnly.CheckOnClick = true;
            this.toolStripButtonShowErrorsOnly.CheckedChanged += (sender, e) => this.ShowList(this.GetSelectedFile()?.FullName);
            this.UpdateErrorsButton();

            this.FitNameColumn();
        }

        private void MainForm_Load(Object sender, EventArgs e)
        {
            // The root folder of the previous session, if it still exists
            if (!String.IsNullOrEmpty(this._settings.RootFolder) && Directory.Exists(this._settings.RootFolder))
            {
                this._rootFolder = new DirectoryInfo(this._settings.RootFolder);
            }

            this.ShowRootFolder();
            this.ShowInfo(null); // no file selected yet
            this.ShowTestResults(null);

            if (this._rootFolder != null)
            {
                this.FillFileList();
            }
        }

        private void FitNameColumn() => this.columnHeader1.Width = Math.Max(this.listViewFiles.ClientSize.Width - SystemInformation.VerticalScrollBarWidth, 0);

        private void ShowRootFolder() => this.toolStripStatusLabelRootFolder.Text = null == this._rootFolder ? "Root folder: (none, use File / Select Folder)" : $"Root folder: {this._rootFolder.FullName}";

        private void SelectFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder with FB2 files";
                dialog.UseDescriptionForTitle = true;
                dialog.ShowNewFolderButton = false;
                dialog.SelectedPath = this._rootFolder?.FullName ?? String.Empty;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                this._rootFolder = new DirectoryInfo(dialog.SelectedPath);
                this._settings.RootFolder = this._rootFolder.FullName;
                this._settings.Save();
                this.ShowRootFolder();
                this.FillFileList();
            }
        }

        // Lists all .fb2 and .fb2.zip files of the root folder and its sub-folders, with paths relative to the root folder, sorted by folder and name.
        // Hidden files and folders and those with a name that starts with a dot are not shown. Selects the given file or else the first one.
        private void FillFileList(String selectFilePath = null)
        {
            if (this._rootFolder == null)
            {
                return;
            }

            var rootPath = this._rootFolder.FullName;
            var options = new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.Hidden, IgnoreInaccessible = true };

            List<(FileInfo File, String RelativePath)> files;
            try
            {
                files = this._rootFolder.EnumerateFiles("*", options)
                    .Where(file => file.Name.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase) || file.Name.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase))
                    .Select(file => (File: file, RelativePath: Path.GetRelativePath(rootPath, file.FullName)))
                    .Where(entry => !entry.RelativePath.Split(Path.DirectorySeparatorChar).Any(segment => segment.StartsWith('.')))
                    .OrderBy(entry => Path.GetDirectoryName(entry.RelativePath), StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(entry => entry.File.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                this.ShowError($"{rootPath} : {exception.Message}");
                return;
            }

            this._files = files;
            this._fileIndex = files.Select((entry, index) => (entry.File.FullName, index)).ToDictionary(pair => pair.FullName, pair => pair.index, StringComparer.OrdinalIgnoreCase);
            this._errorCount = files.Count(entry => this.HasProblems(entry.File));

            this.ShowList(selectFilePath);

            // The tests of all files can take a while, so the list is shown first and the error icons appear as the tests finish
            this.StartBackgroundTests(files.Select(entry => entry.File).ToList());
            this.StartBackgroundIndex();
        }

        private Boolean HasProblems(FileInfo file) => this._problems.TryGetValue(file.FullName, out var problems) && (problems.Count > 0);

        // Positions of the icons in the image list (the order they are added in the constructor); the image list may already be gone when the control is destroyed
        private Int32 ImageIndexFor(FileInfo file) => this.HasProblems(file) ? 2 : (file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? 1 : 0);

        // The item for a row of the (virtual) list: the file's relative path, with the icon and the tool tip of its test result
        private ListViewItem CreateItem(Int32 position)
        {
            if ((position < 0) || (position >= this._shown.Count))
            {
                return new ListViewItem(); // the control asks for a row of an older list size
            }

            var (file, relativePath) = this._files[this._shown[position]];
            var item = new ListViewItem(relativePath) { Tag = file, ImageIndex = this.ImageIndexFor(file) }; // virtual items are not attached to the list, so an image key cannot be resolved

            if (this._problems.TryGetValue(file.FullName, out var problems))
            {
                item.ToolTipText = String.Join("\n", problems);
            }

            return item;
        }

        // Row of the file (index in _files) in the list, or -1 if the file is not shown
        private Int32 FindPosition(Int32 fileIndex)
        {
            var position = this._shown.BinarySearch(fileIndex);

            return (position >= 0) ? position : -1;
        }

        private void SelectPosition(Int32 position)
        {
            this.listViewFiles.SelectedIndices.Clear();
            this.listViewFiles.SelectedIndices.Add(position);
            this.listViewFiles.EnsureVisible(position);
        }

        private Int32 GetTopPosition() => this.listViewFiles.IsHandleCreated ? (Int32)MainForm.SendMessage(this.listViewFiles.Handle, LvmGetTopIndex, IntPtr.Zero, IntPtr.Zero) : 0;

        // Scrolls so that the row is the first visible one (scrolling to the end first makes EnsureVisible put the row at the top)
        private void ScrollToTop(Int32 position)
        {
            if ((position < 0) || (position >= this.listViewFiles.VirtualListSize))
            {
                return;
            }

            this.listViewFiles.EnsureVisible(this.listViewFiles.VirtualListSize - 1);
            this.listViewFiles.EnsureVisible(position);
        }

        // Fills the list control from the files found: all of them, or only those with errors. Selects the given file or else the first one, keeps the scroll position.
        private void ShowList(String selectFilePath)
        {
            var errorsOnly = this.toolStripButtonShowErrorsOnly.Checked;

            // The first visible row is remembered, so that the list does not jump when it is refilled
            var shownCount = this._shown.Count;
            var topPosition = this.GetTopPosition();
            var topPath = ((topPosition >= 0) && (topPosition < this._shown.Count)) ? this._files[this._shown[topPosition]].File.FullName : null;

            this.listViewFiles.BeginUpdate();
            this.listViewFiles.SelectedIndices.Clear();
            this._shown = Enumerable.Range(0, this._files.Count).Where(this.IsVisible).ToList();

            // The control keeps its scroll position and old rows when the size changes; emptying it first makes it forget them
            this.listViewFiles.VirtualListSize = 0;
            this.listViewFiles.VirtualListSize = this._shown.Count;
            this.listViewFiles.EndUpdate();
            this.listViewFiles.Invalidate();
            this.UpdateErrorsButton();
            this.UpdateFilterLabel();

            if (this._shown.Count > 0)
            {
                if (shownCount == this._shown.Count)
                {
                    var position = (selectFilePath != null) && this._fileIndex.TryGetValue(selectFilePath, out var selectIndex) ? this.FindPosition(selectIndex) : -1;

                    // Back to the old scroll position (by name, else by position)
                    var top = (topPath != null) && this._fileIndex.TryGetValue(topPath, out var topIndex) ? this.FindPosition(topIndex) : -1;
                    this.ScrollToTop((top >= 0) ? top : Math.Clamp(topPosition, 0, this._shown.Count - 1));

                    this.SelectPosition(Math.Max(position, 0));
                }
                else
                {
                    this.ScrollToTop(0);
                    this.SelectPosition(0);
                }
            }
            else
            {
                this.ShowInfo(null);
                this.ShowTestResults(null);
                this.textBoxFileName.Text = String.Empty;
                this.toolStripStatusLabelStatus.Text = (errorsOnly || (this._filter.Length > 0)) ? "No files match" : "No .fb2 or .fb2.zip files";
            }
        }

        // The button is a check box drawn with two images; its text has the number of files with errors
        private void UpdateErrorsButton()
        {
            var button = this.toolStripButtonShowErrorsOnly;

            button.Image = button.Checked ? this._checkedImage : this._uncheckedImage;
            button.Text = $"Show errors only ({this._errorCount})";
        }

        // Remembers the result of the tests of a file and shows it: error icon and tool tip, the error count, and in the "errors only" list the files that turn out to have errors
        private void ApplyResult(FileInfo file, List<String> problems)
        {
            if (!this._fileIndex.TryGetValue(file.FullName, out var index))
            {
                return; // the list was refilled and the file is not in it
            }

            var hadErrors = this.HasProblems(file);
            this._problems[file.FullName] = problems;
            var hasErrors = problems.Count > 0;

            if (hadErrors != hasErrors)
            {
                this._errorCount += hasErrors ? 1 : -1;
                this.UpdateErrorsButton();
            }

            var position = this.FindPosition(index);
            if (position >= 0)
            {
                this.listViewFiles.RedrawItems(position, position, false);
            }
            else
            {
                this.InsertRowIfVisible(index);
            }
        }

        // Remembers the book data of a file that the filter looks at; the file may now match and get its row
        private void ApplySearchText(FileInfo file, String text)
        {
            if (!this._fileIndex.TryGetValue(file.FullName, out var index))
            {
                return;
            }

            this._texts[file.FullName] = text;

            if (this.FindPosition(index) < 0)
            {
                this.InsertRowIfVisible(index);
            }
        }

        // Gives the file a row at the place it has in the full list, if the filters now let it through; the rows below move down, and so does the selection
        private void InsertRowIfVisible(Int32 index)
        {
            if (!this.IsVisible(index))
            {
                return;
            }

            var insertAt = ~this._shown.BinarySearch(index);
            var selected = (this.listViewFiles.SelectedIndices.Count > 0) ? this.listViewFiles.SelectedIndices[0] : -1;

            this._shown.Insert(insertAt, index);
            this.listViewFiles.VirtualListSize = this._shown.Count;
            this.UpdateFilterLabel();

            if (selected >= 0)
            {
                this._keepSelectedFile = true;
                try
                {
                    this.listViewFiles.SelectedIndices.Clear();
                    this.listViewFiles.SelectedIndices.Add((selected >= insertAt) ? (selected + 1) : selected);
                }
                finally
                {
                    this._keepSelectedFile = false;
                }
            }
            else if (this._shown.Count == 1)
            {
                this.SelectPosition(0);
            }
        }

        // Passes the "errors only" button and the filter text. The text may be in the relative path or in any field of the book (known once the file was read).
        private Boolean IsVisible(Int32 index)
        {
            var (file, relativePath) = this._files[index];

            if (this.toolStripButtonShowErrorsOnly.Checked && !this.HasProblems(file))
            {
                return false;
            }

            return (this._filter.Length == 0)
                || relativePath.Contains(this._filter, StringComparison.CurrentCultureIgnoreCase)
                || (this._texts.TryGetValue(file.FullName, out var text) && text.Contains(this._filter, StringComparison.CurrentCultureIgnoreCase));
        }

        private static String BuildSearchText(Fb2Info info) => String.Join(" ", new[] { info.AuthorFirstName, info.AuthorLastName, info.TranslatorFirstName, info.TranslatorLastName, info.Title, info.Series, info.SeriesNumber }.Where(s => !String.IsNullOrEmpty(s)));

        private void UpdateFilterLabel() => this.toolStripLabelFilter.Text = (this._filter.Length == 0) ? "Filter:" : $"Filter ({this._shown.Count}/{this._files.Count}):";

        private void OnFilterChanged()
        {
            this._filter = this.toolStripTextBoxFilter.Text.Trim();
            this.ShowList(this.GetSelectedFile()?.FullName);
            this.StartBackgroundIndex();
        }

        // Reads the book data of the files that were not read yet, on another thread, as long as a filter text is typed; the files that match get their rows as the data comes in
        private void StartBackgroundIndex()
        {
            this._indexCancellation?.Cancel();

            if (this._filter.Length == 0)
            {
                return;
            }

            var cancellation = new CancellationTokenSource();
            this._indexCancellation = cancellation;

            var entries = this._files.Select(entry => entry.File).Where(file => !this._texts.ContainsKey(file.FullName)).ToList();

            Task.Run(() =>
            {
                foreach (var file in entries)
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        return;
                    }

                    String text;
                    try
                    {
                        text = MainForm.BuildSearchText(Fb2InfoReader.ReadFile(file.FullName));
                    }
                    catch (Exception)
                    {
                        text = String.Empty; // the file cannot be read; it is found by its name only
                    }

                    try
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            if (!cancellation.IsCancellationRequested)
                            {
                                this.ApplySearchText(file, text);
                            }
                        }));
                    }
                    catch (Exception exception) when (exception is InvalidOperationException or ObjectDisposedException)
                    {
                        return; // the form is closed
                    }
                }
            });
        }

        // Tests the files one by one on another thread; the results are shown on the UI thread. A new list stops the tests of the old one.
        private void StartBackgroundTests(List<FileInfo> entries)
        {
            this._checkCancellation?.Cancel();

            var cancellation = new CancellationTokenSource();
            this._checkCancellation = cancellation;

            Task.Run(() =>
            {
                foreach (var file in entries)
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        return;
                    }

                    var problems = MainForm.CheckFile(file);

                    try
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            if (!cancellation.IsCancellationRequested)
                            {
                                this.ApplyResult(file, problems);
                            }
                        }));
                    }
                    catch (Exception exception) when (exception is InvalidOperationException or ObjectDisposedException)
                    {
                        return; // the form is closed
                    }
                }
            });
        }

        private void ShowSelectedItem()
        {
            // Changing the selection first deselects the old item, which is not interesting
            var file = this.GetSelectedFile();
            if (file == null)
            {
                return;
            }

            var problems = MainForm.CheckFile(file);
            this.ApplyResult(file, problems);

            try
            {
                var info = Fb2InfoReader.ReadFile(file.FullName);
                this._texts[file.FullName] = MainForm.BuildSearchText(info);
                this.ShowInfo(info);
                this.toolStripStatusLabelStatus.Text = file.FullName;
            }
            catch (Exception exception)
            {
                this.ShowInfo(null);
                this.ShowError($"{file.Name}: {exception.Message}");
            }

            this.textBoxFileName.Text = file.Name;
            this.ShowTestResults(problems);
        }

        // What is wrong with the file: the tests of "fb2zip test"
        private static List<String> CheckFile(FileInfo file)
        {
            try
            {
                return Fb2FileChecker.Check(file.FullName);
            }
            catch (Exception exception)
            {
                return new List<String> { $"cannot check: {exception.Message}" };
            }
        }

        private FileInfo GetSelectedFile() => (this.listViewFiles.SelectedIndices.Count > 0) ? this._files[this._shown[this.listViewFiles.SelectedIndices[0]]].File : null;

        private const Int32 LvmGetTopIndex = 0x1027;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, Int32 msg, IntPtr wParam, IntPtr lParam);

        // Re-read is available only after the user changed a text field; Save also if the file does not pass the tests (the rename may fix it)
        private void SetModified(Boolean modified)
        {
            this._modified = modified;
            this.UpdateButtons();
        }

        private void UpdateButtons()
        {
            this.buttonReRead.Enabled = true; // the right pane is disabled as a whole when no file is selected
            this.buttonSave.Enabled = this._modified || this._hasProblems;

            // Pack is available only if an FB2 file (not a zip) is selected
            var file = this.GetSelectedFile();
            this.buttonPack.Enabled = (file != null) && file.Extension.Equals(".fb2", StringComparison.OrdinalIgnoreCase);
        }

        // The list of problems of the selected file, or "Test passed"; null (no file selected) clears the box
        private void ShowTestResults(List<String> problems)
        {
            this._hasProblems = (problems != null) && (problems.Count > 0);
            this.textBoxTestResults.Text = (problems == null) ? String.Empty : ((problems.Count == 0) ? "Test passed" : String.Join(Environment.NewLine, problems));
            this.UpdateButtons();
        }

        private void OnFieldChanged()
        {
            if (!this._updatingFields)
            {
                this.UpdateNewName();
                this.SetModified(true);
            }
        }

        // The name the file will get on Save, built from the text fields; empty if there is no file or no title
        private static String BuildNewName(FileInfo file, Fb2Info fields)
        {
            var baseName = Fb2FileName.Build(fields, out _);
            if ((file == null) || (baseName == null))
            {
                return null;
            }

            var extension = file.Name.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase) ? ".fb2.zip" : ".fb2";

            return Fb2FileName.MakeFileSystemSafe(baseName) + extension;
        }

        private void UpdateNewName() => this.textBoxNewName.Text = MainForm.BuildNewName(this.GetSelectedFile(), this.ReadFields()) ?? String.Empty;

        // Checked: the book has no author of its own (an anthology), so the author fields are cleared and unavailable
        private void OnAnthologyCheckedChanged()
        {
            if (this._updatingFields)
            {
                return;
            }

            this.UpdateAuthorFields();

            // The author is remembered while the box is checked, and filled in again when it is unchecked
            if (this.checkBoxAnthology.Checked)
            {
                if (!String.IsNullOrEmpty(this.textBoxAuthorFirst.Text) || !String.IsNullOrEmpty(this.textBoxAuthorLast.Text))
                {
                    this._rememberedAuthor = (this.textBoxAuthorFirst.Text, this.textBoxAuthorLast.Text);
                }
            }

            this._updatingFields = true;
            try
            {
                this.textBoxAuthorFirst.Text = this.checkBoxAnthology.Checked ? String.Empty : this._rememberedAuthor.First;
                this.textBoxAuthorLast.Text = this.checkBoxAnthology.Checked ? String.Empty : this._rememberedAuthor.Last;
            }
            finally
            {
                this._updatingFields = false;
            }

            this.OnFieldChanged();
        }

        private void UpdateAuthorFields() => this.textBoxAuthorFirst.Enabled = this.textBoxAuthorLast.Enabled = !this.checkBoxAnthology.Checked;

        private Fb2Info ReadFields()
        {
            var anthology = this.checkBoxAnthology.Checked;
            var authorFirstName = this.textBoxAuthorFirst.Text.Trim();
            var authorLastName = this.textBoxAuthorLast.Text.Trim();

            return new Fb2Info
            {
                Anthology = anthology,
                Author = $"{authorFirstName} {authorLastName}".Trim(),
                AuthorFirstName = authorFirstName,
                AuthorLastName = authorLastName,
                Translator = $"{this.textBoxTranslatorFirst.Text.Trim()} {this.textBoxTranslatorLast.Text.Trim()}".Trim(),
                TranslatorFirstName = this.textBoxTranslatorFirst.Text.Trim(),
                TranslatorLastName = this.textBoxTranslatorLast.Text.Trim(),
                Title = this.textBoxTitle.Text.Trim(),
                Series = this.textBoxSeriesTitle.Text.Trim(),
                SeriesNumber = this.textBoxSeriesNumber.Text.Trim(),
            };
        }

        private void ShowError(String text) => MessageBox.Show(this, text, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

        // Throws away the changes in the text fields, reads the file again and runs the tests again
        private void ReReadSelectedFile()
        {
            if (this.GetSelectedFile() == null)
            {
                return;
            }

            this.ShowSelectedItem();

            this.toolStripStatusLabelStatus.Text += " (re-read)";
        }

        // Packs the FB2 file into a .fb2.zip next to it and, only if that succeeded, deletes the FB2 file
        private void PackSelectedFile()
        {
            var file = this.GetSelectedFile();
            if ((file == null) || !file.Extension.Equals(".fb2", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var zipFilePath = file.FullName + ".zip";
            if (File.Exists(zipFilePath))
            {
                this.ShowError($"{Path.GetFileName(zipFilePath)} already exists.");
                return;
            }

            try
            {
                Fb2ZipPacker.Pack(file.FullName, zipFilePath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                this.ShowError($"Cannot pack {file.Name}:\n{exception.Message}");
                return;
            }

            try
            {
                File.Delete(file.FullName);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                this.ShowError($"Packed, but cannot delete {file.Name}:\n{exception.Message}");
            }

            // Select the zip in the refreshed list
            this.FillFileList(zipFilePath);
            this.toolStripStatusLabelStatus.Text = $"{zipFilePath} (packed)";
        }

        // Writes the text fields into the file and then renames the file to match them
        private void SaveSelectedFile()
        {
            var file = this.GetSelectedFile();
            if (file == null)
            {
                return;
            }

            var fields = this.ReadFields();

            try
            {
                Fb2InfoWriter.Update(file.FullName, fields);
            }
            catch (Exception exception)
            {
                this.ShowError($"Cannot save {file.Name}:\n{exception.Message}");
                return;
            }

            var newName = this.TryRenameFile(file, fields);

            try
            {
                if (newName != null)
                {
                    // The selection of the renamed file loads what is really in the file now
                    var newPath = Path.Combine(file.DirectoryName, newName);
                    this.FillFileList(newPath);
                    this.toolStripStatusLabelStatus.Text = $"{newPath} (saved and renamed)";
                }
                else
                {
                    this.ShowInfo(Fb2InfoReader.ReadFile(file.FullName));

                    // The file may pass or fail the tests differently now
                    var problems = MainForm.CheckFile(file);
                    this.ApplyResult(file, problems);
                    this.ShowTestResults(problems);
                    this.toolStripStatusLabelStatus.Text = $"{file.FullName} (saved)";
                }
            }
            catch (Exception exception)
            {
                this.ShowError($"Saved, but cannot read {file.Name} back:\n{exception.Message}");
            }
        }

        // Renames the file to "Author - Title" or "Author - [Series, number] - Title" built from the fields; without an author it is "Title" or "[Series, number] - Title"; with a translator " (пер. Translator)" is added at the end.
        // Returns the new name, or null if the file was not renamed (problems are reported to the user).
        private String TryRenameFile(FileInfo file, Fb2Info fields)
        {
            var newName = MainForm.BuildNewName(file, fields);
            if (newName == null)
            {
                this.ShowError($"{MainForm.NotRenamedMessage}The file name is built from the title (and the author, if any); the title is required.");
                return null;
            }

            if (String.Equals(newName, file.Name, StringComparison.Ordinal))
            {
                this.ShowError($"{file.Name} already has this name");
                return null;
            }

            var newPath = Path.Combine(file.DirectoryName, newName);
            if (File.Exists(newPath))
            {
                this.ShowError($"{MainForm.NotRenamedMessage}{newName} already exists.");
                return null;
            }

            try
            {
                File.Move(file.FullName, newPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                this.ShowError($"{MainForm.NotRenamedMessage}Cannot rename {file.Name}:\n{exception.Message}");
                return null;
            }

            return newName;
        }

        // Shows the book data; null (no file selected, or the file cannot be read) clears the fields and disables the right pane
        private void ShowInfo(Fb2Info info)
        {
            this.splitContainer1.Panel2.Enabled = info != null;

            this._updatingFields = true;
            try
            {
                if (null == info)
                {
                    this.toolStripStatusLabelStatus.Text = "";
                }

                var anthology = info?.Anthology ?? false;

                this.labelAuthor.Text = (!anthology && (info?.AuthorCount > 1)) ? $"Authors ({info.AuthorCount}):" : "Author:";
                this._rememberedAuthor = (info?.AuthorFirstName ?? String.Empty, info?.AuthorLastName ?? String.Empty);
                this.checkBoxAnthology.Checked = anthology;
                this.UpdateAuthorFields();
                this.textBoxAuthorFirst.Text = anthology ? String.Empty : (info?.AuthorFirstName ?? String.Empty);
                this.textBoxAuthorLast.Text = anthology ? String.Empty : (info?.AuthorLastName ?? String.Empty);
                this.textBoxTranslatorFirst.Text = info?.TranslatorFirstName ?? String.Empty;
                this.textBoxTranslatorLast.Text = info?.TranslatorLastName ?? String.Empty;
                this.textBoxTitle.Text = info?.Title ?? String.Empty;
                this.textBoxSeriesTitle.Text = info?.Series ?? String.Empty;
                this.textBoxSeriesNumber.Text = info?.SeriesNumber ?? String.Empty;
            }
            finally
            {
                this._updatingFields = false;
            }

            this.UpdateNewName();
            this.SetModified(false);
        }

        private void selectFolderToolStripMenuItem_Click(Object sender, EventArgs e) => this.SelectFolder();

        private void exitToolStripMenuItem_Click(Object sender, EventArgs e) => this.Close();
    }
}
