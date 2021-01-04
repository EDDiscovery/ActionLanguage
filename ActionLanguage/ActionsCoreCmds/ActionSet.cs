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
using System;
using System.Collections.Generic;
using BaseUtils;

namespace ActionLanguage
{
    public class ActionSetLetBase : ActionBase
    {
        protected bool FromString(string ud, out Variables vars, out Dictionary<string, string> operations)
        {
            vars = new Variables();
            operations = new Dictionary<string, string>();
            StringParser p = new StringParser(ud);
            return vars.FromString(p, Variables.FromMode.OnePerLine, altops:operations);
        }

        protected string ToString(Variables vars, Dictionary<string, string> operations)
        {
            return vars.ToString(operations, pad: " ", comma:false, bracket:false, space:false);
        }

        public bool Configure(ActionCoreController cp, List<TypeHelpers.PropertyNameInfo> eventvars, ActionConfigFuncs configFuncs, bool allowaddv, bool allownoexpandv)
        {
            Variables av;
            Dictionary<string, string> operations;
            FromString(userdata, out av, out operations);

            return configFuncs.SetVariables("Define Variable:", cp.Icon, av, showatleastoneentry: true, allowadd: allowaddv, allownoexpand: allownoexpandv, altops: operations, allowmultipleentries: false, resultact: (v, a) =>
            {
                userdata = ToString(v, a);
            });
        }

        public override string VerifyActionCorrect()
        {
            Variables av;
            Dictionary<string, string> operations;
            bool ok = FromString(userdata, out av ,out operations);

            System.Diagnostics.Debug.Assert(ok == false || operations.Count == av.Count);

            return ok ? null : "Variable command not in correct format";
        }


        Variables av;
        Dictionary<string, string> operations;

        public bool ExecuteAction(ActionProgramRun ap, bool setit, bool globalit =false, bool persistentit =false, bool staticit = false )
        {
            if (av == null)
                FromString(userdata, out av, out operations);

            foreach (string key in av.NameEnumuerable)
            {
                string keyname = key;

                if (keyname.Contains("%"))      // if its an expansion, got for expansion
                {
                    if (ap.functions.ExpandString(key, out keyname) == Functions.ExpandResult.Failed)
                    {
                        ap.ReportError(keyname);
                        break;
                    }
                }
                else
                    keyname = ap.variables.Qualify(key);    // else allow name to be mangled

                string res;

                if (operations[key].Contains("$"))
                    res = av[key];
                else if (ap.functions.ExpandString(av[key], out res) == Functions.ExpandResult.Failed)       //Expand out.. and if no errors
                {
                    ap.ReportError(res);
                    break;
                }

                if (setit)
                {
                    if (operations[key].Contains("+") && ap.VarExist(keyname))
                        res = ap[keyname] + res;
                }
                else
                {
                    Eval ev = new Eval(res, checkend: true, allowfp: true, allowstrings: false);

                    Object ret = ev.Evaluate();

                    if (ev.InError)
                    {
                        ap.ReportError(ev.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    }
                    else
                    {
                        res = ev.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                //System.Diagnostics.Debug.WriteLine("Var " + keyname + "=" + res + "  :" + globalit + ":" + persistentit);
                ap[keyname] = res;

                if (globalit)
                    ap.actioncontroller.SetNonPersistentGlobal(keyname, res);

                if (persistentit )
                    ap.actioncontroller.SetPeristentGlobal(keyname, res);

                if (staticit )
                    ap.actionfile.SetFileVariable(keyname, res);
            }

            if (av.Count == 0)
                ap.ReportError("No variable name given in variable assignment");

            return true;
        }
    }

    public class ActionSet : ActionSetLetBase
    {
        public override bool Configure(ActionCoreController cp, List<TypeHelpers.PropertyNameInfo> eventvars, ActionConfigFuncs configFuncs)
        {
            return base.Configure(cp, eventvars, configFuncs, true, true);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            return ExecuteAction(ap, true);
        }
    }

    public class ActionGlobal : ActionSetLetBase
    {
        public override bool Configure(ActionCoreController cp, List<TypeHelpers.PropertyNameInfo> eventvars, ActionConfigFuncs configFuncs)
        {
            return base.Configure(cp, eventvars, configFuncs, true, true);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            return ExecuteAction(ap, true, globalit:true);
        }
    }

    public class ActionLet : ActionSetLetBase
    {
        public override bool Configure(ActionCoreController cp, List<TypeHelpers.PropertyNameInfo> eventvars, ActionConfigFuncs configFuncs)
        {
            return base.Configure(cp, eventvars, configFuncs, false, true);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            return ExecuteAction(ap, false);
        }
    }

    public class ActionGlobalLet : ActionSetLetBase
    {
        public override bool Configure(ActionCoreController cp, List<TypeHelpers.PropertyNameInfo> eventvars, ActionConfigFuncs configFuncs)
        {
            return base.Configure(cp, eventvars, configFuncs, false, true);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            return ExecuteAction(ap, false, globalit:true);
        }
    }

    public class ActionStaticLet : ActionSetLetBase
    {
        public override bool Configure(ActionCoreController cp, List<TypeHelpers.PropertyNameInfo> eventvars, ActionConfigFuncs configFuncs)
        {
            return base.Configure(cp, eventvars, configFuncs, false, true);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            return ExecuteAction(ap, false, staticit: true);
        }
    }

    public class ActionPersistentGlobal : ActionSetLetBase
    {
        public override bool Configure(ActionCoreController cp, List<TypeHelpers.PropertyNameInfo> eventvars, ActionConfigFuncs configFuncs)
        {
            return base.Configure(cp, eventvars, configFuncs, true, true);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            return ExecuteAction(ap, true, persistentit: true);
        }
    }

    public class ActionStatic : ActionSetLetBase
    {
        public override bool Configure(ActionCoreController cp, List<TypeHelpers.PropertyNameInfo> eventvars, ActionConfigFuncs configFuncs)
        {
            return base.Configure(cp, eventvars, configFuncs, true, true);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            return ExecuteAction(ap, true, staticit: true);
        }
    }


    public class ActionDeleteVariable: ActionBase
    {
        public override bool AllowDirectEditingOfUserData { get { return true; } }

        public override bool Configure(ActionCoreController cp, List<TypeHelpers.PropertyNameInfo> eventvars, ActionConfigFuncs configFuncs)
        {
            string promptValue = configFuncs.PromptSingleLine("Variable name", UserData, "Configure DeleteVariable Command", cp.Icon);
            if (promptValue != null)
            {
                userdata = promptValue;
            }

            return (promptValue != null);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            string res;
            if (ap.functions.ExpandString(UserData,  out res) != Functions.ExpandResult.Failed)
            {
                StringParser p = new StringParser(res);

                string v;
                while ((v = p.NextWord(", ")) != null)
                {
                    v = ap.variables.Qualify(v);
                    ap.actioncontroller.DeleteVariableWildcard(v);
                    ap.actionfile.DeleteFileVariableWildcard(v);
                    ap.DeleteVariableWildcard(v);
                    p.IsCharMoveOn(',');
                }
            }
            else
                ap.ReportError(res);

            return true;
        }
    }

    public class ActionExpr: ActionBase
    {
        public override bool AllowDirectEditingOfUserData { get { return true; } }

        public override bool Configure(ActionCoreController cp, List<TypeHelpers.PropertyNameInfo> eventvars, ActionConfigFuncs configFuncs)
        {
            string promptValue = configFuncs.PromptSingleLine("Expression", UserData, "Configure Function Expression", cp.Icon);
            if (promptValue != null)
            {
                userdata = promptValue;
            }

            return (promptValue != null);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            string res;
            if (ap.functions.ExpandString(UserData, out res) != Functions.ExpandResult.Failed)
            {
                ap["Result"] = res;
            }
            else
                ap.ReportError(res);

            return true;
        }
    }
}
