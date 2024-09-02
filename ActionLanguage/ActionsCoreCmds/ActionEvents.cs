/*
 * Copyright 2021 - 2021 EDDiscovery development team
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
using System.Windows.Forms;

namespace ActionLanguage
{
    class ActionEvents : ActionBase
    {
        public override bool AllowDirectEditingOfUserData { get { return true; } }

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            string promptValue = ExtendedControls.PromptSingleLine.ShowDialog(parent, "Command:", UserData, "Configure Events Command" , cp.Icon);

            if (promptValue != null)
            {
                userdata = promptValue;
            }

            return (promptValue != null);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            string res;
            if (ap.Functions.ExpandString(UserData, out res) != Functions.ExpandResult.Failed)
            {
                StringParser sp = new StringParser(res);
                string prefix = "EV_";

                string cmdname = sp.NextWordLCInvariant(" ");

                if (cmdname != null && cmdname.Equals("prefix"))
                {
                    prefix = sp.NextWord();

                    if (prefix == null)
                    {
                        ap.ReportError("Missing name after Prefix in Event");
                        return true;
                    }

                    cmdname = sp.NextWordLCInvariant(" ");
                }

                if (cmdname.Equals("add"))
                {
                    bool res1 = false;
                    string err = "Bad Groupname",groupname=null;
                    Condition c = new Condition();

                    if (!(groupname = sp.NextQuotedWord(", ")).HasChars() || !sp.IsCharMoveOn(',') || (err = c.Read(sp, true)).HasChars())
                    {
                        ap.ReportError("Bad conditional line: " + err);
                    }
                    else if (c.Action.Length == 0 || c.EventName.Length == 0)
                    {
                        ap.ReportError("Missing event name or action: " + err);
                    }
                    else
                    {
                        c.GroupName = groupname;
                        if (ap.ActionFile.InUseEventList.Contains(c))
                        {
                            res1 = false;
                        }
                        else
                        {
                            ap.ActionFile.InUseEventList.Add(c);
                            res1 = true;
                        }
                    }

                    ap[prefix + "Result"] = res1.ToStringIntValue();
                }
                else
                {
                    string groupname = null;
                    string eventname = null;
                    string action = null;
                    string actionvarsstr = null;
                    string condition = null;

                    if ((groupname = sp.NextQuotedWord(", ")) == null || !sp.IsCharMoveOn(',') ||
                            (eventname = sp.NextQuotedWord(", ")) == null || !sp.IsCharMoveOn(',') ||
                            (action = sp.NextQuotedWord(", ")) == null || !sp.IsCharMoveOn(',') ||
                            (actionvarsstr = sp.NextQuotedWord(", ")) == null || !sp.IsCharMoveOn(',') ||
                            (condition = sp.NextQuotedWord("")) == null
                            )
                    {
                        ap.ReportError("Missing event pattern");
                    }
                    else
                    {
                       // System.Diagnostics.Debug.WriteLine($"Events command {cmdname} g `{groupname}` e `{eventname}` a `{action}` v `{actionvarsstr}` c `{condition}`");
                        List<Condition> matching = ap.ActionFile.InUseEventList.Find(groupname,eventname, action, actionvarsstr, condition);

                        ap[prefix + "Count"] = matching.Count.ToStringInvariant();

                        if (cmdname == "delete")            // checked 14/5/21
                        {
                            foreach (var e in matching)
                            {
                               // System.Diagnostics.Debug.WriteLine("Event Delete " + e.GroupName + ":" + e.ToString(true));
                                ap.ActionFile.InUseEventList.Remove(e);
                            }

                        }
                        else if (cmdname == "disable" || cmdname == "enable" )      // checked 14/5/21
                        {
                            bool disabled = cmdname == "disable";
                            foreach (var e in matching)
                            {
                                e.Disabled = disabled;
                             //   System.Diagnostics.Debug.WriteLine("Event " + !disabled + ": " + e.GroupName + ":" + e.ToString(true));
                            }
                        }
                        else if (cmdname == "disableallbut" || cmdname == "enableallbut")
                        {
                            bool enabled = cmdname == "enableallbut";
                            foreach (var e in ap.ActionFile.InUseEventList.Enumerable)     // all events
                            {
                                e.Disabled = matching.Contains(e) ? enabled : !enabled;    // if matching list has it, we want it to state, else to opposite state
                               // System.Diagnostics.Debug.WriteLine("Event " + !e.Disabled + ": " + e.GroupName + ":" + e.ToString(true));
                            }
                        }
                        else if (cmdname == "list")         // checked 14/5/21
                        {
                            for (int i = 0; i < matching.Count; i++)
                            {
                                ap[prefix + "Event[" + (i + 1).ToStringInvariant() + "]"] = matching[i].ToString(true);
                                ap[prefix + "Enabled[" + (i + 1).ToStringInvariant() + "]"] = matching[i].Disabled ? "0" : "1";
                                ap[prefix + "GroupName[" + (i + 1).ToStringInvariant() + "]"] = matching[i].GroupName ?? "";        // groupnames can be null
                            }
                        }
                        else
                            ap.ReportError("Events unknown command");
                    }
                }

            }
            else
                ap.ReportError("Events command line not in correct format");

            return true;
        }
    }
}
