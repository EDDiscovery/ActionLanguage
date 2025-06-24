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
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ActionLanguage
{

    public class ActionWhile : ActionIfElseBase
    {
        public override bool ExecuteAction(ActionProgramRun ap)             // WHILE at top of loop
        {
            if (ap.IsExecuteOn)       // if executing
            {
                if (evaluationexpression)
                {
                    if (ExecuteEval(ap, userdata, out bool res))
                    {
                        ap.PushState(Type, res, res);        // set execute state, and push position if executing, returning us here when it drop out a level..
                    }
                }
                else
                {
                    if (FillCondition(ap, userdata))         // make sure condition is set..
                    {
                        if (RunCondition(ap, out bool? condres))
                        {
                            bool res = condres.HasValue && condres.Value;
                            ap.PushState(Type, res, res);   // set execute state, and push position if executing, returning us here when it drop out a level..
                        }
                    }
                }
            }
            else
                ap.PushState(Type, ActionProgramRun.ExecState.OffForGood);

            return true;
        }

        // WHILE at end of DO..WHILE, detected by LevelUp, return true if rolling back for another go
        public bool ExecuteEndDo(ActionProgramRun ap)               
        {
            if (ap.IsExecuteOn)                                     // if executing
            {
                if (evaluationexpression)
                {
                    if (ExecuteEval(ap, userdata, out bool res))
                    {
                        if (res)
                        {
                            ap.Goto(ap.PushPos + 1);                        // back to DO+1, keep level
                            return true;                                    // Else drop the level, and finish the do.
                        }
                    }
                }
                else
                {
                    if ( FillCondition(ap, userdata))
                    {
                        if (RunCondition(ap, out bool? condres))
                        {
                            bool res = condres.HasValue && condres.Value;

                            if (res)
                            {
                                ap.Goto(ap.PushPos + 1);                        // back to DO+1, keep level
                                return true;                                    // Else drop the level, and finish the do.
                            }
                        }
                    }
                }
            }

            return false;                                           // not executing the DO, so we just let the standard code drop the level.  Position will not be pushed
        }
    }
    public class ActionWhilee : ActionWhile
    {
        public ActionWhilee() : base()
        {
            evaluationexpression = true;
        }
    }

    public class ActionDo : ActionBase
    {
        public override bool ConfigurationMenuInUse { get { return false; } }
        public override string DisplayedUserData { get { return null; } }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            if (ap.IsExecuteOn)       // if executing
            {
                ap.PushState(Type, true, true);   // set execute to On (it is on already) and push the position of the DO
            }
            else
            {
                ap.PushState(Type, ActionProgramRun.ExecState.OffForGood);  // push off for good, don't push position since we don't want to loop
            }

            return true;
        }
    }

    public class ActionLoop : ActionBase
    {
        private bool inloop = false;
        private long loopcount = 0;
        private string loopvar;

        public override bool AllowDirectEditingOfUserData { get { return true; } }    // and allow editing?

        List<string> FromString(string input)       // returns in raw esacped mode
        {
            StringParser sp = new StringParser(input);
            List<string> s = sp.NextQuotedWordList(replaceescape: true);
            return (s != null && s.Count >= 1 && s.Count <= 2) ? s : null;
        }

        public override string VerifyActionCorrect()
        {
            return (FromString(userdata) != null) ? null : "Loop command line not in correct format";
        }

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            List<string> l = FromString(userdata);
            List<string> r = ExtendedControls.PromptMultiLine.ShowDialog(parent, "Configure Loop", cp.Icon,
                            new string[] { "Loop count", "Optional var name" }, l?.ToArray(), true);

            if (r != null)
                userdata = r.ToStringCommaList(1, true);     // and escape them back

            return (r != null);
        }

        public override bool ExecuteAction(ActionProgramRun ap)     // LOOP when encountered
        {
            if (ap.IsExecuteOn)         // if executing
            {
                if (!inloop)            // if not in a loop
                {
                    List<string> ctrl = FromString(UserData);
                    List<string> exp;

                    if (ap.Functions.ExpandStrings(ctrl, out exp) != Functions.ExpandResult.Failed)
                    {
                        Eval ev = new Eval(ap.variables, new BaseFunctionsForEval(), checkend: true, allowfp: false, allowstrings: false);
                        Object ret = ev.Evaluate(exp[0]);

                        if (!ev.InError)
                        {
                            loopcount = (long)ret;
                            inloop = true;
                            ap.PushState(Type, (loopcount > 0), true);   // set execute to On (if loop count is >0) and push the position of the LOOP
                            loopvar = (exp.Count >= 2 && exp[1].Length > 0) ? exp[1] : ("Loop" + ap.ExecLevel);     // pick name.. if not given, use backwards compat name
                            ap[loopvar] = "1";
                        }
                        else
                            ap.ReportError("Loop count must be an integer");
                    }
                    else
                        ap.ReportError(exp[0]);
                }
                else
                    ap.ReportError("Internal error - Loop is saying counting when run");
            }
            else
            {
                ap.PushState(Type, ActionProgramRun.ExecState.OffForGood, true);  // push off for good and save position so we know which loop we are executing
                inloop = true;    // we are in the loop properly.
                loopcount = 0;      // and with no count
            }

            return true;
        }

        public bool ExecuteEndLoop(ActionProgramRun ap)     // only called if Push pos is set.  break clears the push pos
        {
            if (inloop)                   // if in a count, we were executing at the loop, either on or off
            {
                if (--loopcount > 0)        // any count left?
                {
                    ap.Goto(ap.PushPos + 1);                    // back to LOOP+1, keep level

                    int c = 0;
                    if (ap[loopvar].InvariantParse(out c)) // update LOOP level variable.. don't if they have mucked it up
                        ap[loopvar] = (c + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

                    return true;
                }
                else
                {
                    inloop = false;                           // turn off check flag, and we just exit the loop normally
                }
            }
            else
                ap.ReportError("Internal error - END Loop is saying not counting when run");

            return false;                                           // not executing the LOOP, so we just let the standard code drop the level.  Position will not be pushed
        }
    }

    public class ActionForEach : ActionBase
    {
        static public bool FromString(string s, out string macroname, out string searchterm)
        {
            StringParser sp = new StringParser(s);
            macroname = sp.NextQuotedWord();
            searchterm = null;

            return (macroname != null && sp.IsStringMoveOn("in",StringComparison.InvariantCultureIgnoreCase) && (searchterm = sp.NextQuotedWord()) != null);
        }

        static public string ToString(string macro, string searchterm)
        {
            return macro.QuoteString() + " in " + searchterm.QuoteString();
        }

        public override string VerifyActionCorrect()
        {
            string mn, st;
            return FromString(UserData, out mn, out st) ? null : "ForEach command line not in correct format";
        }

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            string mn = "", st = "";
            FromString(UserData, out mn, out st);

            List<string> promptValue = ExtendedControls.PromptMultiLine.ShowDialog(parent, "ForEach:", cp.Icon,
                             new string[] { "Var Name", "Search" },
                             new string[] { mn, st },
                             false,
                             new string[] { "Enter the variable to be assigned with the variable name", "Enter the front search pattern" });

            if (promptValue != null)
                userdata = ToString(promptValue[0], promptValue[1]);

            return (promptValue != null);
        }

        int count;
        string expmacroname;
        List<string> values;

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            if (ap.IsExecuteOn)
            {
                string macroname, searchterm;

                if (FromString(UserData, out macroname, out searchterm))
                {
                    if (ap.Functions.ExpandString(macroname, out expmacroname) == BaseUtils.Functions.ExpandResult.Failed)       //Expand out.. and if no errors
                    {
                        ap.ReportError(expmacroname);
                        return true;
                    }

                    string expsearchterm;

                    if (ap.Functions.ExpandString(searchterm, out expsearchterm) == BaseUtils.Functions.ExpandResult.Failed)       //Expand out.. and if no errors
                    {
                        ap.ReportError(expmacroname);
                        return true;
                    }

                    values = new List<String>();

                    expsearchterm = expsearchterm.RegExWildCardToRegular();

                    foreach (string key in ap.variables.NameEnumuerable)
                    {
                        if ( System.Text.RegularExpressions.Regex.IsMatch(key,expsearchterm))
                            values.Add(key);
                    }

                    ap.PushState(Type, values.Count>0, true);   // set execute to On and push the position of the ForEach
                    count = 0;
                    if (values.Count > 0)
                    {
                        ap[expmacroname] = values[count++];
                        ap["Index"] = count.ToStringInvariant();
                        System.Diagnostics.Debug.WriteLine("First value " + ap[expmacroname]);
                    }
                }
            }
            else
            {
                ap.PushState(Type, ActionProgramRun.ExecState.OffForGood, true);
                values = null;
            }

            return true;
        }

        public bool ExecuteEndFor(ActionProgramRun ap)   // only called if Push pos is set.  break clears the push pos
        {
            if ( values != null )
            {
                if (count < values.Count)
                {
                    ap.Goto(ap.PushPos + 1);
                    ap[expmacroname] = values[count++];
                    ap["Index"] = count.ToStringInvariant();
                    //System.Diagnostics.Debug.WriteLine("New value " + ap[expmacroname]);

                    return true;
                }
                else
                    values = null;
            }

            return false;
        }
    }
}

