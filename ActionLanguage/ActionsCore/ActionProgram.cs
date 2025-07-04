﻿/*
 * Copyright 2017-2020 EDDiscovery development team
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
using System.Text;
using System.Windows.Forms;

namespace ActionLanguage
{
    public class ActionProgram              // HOLDS the program, can write it JSON or Text FILES
    {
        protected List<ActionBase> programsteps = new List<ActionBase>();
        public string Name { get; private set; } = "";
        public string StoredInSubFile { get; private set; } = null;       // if null, then its stored in its master ActionFile, else its stored in a subfile
        public string HeaderText { get; set; } =null;       // if null no custom header text.
        public enum ProgramConditionClass { Full, Key, Say, KeySay };       // classification of program, for auto generated scripts
        public ProgramConditionClass ProgramClass { get; private set; } = ProgramConditionClass.Full;  // set on read
        public string ProgramClassKeyUserData;        // if progclass includes key, here is the key (full include any paras)
        public string ProgramClassSayUserData;        // if progclass includes say, here is the speech (full)

        public int Count { get { return programsteps.Count; } }
        public IEnumerable<ActionBase> Enumerable { get { return programsteps; } }

        public ActionProgram(string name = null, string subfile = null, string precomments = null)
        {
            if (name != null)
                Name = name;

            StoredInSubFile = subfile;
            HeaderText = precomments;
        }

        public void Add(ActionBase ap)
        {
            programsteps.Add(ap);
        }

        public void Insert(int pos, ActionBase ap)
        {
            programsteps.Insert(pos,ap);
        }

        public void Clear()
        {
            programsteps.Clear();
        }

        public void CancelSubFileStorage()
        {
            StoredInSubFile = null;
        }

        public void SetSubFileStorage(string s)
        {
            StoredInSubFile = s;
        }

        public void Rename(string s)
        {
            Name = s;
        }

        public ActionBase GetStep(int a)
        {
            if (a < programsteps.Count)
                return programsteps[a];
            else
                return null;
        }

        public void SetStep(int step, ActionBase act)
        {
            while (step >= programsteps.Count)
                programsteps.Add(null);

            programsteps[step] = act;
        }

        public void MoveUp(int step)
        {
            ActionBase act = programsteps[step];
            programsteps.RemoveAt(step);
            programsteps.Insert(step - 1, act);
        }

        public void Delete(int step)
        {
            if (step < programsteps.Count)
                programsteps.RemoveAt(step);
        }

        #region Read and create program

        // read from stream. At this lineno, plus it may already be named
        public string Read(System.IO.TextReader sr, ref int lineno, string prenamed = "")         
        {
            string err = "";

            programsteps = new List<ActionBase>();
            Name = prenamed;

            List<int> indents = new List<int>();
            List<int> level = new List<int>();
            int indentpos = -1;
            int structlevel = 0;

            string completeline;

            int initiallineno = lineno;

            while (( completeline = sr.ReadLine() )!=null)
            {
                lineno++;

                completeline = completeline.Replace("\t", "    ");  // detab, to spaces, tabs are worth 4.
                BaseUtils.StringParser p = new BaseUtils.StringParser(completeline);

                if (!p.IsEOL)
                {
                    int curindent = p.Position;
                    string cmd = "";
                    if (p.IsStringMoveOn("//"))         // special, this is allowed to butt against text and still work
                        cmd = "//";
                    else
                        cmd = p.NextWord();

                    if (cmd.Equals("Else", StringComparison.InvariantCultureIgnoreCase))
                    {
                        //System.Diagnostics.Debug.WriteLine("Else " + cmd + " " + p.LineLeft);
                        if (p.IsStringMoveOn("If", StringComparison.InvariantCultureIgnoreCase))   // if Else followed by IF 
                            cmd = "Else If";
                    }

                    string line = p.LineLeft;           // and the rest of the line..

                    int commentpos = line.LastIndexOf("//");
                    string comment = "";

                    if (cmd != "//" && commentpos >= 0 && !line.InQuotes(commentpos))       // if not // command, and we have one..
                    {
                        comment = line.Substring(commentpos + 2).Trim();
                        line = line.Substring(0, commentpos).TrimEnd();
                    }

                    if (cmd.Equals("PROGRAM", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (Name.Length == 0) // should not be named at this point.. otherwise a block failure
                            Name = line;
                        else
                            err += lineno + " " + Name + " Duplicate PROGRAM line" + Environment.NewLine;
                    }
                    else if (cmd.Equals("END", StringComparison.InvariantCultureIgnoreCase) && line.Equals("PROGRAM", StringComparison.InvariantCultureIgnoreCase))
                    {
                        break;
                    }
                    else
                    {
                        ActionBase a = ActionBase.CreateAction(cmd, line, comment);
                        string vmsg;

                        if (a == null)
                        {
                            err += lineno + " " + Name + " Unrecognised command " + cmd + Environment.NewLine;
                        }
                        else if ((vmsg = a.VerifyActionCorrect()) != null)
                        {
                            err += lineno + " " + Name + ":" + vmsg + Environment.NewLine + " " + completeline.Trim() + Environment.NewLine;
                        }
                        else
                        {
                            if (indentpos == -1)        // starting condition, no idea on indent, set, with structlevel = 0
                            {
                                indentpos = curindent;
                            }
                            else if (curindent > indentpos)        // more indented, up one structure
                            {
                                structlevel++;
                                indentpos = curindent;
                            }
                            else if (curindent < indentpos)   // deindented
                            {
                                int statementatlowerlevel = -1;
                                for (int i = indents.Count - 1; i >= 0; i--)            // search up and find the entry with the indent below current
                                {
                                    if (indents[i] <= curindent)
                                    {
                                        statementatlowerlevel = i;
                                        break;
                                    }
                                }

                                if (statementatlowerlevel >= 0)       // if found, we have a statement to hook to..
                                {
                                    // while in DO loop

                                    if ((a.Type == ActionBase.ActionType.While && programsteps[statementatlowerlevel].Type == ActionBase.ActionType.Do))
                                    {
                                        int reallevel = level[statementatlowerlevel];        // we indicate its at the same level as the while
                                        a.LevelUp = structlevel - reallevel;                 // which causes a backup, and causes the LevelUp procedure to kick in
                                        structlevel = reallevel;                             // for next go, we are now at this level
                                        indentpos = indents[statementatlowerlevel];          // and our indent is now at the while level
                                    }

                                    // or Else/Elseif in IF 
                                    else if ((a.Type == ActionBase.ActionType.Else || a.Type == ActionBase.ActionType.ElseIf) && programsteps[statementatlowerlevel].Type == ActionBase.ActionType.If)
                                    {
                                        int reallevel = level[statementatlowerlevel] + 1;     // else are dedented, but they really are on 1 level above the if
                                        a.LevelUp = structlevel - reallevel;                  // if we backed up
                                        structlevel = reallevel;                              // now at this level
                                        indentpos = indents[statementatlowerlevel] + 4;       // and our indent should continue 4 in, so we don't match against this when we do indent
                                    }
                                    else
                                    {
                                        a.LevelUp = structlevel - level[statementatlowerlevel];
                                        structlevel = level[statementatlowerlevel];   // if found, we are at that.. except..
                                        indentpos = indents[statementatlowerlevel]; // and back to this level
                                    }
                                }
                            }

                            // System.Diagnostics.Debug.WriteLine($"{curindent} : ip {indentpos} sl {structlevel} lu {a.LevelUp} : Cmd {cmd} : {line}");

                            a.LineNumber = lineno;

                            //System.Diagnostics.Debug.WriteLine("    >>>> " + indentpos + ":" + structlevel);

                            indents.Add(indentpos);
                            level.Add(structlevel);
                            programsteps.Add(a);
                        }
                    }
                }
                else
                {
                    if (programsteps.Count > 0)
                        programsteps[programsteps.Count - 1].Whitespace = 1;
                }
            }

            if (programsteps.Count > 0 )
                programsteps[programsteps.Count - 1].Whitespace = 0;        // last cannot have whitespace..

            ProgramClass = Classify();

            err += CalculateLevels();       // this will moan if there is any errors

           // string s = ToString(true); BaseUtils.FileHelpers.TryWriteToFile(@"c:\code\prog.act", s);

            return err;
        }

        // go thru the program, and calc some values for editing purposes. Also look for errors
        public string CalculateLevels()         
        {
            string errlist = "";

            int structlevel = 0;
            int[] structcount = new int[50];
            ActionBase.ActionType[] structtype = new ActionBase.ActionType[50];

            System.Globalization.CultureInfo ct = System.Globalization.CultureInfo.InvariantCulture;
            int step = 1;
            bool lastwaswhileafterdo = false;

            int lineno = 1;

            foreach (ActionBase act in programsteps)
            {
                if (act != null)
                {
                    act.CalcAllowRight = act.CalcAllowLeft = false;
                    bool indo = structtype[structlevel] == ActionBase.ActionType.Do;        // if in a DO..WHILE, and we are a WHILE, we don't indent.

                    if (act.LevelUp > 0)            // if backing up (do..while the while also backs up)
                    {
                        if (structcount[structlevel] == 0)        // if we had nothing on this level, its wrong
                            errlist += "Step " + step.ToString(ct) + " no statements at indented level after " + structtype[structlevel].ToString() + " statement" + Environment.NewLine;

                        if (act.LevelUp > structlevel)            // ensure its not too big.. this may happen due to copying
                            act.LevelUp = structlevel;

                        structlevel -= act.LevelUp;                 // back up
                        act.CalcAllowRight = act.LevelUp > 1 || !lastwaswhileafterdo;      // normally we can go right, but can't if its directly after do..while and only 1 level of detent
                    }

                    lastwaswhileafterdo = false;        // records if last entry was while after do

                    structcount[structlevel]++;         // 1 more on this level

                    if (structlevel > 0 && structcount[structlevel] > 1)        // second further on can be moved back..
                        act.CalcAllowLeft = true;

                    act.CalcStructLevel = act.CalcDisplayLevel = structlevel;

                    if (act.Type == ActionBase.ActionType.ElseIf)
                    {
                        if (structtype[structlevel] == ActionBase.ActionType.Else)
                            errlist += "Step " + step.ToString(ct) + " ElseIf after Else found" + Environment.NewLine;
                        else if (structtype[structlevel] != ActionBase.ActionType.If && structtype[structlevel] != ActionBase.ActionType.ElseIf)
                            errlist += "Step " + step.ToString(ct) + " ElseIf without IF found" + Environment.NewLine;
                    }
                    else if (act.Type == ActionBase.ActionType.Else)
                    {
                        if (structtype[structlevel] == ActionBase.ActionType.Else)
                            errlist += "Step " + step.ToString(ct) + " Else after Else found" + Environment.NewLine;
                        else if (structtype[structlevel] != ActionBase.ActionType.If && structtype[structlevel] != ActionBase.ActionType.ElseIf)
                            errlist += "Step " + step.ToString(ct) + " Else without IF found" + Environment.NewLine;
                    }

                    if (act.Type == ActionBase.ActionType.ElseIf || act.Type == ActionBase.ActionType.Else)
                    {
                        structtype[structlevel] = act.Type;

                        if (structlevel == 1)
                            act.CalcAllowLeft = false;         // can't move an ELSE back to level 0

                        if (structlevel > 0)      // display else artifically indented.. display only
                            act.CalcDisplayLevel--;

                        structcount[structlevel] = 0;   // restart count so we don't allow a left on next one..
                    }
                    else if (act.Type == ActionBase.ActionType.While && indo)     // do..while
                    {
                        lastwaswhileafterdo = true;     // be careful backing up
                    }
                    else if (act.Type == ActionBase.ActionType.If || (act.Type == ActionBase.ActionType.While && !indo) ||
                                act.Type == ActionBase.ActionType.Do || act.Type == ActionBase.ActionType.Loop || act.Type == ActionBase.ActionType.ForEach)
                    {
                        structlevel++;
                        structcount[structlevel] = 0;
                        structtype[structlevel] = act.Type;
                    }

                    string vmsg = act.VerifyActionCorrect();
                    if (vmsg != null)
                        errlist += "Step " + step.ToString(ct) + " " + vmsg + Environment.NewLine;

                    act.LineNumber = lineno;
                    lineno += act.Whitespace;
                }
                else
                {
                    errlist += "Step " + step.ToString(ct) + " not defined" + Environment.NewLine;
                }

                step++;
                lineno++;
            }

            if (structlevel > 0 && structcount[structlevel] == 0)
            {
                errlist += "At End of program, no statements present after " + structtype[structlevel].ToString() + " statement" + Environment.NewLine;
            }

            return errlist;
        }


        public string ReadFile(string file)               // Read from File the program
        {
            try
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(file))
                {
                    int lineno = 0;
                    return Read(sr, ref lineno);
                }
            }
            catch
            {
                return "File " + file + " missing or IO failure";
            }
        }

        #endregion

        #region Write

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool debug)
        { 
            CalculateLevels();

            StringBuilder sb = new StringBuilder(256);

            sb.AppendLine("PROGRAM " + Name);
            sb.AppendLine("");

            foreach (ActionBase act in programsteps)
            {
                if (act != null)    // don't include ones not set..
                {
                    string output = "";
                    if (debug)
                        output = $"{act.LineNumber} - s{act.CalcStructLevel} - d{act.CalcDisplayLevel} -lu{act.LevelUp} {(act.CalcAllowLeft?"<" : " ")} {(act.CalcAllowRight?">" : " ")} : ";
                 
                    output += new String(' ', act.CalcDisplayLevel * 4) + act.Name + " " + act.UserData;

                    if (act.Comment.Length > 0)
                        output += new string(' ', output.Length < 64 ? (64 - output.Length) : 4) + "// " + act.Comment;

                    sb.AppendLine(output);
                    if (act.Whitespace > 0)
                        sb.AppendLine("");
                }
            }

            sb.AppendLine("");
            sb.AppendLine("END PROGRAM");

            return sb.ToNullSafeString();
        }

        public void Write(System.IO.TextWriter sr)
        {
            sr.Write(ToString());
        }

        public bool WriteFile(string file)                       // write to file the program
        {
            try
            {
                using (System.IO.StreamWriter sr = new System.IO.StreamWriter(file))
                {
                    sr.Write(ToString());
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion


        #region Editor

        public bool EditInEditor(string file = null )          // edit in editor, swap to this
        {
            try
            {
                if ( file == null)                              // if not associated directly with a file, save to a temp one
                {
                    string filename = Name.Length > 0 ? Name : "Default";
                    file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), filename.SafeFileString() + ".atf");

                    if (!WriteFile(file))
                        return false;
                }

                while (true)
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = BaseUtils.AssociateExe.AssocQueryString(BaseUtils.Win32.UnsafeNativeMethods.AssocStr.Executable, ".txt");
                    p.StartInfo.Arguments = file.QuoteString();
                    p.Start();
                    p.WaitForExit();

                    ActionProgram apin = new ActionProgram();
                    string err = apin.ReadFile(file);
                    if (err.Length>0)
                    {
                        DialogResult dr = ExtendedControls.MessageBoxTheme.Show("Editing produced the following errors" + Environment.NewLine + Environment.NewLine + err + Environment.NewLine +
                                            "Click Retry to correct errors, Cancel to abort editing",
                                            "Warning", MessageBoxButtons.RetryCancel);

                        if (dr == DialogResult.Cancel)
                            return false;
                    }
                    else
                    {
                        Name = apin.Name;
                        programsteps = apin.programsteps;
                        HeaderText = apin.HeaderText;
                        return true;
                    }
                }
            }
            catch { }

            ExtendedControls.MessageBoxTheme.Show("Unable to run text editor - check association for .txt files");
            return false;
        }

        #endregion

        #region Classify and auto set

        public ProgramConditionClass Classify()
        {
            ProgramClassKeyUserData = ProgramClassSayUserData = null;

            if (programsteps.Count >= 2 && programsteps[0].Name == "Rem" && programsteps[0].UserData == "Autogenerated V1")
            {
                if (programsteps[1].Name == "Key")
                {
                    ProgramClassKeyUserData = programsteps[1].UserData;

                    if (programsteps.Count == 3 && programsteps[2].Name == "Say")
                    {
                        ProgramClassSayUserData = programsteps[2].UserData;
                        return ProgramConditionClass.KeySay;
                    }
                    else if (programsteps.Count == 2)
                        return ProgramConditionClass.Key;
                }
                else if (programsteps[1].Name == "Say" && programsteps.Count == 2)
                {
                    ProgramClassSayUserData = programsteps[1].UserData;
                    return ProgramConditionClass.Say;
                }
            }

            return ProgramConditionClass.Full;
        }

        public void SetKeySayProgram( string key, string say )
        {
            programsteps = new List<ActionBase>();
            programsteps.Add(ActionBase.CreateAction("Rem", "Autogenerated V1"));
            if (key.HasChars())
                programsteps.Add(ActionBase.CreateAction("Key", key));
            if (say.HasChars())
                programsteps.Add(ActionBase.CreateAction("Say", say));

            ProgramClass = Classify();
        }

        #endregion
    }
}
