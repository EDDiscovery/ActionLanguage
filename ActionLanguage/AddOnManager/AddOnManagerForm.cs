/*
 * Copyright © 2017-2025 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ActionLanguage.Manager
{
    public partial class AddOnManagerForm : ExtendedControls.DraggableForm
    {
        // list of packs changed now, - remove, + install, ++ updated
        public Dictionary<string, string> InstalledDeinstallNow { get; set; }  = new Dictionary<string, string>();
        // list of packs to change at start up, - remove, + install, ++ updated
        public Dictionary<string, string> InstallDeinstallAtStartupList { get { return gitactionfiles.VersioningManager.InstallDeinstallAtStartupList; } }

        public Action<string> EditActionFile;
        public Action EditGlobals;
        public Action CreateActionFile;
        public Func<string,bool> CheckActionLoaded;
        public Action<DownloadItem> DeleteActionFile;

        static public string ActionFileWildCard { get; set; } = "*.act";
        static public string InfFileWildCard { get; set; } = "*.inf";
        public AddOnManagerForm()
        {
            InitializeComponent();
        }

        // manageddownloadmode = true = manage downloads, get from git (or use temp folder dependent on downloadgit)
        // else just show actions and allow editing of them
        public void Init(string progtype, 
                        bool managedownloadmode,        // if in manage download, or just edit
                        bool downloadgit,       // if to go to github for data
                        Icon ic, Version version, 
                        string approotfolder,  // root folder
                        string actfolder,       // where act's are stored
                        string otherinstalledfilesfolder,       // where other ones are stored, null if not supported
                        string tempdatafolder, // where to download temp files to
                        string githuburl,       // url for github
                        Dictionary<string,string> installdeinstallatstartup,    // list of installdeinstalls
                        string prepopulatedzipfolder = null 
            )
        {
            this.progtype = progtype;
            this.Icon = ic;
            this.managedownloadmode = managedownloadmode;
            this.githuburl = githuburl;
            this.downloadgit = downloadgit;
            this.edversion = version;

            // make the git class which knows about the structure of git etc
            gitactionfiles = new GitActionFiles(approotfolder, tempdatafolder,prepopulatedzipfolder);

            // set the stored install/deinstall list
            gitactionfiles.VersioningManager.InstallDeinstallAtStartupList = installdeinstallatstartup;
            // read local stored files
            gitactionfiles.ReadLocalFolder(actfolder, gitactionfiles.ActionFileWildCard, "Action Files");
            if ( otherinstalledfilesfolder!=null)
                gitactionfiles.ReadLocalFolder(otherinstalledfilesfolder, gitactionfiles.InfFileWildCard, "Other Files");

            SizeF prev = this.AutoScaleDimensions;

            var theme = ExtendedControls.Theme.Current;
            bool winborder = theme.ApplyStd(this);      // changing FONT changes the autoscale since form is in AutoScaleMode=font

            //System.Diagnostics.Debug.WriteLine("Scale factor " + prev + "->" + this.AutoScaleDimensions);

            statusStripCustom.Visible = panelTop.Visible = panelTop.Enabled = !winborder;
            richTextBoxScrollDescription.ReadOnly = true;

            buttonExtGlobals.Visible = !this.managedownloadmode;

            var enumlist = new Enum[] { EDTx.AddOnManagerForm_buttonExtGlobals };
            BaseUtils.Translator.Instance.TranslateControls(this, enumlist);

            label_index.Text = this.Text = (this.managedownloadmode) ? "Add-On Manager".T(EDTx.AddOnManagerForm_AddOnTitle) : "Edit Add-Ons".T(EDTx.AddOnManagerForm_EditTitle);
        }


        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.Cursor = Cursors.WaitCursor;

            if (managedownloadmode && downloadgit)      // if from git, we run a task with the display
            {
                gitactionfiles.DownloadFromGit(canceldownload, githuburl, (good) => this.BeginInvoke((MethodInvoker)delegate { AfterDownload(good); }));
            }
            else
            {
                AfterDownload(true);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            canceldownload.Cancel();        // on closing, set cancel token, so downloads are aborted if occurring.  This will stop the DownloadFromGit asap and it will stop the callback
        }

        // in the UI thread, present.  Good indicates all downloads were good
        void AfterDownload(bool gooddownload)
        {
            System.Diagnostics.Debug.Assert(Application.MessageLoop);
            System.Diagnostics.Debug.WriteLine("After download running");

            if (!gooddownload)
            {
                ExtendedControls.MessageBoxTheme.Show("Github download failed\r\nYour internet may be down\r\nGithub may be rate limiting you (Wait 1 hour and try again)\r\nGithub may be down\r\nEDDiscovery will present what it already has but some action packs won't install without internet",
                 "Github failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (canceldownload.IsCancellationRequested)     // if cancelled between thread asking for invoke and here, stop doing anything
            {
                System.Diagnostics.Debug.WriteLine("After download thread cancel recognised ending");
                return;
            }

            this.Cursor = Cursors.Default;

            if (managedownloadmode)                     // if in manage mode, we read the downloaded files and process. If not, we just have the local files
            {
                gitactionfiles.ReadDownloadedFolder(githuburl, edversion, progtype);
            }

            gitactionfiles.VersioningManager.Sort();

            System.Diagnostics.Debug.WriteLine("After download finished, ready to display");
            ReadyToDisplay();
        }

        // refresh the UI

        void ReadyToDisplay()
        {
            panelVScroll.RemoveAllControls();       // blank
            panelVScroll.SuspendLayout();

            //                       0      1       2       3       4       5       6       7       8
            //                      type    i/info  ver     shtdes  state   actbut  delbut  enachk
            int[] tabs = new int[] { 0,     80,     280,    360,    560,    760,    860,    920,    1080};

            var theme = ExtendedControls.Theme.Current;

            int fonth = (int)theme.GetFont.GetHeight() + 1;
            int headervsize =  fonth + panelheightmargin + 2;

            int vpos = headervsize + 8;

            // draw everything in 8.25 point position then scale

            int maxpanelwidth = 0;

            foreach (DownloadItem di in gitactionfiles.VersioningManager.DownloadItems)
            {
                Group g = new Group();
                g.di = di;
                g.panel = new Panel();
                g.panel.BorderStyle = BorderStyle.FixedSingle;
                g.panel.Tag = g;
                g.panel.MouseEnter += MouseEnterControl;

                g.type = new Label();
                g.type.Location = new Point(tabs[0], labelheightmargin);      // 8 spacing, allow 8*4 to indent
                g.type.Size = new Size(tabs[1] - tabs[0], 24);
                g.type.Text = di.ItemType;
                g.panel.Controls.Add(g.type);

                g.info = new ExtendedControls.ExtButton();
                g.info.Location = new Point(tabs[1], labelheightmargin - 4);      // 8 spacing, allow 8*4 to indent
                g.info.Size = new Size(16, 16);
                g.info.Text = "i";
                g.info.Click += Info_Click;
                g.info.Tag = g;
                g.panel.Controls.Add(g.info);

                g.name = new Label();
                g.name.Location = new Point(tabs[1] + 32, labelheightmargin);      // 8 spacing, allow 8*4 to indent
                g.name.Size = new Size(tabs[2] - tabs[1] - 32, 24);
                g.name.Text = di.ItemName;
                g.panel.Controls.Add(g.name);

                g.version = new Label();
                g.version.Location = new Point(tabs[2], labelheightmargin);      // 8 spacing, allow 8*4 to indent
                g.version.Size = new Size(tabs[3] - tabs[2], 24);
                g.version.Text = di.LocalPresent ? di.LocalVersion.ToString() : "N/A";
                g.panel.Controls.Add(g.version);

                g.shortdesc = new ExtendedControls.ExtLabelAutoHeight();
                g.shortdesc.Location = new Point(tabs[3], labelheightmargin);      // 8 spacing, allow 8*4 to indent
                g.shortdesc.Size = new Size(tabs[4] - tabs[3] - 4, 24);
                g.shortdesc.Text = di.ShortLocalDescription.HasChars() ? di.ShortLocalDescription : di.ShortDownloadedDescription;
                if (g.shortdesc.Text.Length == 0)
                    g.shortdesc.Text = "N/A";
                g.panel.Controls.Add(g.shortdesc);

                bool enablebuttons = true;
                bool allowdownload = false;

                string text;
                if (di.State == DownloadItem.ItemState.LocalOnly)
                {
                    text = "Local".T(EDTx.AddOnManagerForm_LocalOnly);
                }
                else if (di.State == DownloadItem.ItemState.DownloadServerOnly)
                {
                    // double check we don't have incompatibilities
                    var notcompatiblelist = di.NotCompatibleWithList().Where(x => gitactionfiles.VersioningManager.DownloadItems.Find(y => y.ItemName == x)?.LocalPresent ?? false);

                    if (notcompatiblelist.Count() > 0)
                        text = "Incompatible".T(EDTx.Incompatible) + " " + string.Join(",", notcompatiblelist);
                    else
                    {
                        allowdownload = true;
                        text = "Version".T(EDTx.AddOnManagerForm_Version) + " " + di.DownloadedVersion.ToString() + ((di.LocalModified) ? "*" : "");
                    }
                }
                else if (di.State == DownloadItem.ItemState.UpToDate)
                {
                    text = (di.LocalModified) ? "Locally modified".T(EDTx.AddOnManagerForm_Locallymodified) : "Up to Date".T(EDTx.AddOnManagerForm_UptoDate);

#if DEBUG
                    allowdownload = true;       // allow it all the time to test update
#endif            
                }
                else if (di.State == DownloadItem.ItemState.OutOfDate)
                {
                    allowdownload = true;
                    text = "New version".T(EDTx.AddOnManagerForm_Newversion) + " " + di.DownloadedVersion.ToString() + ((di.LocalModified) ? "*" : "");
                }
                else if (di.State == DownloadItem.ItemState.EDOutOfDate)
                {
                    text = "Newer EDD required".T(EDTx.AddOnManagerForm_Newer);
                }
                else if (di.State == DownloadItem.ItemState.EDTooOld)
                {
                    text = "Too old for EDD".T(EDTx.AddOnManagerForm_Old);
                }
                else if (di.State == DownloadItem.ItemState.ToBeInstalled)
                {
                    text = "To be installed".T(EDTx.ToBeInstalled); ;
                    enablebuttons = false;
                }
                else if (di.State == DownloadItem.ItemState.ToBeRemoved)
                {
                    text = "To be deleted".T(EDTx.ToBeDeleted); ;
                    enablebuttons = false;
                }
                else if (di.State == DownloadItem.ItemState.Removed)
                {
                    text = "Removed".T(EDTx.Removed);
                    enablebuttons = false;
                }
                else
                    text = "?????";

                g.actionstate = new ExtendedControls.ExtLabelAutoHeight();
                g.actionstate.Location = new Point(tabs[4], labelheightmargin);      // 8 spacing, allow 8*4 to indent
                g.actionstate.Size = new Size(tabs[5] - tabs[4] - 4, 24);
                g.actionstate.Text = text;
                // g.actionlabel.BackColor = Color.AliceBlue; // debug
                g.panel.Controls.Add(g.actionstate);

                if (enablebuttons)
                {
                    if (managedownloadmode)
                    {
                        if (allowdownload)
                        {
                            g.actionbutton = new ExtendedControls.ExtButton();
                            g.actionbutton.Location = new Point(tabs[5], labelheightmargin - 4);      // 8 spacing, allow 8*4 to indent
                            g.actionbutton.Size = new Size(tabs[6] - tabs[5] - 20, 24);
                            g.actionbutton.Text = di.State == DownloadItem.ItemState.DownloadServerOnly ? "Install".T(EDTx.AddOnManagerForm_Install) : 
                                                 di.State == DownloadItem.ItemState.OutOfDate ? "Update".T(EDTx.AddOnManagerForm_Update) : "Refresh";       // Refresh can only be in debug mode
                            g.actionbutton.Click += Actionbutton_Click;
                            g.actionbutton.Tag = g;
                            g.panel.Controls.Add(g.actionbutton);
                        }
                    }
                    else
                    {
                        bool loaded = CheckActionLoaded != null ? CheckActionLoaded(g.di.ItemName) : false;

                        if (loaded)     // may not be loaded IF its got an error.
                        {
                            if (!di.LocalNotEditable)
                            {
                                g.actionbutton = new ExtendedControls.ExtButton();
                                g.actionbutton.Location = new Point(tabs[5], labelheightmargin - 4);      // 8 spacing, allow 8*4 to indent
                                g.actionbutton.Size = new Size(tabs[6] - tabs[5] - 20, 24);
                                g.actionbutton.Text = "Edit".T(EDTx.AddOnManagerForm_Edit);
                                g.actionbutton.Click += ActionbuttonEdit_Click;
                                g.actionbutton.Tag = g;
                                g.panel.Controls.Add(g.actionbutton);
                            }
                        }
                    }

                    if (di.LocalPresent)
                    {
                        g.deletebutton = new ExtendedControls.ExtButton();
                        g.deletebutton.Location = new Point(tabs[6], labelheightmargin - 4);      // 8 spacing, allow 8*4 to indent
                        g.deletebutton.Size = new Size(24, 24);
                        g.deletebutton.Text = "X";
                        g.deletebutton.Click += Deletebutton_Click;
                        g.deletebutton.Tag = g;
                        g.panel.Controls.Add(g.deletebutton);
                    }

                    if (di.LocalEnable.HasValue)
                    {
                        g.enabled = new ExtendedControls.ExtCheckBox();
                        g.enabled.Location = new Point(tabs[7], labelheightmargin - 4);
                        g.enabled.Size = new Size(tabs[8] - tabs[7], 24);
                        g.enabled.Text = "";
                        g.enabled.Checked = di.LocalEnable.Value;
                        g.enabled.Click += Enabled_Click;
                        g.enabled.Tag = g;
                        g.enabled.Enabled = !di.LocalNotDisableable;
                        g.panel.Controls.Add(g.enabled);
                    }
                }

                g.panel.Location= new Point(panelleftmargin, vpos);
                g.panel.Size = g.panel.FindMaxSubControlArea(4, 4);

                maxpanelwidth = Math.Max(maxpanelwidth, g.panel.Width);

                panelVScroll.Controls.Add(g.panel);
                vpos += g.panel.Height + 4;
            }

            foreach( Control c in panelVScroll.Controls )       // set all the sub items, which are panels, to max panel width
            {
                c.Width = maxpanelwidth;
            }

            // then add the titles

            panelVScroll.Controls.Add(new Label() { Location = new Point(tabs[0] + panelleftmargin, panelheightmargin), Size = new Size(tabs[1] - tabs[0] - 2, headervsize), Text = "Type".T(EDTx.AddOnManagerForm_Type) });
            panelVScroll.Controls.Add(new Label() { Location = new Point(tabs[1] + panelleftmargin, panelheightmargin), Size = new Size(tabs[2] - tabs[1] - 2, headervsize), Text = "Name".T(EDTx.AddOnManagerForm_Name) });
            panelVScroll.Controls.Add(new Label() { Location = new Point(tabs[2] + panelleftmargin, panelheightmargin), Size = new Size(tabs[3] - tabs[2] - 2, headervsize), Text = "Version".T(EDTx.AddOnManagerForm_Version) });
            panelVScroll.Controls.Add(new Label() { Location = new Point(tabs[3] + panelleftmargin, panelheightmargin), Size = new Size(tabs[4] - tabs[3] - 2, headervsize), Text = "Description".T(EDTx.AddOnManagerForm_Description) });
            panelVScroll.Controls.Add(new Label() { Location = new Point(tabs[4] + panelleftmargin, panelheightmargin), Size = new Size(tabs[5] - tabs[4] - 2, headervsize), Text = "Status".T(EDTx.AddOnManagerForm_Status) });
            panelVScroll.Controls.Add(new Label() { Location = new Point(tabs[5] + panelleftmargin, panelheightmargin), Size = new Size(tabs[6] - tabs[5] - 2, headervsize), Text = "Action".T(EDTx.AddOnManagerForm_Action) });
            panelVScroll.Controls.Add(new Label() { Location = new Point(tabs[6] + panelleftmargin, panelheightmargin), Size = new Size(tabs[7] - tabs[6] - 2, headervsize), Text = "Delete".T(EDTx.AddOnManagerForm_Delete) });
            panelVScroll.Controls.Add(new Label() { Location = new Point(tabs[7] + panelleftmargin, panelheightmargin), Size = new Size(tabs[8] - tabs[7] - 2, headervsize), Text = "Enabled".T(EDTx.AddOnManagerForm_Enabled) });

            if (!managedownloadmode)        // add on a more button if in edit pack mode
            {
                ExtendedControls.ExtButton dynmore = new ExtendedControls.ExtButton();
                dynmore.Size = new System.Drawing.Size(24, 24);
                dynmore.Text = "+";
                dynmore.Click += new System.EventHandler(this.buttonMore_Click);
                dynmore.UseVisualStyleBackColor = true;
                dynmore.Location = new Point(panelleftmargin, vpos);
                panelVScroll.Controls.Add(dynmore);
            }

            panelVScroll.Scale(this.CurrentAutoScaleFactor());       // scale newly added children to form

            theme.ApplyStd(panelVScroll);
            

            panelVScroll.ResumeLayout();
        }

        bool infoclicked = false;
        private void Info_Click(object sender, EventArgs e)
        {
            Control c = sender as Control;
            Group g = c.Tag as Group;

            infoclicked = true;
            string d = g.di.LongDownloadedDescription;
            if (d == "")
                d = g.di.LongLocalDescription;

            richTextBoxScrollDescription.Text = d.ReplaceEscapeControlChars();
        }

        private void MouseEnterControl(object sender, EventArgs e)
        {
            Control c = sender as Control;
            Group g = c.Tag as Group;

            if (!infoclicked)
            {
                string d = g.di.LongDownloadedDescription;
                if (d == "")
                    d = g.di.LongLocalDescription;

                richTextBoxScrollDescription.Text = d.ReplaceEscapeControlChars();
            }
        }

        private void Enabled_Click(object sender, EventArgs e)
        {
            ExtendedControls.ExtCheckBox cb = sender as ExtendedControls.ExtCheckBox;
            Group g = cb.Tag as Group;
            g.di.SetLocalCopyEnableFlag(cb.Checked, gitactionfiles.AppRootFolder);

            if (g.di.LocalEnable == cb.Checked)
                InstalledDeinstallNow.Remove(g.di.ItemName);
            else
                InstalledDeinstallNow[g.di.ItemName] = cb.Checked ? "+" : "-";
        }

        private void Actionbutton_Click(object sender, EventArgs e)
        {
            ExtendedControls.ExtButton cb = sender as ExtendedControls.ExtButton;
            Group gd = cb.Tag as Group;

            if (gd.di.LocalModified)
            {
                if (ExtendedControls.MessageBoxTheme.Show(this, "Modified locally, do you wish to overwrite the changes".T(EDTx.AddOnManagerForm_Modwarn), "Warning".T(EDTx.Warning), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                    return;
            }

            // if its a complex install, or its present and its a complex delete, we need to pend it

            if ( gd.di.IsComplexInstall() || (gd.di.LocalPresent && gd.di.IsComplexDelete()))
            {
                // get list of remove others, where X is found and has Local Present flag (a profusion of ?)
                var removeotherslist = gd.di.RemoveOtherPacksList();
                var removeotherspresent = removeotherslist.Where(x=> gitactionfiles.VersioningManager.Find(x)?.LocalPresent ?? false);
                if ( removeotherspresent.Count()>0)
                {
                    if (ExtendedControls.MessageBoxTheme.Show(this, "This pack will remove the following other packs\r\n\r\n" + string.Join(",", removeotherspresent) + "\r\n\r\nConfirm?", "Warning".T(EDTx.Warning), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                        return;

                    foreach (var pack in removeotherspresent)
                    {
                        foreach( Control ctrl in panelVScroll.Controls)
                        {
                            Group g = ctrl.Tag as Group;
                            if ( g!= null && g.di.ItemName == pack)
                                g.di.State = DownloadItem.ItemState.ToBeRemoved;
                        }

                        InstallDeinstallAtStartupList[pack] = "-";      // remove it!
                    }
                }

                ExtendedControls.MessageBoxTheme.Show(this, "Add-on will be installed at next restart", "Information".T(EDTx.Information), MessageBoxButtons.OK, MessageBoxIcon.Information);
                gd.di.State = DownloadItem.ItemState.ToBeInstalled;
                InstallDeinstallAtStartupList[gd.di.ItemName] = gd.di.LocalPresent ? "++" : "+";
            }
            else
            {
                bool localpresent = gd.di.LocalPresent; // keep since we alter it

                if ( localpresent)        // if local is there, inform its being removed
                    DeleteActionFile?.Invoke(gd.di);

                if ( gd.di.Install(this, new System.Threading.CancellationToken(), githuburl, gitactionfiles.AppRootFolder)) // if succeeded, make a note
                    InstalledDeinstallNow[gd.di.ItemName] = localpresent ? "++" : "+";
            }

            ReadyToDisplay();
        }

        private void Deletebutton_Click(object sender, EventArgs e)
        {
            ExtendedControls.ExtButton cb = sender as ExtendedControls.ExtButton;
            Group gd = cb.Tag as Group;

            if (ExtendedControls.MessageBoxTheme.Show(this, string.Format("Do you really want to delete {0}".T(EDTx.AddOnManagerForm_DeleteWarn), gd.di.ItemName), "Warning".T(EDTx.Warning), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                if (gd.di.IsComplexDelete() )
                {
                    ExtendedControls.MessageBoxTheme.Show(this, "Add-on will be deleted at next restart", "Information".T(EDTx.Information), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    gd.di.State = DownloadItem.ItemState.ToBeRemoved;
                    InstallDeinstallAtStartupList[gd.di.ItemName] = "-";
                }
                else
                {
                    DeleteActionFile?.Invoke(gd.di);
                    if ( gd.di.Remove(this, gitactionfiles.AppRootFolder) )
                        InstalledDeinstallNow[gd.di.ItemName] = "-";
                }

                ReadyToDisplay();
            }
        }

        private void ActionbuttonEdit_Click(object sender, EventArgs e)
        {
            ExtendedControls.ExtButton cb = sender as ExtendedControls.ExtButton;
            Group gd = cb.Tag as Group;
            EditActionFile?.Invoke(gd.name.Text);
            ReadyToDisplay();
        }

        private void buttonMore_Click(object sender, EventArgs e)
        {
            CreateActionFile?.Invoke();
            ReadyToDisplay();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonExtGlobals_Click(object sender, EventArgs e)
        {
            EditGlobals?.Invoke();
        }

        private void label_index_MouseDown(object sender, MouseEventArgs e)
        {
            OnCaptionMouseDown((Control)sender, e);
        }

        private void label_index_MouseUp(object sender, MouseEventArgs e)
        {
            OnCaptionMouseUp((Control)sender, e);
        }

        private void panel_minimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void panel_close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private class Group
        {
            public DownloadItem di;
            public Panel panel;
            public Button info;
            public Label type;
            public Label name;
            public Label version;
            public Label shortdesc;
            public ExtendedControls.ExtLabelAutoHeight actionstate;
            public ExtendedControls.ExtButton actionbutton;
            public ExtendedControls.ExtButton deletebutton;
            public ExtendedControls.ExtCheckBox enabled;
        }

        private const int panelheightmargin = 1;
        private const int labelheightmargin = 6;
        private const int panelleftmargin = 3;

        private GitActionFiles gitactionfiles;

        private string githuburl;
        private bool managedownloadmode;
        private bool downloadgit;
        private Version edversion;
        private string progtype;

        private System.Threading.CancellationTokenSource canceldownload = new System.Threading.CancellationTokenSource();
    }
}
