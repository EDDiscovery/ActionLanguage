/*
 * Copyright © 2017-2023 EDDiscovery development team
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

using BaseUtils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ActionLanguage
{
    // edit the event list

    public partial class ActionPackEditPackForm : ExtendedControls.DraggableForm
    {
        // these are controlled by the caller

        // Call back when we need more variable names, by event string
        public Func<string, List<TypeHelpers.PropertyNameInfo>> AdditionalNames;
        // given a class and condition, what editor (ActionPackEditEventBas derived) do you want? change the condition if you need to
        public Func<string, Condition, ActionPackEditEventBase> GetEventEditor;
        // Given a condition, what is its class?
        public Func<Condition,string> GetClassNameFromCondition;                    

        #region Init

        public ActionPackEditPackForm()
        {
            entries = new List<Entry>();
            InitializeComponent();
        }

        public void Init(string title, Icon ic, ActionCoreController cp, string appfolder, ActionFile file, List<ActionEvent> evlist , string collapsestate)       // here, change to using events
        {
            System.Diagnostics.Debug.Assert(AdditionalNames != null);
            System.Diagnostics.Debug.Assert(GetEventEditor != null);
            System.Diagnostics.Debug.Assert(GetClassNameFromCondition != null);

            this.Icon = ic;
            actioncorecontroller = cp;
            applicationfolder = appfolder;
            actionfile = file;
            events = evlist;

            BaseUtils.TranslatorMkII.Instance.TranslateControls(this);

            classtypenames = (from e in events select e.UIClass).ToList().Distinct().ToList();      // here we extract from events relevant data
            eventsperclass = new Dictionary<string, List<string>>();
            foreach (string s in classtypenames)
                eventsperclass.Add(s, (from e in events where e.UIClass == s select e.TriggerName).ToList());

            bool winborder = ExtendedControls.Theme.Current.ApplyDialog(this);    // scale to font

            statusStripCustom.Visible = panelTop.Visible = panelTop.Enabled = !winborder;
            initialtitle = this.Text = label_index.Text = title;

            ConditionLists clist = actionfile.FileEventList;    // now load the initial conditions from the action file

            string eventname = null;
            for (int i = 0; i < clist.Count; i++)       // for ever event, find the condition, create the group, theme
            {
                string gname = clist[i].GroupName;
                if (gname != eventname)
                {
                    eventname = gname;
                    Entry gg = CreateEntry(false, null, gname);
                    gg.groupcollapsed = collapsestate.Contains("<" + gname + ";");
                    entries.Add(gg);
                }

                Condition cd = clist[i];
                Entry g = CreateEntry(true, cd, null);
                entries.Add(g);
            }

            panelVScroll.SuspendLayout();
            foreach (Entry g in entries)     // add the groups to the vscroller
                panelVScroll.Controls.Add(g.panel);
            panelVScroll.ResumeLayout();

            PositionEntries(true);       //repositions all items

            Usercontrol_RefreshEvent();
        }

        #endregion

        #region Entry Making and positions

        // create a group, create the UC under it if required
        private Entry CreateEntry(bool isevent, Condition cd, string name)     
        {
            Entry g = new Entry();

            g.panel = new Panel();
            g.panel.Name = name ?? (cd?.EventName + " " + cd?.ToString());
            g.panel.SuspendLayout();

            // We draw as it was 8.25 point then scale.

            // g.panel.BackColor = Color.Yellow; // for debug

            if (isevent)
            {
                //g.panel.BackColor = Color.Green; // useful for debug
                g.classtype = new ExtendedControls.ExtComboBox();
                g.classtype.Items.AddRange(classtypenames);
                g.classtype.Location = new Point(panelxmargin, panelymargin);
                g.classtype.Size = new Size(80, 24);
                g.classtype.SetTipDynamically(toolTip, "Select event class");

                if (cd != null)
                {
                    g.classtype.Enabled = false;
                    g.classtype.SelectedItem = GetClassNameFromCondition(cd);
                    g.classtype.Enabled = true;

                    // given the condition, pick the editor control to use from it

                    CreateEditorControl(g, cd);
                    
                    //System.Diagnostics.Debug.WriteLine($"APEF Event {GetClassNameFromCondition(cd)} {cd.ToString()}");
                }

                g.classtype.SelectedIndexChanged += Classtype_SelectedIndexChanged;
                g.classtype.Tag = g;
                g.panel.Controls.Add(g.classtype);
            }
            else
            {
                g.groupnamepanel = new Panel();
                g.groupnamepanel.Location = new Point(3, 2);
                g.panel.Controls.Add(g.groupnamepanel);

                g.groupnamecollapsebutton = new ExtendedControls.ExtButton();
                g.groupnamecollapsebutton.Text = "-";
                g.groupnamecollapsebutton.Location = new Point(2, 2);
                g.groupnamecollapsebutton.Size = new Size(16, 16);
                g.groupnamecollapsebutton.Tag = g;
                g.groupnamecollapsebutton.MouseDown += Groupnamecollapsebutton_MouseDown;
                g.groupnamecollapsebutton.Click += Groupnamecollapsebutton_Click;
                g.groupnamepanel.Controls.Add(g.groupnamecollapsebutton);

                g.groupnamelabel = new Label();
                g.groupnamelabel.Name = g.groupnamelabel.Text = name;
                g.groupnamelabel.Location = new Point(32, 2);
                g.groupnamepanel.Controls.Add(g.groupnamelabel);

                g.groupactionscombobox = new ExtendedControls.ExtComboBox();
                g.groupactionscombobox.Items.AddRange(new string[] { "Select Action", "Disable All", "Enable All", "Delete All"});
                g.groupactionscombobox.Size = new Size(200, 20);
                g.groupactionscombobox.Tag = g;
                g.groupactionscombobox.SelectedIndex = 0;
                g.groupactionscombobox.SelectedIndexChanged += Groupactionscombobox_SelectedIndexChanged;
                g.groupnamepanel.Controls.Add(g.groupactionscombobox);
            }

            g.actionbutton = new ExtendedControls.ExtButton();
            g.actionbutton.Text = ">";
            g.actionbutton.Size = new Size(24, 24);
            g.actionbutton.Tag = g;
            g.actionbutton.Click += Action_Click;
            toolTip.SetToolTip(g.actionbutton, "Move up");
            g.panel.Controls.Add(g.actionbutton);

            ExtendedControls.Theme.Current.ApplyDialog(g.panel);
            g.panel.Scale(this.CurrentAutoScaleFactor());

            if (g.groupnamepanel != null)
            {
                g.groupnamepanel.BackColor = ExtendedControls.Theme.Current.TextBlockBackColor;
            }

            g.panel.ResumeLayout();

            return g;
        }

        // create the editor control, of type ActionPackEditEventBase under the entry.
        // Condition will be either AlwaysTrue if new, or stored condition from actionprogram
        private void CreateEditorControl(Entry entry, Condition inputcond)        
        {
            if (entry.editoruc != null)
            {
                Controls.Remove(entry.editoruc);
                entry.editoruc.Dispose();
            }

            Condition cd = new Condition(inputcond);                       // make a copy for editing purposes.

            entry.editoruc = GetEventEditor(entry.classtype.Text, cd);      // make the editor user control, based on text/cond.  Allow cd to be altered

            // init, with the copy of inputcond so they can edit it without commiting change
            entry.editoruc.Init(cd, eventsperclass[entry.classtype.Text], actioncorecontroller, applicationfolder, actionfile, AdditionalNames,
                                    this.Icon, toolTip);

            entry.editoruc.RefreshEvent += Usercontrol_RefreshEvent;
            entry.panel.Controls.Add(entry.editoruc);
        }

        private void PositionEntries(bool calcminsize, bool toend = false)
        {
            int y = panelymargin;
            int panelwidth = Math.Max(panelVScroll.Width, 10);

            bool collapsed = false;

            int curpos = panelVScroll.BeingPosition();      // we are going to restablish the whole co-ords again, so reset.

            for (int i = 0; i < entries.Count; i++)
            {
                Entry g = entries[i];

                if (g.IsGroupName)
                { 
                    collapsed = g.groupcollapsed;
                    g.groupnamecollapsebutton.Text = collapsed ? "+" : "-";
                }

                if (g.IsGroupName || collapsed == false)
                {
                    g.actionbutton.Location = new Point(panelwidth - g.actionbutton.Width - 10, panelymargin);

                    if (g.editoruc != null)
                    {
                        g.editoruc.Location = new Point(g.classtype.Right + 16, 0);
                        g.editoruc.Size = g.editoruc.FindMaxSubControlArea(0, 0);
                    }

                    if (g.groupnamepanel != null)
                    {
                        g.groupnamepanel.Size = new Size(g.actionbutton.Left - g.groupnamepanel.Left - 8, g.groupnamecollapsebutton.Bottom + Font.ScalePixels(2));
                        g.groupactionscombobox.Location = new Point(g.groupnamepanel.Width - g.groupactionscombobox.Width - 16, 2);
                        g.groupnamelabel.Size = new Size(g.groupactionscombobox.Location.X - g.groupnamelabel.Location.X - 8, g.groupnamelabel.Height);
                    }

                    g.panel.Visible = true;
                    g.panel.Location = new Point(panelxmargin, y);
                    g.panel.Size = g.panel.FindMaxSubControlArea(2, 2);

                   // System.Diagnostics.Debug.WriteLine("Panel " + i + " " + g.panel.Name + " loc " + g.panel.Location + " size " + g.panel.Size);
                    y += g.panel.Height + Font.ScalePixels(4);
                }
                else
                {
                    g.panel.Visible = false;
                    g.panel.Location = new Point(0, 0);     // we put it at 0,0 so it won't interfer with calculations on scroll area
                }
            }

            buttonMore.Location = new Point(panelxmargin, y );
           // System.Diagnostics.Debug.WriteLine("More " + buttonMore.Location);

            if (calcminsize)
            {
                stopresizepositioning = true;
                int titleHeight = RectangleToScreen(this.ClientRectangle).Top - this.Top;
                y += buttonMore.Height + titleHeight + ((panelTop.Enabled) ? (panelTop.Height + statusStripCustom.Height) : 8) + 16 + panelOK.Height;
                this.MaximumSize = new Size(Screen.FromControl(this).WorkingArea.Width - 100, Screen.FromControl(this).WorkingArea.Height - 100);
                this.MinimumSize = new Size(800,Math.Min(this.MaximumSize.Height,y));

                if (Bottom > Screen.FromControl(this).WorkingArea.Height)
                    Top = Screen.FromControl(this).WorkingArea.Height - Height - 50;

                stopresizepositioning = false;
            }

            this.Text = label_index.Text = initialtitle + " (" + entries.Count.ToString() + ")";

            panelVScroll.FinishedPosition(toend ? int.MaxValue : curpos);
        }
    
        #endregion

        private void Usercontrol_RefreshEvent()
        {
            comboBoxCustomEditProg.Enabled = false;
            comboBoxCustomEditProg.Items.Clear();
            comboBoxCustomEditProg.Items.AddRange(actionfile.ProgramList.GetActionProgramList(true));
            comboBoxCustomEditProg.Enabled = true;

            foreach (Entry g in entries)
            {
                if (g.editoruc != null)
                    g.editoruc.UpdateProgramList(actionfile.ProgramList.GetActionProgramList());
            }
        }

        #region UI

        private void buttonMore_Click(object sender, EventArgs e)
        {
            Entry g = CreateEntry(true, null, null);
            entries.Add(g);
            panelVScroll.Controls.Add(g.panel);

            int groupabove = GetGroupAbove(entries.Count - 1);       // if group above exists and is collapsed, need to expand it to show
            if (groupabove != -1 && entries[groupabove].groupcollapsed == true)
                entries[groupabove].groupcollapsed = false;

            PositionEntries(true,true);
        }

        private void Classtype_SelectedIndexChanged(object sender, EventArgs e)
        {
            ExtendedControls.ExtComboBox b = sender as ExtendedControls.ExtComboBox;
            Entry g = (Entry)b.Tag;
            CreateEditorControl(g, Condition.AlwaysTrue());
            ExtendedControls.Theme.Current.ApplyDialog(g.editoruc);
            g.editoruc.Scale(this.CurrentAutoScaleFactor());
            PositionEntries(true);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            string res = Check();

            if (res.Length > 0)
            {
                string acceptstr = "Click Retry to correct errors, Abort to cancel, Ignore to accept valid entries";

                DialogResult dr = ExtendedControls.MessageBoxTheme.Show(this, "Filters produced the following warnings and errors" + Environment.NewLine + Environment.NewLine + res + Environment.NewLine + acceptstr,
                                                  "Warning", MessageBoxButtons.AbortRetryIgnore);

                if (dr == DialogResult.Retry)
                {
                    return;
                }
                else if (dr == DialogResult.Abort || dr == DialogResult.Cancel)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            }

            actionfile.ChangeEventList(result);
            actionfile.WriteFile();

            DialogResult = DialogResult.OK;
            Close();
        }

        private string Check()
        {
            string errorlist = "";
            result = new ConditionLists();

            string eventgroup = null;

            int index = 1;
            foreach (Entry entry in entries)
            {
                string prefix = "Event " + index.ToStringInvariant() + ": ";

                if (entry.groupnamelabel != null)
                {
                    eventgroup = entry.groupnamelabel.Text;
                }
                else if (entry.editoruc == null)
                {
                    errorlist += prefix + "Ignored group with empty name" + Environment.NewLine;
                }
                else
                {
                    Condition c = entry.editoruc.cd;

                    if (!c.EventName.HasChars())
                        errorlist += prefix + "Event " + entry.editoruc.ID() + " does not have an event name defined" + Environment.NewLine;
                    else if (!c.Action.HasChars() || c.Action.Equals("New"))        // actions, but not selected one..
                        errorlist += prefix + "Event " + entry.editoruc.ID() + " does not have an action program defined" + Environment.NewLine;
                    else if (c.Fields == null || c.Fields.Count == 0)
                        errorlist += prefix + "Event " + entry.editoruc.ID() + " does not have a condition" + Environment.NewLine;
                    else
                    {
                        entry.editoruc.cd.GroupName = eventgroup;
                        result.Add(entry.editoruc.cd);
                    }
                }

                index++;
            }

            return errorlist;
        }

        private void comboBoxCustomEditProg_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxCustomEditProg.Enabled)
            {
                string progname = comboBoxCustomEditProg.Text.Replace(" (Ext)", "");     // remove the EXT marker

                ActionProgram p = null;
                if (!progname.Equals("New"))
                    p = actionfile.ProgramList.Get(progname);

                if (p != null && p.StoredInSubFile != null)
                {
                    p.EditInEditor(p.StoredInSubFile);         // Edit in the editor..
                }
                else
                {
                    ActionProgramEditForm apf = new ActionProgramEditForm();
                    apf.EditProgram += EditProgram;

                    apf.Init( this.Icon, actioncorecontroller, applicationfolder,
                                AdditionalNames(null),        // no event associated, so just return the globals in effect
                                actionfile.Name, p,
                                actionfile.ProgramList.GetActionProgramList(), "", ModifierKeys.HasFlag(Keys.Shift));

                    DialogResult res = apf.ShowDialog(this);

                    if (res == DialogResult.OK)
                    {
                        ActionProgram np = apf.GetProgram();
                        actionfile.ProgramList.AddOrChange(np);                // replaces or adds (if its a new name) same as rename
                        Usercontrol_RefreshEvent();
                    }
                    else if (res == DialogResult.Abort)   // delete
                    {
                        ActionProgram np2 = apf.GetProgram();
                        actionfile.ProgramList.Delete(np2.Name);
                        Usercontrol_RefreshEvent();
                    }
                }
            }
        }

        private void EditProgram(string s)  // Callback by APF to ask to edit another program..
        {
            if (!actionfile.ProgramList.EditProgram(s, actionfile.Name, actioncorecontroller, applicationfolder))
                ExtendedControls.MessageBoxTheme.Show(this, "Unknown program or not in this file " + s);
        }

        private void buttonInstallationVars_Click(object sender, EventArgs e)
        {
            ExtendedConditionsForms.VariablesForm avf = new ExtendedConditionsForms.VariablesForm();
            avf.Init("Configuration items for installation - specialist use".Tx(), this.Icon, actionfile.InstallationVariables, showatleastoneentry: false);

            if (avf.ShowDialog(this) == DialogResult.OK)
            {
                actionfile.ChangeInstallationVariables(avf.Result);
            }
        }

        #endregion

        #region Form control 

        bool stopresizepositioning = false;
        private void panelVScroll_Resize(object sender, EventArgs e)
        {
            if ( !stopresizepositioning )
                PositionEntries(false);
        }

        private void panel_minimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        public void panel_close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void label_index_MouseDown(object sender, MouseEventArgs e)
        {
            OnCaptionMouseDown((Control)sender, e);
        }

        private void label_index_MouseUp(object sender, MouseEventArgs e)
        {
            OnCaptionMouseUp((Control)sender, e);
        }

        #endregion

        #region Action Click

        private void Action_Click(object sender, EventArgs e)
        {
            Button b = sender as Button;
            clickgroup = b.Tag as Entry;
            clickgroupindex = entries.IndexOf(clickgroup);
            moveDownToolStripMenuItem.Enabled = clickgroupindex < entries.Count - 1;
            moveUpToolStripMenuItem.Enabled = clickgroupindex > 0;
            insertGroupToolStripMenuItem.Enabled = clickgroup.IsEvent && (clickgroupindex == 0 || entries[clickgroupindex - 1].groupnamelabel == null);
            moveToGroupAboveToolStripMenuItem.Enabled = clickgroup.IsEvent && GetGroupAbove(clickgroupindex) > 0;
            moveToGroupBelowToolStripMenuItem.Enabled = clickgroup.IsEvent && GetGroupBelow(clickgroupindex) != -1;
            moveGroupDownToolStripMenuItem.Enabled = clickgroup.IsGroupName && GetGroupBelow(clickgroupindex + 1) != -1;
            moveGroupUpToolStripMenuItem.Enabled = clickgroup.IsGroupName && GetGroupAbove(clickgroupindex - 1) != -1;
            renameGroupToolStripMenuItem.Enabled = clickgroup.IsGroupName;
            contextMenuStripAction.Show(b.PointToScreen(new Point(b.Width, 0)));
        }

        private void insertGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string s = ExtendedControls.PromptSingleLine.ShowDialog(this, "Group Name:", "", "Enter group name", Icon, requireinput:true);

            if (s != null)
            {
                Entry gg = CreateEntry(false, null, s);
                panelVScroll.Controls.Add(gg.panel);
                entries.Insert(clickgroupindex, gg);
                PositionEntries(true);
            }
        }

        private void insertNewEventAboveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Entry gg = CreateEntry(true, null, null);
            panelVScroll.Controls.Add(gg.panel);
            entries.Insert(clickgroupindex, gg);
            PositionEntries(true);
        }

        private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            entries.RemoveAt(clickgroupindex);
            entries.Insert(clickgroupindex + 1, clickgroup);
            PositionEntries(true);
        }

        private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            entries.RemoveAt(clickgroupindex);
            entries.Insert(clickgroupindex - 1, clickgroup);
            PositionEntries(true);
        }

        private void moveToGroupBelowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int inspos = GetGroupBelow(clickgroupindex);
            entries.RemoveAt(clickgroupindex);
            entries.Insert(inspos, clickgroup);
            PositionEntries(true);
        }

        private void moveToGroupAboveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int inspos = GetGroupAbove(clickgroupindex);
            entries.RemoveAt(clickgroupindex);
            entries.Insert(inspos, clickgroup);
            PositionEntries(true);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clickgroup.Dispose();
            entries.Remove(clickgroup);
            PositionEntries(false);
        }

        private void moveGroupUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int groupabove = GetGroupAbove(clickgroupindex - 1);
            int enditem = GetGroupBelow(clickgroupindex + 1);
            if (enditem == -1)
                enditem = entries.Count;

            List<Entry> tomove = entries.GetRange(clickgroupindex, enditem - clickgroupindex);
            entries.RemoveRange(clickgroupindex, enditem - clickgroupindex);
            entries.InsertRange(groupabove, tomove);
            PositionEntries(false);
        }

        private void moveGroupDownToolStripMenuItem_Click(object sender, EventArgs e)
        {                                                                          
            // click item = 10, ng = 20 , sg = 30.. so remove 10-19.  ng->10.  Insert at 10+sg-ng
            int nextgroup = GetGroupBelow(clickgroupindex + 1); // must be there.. 
            int sgenditem = GetGroupBelow(nextgroup + 1); // may be there
            if (sgenditem == -1)
                sgenditem = entries.Count;

            int toremove = nextgroup - clickgroupindex;
            List<Entry> tomove = entries.GetRange(clickgroupindex, toremove);
            entries.RemoveRange(clickgroupindex, toremove);

            entries.InsertRange(clickgroupindex + sgenditem - nextgroup, tomove);
            PositionEntries(false);
        }

        private int GetGroupBelow(int start)
        {
            for (int insert = start; insert < entries.Count; insert++)
            {
                if (entries[insert].IsGroupName)
                    return insert;
            }

            return -1;
        }

        private int GetGroupAbove(int start)
        {
            for (int insert = start; insert >= 0; insert--)
            {
                if (entries[insert].IsGroupName)
                    return insert;
            }

            return -1;
        }

        private void renameGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string s = ExtendedControls.PromptSingleLine.ShowDialog(this, "Group Name:", "", "Enter group name", Icon, requireinput:true);

            if (s != null)
            {
                clickgroup.groupnamelabel.Text = s;
                PositionEntries(true);
            }
        }

        #endregion

        #region Collapse
        private void Groupnamecollapsebutton_MouseDown(object sender, MouseEventArgs e)
        {
            Button b = sender as Button;
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStripCollapse.Show(b.PointToScreen(new Point(b.Width, 0)));
            }
        }

        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Entry g in entries)
            {
                if (g.IsGroupName)
                    g.groupcollapsed = true;
            }

            PositionEntries(false);
        }

        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Entry g in entries)
            {
                if (g.IsGroupName)
                    g.groupcollapsed = false;
            }

            PositionEntries(true);
        }

        private void Groupnamecollapsebutton_Click(object sender, EventArgs e)
        {
            Button b = sender as Button;
            Entry g = b.Tag as Entry;
            g.groupcollapsed = !g.groupcollapsed;
            PositionEntries(g.groupcollapsed == false);
        }

        private void Groupactionscombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ExtendedControls.ExtComboBox cb = sender as ExtendedControls.ExtComboBox;

            if ( cb.SelectedIndex > 0)
            { 
                Entry selected = cb.Tag as Entry;
                string action = (string)cb.SelectedItem;

                List<Entry> todelete = new List<Entry>();
                bool change = false;
                for (int i = 0; i < entries.Count; i++)
                {
                    Entry g = entries[i];

                    if (g.IsGroupName)
                    {
                        if (g == selected)
                            change = true;
                        else if (change == true)
                            break;
                    }
                    else
                    {
                        if (change)
                        {
                            if ( action == "Delete All" )
                            {
                                todelete.Add(g);
                            }
                            else
                                g.editoruc.PerformAction(action);
                        }
                    }
                }

                if ( todelete.Count>0)
                {
                    if (ExtendedControls.MessageBoxTheme.Show("Are you sure you want to delete all in this group?", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                    {
                        foreach (Entry g in todelete)
                        {
                            g.Dispose();
                            entries.Remove(g);
                        }

                        PositionEntries(false);
                    }
                }


                cb.SelectedIndex = 0;
            }
        }

        public string CollapsedState()      // save it per session so its not so horrible if you reenter
        {
            string str = "";

            foreach (Entry g in entries)
            {
                if (g.IsGroupName && g.groupcollapsed == true)
                    str += "<" + g.groupnamelabel.Text + ";";
            }

            return str;
        }

        #endregion

        #region Variables

        private ActionFile actionfile;      // file we are editing
        private string applicationfolder;   // folder where the file is
        private ActionCoreController actioncorecontroller;  // need this for some access to data
        private List<ActionEvent> events;   // list of events, UIs
        private List<string> classtypenames;    // groupnames extracted from events
        private Dictionary<string, List<string>> eventsperclass;    // events per group name, extracted from events
        private string initialtitle;

        const int panelxmargin = 3;
        const int panelymargin = 1;

        class Entry     // this top level form has a list of entries, each containing a grouptype CBC, a delete button, and a UC containing its controls
        {
            public Panel panel;                                     // surrounding panel

            public ExtendedControls.ExtComboBox classtype;          // present for any other than group name

            public ActionPackEditEventBase editoruc;                // the editor to use for this event. present for any other than group name, but may or may not be set

            public ExtendedControls.ExtButton actionbutton;               // always present

            public Panel groupnamepanel;                            // present for group entry
            public Label groupnamelabel;                            // present for group entry
            public ExtendedControls.ExtButton groupnamecollapsebutton;   // present for group entry
            public ExtendedControls.ExtComboBox groupactionscombobox;       // action list
            public bool groupcollapsed;                             // if collapsed..

            public bool IsGroupName { get { return groupnamepanel != null; } }
            public bool IsEvent { get { return groupnamepanel == null; } }

            public void Dispose()
            {
                panel.Controls.Clear();

                if (editoruc != null)
                    editoruc.Dispose();

                if (classtype != null)
                    classtype.Dispose();

                if (groupnamepanel != null)
                    groupnamepanel.Dispose();

                if (groupnamelabel != null)
                    groupnamelabel.Dispose();

                if (groupnamecollapsebutton != null)
                    groupnamecollapsebutton.Dispose();

                if (groupactionscombobox != null)
                    groupactionscombobox.Dispose();

                if (actionbutton != null)
                    actionbutton.Dispose();

                panel.Dispose();
            }
        }

        private List<Entry> entries; // the groups
        private ConditionLists result;

        private Entry clickgroup;
        private int clickgroupindex;

        #endregion
    }
}

