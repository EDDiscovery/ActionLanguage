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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BaseUtils;

namespace ActionLanguage
{
    [System.Diagnostics.DebuggerDisplay("ActionFile E:{Enabled} {Name} {FilePath} ")]
    public class ActionFile
    {
        #region Properties and startup

        public ConditionLists FileEventList { get; private set; }             // list read from file
        public ConditionLists InUseEventList { get; private set; }            // this is the one active after load - action of reading the file, or changing the event list, synchronises this back to FileEventList
        public ActionProgramList ProgramList { get; private set; }            // programs associated with this pack
        public Variables InstallationVariables { get; private set; }          // used to pass to the installer various options, such as disable other packs
        public Version Version() { return InstallationVariables.GetString("Version").VersionFromString(); }     // may be null...
        public Variables FileVariables { get; private set; }                  // variables defined using the static.. private to this program.  Not persistent. 
        public Dictionary<string, ExtendedControls.IConfigurableDialog> Dialogs { get; private set; } // persistent dialogs owned by this file
        public string FilePath { get; private set; }                          // where it came from
        public string Name { get; private set; }                              // its logical name
        public DateTime WriteTimeUTC { get; private set; }                    // last modified time    
        public bool Enabled { get; private set; }                             // if enabled.
        public Action<ActionFile,string,string> ReportClosingReturn { get; set; }    // hook to get closing return value, the return from the top level function 
        public Encoding FileEncoding {get; private set;}                      // file encoding (auto calc, not saved)

        public ActionFile()
        {
            FileVariables = new Variables();       // filevariables are only cleared on creation
            Dialogs = new Dictionary<string, ExtendedControls.IConfigurableDialog>();
            Clear();
        }

        public ActionFile(string f, string n)
        {
            FileVariables = new Variables();
            Dialogs = new Dictionary<string, ExtendedControls.IConfigurableDialog>();
            Clear(f, n);
        }

        public void Clear(string f = "", string n = "")         // clear all data read from file
        {
            InUseEventList = new ConditionLists();
            FileEventList = new ConditionLists();
            ProgramList = new ActionProgramList();
            Enabled = true;                             // default enabled
            InstallationVariables = new Variables();
            FilePath = f;
            Name = n;
            WriteTimeUTC = DateTime.MinValue;
            FileEncoding = Encoding.UTF8;
            FileVariables["ActionPackName"] = Name;             // static file variables giving name and file path
            FileVariables["ActionPackFilePath"] = FilePath;
            FileVariables["ActionPackFolder"] = FilePath.HasChars() ? Path.GetDirectoryName(FilePath) : "";
        }

        public void ChangeEventList(ConditionLists s)
        {
            FileEventList = s;
            InUseEventList = new ConditionLists(InUseEventList);              // deep copy
        }

        public void ChangeInstallationVariables(Variables v)
        {
            InstallationVariables = v;
        }

        public void SetFileVariable(string n, string v)
        {
            FileVariables[n] = v;
        }

        public void DeleteFileVariable(string n)
        {
            FileVariables.Delete(n);
        }

        public void DeleteFileVariableWildcard(string n)
        {
            FileVariables.DeleteWildcard(n);
        }

        public void CloseDown()     // close any system stuff
        {
            foreach (string s in Dialogs.Keys)
                Dialogs[s].ReturnResult(System.Windows.Forms.DialogResult.Cancel);

            Dialogs.Clear();
        }

        #endregion

        #region Reading and creating a file

        // string, empty if no errors.
        // you can stop as soon as an EVENT or PROGRAM command occurs. This is useful for just reading INSTALL and ENABLE variables
        public string ReadFile(string filename, bool stopatprogramorevent)     
        {
            Clear(filename, Path.GetFileNameWithoutExtension(filename));    // clear this class

            try
            {
                var utc8nobom = new UTF8Encoding(false);        // give it the default UTF8 no BOM encoding, it will detect BOM or UCS-2 automatically

                string currenteventgroup = null;

                using (StreamReader sr = new StreamReader(filename, utc8nobom))         // read directly from file.. presume UTF8 no bom
                {
                    WriteTimeUTC = File.GetLastWriteTimeUtc(filename);

                    string firstline = sr.ReadLine();

                    string precomments = null;
                    bool inautocomments = false;

                    FileEncoding = sr.CurrentEncoding;

                    //System.Diagnostics.Trace.WriteLine("File " + filename + " is in " + fileencoding.BodyName + "   is utc8nobom? " + Equals(utc8nobom, fileencoding));

                    if (firstline == "{")
                    {
                        return "JSON Not supported" + Environment.NewLine;
                    }
                    else if (firstline == "ACTIONFILE V4")
                    {
                        string line;
                        int lineno = 1;     // on actionFILE V4

                        while ((line = sr.ReadLine()) != null)
                        {
                            lineno++;       // on line of read..

                            //System.Diagnostics.Debug.WriteLine($"{filename} {lineno} : {line}");

                            line = line.Trim();
                            if (line.StartsWith("ENABLED", StringComparison.InvariantCultureIgnoreCase))
                            {
                                line = line.Substring(7).Trim().ToLowerInvariant();
                                if (line == "true")
                                    Enabled = true;
                                else if (line == "false")
                                    Enabled = false;
                                else
                                    return Name + " " + lineno + " ENABLED is neither true or false" + Environment.NewLine;
                            }
                            else if (line.StartsWith("PROGRAM", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (stopatprogramorevent)
                                    return "";

                                ActionProgram ap = new ActionProgram(null, null, precomments);
                                string err = ap.Read(sr, ref lineno, line.Substring(7).Trim()); // Read it, prename it..

                                if (err.Length > 0)
                                    return err;

                                //System.Diagnostics.Debug.WriteLine($"Loaded program {ap.Name}\r\n{ap.ToString(true)}");

                                ProgramList.Add(ap);

                                precomments = null;
                                inautocomments = false;
                            }
                            else if (line.StartsWith("INCLUDE", StringComparison.InvariantCultureIgnoreCase))
                            {
                                string incfilename = line.Substring(7).Trim();
                                if (!incfilename.Contains("/") && !incfilename.Contains("\\"))
                                    incfilename = Path.Combine(Path.GetDirectoryName(filename), incfilename);

                                ActionProgram ap = new ActionProgram("", incfilename);   // NAME will be filled in by PROGRAM statement in file

                                string err = ap.ReadFile(incfilename);

                                if (err.Length > 0)
                                    return err;

                                ProgramList.Add(ap);
                            }
                            else if (line.StartsWith("EVENT", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (stopatprogramorevent)
                                    return "";

                                Condition c = new Condition();
                                string err = c.Read(line.Substring(5).Trim(), true);
                                if (err.Length > 0)
                                    return Name + " " + lineno + " " + err + Environment.NewLine;
                                else if (c.Action.Length == 0 || c.EventName.Length == 0)
                                    return Name + " " + lineno + " EVENT Missing event name or action" + Environment.NewLine;

                                c.GroupName = currenteventgroup;
                                c.Tag = precomments;        // use the tag for precomments

                                FileEventList.Add(c);
                                InUseEventList.Add(new Condition(c));        // full clone

                                precomments = null;
                                inautocomments = false;
                            }
                            else if (line.StartsWith("GROUP", StringComparison.InvariantCultureIgnoreCase))
                            {
                                currenteventgroup = line.Substring(5).Trim();
                            }
                            else if (line.StartsWith("INSTALL", StringComparison.InvariantCultureIgnoreCase))
                            {
                                Variables c = new Variables();
                                if (c.FromString(line.Substring(7).Trim(), Variables.FromMode.OnePerLine) && c.Count == 1)
                                {
                                    InstallationVariables.Add(c);
                                }
                                else
                                    return Name + " " + lineno + " Incorrectly formatted INSTALL variable" + Environment.NewLine;
                            }
                            else if (line.StartsWith("//") || line.StartsWith("REM", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (line == automarker)     // look out for auto generated areas, marked by this starter
                                {
                                    inautocomments = !inautocomments;
                                }
                                else if (!inautocomments)   // if not, its a comment on the below item, store in precomments and apply to the next item
                                {
                                    precomments = (precomments == null ? line : precomments + line) + Environment.NewLine;
                                }
                            }
                            else if (line.Length == 0)
                            {
                            }
                            else
                                return Name + " " + lineno + " Invalid command" + Environment.NewLine;
                        }

                        string missing = "";
                        foreach( Condition c in FileEventList.Enumerable )        // lets see if any programs are missing
                        {
                            string progname = c.Action;
                            if ( ProgramList.Get(progname) == null )
                                missing += "Missing program " + progname + Environment.NewLine;
                        }

                        return missing;
                    }
                    else
                    {
                        return Name + " Header file type not recognised" + Environment.NewLine;
                    }
                }
            }
            catch (Exception e)
            {
                return filename + " Not readable" + Environment.NewLine + " " + e.Message;
            }
        }

        #endregion

        #region Writing back file

        public bool WriteFile()
        {
            try
            {
                //var utc8nobom = new UTF8Encoding(false); System.Diagnostics.Trace.WriteLine("File " + FilePath + " written in " + FileEncoding.BodyName + " is utf8 no bom " + Equals(utc8nobom,FileEncoding));

                using (StreamWriter sr = new StreamWriter(FilePath, false, FileEncoding))
                {
                    string rootpath = Path.GetDirectoryName(FilePath) + "\\";

                    sr.WriteLine("ACTIONFILE V4");
                    sr.WriteLine();
                    sr.WriteLine("ENABLED " + Enabled);
                    sr.WriteLine();

                    // write any installation vars first
                    if (InstallationVariables.Count > 0)
                    {
                        sr.WriteLine(InstallationVariables.ToString(prefix: "INSTALL ", separ: Environment.NewLine));
                        sr.WriteLine();
                    }

                    // then the event list

                    if (FileEventList.Count > 0)
                    {
                        string currenteventgroup = null;

                        for (int i = 0; i < FileEventList.Count; i++)
                        {
                            string evgroup = FileEventList[i].GroupName;
                            
                            // swap group..
                            if ( evgroup != currenteventgroup )
                            {
                                if (currenteventgroup != null)
                                {
                                    if (FileEventList[i].Tag != null)           // the tag holds pre comments to the group
                                        sr.WriteLine(Environment.NewLine + (string)FileEventList[i].Tag);
                                    else
                                        sr.WriteLine();
                                }
                                else
                                {
                                    if (FileEventList[i].Tag != null)
                                        sr.WriteLine((string)FileEventList[i].Tag);
                                    else
                                        sr.WriteLine();
                                }

                                currenteventgroup = evgroup;
                                sr.WriteLine("GROUP " + currenteventgroup + Environment.NewLine);
                            }
                            else
                            {
                                if (FileEventList[i].Tag != null)
                                    sr.Write((string)FileEventList[i].Tag);
                            }

                            sr.WriteLine("EVENT " + FileEventList[i].ToString(includeaction: true));
                        }

                        sr.WriteLine();
                    }

                    // write out the programs

                    if (ProgramList.Count > 0)
                    {
                        for (int i = 0; i < ProgramList.Count; i++)
                        {
                            ActionProgram f = ProgramList.Get(i);

                            // auto gen header ..
                            List<string> eventlistcomments = new List<string>() { "// Events: " };
                            int totonline = 0;

                            // look thru event list and see if its attached to this program, if so, add to event list
                            for (int ic = 0; ic < FileEventList.Count; ic++)
                            {
                                Condition c = FileEventList[ic];
                                if (c.Action.Equals(f.Name))
                                {
                                    string e = c.EventName;

                                    if (!c.IsAlwaysTrue())
                                    {
                                        e += "?(" + c.ToString() + ")";
                                    }

                                    if (c.ActionVars.Count > 0)
                                        e += "(" + c.ActionVars.ToString() + ")";

                                    if (eventlistcomments.Last().Length > 120)
                                    {
                                        eventlistcomments.Add("// Events: ");
                                        totonline = 0;
                                    }

                                    eventlistcomments[eventlistcomments.Count - 1] += (totonline > 0 ? ", " : "") + e;
                                    totonline++;
                                }
                            }

                            if (eventlistcomments[0] == "// Events: ")
                                eventlistcomments[0] += "None";

                            // if we have a manual header, then write that out, looking out for an Events line which then gets replaced by the  event list above
                            if (f.HeaderText.HasChars())
                            {
                                string[] lines = f.HeaderText.Split(Environment.NewLine);
                                bool doneevents = false;
                                foreach (var l in lines)
                                {
                                    if (l.StartsWith("// Events:"))
                                    {
                                        if (!doneevents)
                                        {
                                            sr.WriteLine(string.Join(Environment.NewLine, eventlistcomments));
                                            doneevents = true;
                                        }
                                    }
                                    else
                                        sr.WriteLine(l);
                                }
                            }
                            else
                            {
                                // no manual header, write out the automarker plus events
                                sr.WriteLine(automarker);
                                sr.WriteLine("// " + f.Name);
                                sr.WriteLine(string.Join(Environment.NewLine,eventlistcomments));
                                sr.WriteLine(automarker);
                            }

                            // now finally write the program, as long as not stored in a sub file
                            if (f.StoredInSubFile != null)
                            {
                                string full = f.StoredInSubFile;        // try and simplify the path here..
                                if (full.StartsWith(rootpath))
                                    full = full.Substring(rootpath.Length);

                                sr.WriteLine("INCLUDE " + full);
                                f.WriteFile(f.StoredInSubFile);
                            }
                            else
                                f.Write(sr);

                            sr.WriteLine();
                        }
                    }


                    sr.Close();
                }

                return true;
            }
            catch
            { }

            return false;
        }

        #endregion

        #region Misc

        // change the enable flag. Read in
        // write out updated file if enable has changed
        // true if it changed (5/11/24)
        static public bool SetEnableFlag(string file, bool enable)              
        {
            ActionFile f = new ActionFile();

            string res = f.ReadFile(file, true);    // read only up to EVENT/INSTALL

            if (res.Length == 0 && f.Enabled != enable)  // if read okay, and its not got the same enable..      
            {
                ActionFile g = new ActionFile();        
                res = g.ReadFile(file, false); // read the lot
                if (res.Length == 0)
                {
                    g.Enabled = enable;         // set and write back whole file
                    g.WriteFile();
                    return true;
                }
            }

            return false;
        }

        // read all the install variables and the enable flag and report on them (5/11/24)
        static public bool ReadVarsAndEnableFromFile(string file, out Variables vars, out bool enable)
        {
            ActionFile f = new ActionFile();

            string res = f.ReadFile(file, true);        // read only up to EVENT/INSTALL

            if (res.Length == 0)        // read it in..
            {
                enable = f.Enabled;
                vars = f.InstallationVariables;
                //System.Diagnostics.Debug.WriteLine($"Enable vars read {file} {enable}");
                return true;
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("Error reading pack " + file + ":" + res);
                enable = false;
                vars = null;
                return false;
            }
        }

        #endregion

        const string automarker = "//*************************************************************";

    }
}
