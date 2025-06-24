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
    public class ActionIfElseBase : ActionBase
    {
        protected bool evaluationexpression = false;
        protected ConditionLists condition;         // decoded condition

        protected bool FillCondition(ActionProgramRun ap, string conditionstr)
        {
            condition = new ConditionLists();
            if (condition.Read(conditionstr) != null)
            {
                condition = null;
                ap.ReportError("IF condition is not correctly formed");
                return false;
            }
            return true;
        }

        protected bool RunCondition(ActionProgramRun ap, out bool? condres)
        {
            condres = condition.CheckAll(ap.Functions.Vars, out string errlist, ap.Functions);     // may return null.. and will return errlist

            if (errlist == null)
                return true;
            else
            {
                ap.ReportError(errlist);
                return false;
            } 
        }

        protected bool ExecuteEval(ActionProgramRun ap, string expr, out bool res)
        {
            Eval ev = new Eval(ap.variables, new BaseFunctionsForEval(), checkend: true, allowfp: true, allowstrings: true);
            Object ret = ev.Evaluate(expr);
            res = false;

            if (ev.InError)
            {
                ap.ReportError(ev.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return false;
            }
            else if (ret is long)
            {
                res = ((long)ret) != 0;
                return true;
            }
            else if (ret is double)
            {
                res = ((double)ret) != 0;
                return true;
            }
            else
            {
                ap.ReportError("Evaluation resulted in a string result - must be a number");
                return false;
            }
        }

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars) //standard one used for most
        {
            if (evaluationexpression)
            {
                string promptValue = ExtendedControls.PromptSingleLine.ShowDialog(parent, "Condition", UserData, "Configure Evaluation Condition", cp.Icon);
                if (promptValue != null)
                {
                    userdata = promptValue;
                }

                return (promptValue != null);
            }
            else
            {
                ConditionLists jf = new ConditionLists();
                jf.Read(userdata);
                bool ok = ConfigurationMenuCondition(parent, cp, eventvars, ref jf);
                if (ok)
                    userdata = jf.ToString();
                return ok;
            }
        }

        // for use by ErrorIF
        public bool ConfigurationMenuCondition(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars, ref ConditionLists jf)
        {
            ExtendedConditionsForms.ConditionFilterForm frm = new ExtendedConditionsForms.ConditionFilterForm();
            frm.VariableNames = eventvars;
            frm.InitCondition("Define condition", cp.Icon, jf);

            if (frm.ShowDialog(parent) == DialogResult.OK)
            {
                jf = frm.Result;
                return true;
            }
            else
                return false;
        }

        public override string VerifyActionCorrect()
        {
            if (evaluationexpression)
            {
                return null;
            }
            else
            {
                ConditionLists cl2 = new ConditionLists();
                string ret = cl2.Read(userdata);
                if (ret == null)
                {
                    userdata = cl2.ToString();  // Normalize it!
                }
                return ret;
            }
        }
    }

    public class ActionIf : ActionIfElseBase
    {

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            if (ap.IsExecuteOn)
            {
                if (evaluationexpression)
                {
                    if ( ExecuteEval(ap,userdata,out bool res))
                    {
                        ap.PushState(Type, res);       // true if has values and true, else false
                    }
                }
                else
                {
                    if (FillCondition(ap, userdata))         // make sure condition is set..
                    {
                        if (RunCondition(ap, out bool? condres))
                        {
                            bool res = condres.HasValue && condres.Value;
                            ap.PushState(Type, res);       // true if has values and true, else false
                        }
                    }
                }
            }
            else
                ap.PushState(Type, ActionProgramRun.ExecState.OffForGood);

            return true;

        }
    }

    public class ActionIfe : ActionIf
    {
        public ActionIfe() : base()
        {
            evaluationexpression = true;
        }
    }


    public class ActionElseIf : ActionIfElseBase
    {
        public override bool ExecuteAction(ActionProgramRun ap)
        {
            if (ap.IsExecutingType(ActionBase.ActionType.If))
            {
                if (ap.IsExecuteOff)       // if not executing, check condition
                {
                    if (evaluationexpression)
                    {
                        if ( ExecuteEval(ap, userdata, out bool res))
                        { 
                            ap.ChangeState(res);            // either to ON.. or to OFF, continuing the off
                        }

                    }
                    else
                    {
                        if (FillCondition(ap, userdata))         // make sure condition is set..
                        {
                            if (RunCondition(ap, out bool? condres))
                            {
                                bool res = condres.HasValue && condres.Value;
                                ap.ChangeState(res);            // either to ON.. or to OFF, continuing the off
                            }
                        }
                    }
                }
                else
                {
                    ap.ChangeState(ActionProgramRun.ExecState.OffForGood);      // make sure off for good
                }
            }
            else
                ap.ReportError("ElseIf without IF/IFF");

            return true;
        }
    }

    public class ActionElseIfe : ActionElseIf
    {
        public ActionElseIfe() : base()
        {
            evaluationexpression = true;
        }
    }

    public class ActionElse : ActionBase
    {
        public override bool ConfigurationMenuInUse { get { return false; } }
        public override string DisplayedUserData { get { return null; } }

        public override string VerifyActionCorrect()
        {
            return (UserData.Length == 0) ? null : " Text after else is not allowed";
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            if (ap.IsExecutingType(ActionBase.ActionType.If))
            {
                if (ap.IsExecuteOff)       // if not executing, turn on
                    ap.ChangeState(true); // go true
                else
                    ap.ChangeState(ActionProgramRun.ExecState.OffForGood);      // make sure off for good
            }
            else
                ap.ReportError("Else without IF/IFF");

            return true;
        }
    }

    public class ActionErrorIf : ActionIfElseBase
    {
        string errmsg;

        public bool FromStringCondition(string s, out ConditionLists cond, out string errmsg)    // errmsg has real control chars
        {
            cond = new ConditionLists();

            StringParser p = new StringParser(s);
            errmsg = p.NextQuotedWord(" ,", replaceescape: true);

            if (errmsg != null && p.IsCharMoveOn(','))
            {
                string condstring = p.LineLeft;

                if (cond.Read(condstring) == null)
                    return true;
            }

            errmsg = "";
            return false;
        }
        public string ToStringCondition(ConditionLists cond, string errmsg)
        {
            return errmsg.EscapeControlChars().QuoteString(comma: true) + ", " + cond.ToString();       // this will return with escaped chars
        }

        public bool FromStringEvaluation(string s, out string cond, out string errmsg)    // errmsg has real control chars
        {
            StringParser p = new StringParser(s);
            errmsg = p.NextQuotedWord(" ,", replaceescape: true);

            if (errmsg != null && p.IsCharMoveOn(','))
            {
                cond = p.LineLeft;

                if (cond.HasChars())
                    return true;
            }

            cond = "";
            errmsg = "";
            return false;
        }
        public string ToStringEvaluation(string cond, string errmsg)
        {
            return errmsg.EscapeControlChars().QuoteString(comma: true) + ", " + cond;       // this will return with escaped chars
        }

        public override string VerifyActionCorrect()
        {
            if (evaluationexpression)
            {
                return FromStringEvaluation(userdata, out string cond1, out string errmsg) ? null : "ErrorIf.E not in correct format";
            }
            else
            {
                return FromStringCondition(userdata, out ConditionLists cond, out string errmsg) ? null : "ErrorIf not in correct format: \"Error string\", condition";
            }
        }

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            if (evaluationexpression)
            {
                FromStringEvaluation(userdata, out string cond, out string errmsg);

                var ret = ExtendedControls.PromptMultiLine.ShowDialog(parent, "Error to display", cp.Icon,
                                    new string[] { "Eval Condition", "Error to display" },
                                    new string[] { cond, errmsg } );
                
                if (ret != null)
                {
                    userdata = ToStringEvaluation(ret[0], ret[1]);
                    return true;
                }
            }
            else
            {
                FromStringCondition(userdata, out ConditionLists cond, out string errmsg);

                if (base.ConfigurationMenuCondition(parent, cp, eventvars, ref cond))
                {
                    string promptValue = ExtendedControls.PromptSingleLine.ShowDialog(parent, "Error to display", errmsg, "Configure ErrorIf Command", cp.Icon);
                    if (promptValue != null)
                    {
                        userdata = ToStringCondition(cond, promptValue);
                        return true;
                    }
                }

            }
            return false;
        }


        public override bool ExecuteAction(ActionProgramRun ap)
        {
            if (evaluationexpression)
            {
                FromStringEvaluation(userdata, out string cond, out string errmsg);

                if ( ExecuteEval(ap,cond,out bool res))
                { 
                    if (res)
                    {
                        if (ap.Functions.ExpandString(errmsg, out string exprerr) != Functions.ExpandResult.Failed)
                        {
                            ap.ReportError(exprerr);
                        }
                        else
                            ap.ReportError(exprerr);
                    }
                }
            }
            else
            {
                if (condition == null)
                {
                    if (!FromStringCondition(userdata, out condition, out errmsg))
                    {
                        ap.ReportError("ErrorIF condition is not correctly formed");
                        return true;
                    }
                }

                if ( RunCondition(ap,out bool? condres))
                { 
                    bool res = condres.HasValue && condres.Value;

                    if (res)
                    {
                        if (ap.Functions.ExpandString(errmsg, out string exprerr) != Functions.ExpandResult.Failed)
                        {
                            ap.ReportError(exprerr);
                        }
                        else
                            ap.ReportError(exprerr);
                    }
                }
            }

            return true;
        }
    }


    public class ActionErrorIfe : ActionErrorIf
    {
        public ActionErrorIfe() : base()
        {
            evaluationexpression = true;
        }
    }
}