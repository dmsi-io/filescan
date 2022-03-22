using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Dmsi.Agility.Resource.MatchBuilder
{
    public partial class MainForm : Form
    {
        private Matcher _definition;
        private ListViewColumnSorter _lvwColumnSorter;

        public MainForm()
        {
            InitializeComponent();

            // Create an instance of a ListView column sorter and assign it to the ListView control.
            _lvwColumnSorter = new ListViewColumnSorter();
            this.listView2.ListViewItemSorter = _lvwColumnSorter;

            Application.Idle += new EventHandler(Application_Idle);
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            newToolStripMenuItem.Enabled = true;
            newToolStripButton.Enabled = true;

            openToolStripMenuItem.Enabled = true;
            openToolStripButton.Enabled = openToolStripMenuItem.Enabled;
            
            saveToolStripMenuItem.Enabled = _definition != null && _definition.IsDirty;
            saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled;

            saveAsToolStripMenuItem.Enabled = _definition != null;
            importFilesToolStripMenuItem.Enabled = _definition != null;
            importDirectoryToolStripMenuItem.Enabled = _definition != null;

            generateResxToolStripMenuItem.Enabled = _definition != null && !stopToolStripButton.Enabled;
            generateToolStripButton.Enabled = generateResxToolStripMenuItem.Enabled;
            cmdGo.Enabled = generateToolStripButton.Enabled;


            closeToolStripMenuItem.Enabled = _definition != null;

            deleteToolStripMenuItem.Enabled = listView1.SelectedItems.Count > 0;
            deleteToolStripButton.Enabled = deleteToolStripMenuItem.Enabled;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "*.rgex";
            openFileDialog1.Filter = "Definition files (*.rgex)|*.rgex|All files (*.*)|*.*";
            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _definition = new Matcher(openFileDialog1.FileName);
                    this.Text = Path.GetFileName(_definition.FileName) + " - File Scan";
                    toolStripStatusLabel1.Text = _definition.FileName;
                    txtRegEx.Text = _definition.RegEx;
                    firstMatchToolStripButton.Checked = _definition.FirstMatchOnly;

                    listView1.Items.Clear();

                    foreach (ResourceNode node in _definition.Nodes)
                    {
                        try
                        {
                            ListViewItem item;

                            if (node.IsFolder)
                            {
                                item = new ListViewItem(node.Name, "#FOLDER#");
                            }
                            else
                            {
                                AddToImageList(node.Name, node.Value);
                                item = new ListViewItem(node.Name, node.Name);
                            }

                            item.SubItems.Add(node.Value);
                            item.SubItems.Add(node.IsFolder ? "Folder" : "File");
                            item.Tag = node;

                            listView1.Items.Add(item);
                        }
                        catch
                        {
                            MessageBox.Show(this, "Unable to load '" + node.Name + "'.\n\nVerify that the image is not corrupted,\nor the image has the wrong extension. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch
                {
                    MessageBox.Show(this, "Unable to load definition file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeToolStripMenuItem_Click(null, null);

            _definition = new Matcher();
            _definition.IsDirty = true;
            txtRegEx.Text = "";
            firstMatchToolStripButton.Checked = true;
            this.Text = _definition.FileName + " - File Scan";
            toolStripStatusLabel1.Text = _definition.FileName;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_definition.FileName.Equals("untitled.rgex", StringComparison.CurrentCultureIgnoreCase))
            {
                saveFileDialog1.FileName = _definition.FileName;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    _definition.FirstMatchOnly = firstMatchToolStripButton.Checked; 
                    _definition.RegEx = txtRegEx.Text;
                    _definition.Save(saveFileDialog1.FileName);
                    this.Text = _definition.FileName + " - File Scan";
                    toolStripStatusLabel1.Text = Path.GetFullPath(_definition.FileName);
                }
            }
            else
            {
                _definition.FirstMatchOnly = firstMatchToolStripButton.Checked;
                _definition.RegEx = txtRegEx.Text;
                _definition.Save();
                this.Text = _definition.FileName + " - File Scan";
                toolStripStatusLabel1.Text = Path.GetFullPath(_definition.FileName);
            }
        }

        private void importFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "*.cls;*.w;*.p;*.i;*.t";
            openFileDialog1.Filter = "All files (*.*)|*.*";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK && openFileDialog1.FileNames.Length > 0)
            {
                foreach (string fileName in openFileDialog1.FileNames)
                {
                    try
                    {
                        ResourceNode node = new ResourceNode();
                        node.Name = Path.GetFileNameWithoutExtension(fileName);
                        node.Extension = Path.GetExtension(fileName).Trim('.').ToUpper();
                        node.Value = fileName;
                        node.IsFolder = false;

                        if (!_definition.Contains(node.Name))
                        {
                            _definition.Nodes.Add(node);
                            _definition.IsDirty = true;

                            try
                            {
                                ListViewItem item;

                                AddToImageList(node.Name, node.Value);
                                item = new ListViewItem(node.Name, node.Name);
                                item.SubItems.Add(node.Value);
                                item.SubItems.Add("File");
                                item.Tag = node;

                                listView1.Items.Add(item);
                            }
                            catch
                            {
                                MessageBox.Show(this, "Unable to load '" + node.Name + "'.\n\nVerify that the file is not corrupted. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }                            
                        }
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(this, "Unable to load " + fileName + ". " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void importDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    ResourceNode node = new ResourceNode();
                    node.Name = Path.GetFileName(folderBrowserDialog1.SelectedPath);

                    if (node.Name.Trim() == "")
                        node.Name = folderBrowserDialog1.SelectedPath;

                    node.Value = folderBrowserDialog1.SelectedPath;
                    node.IsFolder = true;

                    if (!_definition.ContainsValue(node.Value))
                    {
                        _definition.Nodes.Add(node);
                        _definition.IsDirty = true;

                        ListViewItem item = new ListViewItem(node.Name, "#FOLDER#");
                        item.SubItems.Add(node.Value);
                        item.SubItems.Add("Folder");
                        item.Tag = node;

                        listView1.Items.Add(item);
                    }
                }
                catch
                {
                    MessageBox.Show(this, "Unable to load directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            newToolStripMenuItem.Enabled = true;
            openToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = _definition != null && _definition.IsDirty;
            saveAsToolStripMenuItem.Enabled = _definition != null;
            importFilesToolStripMenuItem.Enabled = _definition != null;
            importDirectoryToolStripMenuItem.Enabled = _definition != null;
            generateResxToolStripMenuItem.Enabled = _definition != null;
            closeToolStripMenuItem.Enabled = _definition != null;
        }

        private void detailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listToolStripMenuItem1.Checked = false;
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = false;
            tileToolStripMenuItem.Checked = false;

            listView1.View = View.Details;
        }

        private void largeIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            detailsToolStripMenuItem.Checked = false;
            listToolStripMenuItem1.Checked = false;
            smallIconToolStripMenuItem.Checked = false;
            tileToolStripMenuItem.Checked = false;

            listView1.View = View.LargeIcon;
        }

        private void smallIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            detailsToolStripMenuItem.Checked = false;
            listToolStripMenuItem1.Checked = false;
            largeIconToolStripMenuItem.Checked = false;
            tileToolStripMenuItem.Checked = false;

            listView1.View = View.SmallIcon;
        }

        private void listToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            detailsToolStripMenuItem.Checked = false;
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = false;
            tileToolStripMenuItem.Checked = false;

            listView1.View = View.Details;
        }

        private void tileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            detailsToolStripMenuItem.Checked = false;
            largeIconToolStripMenuItem.Checked = false;
            smallIconToolStripMenuItem.Checked = false;
            listToolStripMenuItem1.Checked = false;

            listView1.View = View.Tile;
        }

        private void generateResxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopToolStripButton.Enabled = true;

            _definition.LoadFailed += _definition_LoadFailed;
            _definition.FileProcessed += _definition_FileProcessed;
            _definition.LoadSucceeded += _definition_LoadSucceeded;
            _definition.MessageGenerated += _definition_MessageGenerated;

            richTextBox1.Clear();
            listView2.Items.Clear();
            richTextBox2.Clear();

            _definition.Cancelled = false;
            backgroundWorker1.RunWorkerAsync();
        }

        private void _definition_MessageGenerated(object sender, MessageGeneratedEventArgs e)
        {
            backgroundWorker1.ReportProgress(50, new ParseUserState() { Status = ParseState.Message, Message = e.Text });
        }

        private void _definition_FileProcessed(object sender, FileProcessedEventArgs e)
        {
            backgroundWorker1.ReportProgress(50, new ParseUserState() { Status = ParseState.Processed, Name = e.Name, Source = e.Source, Matches = e.Matches });
        }

        private void _definition_LoadSucceeded(object sender, LoadSucceededEventArgs e)
        {
            backgroundWorker1.ReportProgress(50, new ParseUserState() { Status = ParseState.Success, Name = e.Name, Source = e.Source, Message = $"{e.Source} - Scanned" });
        }

        private void _definition_LoadFailed(object sender, LoadFailedEventArgs e)
        {
            backgroundWorker1.ReportProgress(50, new ParseUserState() { Status = ParseState.Failed, Name = e.Name, Source = e.Source, Message = $"{e.Source} - Failed: {e.Error}" });
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                propertyGrid1.SelectedObject = listView1.SelectedItems[0].Tag;
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = _definition.FileName;
            
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _definition.RegEx = txtRegEx.Text;
                _definition.Save(saveFileDialog1.FileName);
                this.Text = _definition.FileName + " - File Scan";
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_definition != null)
            {
                _definition.Nodes.Clear();
                _definition = null;
            }

            this.Text = "File Scan";
            toolStripStatusLabel1.Text = "";
            listView1.Items.Clear();
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
            {
                if(propertyGrid1.SelectedObject.Equals(item.Tag))
                {
                    ResourceNode node = propertyGrid1.SelectedObject as ResourceNode;
                    item.Text = node.Name;
                    _definition.IsDirty = true;
                }
            }
        }

        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            deleteToolStripMenuItem.Enabled = listView1.SelectedItems.Count > 0;
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach(ListViewItem item in listView1.SelectedItems)
            {
                propertyGrid1.SelectedObject = null;
                
                _definition.Nodes.Remove(item.Tag);
                _definition.IsDirty = true;

                ResourceNode node = item.Tag as ResourceNode;
                if (!node.IsFolder)
                    RemoveFromImageList(node.Name);

                item.Tag = null;
                listView1.Items.Remove(item);
            }
        }

        private void AddToImageList(string key, string fileName)
        {
            largeImageList.Images.Add(key, Image.FromFile(fileName).GetThumbnailImage(32, 32, null, IntPtr.Zero));
            smallImageList.Images.Add(key, Image.FromFile(fileName).GetThumbnailImage(16, 16, null, IntPtr.Zero));
        }

        private void RemoveFromImageList(string key)
        {
            largeImageList.Images.RemoveByKey(key);
            smallImageList.Images.RemoveByKey(key);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            _definition.FirstMatchOnly = firstMatchToolStripButton.Checked;
            _definition.RegEx = txtRegEx.Text;
            string location = _definition.ParseFiles();
            e.Result = location;

            if (_definition.Cancelled)
                e.Cancel = true;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!this.IsDisposed)
            {
                ParseUserState arg = (ParseUserState)e.UserState;
                toolStripStatusLabel1.Text = arg.Source;

                if (arg.Status == ParseState.Processed)
                {
                    var i = listView2.Items.Count;
                    ListViewItem lvi = new ListViewItem(arg.Name, i);
                    lvi.SubItems.Add(arg.Source);
                    lvi.Tag = arg.Matches;
                    listView2.Items.Add(lvi);
                }
                else if (arg.Status == ParseState.Failed)
                {
                    StringBuilder text = new StringBuilder(richTextBox1.Text);
                    text.Append($"{arg.Message}\r\n");
                    richTextBox1.Text = text.ToString();
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.ScrollToCaret();
                } 
                else if (arg.Status == ParseState.Message)
                {
                    StringBuilder text = new StringBuilder(richTextBox1.Text);
                    text.Append($"{arg.Message}\r\n");
                    richTextBox1.Text = text.ToString();
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.ScrollToCaret();
                }
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _definition.LoadFailed -= _definition_LoadFailed;
            _definition.FileProcessed -= _definition_FileProcessed;
            _definition.LoadSucceeded -= _definition_LoadSucceeded;
            _definition.MessageGenerated -= _definition_MessageGenerated;

            if (!this.IsDisposed && !e.Cancelled)
                MessageBox.Show(this, "Finished parsing files.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

            toolStripStatusLabel1.Text = _definition.FileName;
            stopToolStripButton.Enabled = false;
        }

        private void propertyWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = !splitContainer1.Panel2Collapsed;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // wait for the background worker
            if (backgroundWorker1.IsBusy)
            {
                _definition.Cancelled = true;
            }
        }

        private void stopToolStripButton_Click(object sender, EventArgs e)
        {
            stopToolStripButton.Enabled = false;

            _definition.Cancelled = true;
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection items = listView2.SelectedItems;

            foreach (ListViewItem item in items)
            {
                richTextBox2.Clear();

                List<string> result = (List<string>)item.Tag;
                foreach(string s in result)
                {
                    richTextBox2.AppendText($"{s}\r\n");

                    if(result.Count > 1)
                        richTextBox2.AppendText("----------\r\n");
                }
            }


        }

        private void listView2_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == _lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (_lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    _lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    _lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                _lvwColumnSorter.SortColumn = e.Column;
                _lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.listView2.Sort();
        }
    }
    public class ParseUserState
    {
        public ParseState Status
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Source
        {
            get;
            set;
        }
        public string Message
        {
            get;
            set;
        }

        public List<string> Matches
        {
            get;
            set;
        }
    }

    public enum ParseState
    {
        Failed,
        Success,
        Processed,
        Message
    }

    /// <summary>
    /// This class is an implementation of the 'IComparer' interface.
    /// </summary>
    public class ListViewColumnSorter : IComparer
    {
        /// <summary>
        /// Specifies the column to be sorted
        /// </summary>
        private int ColumnToSort;
        /// <summary>
        /// Specifies the order in which to sort (i.e. 'Ascending').
        /// </summary>
        private SortOrder OrderOfSort;
        /// <summary>
        /// Case insensitive comparer object
        /// </summary>
        private CaseInsensitiveComparer ObjectCompare;

        /// <summary>
        /// Class constructor.  Initializes various elements
        /// </summary>
        public ListViewColumnSorter()
        {
            // Initialize the column to '0'
            ColumnToSort = 0;

            // Initialize the sort order to 'none'
            OrderOfSort = SortOrder.None;

            // Initialize the CaseInsensitiveComparer object
            ObjectCompare = new CaseInsensitiveComparer();
        }

        /// <summary>
        /// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First object to be compared</param>
        /// <param name="y">Second object to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        public int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;

            // Cast the objects to be compared to ListViewItem objects
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;

            decimal num = 0;
            if (decimal.TryParse(listviewX.SubItems[ColumnToSort].Text, out num))
            {
                compareResult = decimal.Compare(num, Convert.ToDecimal(listviewY.SubItems[ColumnToSort].Text));
            }
            else
            {
                // Compare the two items
                compareResult = ObjectCompare.Compare(listviewX.SubItems[ColumnToSort].Text, listviewY.SubItems[ColumnToSort].Text);
            }

            // Calculate correct return value based on object comparison
            if (OrderOfSort == SortOrder.Ascending)
            {
                // Ascending sort is selected, return normal result of compare operation
                return compareResult;
            }
            else if (OrderOfSort == SortOrder.Descending)
            {
                // Descending sort is selected, return negative result of compare operation
                return (-compareResult);
            }
            else
            {
                // Return '0' to indicate they are equal
                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
        /// </summary>
        public int SortColumn
        {
            set
            {
                ColumnToSort = value;
            }
            get
            {
                return ColumnToSort;
            }
        }

        /// <summary>
        /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
        /// </summary>
        public SortOrder Order
        {
            set
            {
                OrderOfSort = value;
            }
            get
            {
                return OrderOfSort;
            }
        }

    }
}
