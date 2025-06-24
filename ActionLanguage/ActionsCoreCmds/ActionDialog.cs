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

                    cf.TriggerAdv += Cd_TriggerAdv;

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
                        cf.InitCentred(ap.ActionController.ParentUIForm, minsize, maxsize, createdsize,
                                        ap.ActionController.Icon,
                                        exp[1],
                                        exp[0], new List<Object>() { ap, IsModalDialog() }, // logical name and tag
                                        closeicon: closeicon
                                        );
                    }

                    cf.TopMost = alwaysontop;

                    if ( !noshow )
                        cf.Show(ap.ActionController.ParentUIForm);

                    return noshow || !IsModalDialog();       // if no show, continue. If modal, return false, STOP.  Non modal, continue
                }
                else
                    ap.ReportError(exp[0]);
            }
            else
                ap.ReportError("DialogControl Dialog command line not in correct format");

            return true;
        }

        static private void Cd_TriggerAdv(string lname, string controlname, Object value1, Object value2, Object tag)
        {
            if (controlname == "Close")     // put in backwards compatibility - close is the same as cancel for action programs
                controlname = "Cancel";

            List<Object> tags = tag as List<Object>;        // object is a list of objects! lovely

            ActionProgramRun apr = tags[0] as ActionProgramRun;
            bool ismodal = (bool)tags[1];

            string v1 = null, v2 = null;
            try
            {
                if (value1 != null)
                    v1 = Convert.ToString(value1, System.Globalization.CultureInfo.InvariantCulture);
                if (value2 != null)
                    v2 = Convert.ToString(value2, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch ( Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ActionDialog exception converting value {ex}");
            }

            if (ismodal)
            {
                apr[lname] = controlname;
                if (v1 != null)
                    apr[lname + "_Value"] = v1;
                if (v2 != null)
                    apr[lname + "_Value2"] = v2;
                apr.ResumeAfterPause();
            }
            else
            {
                Variables v = new Variables() { ["Dialog"] = lname, ["Control"] = controlname };
                if (v1 != null)
                    v[lname + "_Value"] = v1;
                if (v2 != null)
                    v[lname + "_Value2"] = v2;
                apr.ActionController.ActionRun(ActionEvent.onNonModalDialog, v);
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
                        cf.Show(ap.ActionController.ParentUIForm);
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
                        if (control != null && value != null)
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

                        if (controlvar.HasChars())
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
                    else if (cmd.Equals("insertcolumns"))
                    {
                        string control = sp.NextQuotedWord(" ,");
                        sp.IsCharMoveOn(',');       // optional
                        int? pos = sp.NextInt("; ");

                        if (pos.HasValue && sp.IsCharMoveOn(';'))
                        {
                            while (sp.IsCharMoveOn('('))
                            {
                                string coltype = sp.NextQuotedWordComma();
                                string headertext = sp.NextQuotedWordComma();
                                int? fillsize = sp.NextInt(", ");
                                string sortmode = sp.IsCharMoveOn(',') ? sp.NextQuotedWord(") ") : "Alpha";
                                if (fillsize.HasValue && sortmode.HasChars() && sp.IsCharMoveOn(')') && (sp.IsEOL || sp.IsCharMoveOn(',')))
                                {
                                    if (!cf.InsertColumn(control, pos.Value, coltype, headertext, fillsize.Value, sortmode))
                                    {
                                        ap.ReportError($"DialogControl InsertColumns bad parameters");
                                        break;
                                    }

                                    pos++;
                                }
                                else
                                    break;

                            }
                        }

                        if ( !sp.IsEOL)
                            ap.ReportError($"DialogControl InsertColumns bad parameters");
                    }
                    else if (cmd.Equals("removecolumns"))
                    {
                        string control = sp.NextQuotedWord(" ,");
                        sp.IsCharMoveOn(',');       // optional
                        int? pos = sp.NextIntComma(", ");
                        int? count = sp.NextInt(", ");
                        if (pos.HasValue && count.HasValue)
                        {
                            if (!cf.RemoveColumns(control, pos.Value, count.Value))
                                ap.ReportError($"DialogControl RemoveColumns bad parameters");
                        }
                    }
                    else if (cmd.Equals("rightclickmenu"))
                    {
                        string control = sp.NextQuotedWord(" ,");
                        sp.IsCharMoveOn(',');       // optional
                        List<string> tags = new List<string>();
                        List<string> text = new List<string>();
                        while (!sp.IsEOL)
                        {
                            string tag = sp.NextQuotedWord(" ,");
                            string item;
                            if (tag.HasChars() && sp.IsCharMoveOn(',') && (item = sp.NextQuotedWord(" ,")).HasChars())
                            {
                                tags.Add(tag);
                                text.Add(item);
                                sp.IsCharMoveOn(',');
                            }
                            else
                            {
                                ap.ReportError("DialogControl RightClickMenu bad parameters");
                                return true;
                            }
                        }

                        if (!cf.SetRightClickMenu(control, tags.ToArray(), text.ToArray()))
                            ap.ReportError($"DialogControl RemoveColumns bad parameters on call");

                    }
                    else if (cmd.Equals("getcolumnssetting"))
                    {
                        string control = sp.NextQuotedWord(" ");
                        object tk = null;
                        if (control.HasChars() && (tk = cf.GetDGVColumnSettings(control)) != null)
                        {
                            string s = Convert.ToString(tk);
                            ap["ColumnsSetting"] = s;
                        }
                        else
                            ap.ReportError($"DialogControl GetColumnSettings bad parameters");
                    }
                    else if (cmd.Equals("setcolumnssetting"))
                    {
                        string control = sp.NextQuotedWord(" ,");
                        sp.IsCharMoveOn(',');       // optional
                        string setting = sp.NextQuotedWord(" ");
                        if (control.HasChars() && setting.HasChars() && cf.SetDGVColumnSettings(control, setting))
                        {
                        }
                        else
                            ap.ReportError($"DialogControl SetColumnSettings bad parameters");
                    }
                    else if (cmd.Equals("setdgvsettings"))
                    {
                        string control = sp.NextQuotedWord(" ,");
                        sp.IsCharMoveOn(',');       // optional
                        bool? cr = sp.NextBoolComma(" ,");
                        bool? pcw = sp.NextBoolComma(" ,");
                        bool? rhv = sp.NextBoolComma(" ,");
                        bool? srs = sp.NextBool(" ");
                        if (control.HasChars() && cr.HasValue && pcw.HasValue && rhv.HasValue && srs.HasValue 
                                    && cf.SetDGVSettings(control, cr.Value, pcw.Value, rhv.Value, srs.Value))
                        {
                        }
                        else
                            ap.ReportError($"DialogControl SetDGVSettings bad parameters");
                    }
                    else if (cmd.Equals("setwordwrap"))
                    {
                        string control = sp.NextQuotedWord(" ,");
                        sp.IsCharMoveOn(',');       // optional
                        bool? ww = sp.NextBool(" ");
                        if (control.HasChars() && ww.HasValue && cf.SetWordWrap(control, ww.Value))
                        {
                        }
                        else
                            ap.ReportError($"DialogControl SetDGVWordWrap bad parameters");
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
                        string control = sp.NextQuotedWord(" ,");
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
                        string control = sp.NextQuotedWord(" ,");
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
                        string control = sp.NextQuotedWord(" ,");
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
                                good = cf.SetPosition(control, new System.Drawing.Point(x.Value, y.Value));
                                good &= cf.SetSize(control, new System.Drawing.Size(w.Value, h.Value));
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

    public class ActionDialogEntry : ActionBase
    {
        public override bool AllowDirectEditingOfUserData { get { return true; } }    // and allow editing?

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            List<string> l = FromString(userdata);
            List<string> r = ExtendedControls.PromptMultiLine.ShowDialog(parent, "Configure MessageBox Dialog", cp.Icon,
                            new string[] { "DVar", "Name", "Type", "Text",          // 0 offset
                                            "X","Y", "Width","Height",              // 4
                                            "ToolTip",                              // 8 tooltip makes it 9 entries
                                            "Panel", "Dock", "Anchor", "Margin",    // 9,10,11,12
                                            "Other Params 1","Other Params 2","Other Params 3","Further Params 4",    // 13,14,15,16
                            },
                            l?.ToArray(),
                            new int[] { 24, 24, 24, 50, 24, 24, 24, 24, 50, 24, 24, 24, 24, 50, 50, 50, 50 },
                            multiline: true, widthboxes: 400, heightscrollarea:-1);

            if (r != null)
                userdata = r.ToStringCommaList(9, true, false);     // and escape them back, minimum 9 entries to tooltip, escape ctrl, don't quote empty

            return (r != null);
        }

        public override string VerifyActionCorrect()
        {
            return (FromString(userdata) != null) ? null : "DialogEntry command line not in correct format";
        }

        List<string> FromString(string input)       // returns in raw esacped mode
        {
            StringParser sp = new StringParser(input);
            List<string> s = sp.NextQuotedWordList(replaceescape: true);
            return (s != null && s.Count >= 9) ? s : null;      // must have up to tooltip
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            List<string> ctrl = FromString(UserData);

            if (ctrl != null)
            {
                List<string> exp;

                if (ap.Functions.ExpandStrings(ctrl, out exp) != Functions.ExpandResult.Failed)
                {
                    if (exp[0].HasChars() && exp[1].HasChars() && exp[2].HasChars())       
                    {
                        string dvar = exp[1] + "," + exp[2] + "," + exp[3].AlwaysQuoteString() +",";       // name,type,text always quoted

                        if (exp.Count >= 10 && exp[9].HasChars())
                            dvar += "In:" + exp[9].QuoteString() + ",";
                        if (exp.Count >= 11 && exp[10].HasChars())
                            dvar += "Dock:" + exp[10] + ",";
                        if (exp.Count >= 12 && exp[11].HasChars())
                            dvar += "Anchor:" + exp[11] + ",";
                        if (exp.Count >= 13 && exp[12].HasChars())
                            dvar += "Margin:" + exp[12] + ",";
    
                        if (TryEvaluate(ap, exp[4], out string x, true) && TryEvaluate(ap, exp[5], out string y, true) && 
                                        TryEvaluate(ap, exp[6], out string w) && TryEvaluate(ap, exp[7], out string h))
                        {
                            dvar += x + "," + y + "," + w + "," + h + "," + exp[8].AlwaysQuoteString();    // x,y,w,h,tooltip always quoted

                            for (int i = 13; i < exp.Count; i++)
                                dvar += "," + exp[i].QuoteString();     // only quote if contains quote or ends with space

                            int v = 1;
                            string key = "x";
                            while(true)
                            {
                                key = exp[0] + "_" + v.ToStringInvariant();
                                if (!ap.variables.Contains(key))
                                    break;
                                v++;
                            }

                            ap[key] = dvar;
                        }
                        else
                            ap.ReportError("DialogEntry x/y/w/h parameters not integer");
                    }
                    else
                        ap.ReportError("DialogEntry parameters 1 to 3 at least one is empty");
                }
                else
                    ap.ReportError(exp[0]);
            }
            else
                ap.ReportError("DialogEntry command line not in correct format");

            return true;
        }

        private bool TryEvaluate(ActionProgramRun ap, string s, out string output, bool allowprefix = false)
        {
            string prefix = "";
            if ( allowprefix && (s.StartsWith("+") || s.StartsWith("-") ))
            {
                prefix = s.Substring(0, 1);
                s = s.Substring(1);
            }

            Eval ev = new Eval(ap.variables, new BaseFunctionsForEval(), checkend: true, allowfp: false, allowstrings: false);

            if (ev.TryEvaluateLong(s, out long v))
            {
                output = prefix + v.ToStringInvariant();
                return true;
            }
            else
            {
                output = "";
                return false;
            }
        }
    }


    public class ActionDialog : ActionDialogBase        // type of class determines Dialog action using IsModal
    {
    }

    public class ActionNonModalDialog : ActionDialogBase
    {
    }
}
