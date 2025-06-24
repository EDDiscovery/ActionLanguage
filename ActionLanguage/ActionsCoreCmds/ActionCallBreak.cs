/*
 * Copyright 2017-2025 EDDiscovery development team
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
using System.Linq;
using System.Windows.Forms;

namespace ActionLanguage
{

    public class ActionCall : ActionBase
    {
        public bool FromString(string s, out string progname, out Variables vars, out Dictionary<string, string> altops)
        {
            StringParser p = new StringParser(s);
            vars = new Variables();
            altops = new Dictionary<string, string>();

            progname = p.NextQuotedWord("( ");        // stop at space or (

            if (progname != null)
            {
                if (p.IsCharMoveOn('('))       // if (, then
                {
                    if (vars.FromString(p, Variables.FromMode.MultiEntryCommaBracketEnds, altops) && p.IsCharMoveOn(')') && p.IsEOL)      // if para list decodes and we finish on a ) and its EOL
                        return true;
                }
                else if (p.IsEOL)   // if EOL, its okay, prog name only
                    return true;
            }

            return false;
        }

        public string ToString(string progname, Variables cond, Dictionary<string, string> altops)
        {
            if (progname.IndexOf('(')>=0)                       // if any ( in name, needs quotes
                progname = progname.AlwaysQuoteString();

            if (cond.Count > 0)
                return progname + "(" + cond.ToString(altops, bracket: true) + ")";
            else
                return progname;
        }

        public string GetProgramName()
        {
            string progname;
            Variables vars;
            Dictionary<string, string> altops;
            return FromString(userdata, out progname, out vars, out altops) ? progname : null;
        }

        public override string VerifyActionCorrect()
        {
            string progname;
            Variables vars;
            Dictionary<string, string> altops;
            return FromString(userdata, out progname, out vars, out altops) ? null : "Call not in correct format: progname (var list v=\"y\")";
        }

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            string progname;
            Variables cond;
            Dictionary<string, string> altops;
            FromString(UserData, out progname, out cond, out altops);

            // test example List<string> p2romptValue = ExtendedControls.PromptMultiLine.ShowDialog(parent, "caption", cp.Icon, new string[] { "wkwkwkw wkw qwjkqwkqw qwkqwk", "wkwkw ejjd2" }, null, true);

            string promptValue = ExtendedControls.PromptSingleLine.ShowDialog(parent, "Program to call (use set::prog if req)", progname, "Configure Call Command", cp.Icon);
                
            if (promptValue != null)
            {
                ExtendedConditionsForms.VariablesForm avf = new ExtendedConditionsForms.VariablesForm();
                avf.Init("Variables to pass into called program".TxID(AFIDs.ActionPackEditForm_cp), cp.Icon, cond, showatleastoneentry: true, allownoexpand: true, altops: altops);

                if (avf.ShowDialog(parent) == DialogResult.OK)
                {
                    userdata = ToString(promptValue, avf.Result, avf.ResultAltOPs);
                    return true;
                }
            }

            return false;
        }

        //special call for execute, needs to pass back more data
        public bool ExecuteCallAction(ActionProgramRun ap, out string progname, out Variables vars)
        {
            Dictionary<string, string> altops;
            if (FromString(UserData, out progname, out vars, out altops) && progname.Length > 0)
            {
                if (ap.Functions.ExpandString(progname, out string prognameexpanded) == Functions.ExpandResult.Failed)
                {
                    ap.ReportError(prognameexpanded);
                    return true;
                }
                else
                    progname = prognameexpanded;

                List<string> wildcards = new List<string>();
                Variables newitems = new Variables();

                foreach (string key in vars.NameEnumuerable)
                {
                    int asterisk = key.IndexOf('*');
                    if (asterisk >= 0)                                    // SEE if any wildcards, if so, add to newitems
                    {
                        bool noexpand = altops[key].Contains("$");            // wildcard operator determines expansion state

                        wildcards.Add(key);
                        string prefix = key.Substring(0, asterisk);

                        foreach (string jkey in ap.variables.NameEnumuerable)
                        {
                            if (jkey.StartsWith(prefix))
                            {
                                if (noexpand)
                                    newitems[jkey] = ap[jkey];
                                else
                                {
                                    string res;
                                    if (ap.Functions.ExpandString(ap[jkey], out res) == Functions.ExpandResult.Failed)
                                    {
                                        ap.ReportError(res);
                                        return false;
                                    }

                                    newitems[jkey] = res;
                                }
                            }
                        }
                    }
                }

                foreach (string w in wildcards)     // remove wildcards
                    vars.Delete(w);

                //foreach ( stKeyValuePair<string,string> k in vars.values)          // for the rest, before we add in wildcards, expand
                foreach (string k in vars.NameEnumuerable.ToList())                            // for the rest, before we add in wildcards, expand. Note ToList
                {
                    bool noexpand = altops[k].Contains("$");            // when required

                    if (!noexpand)
                    {
                        string res;
                        if (ap.Functions.ExpandString(vars[k], out res) == Functions.ExpandResult.Failed)
                        {
                            ap.ReportError(res);
                            return false;
                        }

                        vars[k] = res;
                    }
                }

                vars.Add(newitems);         // finally assemble the variables

                return true;
            }
            else
            {
                ap.ReportError("Call not configured");
                return false;
            }
        }
    }

    public class ActionBreak : ActionBase
    {
        public override bool ConfigurationMenuInUse { get { return false; } }
        public override string DisplayedUserData { get { return null; } }        // null if you dont' want to display

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            ap.Break();
            return true;
        }
    }
}

