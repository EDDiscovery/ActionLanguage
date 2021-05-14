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
    // this class adds on an event field, and program fields

    public class ActionPackEditCondition : UserControl
    {
        public Func<List<BaseUtils.TypeHelpers.PropertyNameInfo>> onAdditionalNames;        // give me more names

        private const int panelxmargin = 3;
        private const int panelymargin = 1;
        private Icon Icon;
        private ExtendedControls.ExtPanelDropDown panelConditionType;
        private ExtendedControls.ExtTextBox textBoxCondition;
        private ExtendedControls.ExtButton buttonKeys;
        private Label labelAlwaysTrue;
        private Label labelAlwaysFalse;
        private Condition cd;
        bool overrideshowfull = false;

        public void Init(Condition c, Icon ic, ToolTip toolTip)
        {
            cd = c;     // point to common condition.  We only change the fields, not the cd.action/actiondata, and we don't replace it.
            Icon = ic;

            // layed out for 12 point. Requires a 28 pixel area to sit in

            panelConditionType = new ExtendedControls.ExtPanelDropDown();
            panelConditionType.Location = new Point(0, 0);
            panelConditionType.Size = new Size(this.Width, 28); // outer panel aligns with this UC 
            panelConditionType.SelectedIndexChanged += PanelConditionType_SelectedIndexChanged;
            toolTip.SetToolTip(panelConditionType, "Use the selector (click on bottom right arrow) to select condition class type");

            textBoxCondition = new ExtendedControls.ExtTextBox();
            textBoxCondition.Location = new Point(panelxmargin, panelymargin );
            textBoxCondition.Size = new Size(this.Width-8-panelxmargin*2, 24);    // 8 for selector
            textBoxCondition.ReadOnly = true;
            textBoxCondition.Click += Condition_Click;
            textBoxCondition.SetTipDynamically(toolTip, "Click to edit the condition that controls when the event is generated");

            buttonKeys = new ExtendedControls.ExtButton();
            buttonKeys.Location = textBoxCondition.Location;
            buttonKeys.Size = textBoxCondition.Size;
            buttonKeys.Click += Keypress_Click;
            toolTip.SetToolTip(buttonKeys,"Click to set the key list that associated this event with key presses");

            labelAlwaysTrue = new Label();
            labelAlwaysTrue.Location = new Point(panelxmargin, panelymargin+1);
            labelAlwaysTrue.Size = textBoxCondition.Size;
            labelAlwaysTrue.Text = "Always Action/True";

            labelAlwaysFalse = new Label();
            labelAlwaysFalse.Location = new Point(panelxmargin, panelymargin+1);
            labelAlwaysFalse.Size = textBoxCondition.Size;
            labelAlwaysFalse.Text = "Never Action/False";

            SuspendLayout();
            panelConditionType.Controls.Add(textBoxCondition);
            panelConditionType.Controls.Add(labelAlwaysTrue);
            panelConditionType.Controls.Add(labelAlwaysFalse);
            panelConditionType.Controls.Add(buttonKeys);
            Controls.Add(panelConditionType);
            SelectRepresentation();
            ResumeLayout();
        }

        public void ChangedCondition()  // someone altered condition externally. 
        {
            SelectRepresentation();
        }

        private void SelectRepresentation()
        {
            ConditionClass c = Classify(cd);

            panelConditionType.Items.Clear();       // dependent on program classification, select what we can pick
            if ( c == ConditionClass.Key )
                panelConditionType.Items.AddRange(new string[] { "Full Condition" , "Key" });
            else
                panelConditionType.Items.AddRange(new string[] { "Always Action/True", "Never Action/False", "Full Condition" });

            if (overrideshowfull)
                c = ConditionClass.Full;

            labelAlwaysTrue.Visible = (c == ConditionClass.AlwaysTrue);
            labelAlwaysFalse.Visible = (c == ConditionClass.AlwaysFalse);
            textBoxCondition.Visible = (c == ConditionClass.Full);
            buttonKeys.Visible = (c == ConditionClass.Key);
            textBoxCondition.Text = cd.ToString();
            buttonKeys.Text = (cd.Fields.Count > 0) ? cd.Fields[0].MatchString.Left(20) : "?";
        }

        private void PanelConditionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            overrideshowfull = false;
            string sel = panelConditionType.SelectedItem;
            if (sel.Contains("True"))    // always true
            {
                cd.SetAlwaysTrue();
            }
            else if (sel.Contains("False"))    // always true
            {
                cd.SetAlwaysFalse();
            }
            else if (sel.Contains("Full"))    // Full
            {
                overrideshowfull = true;    
            }
            else
            {
                if (Classify(cd) != ConditionClass.Key)
                    cd.Set(new ConditionEntry("KeyPress", ConditionEntry.MatchType.Equals, "?"));       // changes fields only
            }

            SelectRepresentation();
        }

        private void Keypress_Click(object sender, EventArgs e)
        {
            ExtendedControls.KeyForm kf = new ExtendedControls.KeyForm();
            kf.Init(this.Icon, false, ",", buttonKeys.Text.Equals("?") ? "" : buttonKeys.Text);
            if ( kf.ShowDialog(FindForm()) == DialogResult.OK)
            {
                buttonKeys.Text = kf.KeyList;
                cd.Fields[0].MatchString = kf.KeyList;
                cd.Fields[0].MatchCondition = (kf.KeyList.Contains(",")) ? ConditionEntry.MatchType.IsOneOf : ConditionEntry.MatchType.Equals;
            }
        }

        private void Condition_Click(object sender, EventArgs e)
        {
            ExtendedConditionsForms.ConditionFilterForm frm = new ExtendedConditionsForms.ConditionFilterForm();
            frm.VariableNames = onAdditionalNames();
            frm.InitCondition("Action condition", this.Icon, cd);
            frm.TopMost = this.FindForm().TopMost;
            if (frm.ShowDialog(this.FindForm()) == DialogResult.OK)
            {
                Condition res = frm.Result[0];
                if (res != null)
                {
                    cd.Fields = res.Fields;
                    cd.InnerCondition = res.InnerCondition;
                }
                else
                    cd.Fields = null;

                textBoxCondition.Text = cd.ToString();
            }
        }

        public new void Dispose()
        {
            panelConditionType.Controls.Clear();
            Controls.Clear();
            textBoxCondition.Dispose();
            panelConditionType.Dispose();    
            base.Dispose();
        }

        enum ConditionClass { Full, Key, AlwaysTrue , AlwaysFalse };

        private ConditionClass Classify(Condition c)
        {
            if (c.IsAlwaysTrue())
                return ConditionClass.AlwaysTrue;
            else if (c.IsAlwaysFalse())
                return ConditionClass.AlwaysFalse;
            else if (c.Fields.Count == 1)
            {
                if (c.Fields[0].ItemName == "KeyPress" && (c.Fields[0].MatchCondition == ConditionEntry.MatchType.Equals || c.Fields[0].MatchCondition == ConditionEntry.MatchType.IsOneOf))
                    return ConditionClass.Key;
            }

            return ConditionClass.Full;
        }

    }
}
