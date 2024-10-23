/*
 * Copyright 2017-2024 EDDiscovery development team
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

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BaseUtils;

namespace ActionLanguage
{
    public class ActionDialogBase : ActionBase
    {
        bool IsModalDialog() { return this.GetType().Equals(typeof(ActionDialog)); }

        public override bool AllowDirectEditingOfUserData { get { return true; } }    // and allow editing?

        List<string> FromString(string input)
        {
            StringParser sp = new StringParser(input);
            List<string> s = sp.NextQuotedWordList();
            return (s != null && s.Count >= 4) ? s : null;
        }

        public override string VerifyActionCorrect()
        {
            return (FromString(userdata) != null) ? null : " command line not in correct format";
        }

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            List<string> l = FromString(userdata);
            List<string> r = ExtendedControls.PromptMultiLine.ShowDialog(parent, "Configure Dialog", cp.Icon,
                            new string[] { "Logical Name", "Caption", "Size [Pos]", "Var Prefix" }, l?.ToArray(),
                            false, new string[] { "Handle name of menu", "Enter title of menu", "Size and optional Position, as w,h [,x,y] 200,300 or 200,300,500,100", "Variable Prefix" });
            if (r != null)
            {
                userdata = r.ToStringCommaList(2);
            }

            return (r != null);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            List<string> ctrl = FromString(UserData);

            if (ctrl != null)
            {
                List<string> exp;

                if (ap.Functions.ExpandStrings(ctrl, out exp) != Functions.ExpandResult.Failed)
                {
                    Variables cv = ap.variables.FilterVars(exp[3] + "*");

                    ExtendedControls.ConfigurableForm cf = new ExtendedControls.ConfigurableForm();

                    foreach (string k in cv.NameList)
                    {
                        string errmsg = cf.Add(cv[k]);
                        if (errmsg != null)
                            return ap.ReportError(errmsg + " in " + k + " variable for Dialog");
                    }

                    StringParser sp2 = new StringParser(exp[2]);
                    int? minw = sp2.NextWordComma().InvariantParseIntNull();      // minimum may be - for not present
                    int? minh = sp2.NextWordComma().InvariantParseIntNull();
                    int? x = sp2.NextWordComma().InvariantParseIntNull();       // position may be - for centred
                    int? y = sp2.NextWordComma().InvariantParseIntNull();
                    int? maxw = sp2.NextWordComma().InvariantParseIntNull();      // max width may be - for crazy max
                    int? maxh = sp2.NextWordComma().InvariantParseIntNull();
                    int? wantedw = sp2.NextWordComma().InvariantParseIntNull();   // wanted w may be - for autosize
                    int? wantedh = sp2.NextWordComma().InvariantParseIntNull();

                    bool closeicon = true, alwaysontop = false;
                    bool noshow = false;

                    for (int i = 4; i < exp.Count; i++)
                    {
                        if (exp[i].Equals("AllowResize", StringComparison.InvariantCultureIgnoreCase))
                            cf.AllowResize = true;
                        else if (exp[i].Equals("Transparent", StringComparison.InvariantCultureIgnoreCase))
                            cf.Transparent = true;
                        else if (exp[i].Equals("NoCloseIcon", StringComparison.InvariantCultureIgnoreCase))
                            closeicon = false;
                        else if (exp[i].Equals("AlwaysOnTop", StringComparison.InvariantCultureIgnoreCase))
                            alwaysontop = true;
                        else if (exp[i].Equals("NoWindowsBorder", StringComparison.InvariantCultureIgnoreCase))
                            cf.ForceNoWindowsBorder = true;
                        else if (exp[i].Equals("NoPanelBorder", StringComparison.InvariantCultureIgnoreCase))
                            cf.PanelBorderStyle = BorderStyle.None;
                        else if (exp[i].StartsWith("TopPanel:", StringComparison.InvariantCultureIgnoreCase))
                            cf.TopPanelHeight = exp[i].Substring(9).InvariantParseInt(32);
                        else if (exp[i].StartsWith("BottomPanel:", StringComparison.InvariantCultureIgnoreCase))
                            cf.BottomPanelHeight = exp[i].Substring(9).InvariantParseInt(32);
                        else if (exp[i].StartsWith("FontScale:", StringComparison.InvariantCultureIgnoreCase))
                            cf.FontScale = exp[i].Substring(10).InvariantParseFloat(1.0f);
                        else if (exp[i].Equals("NoShow", StringComparison.InvariantCultureIgnoreCase))
                            noshow = true;
                        else
                        {
                            ap.ReportError("DialogControl Unknown Dialog option " + exp[i]);
                            return true;
                        }
                    }

                    if (IsModalDialog())
                        ap.Dialogs[exp[0]] = cf;
                    else
                        ap.ActionFile.Dialogs[exp[0]] = cf;

                    cf.Trigger += Cd_Trigger;

                    System.Drawing.Size minsize = new System.Drawing.Size(minw.HasValue ? minw.Value : 10, minh.HasValue ? minh.Value : 10);
                    System.Drawing.Size maxsize = new System.Drawing.Size(maxw.HasValue ? maxw.Value : 50000, maxh.HasValue ? maxh.Value : 50000);
                    System.Drawing.Size createdsize = new System.Drawing.Size(wantedw.HasValue ? wantedw.Value : 1, wantedh.HasValue ? wantedh.Value : 1);

                    if (x != null && y != null)
                    {
                        cf.Init(minsize, maxsize, createdsize,
                                    new System.Drawing.Point(x.Value, y.Value),
                                    ap.ActionController.Icon,
                                    exp[1],
                                    exp[0], new List<Object>() { ap, IsModalDialog() },  // logical name and tag
                                    closeicon: closeicon
                                    );
                    }
                    else
                    {
                        cf.InitCentred(ap.ActionController.Form, minsize, maxsize, createdsize,
                                        ap.ActionController.Icon,
                                        exp[1],
                                        exp[0], new List<Object>() { ap, IsModalDialog() }, // logical name and tag
                                        closeicon: closeicon
                                        );
                    }

                    cf.TopMost = alwaysontop;

                    if ( !noshow )
                        cf.Show(ap.ActionController.Form);

                    return noshow || !IsModalDialog();       // if no show, continue. If modal, return false, STOP.  Non modal, continue
                }
                else
                    ap.ReportError(exp[0]);
            }
            else
                ap.ReportError("DialogControl Dialog command line not in correct format");

            return true;
        }

        static private void Cd_Trigger(string lname, string controlname, Object tag)
        {
            if (controlname == "Close")     // put in backwards compatibility - close is the same as cancel for action programs
                controlname = "Cancel";

            List<Object> tags = tag as List<Object>;        // object is a list of objects! lovely

            ActionProgramRun apr = tags[0] as ActionProgramRun;
            bool ismodal = (bool)tags[1];

            if (ismodal)
            {
                apr[lname] = controlname;
                apr.ResumeAfterPause();
            }
            else
            {
                apr.ActionController.ActionRun(ActionEvent.onNonModalDialog, new BaseUtils.Variables(new string[] { "Dialog", lname, "Control", controlname }));
            }
        }
    }

    public class ActionDialogControl : ActionBase
    {
        public override bool AllowDirectEditingOfUserData { get { return true; } }    // and allow editing?

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            string promptValue = ExtendedControls.PromptSingleLine.ShowDialog(parent, "DialogControl command", UserData, "Configure DialogControl Command", cp.Icon);
            if (promptValue != null)
            {
                userdata = promptValue;
            }

            return (promptValue != null);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            string exp;
            if (ap.Functions.ExpandString(UserData, out exp) != Functions.ExpandResult.Failed)
            {
                StringParser sp = new StringParser(exp);
                string handle = sp.NextWordComma();

                if (handle != null)
                {
                    bool infile = ap.ActionFile.Dialogs.ContainsKey(handle);
                    bool inlocal = ap.Dialogs.ContainsKey(handle);

                    ExtendedControls.IConfigurableDialog cf = infile ? ap.ActionFile.Dialogs[handle] : (inlocal ? ap.Dialogs[handle] : null);

                    string cmd = sp.NextWordLCInvariant();
                    sp.IsCharMoveOn(',');

                    if (cmd == null)
                    {
                        ap.ReportError("DialogControl missing command");
                    }
                    else if (cmd.Equals("exists"))
                    {
                        ap["Exists"] = (cf != null) ? "1" : "0";
                    }
                    else if (cf == null)
                    {
                        ap.ReportError("DialogControl no such dialog exists");
                    }
                    else if (cmd.Equals("show"))
                    {
                        cf.Show(ap.ActionController.Form);
                    }
                    else if (cmd.Equals("continue"))
                    {
                        return (inlocal) ? false : true;    // if local, pause. else just ignore
                    }
                    else if (cmd.Equals("close"))
                    {
                        cf.ReturnResult(cf.DialogResult);
                        if (inlocal)
                            ap.Dialogs.Remove(handle);
                        else
                            ap.ActionFile.Dialogs.Remove(handle);
                    }
                    else if (cmd.Equals("get"))
                    {
                        string control = sp.NextQuotedWord();
                        if (control.HasChars() && cf.Get(control, out string r))
                        {
                            ap["DialogResult"] = r;
                        }
                        else
                            ap.ReportError("DialogControl get missing or invalid dialog name");
                    }
                    else if (cmd.Equals("set") || cmd.Equals("setescape"))
                    {
                        string control = sp.NextQuotedWord(" =");
                        string value = sp.IsCharMoveOn('=') ? sp.NextQuotedWord() : null;
                        if (control != null && value != null && sp.IsEOL)
                        {
                            if (!cf.Set(control, value, cmd.Equals("setescape")))
                                ap.ReportError($"DialogControl set cannot set {control}");
                        }
                        else
                            ap.ReportError("DialogControl set missing or invalid dialog name and/or value");
                    }
                    else if (cmd.Equals("add"))
                    {
                        string cmdline = sp.LineLeft;

                        if (cmdline.HasChars())
                        {
                            string res = cf.Add(cmdline);
                            if (res.HasChars())
                                ap.ReportError($"DialogControl add error: {res}");
                            else
                                cf.UpdateEntries();
                        }
                        else
                            ap.ReportError($"DialogControl add no string");
                    }
                    else if (cmd.Equals("addtext"))
                    {
                        string control = sp.NextQuotedWord(" ,");
                        string value = sp.IsCharMoveOn(',') ? sp.NextQuotedWord() : null;
                        if (control != null && value != null )
                        {
                            if (!cf.AddText(control, value))
                                ap.ReportError($"DialogControl set cannot addtext {control}");
                        }
                        else
                            ap.ReportError($"DialogControl add no string");
                    }
                    else if (cmd.Equals("addrange"))
                    {
                        string controlvar = sp.NextQuotedWord();

                        if ( controlvar.HasChars())
                        {
                            Variables cv = ap.variables.FilterVars(controlvar + "*");

                            foreach (string k in cv.NameList)
                            {
                                string errmsg = cf.Add(cv[k]);
                                if (errmsg != null)
                                    return ap.ReportError($"DialogControl addrange error {errmsg}");
                            }

                            cf.UpdateEntries();
                        }
                        else
                            ap.ReportError($"DialogControl addrange no data variable");
                    }
                    else if (cmd.Equals("remove"))
                    {
                        string control = sp.NextQuotedWord(" ");

                        if (control.HasChars())
                        {
                            if (!cf.Remove(control))
                                ap.ReportError($"DialogControl remove failed for {control}");
                        }
                        else
                            ap.ReportError($"DialogControl remove no string");
                    }
                    else if (cmd.Equals("addsetrows"))
                    {
                        string control = sp.NextQuotedWord(" ,");
                        sp.IsCharMoveOn(',');       // optional

                        string cmdline = sp.LineLeft.Trim();

                        if (control.HasChars() && cmdline.HasChars())
                        {
                            string res = cf.AddSetRows(control, cmdline);
                            if (res.HasChars())
                                ap.ReportError($"DialogControl addsetrows error: {res}");
                        }
                        else
                            ap.ReportError($"DialogControl addsetrows no control name or set options");
                    }
                    else if (cmd.Equals("clear"))
                    {
                        string control = sp.NextQuotedWord(" ");

                        if (control.HasChars())
                        {
                            bool res = cf.Clear(control);
                            if (!res)
                                ap.ReportError($"DialogControl clear error for {control}");
                        }
                        else
                            ap.ReportError($"DialogControl clear no control name");
                    }
                    else if (cmd.Equals("removerows"))
                    {
                        string control = sp.NextQuotedWord();
                        sp.IsCharMoveOn(',');       // optional

                        if (control.HasChars())
                        {
                            int? start = sp.NextIntComma(", ");
                            int? count = sp.NextInt();

                            if (start != null && count != null)
                            {
                                int removed = cf.RemoveRows(control, start.Value, count.Value);
                                if (removed == -1)
                                    ap.ReportError($"DialogControl removerows DGV not found");
                                else
                                    ap["Removed"] = removed.ToStringInvariant();
                            }
                            else
                                ap.ReportError($"DialogControl removerows bad parameters");
                        }
                        else
                            ap.ReportError($"DialogControl clear no control name");
                    }
                    else if (cmd.Equals("enable") || cmd.Equals("visible"))     // verified 10/1/21
                    {
                        string control = sp.NextQuotedWord();
                        sp.IsCharMoveOn(',');       // optional

                        if (control.HasChars())
                        {
                            bool? r = sp.NextBool();

                            if (r == null)
                            {
                                if (cmd.Equals("enable"))
                                    ap["Enabled"] = cf.IsEnabled(control).ToStringIntValue();
                                else
                                    ap["Visible"] = cf.IsVisible(control).ToStringIntValue();
                            }
                            else
                            {
                                bool good = cmd.Equals("enable") ? cf.SetEnable(control, r.Value) : cf.SetVisible(control, r.Value);
                                if (!good)
                                    ap.ReportError($"DialogControl enable/visible control not found");
                            }
                        }
                        else
                            ap.ReportError($"DialogControl enable/visible no control name");
                    }
                    else if (cmd.Equals("position"))    // verified 10/1/21
                    {
                        int? x = sp.NextIntComma(", ");
                        int? y = sp.NextInt();

                        if (x != null)
                        {
                            if (y != null)
                                cf.Location = new System.Drawing.Point(x.Value, y.Value);
                            else
                                ap.ReportError("DialogControl Missing position");
                        }
                        else if (sp.IsEOL)
                        {
                            ap["X"] = cf.Location.X.ToStringInvariant();
                            ap["Y"] = cf.Location.Y.ToStringInvariant();
                        }
                        else
                            ap.ReportError("DialogControl missing position");
                    }
                    else if (cmd.Equals("size"))    // verified 10/1/21
                    {
                        int? w = sp.NextIntComma(",");
                        int? h = sp.NextInt();

                        if (w != null)
                        {
                            if (h != null)
                                cf.Size = new System.Drawing.Size(w.Value, h.Value);
                            else
                                ap.ReportError("DialogControl missing size");
                        }
                        else if (sp.IsEOL)
                        {
                            ap["W"] = cf.Size.Width.ToStringInvariant();
                            ap["H"] = cf.Size.Height.ToStringInvariant();
                        }
                        else
                            ap.ReportError("DialogControl missing size");
                    }
                    else if (cmd.Equals("controlbounds"))       // verified 10/1/21
                    {
                        string control = sp.NextQuotedWord();
                        sp.IsCharMoveOn(',');       // optional

                        if (control.HasChars())
                        {
                            int? x = sp.NextIntComma(", ");
                            int? y = sp.NextIntComma(", ");
                            int? w = sp.NextIntComma(", ");
                            int? h = sp.NextInt();

                            bool good;
                            if (x != null && y != null && w != null && h != null)
                            {
                                good = cf.SetPosition(control, new System.Drawing.Rectangle(x.Value, y.Value, w.Value, h.Value));
                            }
                            else
                            {
                                good = cf.GetPosition(control, out System.Drawing.Rectangle r);
                                if (good)
                                {
                                    ap["X"] = r.Left.ToStringInvariant();
                                    ap["Y"] = r.Top.ToStringInvariant();
                                    ap["W"] = r.Width.ToStringInvariant();
                                    ap["H"] = r.Height.ToStringInvariant();
                                }
                            }

                            if (!good)
                                ap.ReportError("DialogControl controlbounds missing control, or missing parameters");
                        }
                        else
                            ap.ReportError($"DialogControl controlbounds no control name");
                    }
                    else if (cmd.Equals("closedropdownbutton"))
                    {
                        cf.CloseDropDown();
                    }
                    else if (cmd.Equals("isallvalid"))
                    {
                        ap["Valid"] = cf.IsAllValid().ToStringIntValue();
                    }
                    else
                        ap.ReportError("DialogControl Unknown command in DialogControl");
                }
                else
                    ap.ReportError("DialogControl Missing handle in DialogControl");
            }
            else
                ap.ReportError(exp);

            return true;
        }

    }

    public class ActionDialog : ActionDialogBase        // type of class determines Dialog action using IsModal
    {
    }

    public class ActionNonModalDialog : ActionDialogBase
    {
    }
}
