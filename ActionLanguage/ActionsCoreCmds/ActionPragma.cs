﻿/*
 * Copyright 2017 EDDiscovery development team
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

using System.Collections.Generic;
using System.Windows.Forms;
using BaseUtils;

namespace ActionLanguage
{
    public class ActionPragma : ActionBase
    {
        public override bool AllowDirectEditingOfUserData { get { return true; } }

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            string promptValue = ExtendedControls.PromptSingleLine.ShowDialog(parent, "Pragma", UserData, "Configure Pragma Command" , cp.Icon);
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
                StringParser p = new StringParser(res);

                string cmd;
                while ((cmd = p.NextWordLCInvariant(" ")) != null)
                {
                    if (cmd.Equals("dumpvars"))
                    {
                        string rest = p.NextQuotedWord();

                        if (rest != null && rest.Length > 0)
                        {
                            Variables filtered = ap.variables.FilterVars(rest);
                            foreach (string key in filtered.NameEnumuerable)
                            {
                                ap.ActionController.LogLine(key + "=" + filtered[key]);
                            }
                        }
                        else
                        {
                            ap.ReportError("Missing variable wildcard after Pragma DumpVars");
                            return true;
                        }
                    }
                    else if (cmd.Equals("log"))
                    {
                        string rest = p.NextQuotedWord(replaceescape: true);

                        if (rest != null)
                        {
                            ap.ActionController.LogLine(rest);
                        }
                        else
                        {
                            ap.ReportError("Missing string after Pragma Log");
                            return true;
                        }
                    }
                    else if (cmd.Equals("debug"))
                    {
                        string rest = p.NextQuotedWord(replaceescape: true);

                        if (rest != null)
                        {
#if DEBUG
                            ap.ActionController.LogLine(rest);
#endif
                        }
                        else
                        {
                            ap.ReportError("Missing string after Debug");
                        }
                        return true;
                    }
                    else if (cmd.Equals("ignoreerrors"))
                    {
                        ap.SetContinueOnErrors(true);
                    }
                    else if (cmd.Equals("allowerrors"))
                    {
                        ap.SetContinueOnErrors(false);
                    }
                    else if (cmd.Equals("disableasync"))
                    {
                        ap.ActionController.AsyncMode = false;
                    }
                    else if (cmd.Equals("enableasync"))
                    {
                        ap.ActionController.AsyncMode = true;
                    }
                    else if (cmd.Equals("enabletrace"))
                    {
                        string file = p.NextQuotedWord();
                        ap.ActionController.DebugTrace(file == null, file);
                    }
                    else if (cmd.Equals("disabletrace"))
                    {
                        ap.ActionController.DebugTrace(false);
                    }
                    else if ( !ap.ActionController.Pragma(cmd) )
                    {
                        ap.ReportError("Unknown pragma");
                    }
                }
            }
            else
                ap.ReportError(res);

            return true;
        }
    }
}
