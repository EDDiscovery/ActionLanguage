/*
 * Copyright © 2023 EDDiscovery development team
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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ActionLanguage
{
    public class ActionPackEditEventInputToKey : ActionPackEditEventBase
    {
        public System.Func<Form, System.Drawing.Icon, string, string> onEditKeys;   // edit the key string..  must provide
        public System.Func<Form, string, ActionCoreController, string> onEditSay;   // edit the say string..
        public System.Func<string[]> onInputButton;   // edit the input string..

        //public System.Func<Form, string, ActionCoreController, string> onEditInput;   // edit the input string..  must provide

        private ExtendedControls.ExtButton inputbutton;
        private ActionPackEditProgram ucprog;

        private const int panelxmargin = 3;
        private const int panelymargin = 1;

        public override void Init(Condition cond, List<string> events, ActionCoreController cp, string appfolder, ActionFile actionfile,
                        System.Func<string, List<BaseUtils.TypeHelpers.PropertyNameInfo>> func, Icon ic , ToolTip toolTip)
        {
            cd = cond;      // on creation, the cond with be set to onVoice with one condition, checked in ActionController.cs:SetPackEditor..

            // layed out for 12 point.  UC below require 28 point area

            inputbutton = new ExtendedControls.ExtButton();
            inputbutton.Location = new Point(panelxmargin, panelymargin);
            inputbutton.Size = new Size(356, 24);      // manually matched to size of eventprogramcondition bits
            inputbutton.Text = cd.Fields[0].MatchString + ":" + cd.Fields[1].MatchString + ":" + cd.Fields[2].MatchString;
            inputbutton.Click += (s, e) => { 
                var ret = onInputButton(); 
                if ( ret != null )
                {
                    cd.Fields[0].MatchString = ret[0];
                    cd.Fields[1].MatchString = ret[1];
                    cd.Fields[2].MatchString = ret[2];
                    inputbutton.Text = cd.Fields[0].MatchString + ":" + cd.Fields[1].MatchString + ":" + cd.Fields[2].MatchString;
                }
            };
            
            Controls.Add(inputbutton);

            ActionProgram p = cond.Action.HasChars() ? actionfile.ProgramList.Get(cond.Action) : null;
            ActionProgram.ProgramConditionClass classifier = p != null ? p.ProgramClass : ActionProgram.ProgramConditionClass.KeySay;

            ucprog = new ActionPackEditProgram();
            ucprog.Location = new Point(inputbutton.Right+16, 0);
            ucprog.Size = new Size(400, 28);       // init all the panels to 0/this height, select widths
            ucprog.Init(actionfile, cond, cp, appfolder, ic, toolTip, classifier);
            ucprog.onEditKeys = onEditKeys;
            ucprog.onEditSay = onEditSay;
            ucprog.onAdditionalNames += () => { return func(cd.EventName); };
            ucprog.SuggestedName += () => 
            {
                string name = cond.Fields.Count == 3 ? cond.Fields[0].MatchString + "_" + cond.Fields[1].MatchString + "_" + cond.Fields[2].MatchString : "Unknown";
                name = name.SafeVariableString();
                return "InputToKeys_" + name;
            };

            ucprog.RefreshEvent += () => { RefreshIt(); };
            Controls.Add(ucprog);

        }

        public override void UpdateProgramList(string[] proglist)
        {
            ucprog.UpdateProgramList(proglist);
        }

        public override void Dispose()
        {
            base.Dispose();
            ucprog.Dispose();
        }

        public override string ID() { return "Input->Keys"; }
    }
}