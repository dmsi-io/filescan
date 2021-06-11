using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Dmsi.Agility.Resource.ResourceBuilder
{
    public partial class MainForm : Form
    {
        private ResourceDefinition _definition;

        public MainForm()
        {
            InitializeComponent();

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
            
            closeToolStripMenuItem.Enabled = _definition != null;

            deleteToolStripMenuItem.Enabled = listView1.SelectedItems.Count > 0;
            deleteToolStripButton.Enabled = deleteToolStripMenuItem.Enabled;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "*.agil";
            openFileDialog1.Filter = "Definition files (*.agil)|*.agil|All files (*.*)|*.*";
            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    _definition = new ResourceDefinition(openFileDialog1.FileName);
                    this.Text = Path.GetFileName(_definition.FileName) + " - Resource Manager";
                    toolStripStatusLabel1.Text = _definition.FileName;

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

            _definition = new ResourceDefinition();
            _definition.IsDirty = true;
            this.Text = _definition.FileName + " - Resource Manager";
            toolStripStatusLabel1.Text = _definition.FileName;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_definition.FileName.Equals("untitled.agil", StringComparison.CurrentCultureIgnoreCase))
            {
                saveFileDialog1.FileName = _definition.FileName;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    _definition.Save(saveFileDialog1.FileName);
                    toolStripStatusLabel1.Text = Path.GetFullPath(_definition.FileName);
                }
            }
            else
            {
                _definition.Save();
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
                                MessageBox.Show(this, "Unable to load '" + node.Name + "'.\n\nVerify that the image is not corrupted,\nor the image has the wrong extension. ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            richTextBox1.Text = "";

            _definition.Cancelled = false;
            backgroundWorker1.RunWorkerAsync();
        }

        private void _definition_FileProcessed(object sender, FileProcessedEventArgs e)
        {
            backgroundWorker1.ReportProgress(50, new ParseUserState() { Status = ParseState.Processed, Name = e.Name, Source = e.Source });
        }

        private void _definition_LoadSucceeded(object sender, LoadSucceededEventArgs e)
        {
            backgroundWorker1.ReportProgress(50, new ParseUserState() { Status = ParseState.Success, Name = e.Name, Source = e.Source, Message = $"{e.Source} - Successful" });
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
                _definition.Save(saveFileDialog1.FileName);
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_definition != null)
            {
                _definition.Nodes.Clear();
                _definition = null;
            }

            this.Text = "Resource Manager";
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
                    richTextBox2.AppendText("- " + arg.Name + " - " + arg.Source + "\r\n");
                else
                    richTextBox1.AppendText("- " + arg.Message + "\r\n");              
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!this.IsDisposed && !e.Cancelled)
            {
                _definition.LoadFailed -= _definition_LoadFailed;
                _definition.FileProcessed -= _definition_FileProcessed;
                _definition.LoadSucceeded -= _definition_LoadSucceeded;

                MessageBox.Show(this, "Finished parsing files.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

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
    }

    public enum ParseState
    {
        Failed,
        Success,
        Processed
    }

}
