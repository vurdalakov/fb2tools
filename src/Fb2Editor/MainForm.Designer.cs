namespace Fb2Editor
{
    using System.Drawing;
    using System.Windows.Forms;

    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip1 = new MenuStrip();
            this.fileToolStripMenuItem = new ToolStripMenuItem();
            this.selectFolderToolStripMenuItem = new ToolStripMenuItem();
            this.toolStripMenuItem1 = new ToolStripSeparator();
            this.exitToolStripMenuItem = new ToolStripMenuItem();
            this.statusStrip1 = new StatusStrip();
            this.toolStripStatusLabelStatus = new ToolStripStatusLabel();
            this.toolStripStatusLabelRootFolder = new ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new ToolStripStatusLabel();
            this.splitContainer1 = new SplitContainer();
            this.listViewFiles = new ListView();
            this.columnHeader1 = new ColumnHeader();
            this.checkBoxAnthology = new CheckBox();
            this.label7 = new Label();
            this.textBoxNewName = new TextBox();
            this.label6 = new Label();
            this.textBoxFileName = new TextBox();
            this.buttonPack = new Button();
            this.textBoxTestResults = new TextBox();
            this.label5 = new Label();
            this.buttonSave = new Button();
            this.buttonReRead = new Button();
            this.textBoxSeriesNumber = new TextBox();
            this.textBoxSeriesTitle = new TextBox();
            this.label4 = new Label();
            this.textBoxTitle = new TextBox();
            this.label3 = new Label();
            this.textBoxTranslatorLast = new TextBox();
            this.textBoxTranslatorFirst = new TextBox();
            this.label2 = new Label();
            this.textBoxAuthorLast = new TextBox();
            this.textBoxAuthorFirst = new TextBox();
            this.labelAuthor = new Label();
            this.toolStrip1 = new ToolStrip();
            this.toolStripLabelFilter = new ToolStripLabel();
            this.toolStripTextBoxFilter = new ToolStripTextBox();
            this.toolStripSeparator1 = new ToolStripSeparator();
            this.toolStripButtonShowErrorsOnly = new ToolStripButton();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.splitContainer1).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new Size(24, 24);
            this.menuStrip1.Items.AddRange(new ToolStripItem[] { this.fileToolStripMenuItem });
            this.menuStrip1.Location = new Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new Size(1683, 33);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { this.selectFolderToolStripMenuItem, this.toolStripMenuItem1, this.exitToolStripMenuItem });
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new Size(54, 29);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // selectFolderToolStripMenuItem
            // 
            this.selectFolderToolStripMenuItem.Name = "selectFolderToolStripMenuItem";
            this.selectFolderToolStripMenuItem.Size = new Size(227, 34);
            this.selectFolderToolStripMenuItem.Text = "&Select Folder...";
            this.selectFolderToolStripMenuItem.Click += this.selectFolderToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new Size(224, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.X;
            this.exitToolStripMenuItem.Size = new Size(227, 34);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += this.exitToolStripMenuItem_Click;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new Size(24, 24);
            this.statusStrip1.Items.AddRange(new ToolStripItem[] { this.toolStripStatusLabelStatus, this.toolStripStatusLabelRootFolder, this.toolStripStatusLabel1 });
            this.statusStrip1.Location = new Point(0, 879);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new Size(1683, 32);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabelStatus
            // 
            this.toolStripStatusLabelStatus.AutoSize = false;
            this.toolStripStatusLabelStatus.Name = "toolStripStatusLabelStatus";
            this.toolStripStatusLabelStatus.Size = new Size(1498, 25);
            this.toolStripStatusLabelStatus.Spring = true;
            this.toolStripStatusLabelStatus.Text = " ";
            this.toolStripStatusLabelStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toolStripStatusLabelRootFolder
            // 
            this.toolStripStatusLabelRootFolder.Name = "toolStripStatusLabelRootFolder";
            this.toolStripStatusLabelRootFolder.Size = new Size(105, 25);
            this.toolStripStatusLabelRootFolder.Text = "Root Folder";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new Size(19, 25);
            this.toolStripStatusLabel1.Text = "-";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = DockStyle.Fill;
            this.splitContainer1.FixedPanel = FixedPanel.Panel2;
            this.splitContainer1.Location = new Point(0, 67);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listViewFiles);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.checkBoxAnthology);
            this.splitContainer1.Panel2.Controls.Add(this.label7);
            this.splitContainer1.Panel2.Controls.Add(this.textBoxNewName);
            this.splitContainer1.Panel2.Controls.Add(this.label6);
            this.splitContainer1.Panel2.Controls.Add(this.textBoxFileName);
            this.splitContainer1.Panel2.Controls.Add(this.buttonPack);
            this.splitContainer1.Panel2.Controls.Add(this.textBoxTestResults);
            this.splitContainer1.Panel2.Controls.Add(this.label5);
            this.splitContainer1.Panel2.Controls.Add(this.buttonSave);
            this.splitContainer1.Panel2.Controls.Add(this.buttonReRead);
            this.splitContainer1.Panel2.Controls.Add(this.textBoxSeriesNumber);
            this.splitContainer1.Panel2.Controls.Add(this.textBoxSeriesTitle);
            this.splitContainer1.Panel2.Controls.Add(this.label4);
            this.splitContainer1.Panel2.Controls.Add(this.textBoxTitle);
            this.splitContainer1.Panel2.Controls.Add(this.label3);
            this.splitContainer1.Panel2.Controls.Add(this.textBoxTranslatorLast);
            this.splitContainer1.Panel2.Controls.Add(this.textBoxTranslatorFirst);
            this.splitContainer1.Panel2.Controls.Add(this.label2);
            this.splitContainer1.Panel2.Controls.Add(this.textBoxAuthorLast);
            this.splitContainer1.Panel2.Controls.Add(this.textBoxAuthorFirst);
            this.splitContainer1.Panel2.Controls.Add(this.labelAuthor);
            this.splitContainer1.Size = new Size(1683, 812);
            this.splitContainer1.SplitterDistance = 818;
            this.splitContainer1.TabIndex = 3;
            // 
            // listViewFiles
            // 
            this.listViewFiles.Columns.AddRange(new ColumnHeader[] { this.columnHeader1 });
            this.listViewFiles.Dock = DockStyle.Fill;
            this.listViewFiles.FullRowSelect = true;
            this.listViewFiles.Location = new Point(0, 0);
            this.listViewFiles.MultiSelect = false;
            this.listViewFiles.Name = "listViewFiles";
            this.listViewFiles.Size = new Size(818, 812);
            this.listViewFiles.TabIndex = 0;
            this.listViewFiles.UseCompatibleStateImageBehavior = false;
            this.listViewFiles.View = View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            // 
            // checkBoxAnthology
            // 
            this.checkBoxAnthology.AutoSize = true;
            this.checkBoxAnthology.Location = new Point(715, 147);
            this.checkBoxAnthology.Name = "checkBoxAnthology";
            this.checkBoxAnthology.Size = new Size(122, 29);
            this.checkBoxAnthology.TabIndex = 22;
            this.checkBoxAnthology.Text = "Anthology";
            this.checkBoxAnthology.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new Point(29, 82);
            this.label7.Name = "label7";
            this.label7.Size = new Size(100, 25);
            this.label7.TabIndex = 21;
            this.label7.Text = "New name:";
            // 
            // textBoxNewName
            // 
            this.textBoxNewName.Location = new Point(176, 79);
            this.textBoxNewName.Name = "textBoxNewName";
            this.textBoxNewName.ReadOnly = true;
            this.textBoxNewName.Size = new Size(661, 31);
            this.textBoxNewName.TabIndex = 20;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new Point(29, 33);
            this.label6.Name = "label6";
            this.label6.Size = new Size(91, 25);
            this.label6.TabIndex = 19;
            this.label6.Text = "File name:";
            // 
            // textBoxFileName
            // 
            this.textBoxFileName.Location = new Point(176, 30);
            this.textBoxFileName.Name = "textBoxFileName";
            this.textBoxFileName.ReadOnly = true;
            this.textBoxFileName.Size = new Size(661, 31);
            this.textBoxFileName.TabIndex = 18;
            // 
            // buttonPack
            // 
            this.buttonPack.Location = new Point(575, 422);
            this.buttonPack.Name = "buttonPack";
            this.buttonPack.Size = new Size(262, 43);
            this.buttonPack.TabIndex = 17;
            this.buttonPack.Text = "Pack File";
            this.buttonPack.UseVisualStyleBackColor = true;
            // 
            // textBoxTestResults
            // 
            this.textBoxTestResults.Location = new Point(29, 576);
            this.textBoxTestResults.Multiline = true;
            this.textBoxTestResults.Name = "textBoxTestResults";
            this.textBoxTestResults.ReadOnly = true;
            this.textBoxTestResults.Size = new Size(808, 208);
            this.textBoxTestResults.TabIndex = 16;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new Point(29, 536);
            this.label5.Name = "label5";
            this.label5.Size = new Size(102, 25);
            this.label5.TabIndex = 15;
            this.label5.Text = "Test results:";
            // 
            // buttonSave
            // 
            this.buttonSave.Location = new Point(176, 422);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new Size(368, 43);
            this.buttonSave.TabIndex = 14;
            this.buttonSave.Text = "Save Metadata and Rename File";
            this.buttonSave.UseVisualStyleBackColor = true;
            // 
            // buttonReRead
            // 
            this.buttonReRead.Location = new Point(29, 422);
            this.buttonReRead.Name = "buttonReRead";
            this.buttonReRead.Size = new Size(71, 43);
            this.buttonReRead.TabIndex = 11;
            this.buttonReRead.Text = "Reset";
            this.buttonReRead.UseVisualStyleBackColor = true;
            // 
            // textBoxSeriesNumber
            // 
            this.textBoxSeriesNumber.Location = new Point(726, 360);
            this.textBoxSeriesNumber.Name = "textBoxSeriesNumber";
            this.textBoxSeriesNumber.Size = new Size(111, 31);
            this.textBoxSeriesNumber.TabIndex = 10;
            // 
            // textBoxSeriesTitle
            // 
            this.textBoxSeriesTitle.Location = new Point(176, 360);
            this.textBoxSeriesTitle.Name = "textBoxSeriesTitle";
            this.textBoxSeriesTitle.Size = new Size(532, 31);
            this.textBoxSeriesTitle.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new Point(29, 363);
            this.label4.Name = "label4";
            this.label4.Size = new Size(62, 25);
            this.label4.TabIndex = 8;
            this.label4.Text = "Series:";
            // 
            // textBoxTitle
            // 
            this.textBoxTitle.Location = new Point(176, 296);
            this.textBoxTitle.Name = "textBoxTitle";
            this.textBoxTitle.Size = new Size(661, 31);
            this.textBoxTitle.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new Point(29, 299);
            this.label3.Name = "label3";
            this.label3.Size = new Size(48, 25);
            this.label3.TabIndex = 6;
            this.label3.Text = "Title:";
            // 
            // textBoxTranslatorLast
            // 
            this.textBoxTranslatorLast.Location = new Point(509, 236);
            this.textBoxTranslatorLast.Name = "textBoxTranslatorLast";
            this.textBoxTranslatorLast.Size = new Size(328, 31);
            this.textBoxTranslatorLast.TabIndex = 5;
            // 
            // textBoxTranslatorFirst
            // 
            this.textBoxTranslatorFirst.Location = new Point(176, 236);
            this.textBoxTranslatorFirst.Name = "textBoxTranslatorFirst";
            this.textBoxTranslatorFirst.Size = new Size(307, 31);
            this.textBoxTranslatorFirst.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new Point(29, 239);
            this.label2.Name = "label2";
            this.label2.Size = new Size(92, 25);
            this.label2.TabIndex = 3;
            this.label2.Text = "Translator:";
            // 
            // textBoxAuthorLast
            // 
            this.textBoxAuthorLast.Location = new Point(509, 182);
            this.textBoxAuthorLast.Name = "textBoxAuthorLast";
            this.textBoxAuthorLast.Size = new Size(328, 31);
            this.textBoxAuthorLast.TabIndex = 2;
            // 
            // textBoxAuthorFirst
            // 
            this.textBoxAuthorFirst.Location = new Point(176, 182);
            this.textBoxAuthorFirst.Name = "textBoxAuthorFirst";
            this.textBoxAuthorFirst.Size = new Size(307, 31);
            this.textBoxAuthorFirst.TabIndex = 1;
            // 
            // labelAuthor
            // 
            this.labelAuthor.AutoSize = true;
            this.labelAuthor.Location = new Point(29, 185);
            this.labelAuthor.Name = "labelAuthor";
            this.labelAuthor.Size = new Size(71, 25);
            this.labelAuthor.TabIndex = 0;
            this.labelAuthor.Text = "Author:";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new Size(24, 24);
            this.toolStrip1.Items.AddRange(new ToolStripItem[] { this.toolStripLabelFilter, this.toolStripTextBoxFilter, this.toolStripSeparator1, this.toolStripButtonShowErrorsOnly });
            this.toolStrip1.Location = new Point(0, 33);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new Size(1683, 34);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabelFilter
            // 
            this.toolStripLabelFilter.Name = "toolStripLabelFilter";
            this.toolStripLabelFilter.Size = new Size(54, 29);
            this.toolStripLabelFilter.Text = "Filter:";
            // 
            // toolStripTextBoxFilter
            // 
            this.toolStripTextBoxFilter.Name = "toolStripTextBoxFilter";
            this.toolStripTextBoxFilter.Size = new Size(200, 34);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Margin = new Padding(20, 0, 20, 0);
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new Size(6, 34);
            // 
            // toolStripButtonShowErrorsOnly
            // 
            this.toolStripButtonShowErrorsOnly.Image = (Image)resources.GetObject("toolStripButtonShowErrorsOnly.Image");
            this.toolStripButtonShowErrorsOnly.ImageTransparentColor = Color.Magenta;
            this.toolStripButtonShowErrorsOnly.Name = "toolStripButtonShowErrorsOnly";
            this.toolStripButtonShowErrorsOnly.Size = new Size(174, 29);
            this.toolStripButtonShowErrorsOnly.Text = "Show errors only";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new SizeF(10F, 25F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1683, 911);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "FB2 Editor";
            this.Load += this.MainForm_Load;
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)this.splitContainer1).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private StatusStrip statusStrip1;
        private SplitContainer splitContainer1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ListView listViewFiles;
        private ColumnHeader columnHeader1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private Label labelAuthor;
        private TextBox textBoxAuthorLast;
        private TextBox textBoxAuthorFirst;
        private TextBox textBoxTranslatorLast;
        private TextBox textBoxTranslatorFirst;
        private Label label2;
        private TextBox textBoxTitle;
        private Label label3;
        private TextBox textBoxSeriesTitle;
        private Label label4;
        private TextBox textBoxSeriesNumber;
        private ToolStripStatusLabel toolStripStatusLabelStatus;
        private ToolStripStatusLabel toolStripStatusLabelRootFolder;
        private ToolStripMenuItem selectFolderToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStrip toolStrip1;
        private Button buttonSave;
        private Button buttonReRead;
        private TextBox textBoxTestResults;
        private Label label5;
        private Button buttonPack;
        private Label label6;
        private TextBox textBoxFileName;
        private Label label7;
        private TextBox textBoxNewName;
        private CheckBox checkBoxAnthology;
        private ToolStripButton toolStripButtonShowErrorsOnly;
        private ToolStripLabel toolStripLabelFilter;
        private ToolStripTextBox toolStripTextBoxFilter;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripStatusLabel toolStripStatusLabel1;
    }
}
