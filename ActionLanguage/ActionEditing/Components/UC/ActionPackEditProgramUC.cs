/*
 * Copyright © 2017 EDDiscovery development team
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
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using BaseUtils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ActionLanguage
{
    // this class adds on a program selector, a P edit button, and event variable fields, used to the right of the condition
    // it adapts to the program classifer type (Full, Keys, Say, etc)

    public class ActionPackEditProgram : UserControl
    {
        public Func<List<BaseUtils.TypeHelpers.PropertyNameInfo>> onAdditionalNames;        // give me more names must provide
        public Func<Form, System.Drawing.Icon, string, string> onEditKeys;   // edit the key string.. must provide
        public Func<Form, string, ActionCoreController,string> onEditSay;   // edit the say string.. must provide
        public Func<string> SuggestedName;      // give me a suggested program name must provide
        public System.Action RefreshEvent;     

        private Condition cd;
        private ActionFile actionfile;
        private ActionCoreController actioncorecontroller;
        private string applicationfolder;
        private Icon Icon;

        private const int panelxmargin = 3;
        private const int panelymargin = 2;

        private ExtendedControls.ExtPanelDropDown progmajortype;

        private ExtendedControls.ExtComboBox proglist;   //Full
        private ExtendedControls.ExtButton progedit;
        private ExtendedControls.ExtTextBox paras;

        private ExtendedControls.ExtButton buttonSay;
        private ExtendedControls.ExtButton buttonKeys;

        ActionProgram.ProgramConditionClass classifier;
        ActionProgram.ProgramConditionClass[] indextoclassifier;

        public void Init(ActionFile af, Condition c, ActionCoreController ac, string apf , Icon i, ToolTip toolTip,
                    ActionProgram.ProgramConditionClass cls)
        {
            cd = c;     // point at common condition, we never new it, just update action data/action
            actionfile = af;
            actioncorecontroller = ac;
            applicationfolder = apf;
            Icon = i;
            classifier = cls;

            // BackColor = Color.Green; // for debug
            // layed out for 12 point. Requires a 28 pixel area to sit in

            progmajortype = new ExtendedControls.ExtPanelDropDown();
            progmajortype.Items.AddRange(new string[] { "Key" , "Say", "Key+Say" , "Full Program" });
            indextoclassifier = new ActionProgram.ProgramConditionClass[] { ActionProgram.ProgramConditionClass.Key , ActionProgram.ProgramConditionClass.Say ,
                                                                            ActionProgram.ProgramConditionClass.KeySay , ActionProgram.ProgramConditionClass.Full };
            progmajortype.Location = new Point(0, 0);
            progmajortype.Size = new Size(this.Width, 28); // outer panel aligns with this UC 
            progmajortype.SelectedIndexChanged += PanelType_SelectedIndexChanged;
            toolTip.SetToolTip(progmajortype, "Use the selector (click on bottom right arrow) to select program class type");

            int pwidth = (this.Width - 24 - 8 - 8 - 8 - panelxmargin * 2) / 2;       // 24 button, 8+8 gaps, 8 for selector

            proglist = new ExtendedControls.ExtComboBox();
            proglist.Items.Add("New");
            proglist.Items.AddRange(actionfile.ProgramList.GetActionProgramList());
            proglist.Location = new Point(panelxmargin, panelymargin);
            proglist.Size = new Size(pwidth, 24); 
            proglist.SelectedIndexChanged += Proglist_SelectedIndexChanged;
            proglist.SetTipDynamically(toolTip, "Select program to associate with this event");

            progedit = new ExtendedControls.ExtButton();
            progedit.Text = "P";
            progedit.Location = new Point(proglist.Right + 8, panelymargin);
            progedit.Size = new Size(24, 24);
            progedit.Click += Progedit_Click;
            toolTip.SetToolTip(progedit, "Edit associated program");

            paras = new ExtendedControls.ExtTextBox();
            paras.Text = cd.ActionVars.ToString();
            paras.Location = new Point(progedit.Right + 8, panelymargin );
            paras.Size = new Size(pwidth,24);     
            paras.ReadOnly = true;
            paras.Click += Paras_Click;
            paras.SetTipDynamically(toolTip, "Click to enter parameters to pass to program");

            buttonKeys = new ExtendedControls.ExtButton();
            buttonKeys.Location = proglist.Location;
            buttonKeys.Size = new Size((this.Width - 8 - 8 - panelxmargin * 2) / 2, 24);
            buttonKeys.Click += Keypress_Click;
            toolTip.SetToolTip(buttonKeys, "Click to define keystrokes to send");

            buttonSay = new ExtendedControls.ExtButton();
            buttonSay.Location = new Point(buttonKeys.Right + 8, buttonKeys.Top);
            buttonSay.Size = buttonKeys.Size;
            buttonSay.Click += Saypress_Click;
            toolTip.SetToolTip(buttonSay, "Click to set speech to say");

            SuspendLayout();
            progmajortype.Controls.Add(proglist);
            progmajortype.Controls.Add(progedit);
            progmajortype.Controls.Add(paras);
            progmajortype.Controls.Add(buttonKeys);
            progmajortype.Controls.Add(buttonSay);
            Controls.Add(progmajortype);

            UpdateControls();

            ResumeLayout();
        }

        public void ChangedCondition(ActionProgram.ProgramConditionClass cls)  // upper swapped out condition.. brand new clean one
        {
            classifier = cls;
            cd.Action = "";
            cd.ActionVars = new Variables();
            UpdateControls();
        }

        private void UpdateControls()
        {
            proglist.Visible = progedit.Visible = paras.Visible = (classifier == ActionProgram.ProgramConditionClass.Full);
            buttonKeys.Visible = ( classifier == ActionProgram.ProgramConditionClass.KeySay || classifier == ActionProgram.ProgramConditionClass.Key);
            buttonSay.Visible = (classifier == ActionProgram.ProgramConditionClass.KeySay || classifier == ActionProgram.ProgramConditionClass.Say);

            proglist.Enabled = false;
            if (cd.Action.HasChars())
                proglist.SelectedItem = cd.Action;
            else
                proglist.SelectedIndex = 0;
            proglist.Enabled = true;

            paras.Text = cd.ActionVars.ToString();

            buttonKeys.Text = "Enter Key";
            buttonSay.Text = "Enter Speech";
            ActionProgram p = cd.Action.HasChars() ? actionfile.ProgramList.Get(cd.Action) : null;
            if ( p != null )
            {
                buttonKeys.Text = StringParser.FirstQuotedWord(p.ProgramClassKeyUserData, " ,", buttonKeys.Text, prefix: "Key:").Left(25);
                buttonSay.Text = StringParser.FirstQuotedWord(p.ProgramClassSayUserData, " ,", buttonSay.Text, prefix:"Say:").Left(25);
            }
        }

        public void UpdateProgramList(string[] progl)
        {
            proglist.Enabled = false;

            string text = proglist.Text;

            proglist.Items.Clear();
            proglist.Items.Add("New");
            proglist.Items.AddRange(progl);

            if (proglist.Items.Contains(text))
                proglist.SelectedItem = text;

            proglist.Enabled = true;
        }

        private void Proglist_SelectedIndexChanged(object sender, EventArgs e)      //FULL
        {
            if (proglist.Enabled && proglist.SelectedIndex == 0)   // if selected NEW.
            {
                Progedit_Click(null, null);
            }
            else
                cd.Action = proglist.Text;      // set program selected
        }

        private void Progedit_Click(object sender, EventArgs e)     //FULL
        {
            bool shift = ModifierKeys.HasFlag(Keys.Shift);

            ActionProgram p = null;

            if (proglist.SelectedIndex > 0)     // exclude NEW from checking for program
                p = actionfile.ProgramList.Get(proglist.Text);

            if (p != null && p.StoredInSubFile != null && shift)        // if we have a stored in sub file, but we shift hit, cancel it
            {
                if (ExtendedControls.MessageBoxTheme.Show(FindForm(), "Do you want to bring the file back into the main file", "WARNING", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    p.CancelSubFileStorage();
                    shift = false;
                }
                else
                    return; // cancel, abort.
            }

            if (p != null && p.StoredInSubFile != null)
            {
                p.EditInEditor(p.StoredInSubFile);         // Edit in the editor.. this also updated the program steps held internally
            }
            else
            {
                string suggestedname = null;

                if (p == null)        // if no program, create a new suggested name and clear any action data
                {
                    suggestedname = GetSuggestedName();
                    paras.Text = "";
                }

                ActionProgramEditForm apf = new ActionProgramEditForm();
                apf.EditProgram += (s) =>
                {
                    if (!actionfile.ProgramList.EditProgram(s, actionfile.Name, actioncorecontroller, applicationfolder))
                        ExtendedControls.MessageBoxTheme.Show(FindForm(), "Unknown program or not in this file " + s);
                };

                // we init with a variable list based on the field names of the group (normally the event field names got by SetFieldNames)
                // pass in the program if found, and its action data.

                var names = onAdditionalNames();

                apf.Init( this.Icon, actioncorecontroller, applicationfolder, names, actionfile.Name, p, 
                                actionfile.ProgramList.GetActionProgramList(), suggestedname, ModifierKeys.HasFlag(Keys.Shift));

                DialogResult res = apf.ShowDialog(FindForm());

                if (res == DialogResult.OK)
                {
                    ActionProgram np = apf.GetProgram();
                    UpdateProgram(np);
                }
                else if (res == DialogResult.Abort)   // delete
                {
                    ActionProgram np2 = apf.GetProgram();
                    actionfile.ProgramList.Delete(np2.Name);
                    cd.Action = "";
                    RefreshEvent();
                }
            }
        }

        private void Paras_Click(object sender, EventArgs e)        // FULL
        {
            Variables cond = new Variables(paras.Text,Variables.FromMode.MultiEntryComma);

            ExtendedConditionsForms.VariablesForm avf = new ExtendedConditionsForms.VariablesForm();
            avf.Init("Input parameters to pass to program on run".TxID(AFIDs.ActionPackEditForm_ip), this.Icon, cond, showatleastoneentry: true);

            if (avf.ShowDialog(FindForm()) == DialogResult.OK)
            {
                paras.Text = avf.Result.ToString();
                cd.ActionVars = avf.Result;
            }
        }

        private void PanelType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ActionProgram.ProgramConditionClass newcls = indextoclassifier[progmajortype.SelectedIndex];
            if (newcls != classifier)
            {
                ActionProgram p = cd.Action.HasChars() ? actionfile.ProgramList.Get(cd.Action) : null;

                if (p != null) // if we have an existing program, may need to alter it
                {
                    // to not full with prog class being full, need to clear, not in correct form
                    if (newcls != ActionProgram.ProgramConditionClass.Full && p.ProgramClass == ActionProgram.ProgramConditionClass.Full)
                    {
                        cd.Action = "";
                        cd.ActionVars = new Variables();
                    }
                    else if (newcls == ActionProgram.ProgramConditionClass.Key) // key, make sure no say
                        p.SetKeySayProgram(p.ProgramClassKeyUserData, null);
                    else if (newcls == ActionProgram.ProgramConditionClass.Say) //say, make sure no key
                        p.SetKeySayProgram(null, p.ProgramClassSayUserData);
                }

                classifier = newcls;
                UpdateControls();
            }
        }

        private void Keypress_Click(object sender, EventArgs e)
        {
            ActionProgram p = cd.Action.HasChars() ? actionfile.ProgramList.Get(cd.Action) : null;

            string ud = onEditKeys(this.FindForm(), this.Icon, p != null ? p.ProgramClassKeyUserData.Alt("") : "");

            if ( ud != null )
            {
                if (p == null)
                    p = new ActionProgram(GetSuggestedName());

                paras.Text = "";
                cd.ActionVars = new Variables();
                p.SetKeySayProgram(ud, p.ProgramClassSayUserData);
                UpdateProgram(p);
            }
        }

        private void Saypress_Click(object sender, EventArgs e)
        {
            ActionProgram p = cd.Action.HasChars() ? actionfile.ProgramList.Get(cd.Action) : null;

            string ud = onEditSay(this.FindForm(), p != null ? p.ProgramClassSayUserData.Alt("") : "" , actioncorecontroller );

            if ( ud != null )
            {
                if (p == null)
                    p = new ActionProgram(GetSuggestedName());

                paras.Text = "";
                cd.ActionVars = new Variables();
                p.SetKeySayProgram(p.ProgramClassKeyUserData,ud);
                UpdateProgram(p);
            }
        }

        void UpdateProgram(ActionProgram np)
        {
            actionfile.ProgramList.AddOrChange(np);                // replaces or adds (if its a new name) same as rename
            cd.Action = np.Name;
            RefreshEvent();
            UpdateControls();
        }

        public string GetSuggestedName()
        {
            string sroot = SuggestedName();
            string suggestedname = sroot;
            int n = 2;
            while (actionfile.ProgramList.GetActionProgramList().Contains(suggestedname))
            {
                suggestedname = sroot + "_" + n.ToStringInvariant();
                n++;
            }

            return suggestedname;
        }

        public new void Dispose()
        {
            Controls.Clear();
            proglist.Dispose();
            progedit.Dispose();
            paras.Dispose();
            base.Dispose();
        }
    }
}
