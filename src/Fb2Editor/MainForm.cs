namespace Fb2Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;

    using Vurdalakov;

    public partial class MainForm : Form
    {
        private const String ParentDirectoryText = "<parent directory>";
        private const String FolderImageKey = "folder";
        private const String Fb2ImageKey = "fb2";
        private const String ZipImageKey = "zip";
        private const String ErrorImageKey = "error";
        private const String NotRenamedMessage = "Saved, but the file was not renamed.\n";

        private readonly AppSettings _settings = AppSettings.Load();
        private DirectoryInfo _rootFolder;
        private Boolean _updatingFields; // the text fields are being filled by the program, not edited by the user
        private Boolean _modified; // the user changed a text field
        private Boolean _hasProblems; // the selected file does not pass the tests

        public MainForm()
        {
            this.InitializeComponent();

            this.listViewFiles.SizeChanged += (sender, e) => this.FitNameColumn();
            this.listViewFiles.SelectedIndexChanged += (sender, e) => this.ShowSelectedItem();
            this.listViewFiles.ItemActivate += (sender, e) => this.OpenSelectedFolder(); // double-click or Enter

            foreach (var textBox in new[] { this.textBoxAuthorFirst, this.textBoxAuthorLast, this.textBoxTranslatorFirst, this.textBoxTranslatorLast, this.textBoxTitle, this.textBoxSeriesTitle, this.textBoxSeriesNumber })
            {
                textBox.TextChanged += (sender, e) => this.OnFieldChanged();
            }

            this.buttonReRead.Click += (sender, e) => this.ReReadSelectedFile();
            this.buttonSave.Click += (sender, e) => this.SaveSelectedFile();
            this.buttonPack.Click += (sender, e) => this.PackSelectedFile();

            this.components ??= new System.ComponentModel.Container();
            var imageList = new ImageList(this.components)
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = SystemInformation.SmallIconSize,
            };

            var folderImage = ShellIcons.GetFolderImage();
            if (folderImage != null)
            {
                imageList.Images.Add(MainForm.FolderImageKey, folderImage);
            }

            // File icons are drawn from the SVG files at the size of a small icon, so they are sharp at any DPI
            imageList.Images.Add(MainForm.Fb2ImageKey, SvgIcons.Render("fb2file.svg", imageList.ImageSize.Width));
            imageList.Images.Add(MainForm.ZipImageKey, SvgIcons.Render("zipfile.svg", imageList.ImageSize.Width));
            imageList.Images.Add(MainForm.ErrorImageKey, SvgIcons.Render("error.svg", imageList.ImageSize.Width));

            this.listViewFiles.SmallImageList = imageList;

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
                this.FillFileList(this._rootFolder);
            }
        }

        private void FitNameColumn() => this.columnHeader1.Width = Math.Max(this.listViewFiles.ClientSize.Width - SystemInformation.VerticalScrollBarWidth, 0);

        private void ShowRootFolder() => this.toolStripStatusLabelRootFolder.Text = (this._rootFolder == null) ? "Root folder: (none, use File / Select Folder)" : $"Root folder: {this._rootFolder.FullName}";

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
                this.FillFileList(this._rootFolder);
            }
        }

        // Lists, sorted by name: "<parent directory>" (not in the root folder), sub-folders, then .fb2 and .fb2.zip files; selects the file with the given name or else the first entry
        private void FillFileList(DirectoryInfo folder, String selectFileName = null)
        {
            List<DirectoryInfo> subfolders;
            List<FileInfo> files;

            // Hidden files and folders and those with a name that starts with a dot are not shown
            var options = new EnumerationOptions { AttributesToSkip = FileAttributes.Hidden };

            try
            {
                subfolders = folder.EnumerateDirectories("*", options)
                    .Where(directory => !directory.Name.StartsWith('.'))
                    .OrderBy(directory => directory.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                files = folder.EnumerateFiles("*", options)
                    .Where(file => !file.Name.StartsWith('.') && (file.Name.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase) || file.Name.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(file => file.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Stay in the current folder
                this.toolStripStatusLabelFile.Text = $"{folder.FullName}: {exception.Message}";
                return;
            }

            this.toolStripTextBoxDirectoryPath.Text = folder.FullName;

            this.listViewFiles.BeginUpdate();
            this.listViewFiles.Items.Clear();

            if ((folder.Parent != null) && !this.IsRootFolder(folder))
            {
                this.listViewFiles.Items.Add(new ListViewItem(MainForm.ParentDirectoryText) { Tag = folder.Parent, ImageKey = MainForm.FolderImageKey });
            }

            foreach (var subfolder in subfolders)
            {
                this.listViewFiles.Items.Add(new ListViewItem(subfolder.Name) { Tag = subfolder, ImageKey = MainForm.FolderImageKey });
            }

            foreach (var file in files)
            {
                var item = new ListViewItem(file.Name) { Tag = file };
                this.ShowFileState(item, file);
                this.listViewFiles.Items.Add(item);
            }

            this.listViewFiles.EndUpdate();

            if (this.listViewFiles.Items.Count > 0)
            {
                var item = this.listViewFiles.Items.Cast<ListViewItem>().FirstOrDefault(i => (selectFileName != null) && (i.Tag is FileInfo fileInfo) && (fileInfo.Name == selectFileName)) ?? this.listViewFiles.Items[0];
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
            }
            else
            {
                this.ShowInfo(null);
                this.ShowTestResults(null);
                this.toolStripStatusLabelFile.Text = $"No sub-folders or .fb2 / .fb2.zip files in {folder.FullName}";
            }
        }

        private Boolean IsRootFolder(DirectoryInfo folder) => (this._rootFolder != null) && String.Equals(Path.TrimEndingDirectorySeparator(folder.FullName), Path.TrimEndingDirectorySeparator(this._rootFolder.FullName), StringComparison.OrdinalIgnoreCase);

        private void OpenSelectedFolder()
        {
            if ((this.listViewFiles.SelectedItems.Count > 0) && (this.listViewFiles.SelectedItems[0].Tag is DirectoryInfo folder))
            {
                this.FillFileList(folder);
            }
        }

        private void ShowSelectedItem()
        {
            // Changing the selection first deselects the old item, which is not interesting
            if (this.listViewFiles.SelectedItems.Count == 0)
            {
                return;
            }

            if (this.listViewFiles.SelectedItems[0].Tag is not FileInfo file)
            {
                this.ShowInfo(null);
                this.ShowTestResults(null);
                this.toolStripStatusLabelFile.Text = ((DirectoryInfo)this.listViewFiles.SelectedItems[0].Tag!).FullName;
                return;
            }

            var problems = this.ShowFileState(this.listViewFiles.SelectedItems[0], file);

            try
            {
                this.ShowInfo(Fb2InfoReader.ReadFile(file.FullName));
                this.toolStripStatusLabelFile.Text = file.FullName;
            }
            catch (Exception exception)
            {
                this.ShowInfo(null);
                this.toolStripStatusLabelFile.Text = $"{file.Name}: {exception.Message}";
            }

            this.ShowTestResults(problems);
        }

        // The icon of a file is the error icon if the file does not pass the tests of "fb2zip test" (the tool tip tells why), else the FB2 or ZIP icon
        private List<String> ShowFileState(ListViewItem item, FileInfo file)
        {
            List<String> problems;
            try
            {
                problems = Fb2FileChecker.Check(file.FullName);
            }
            catch (Exception exception)
            {
                problems = new List<String> { $"cannot check: {exception.Message}" };
            }

            var isZip = file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

            item.ImageKey = (problems.Count > 0) ? MainForm.ErrorImageKey : (isZip ? MainForm.ZipImageKey : MainForm.Fb2ImageKey);
            item.ToolTipText = String.Join("\n", problems);

            return problems;
        }

        private FileInfo GetSelectedFile() => (this.listViewFiles.SelectedItems.Count > 0) ? this.listViewFiles.SelectedItems[0].Tag as FileInfo : null;

        // Re-read is available only after the user changed a text field; Save also if the file does not pass the tests (the rename may fix it)
        private void SetModified(Boolean modified)
        {
            this._modified = modified;
            this.UpdateButtons();
        }

        private void UpdateButtons()
        {
            this.buttonReRead.Enabled = this._modified;
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
                this.SetModified(true);
            }
        }

        private Fb2Info ReadFields()
        {
            var authorFirstName = this.textBoxAuthorFirst.Text.Trim();
            var authorLastName = this.textBoxAuthorLast.Text.Trim();

            return new Fb2Info
            {
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

        // Throws away the changes in the text fields
        private void ReReadSelectedFile()
        {
            var file = this.GetSelectedFile();
            if (file == null)
            {
                return;
            }

            try
            {
                this.ShowInfo(Fb2InfoReader.ReadFile(file.FullName));
                this.toolStripStatusLabelFile.Text = $"{file.FullName} (re-read)";
            }
            catch (Exception exception)
            {
                this.ShowError($"Cannot read {file.Name}:\n{exception.Message}");
            }
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
            this.FillFileList(file.Directory, Path.GetFileName(zipFilePath));
            this.toolStripStatusLabelFile.Text = $"{zipFilePath} (packed)";
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
                    this.FillFileList(file.Directory, newName);
                    this.toolStripStatusLabelFile.Text = $"{Path.Combine(file.DirectoryName, newName)} (saved and renamed)";
                }
                else
                {
                    this.ShowInfo(Fb2InfoReader.ReadFile(file.FullName));
                    this.ShowTestResults(this.ShowFileState(this.listViewFiles.SelectedItems[0], file)); // the file may pass or fail the tests differently now
                    this.toolStripStatusLabelFile.Text = $"{file.FullName} (saved)";
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
            var baseName = Fb2FileName.Build(fields, out _);
            if (baseName == null)
            {
                this.ShowError($"{MainForm.NotRenamedMessage}The file name is built from the title (and the author, if any); the title is required.");
                return null;
            }

            var extension = file.Name.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase) ? file.Name.Substring(file.Name.Length - ".fb2.zip".Length) : file.Name.Substring(file.Name.Length - ".fb2".Length);
            var newName = Fb2FileName.MakeFileSystemSafe(baseName) + extension;

            if (String.Equals(newName, file.Name, StringComparison.Ordinal))
            {
                this.toolStripStatusLabelFile.Text = $"{file.Name} already has this name";
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
                this.textBoxAuthorFirst.Text = info?.AuthorFirstName ?? String.Empty;
                this.textBoxAuthorLast.Text = info?.AuthorLastName ?? String.Empty;
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

            this.SetModified(false);
        }

        private void selectFolderToolStripMenuItem_Click(Object sender, EventArgs e) => this.SelectFolder();

        private void exitToolStripMenuItem_Click(Object sender, EventArgs e) => this.Close();
    }
}
