/*
 * Copyright © 2017-2020 EDDiscovery development team
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
using System.IO;
using System.Text;
using BaseUtils;

// A file holds a set of conditions and programs associated with them

namespace ActionLanguage
{
    public class ActionFile
    {
        public ActionFile()
        {
            FileVariables = new Variables();       // filevariables are only cleared on creation
            Dialogs = new Dictionary<string, ExtendedControls.ConfigurableForm>();
            Clear();
        }

        public ActionFile(string f, string n)
        {
            FileVariables = new Variables();
            Dialogs = new Dictionary<string, ExtendedControls.ConfigurableForm>();
            Clear(f, n);
        }

        public void Clear(string f = "", string n = "")         // clear all data read from file
        {
            InUseEventList = new ConditionLists();
            FileEventList = new ConditionLists();
            ProgramList = new ActionProgramList();
            Enabled = true;
            InstallationVariables = new Variables();
            FilePath = f;
            Name = n;
            WriteTimeUTC = DateTime.MinValue;
            FileEncoding = Encoding.UTF8;
            FileVariables["ActionPackName"] = Name;         
            FileVariables["ActionPackFilePath"] = FilePath;
        }

        public ConditionLists FileEventList { get; private set; }             // list read from file
        public ConditionLists InUseEventList { get; private set; }            // this is the one active after load - action of reading the file, or changing the event list, synchronises this back to FileEventList
        public ActionProgramList ProgramList { get; private set; }            // programs associated with this pack
        public Variables InstallationVariables { get; private set; }          // used to pass to the installer various options, such as disable other packs
        public Variables FileVariables { get; private set; }                  // variables defined using the static.. private to this program.  Not persistent. 
        public Dictionary<string, ExtendedControls.ConfigurableForm> Dialogs; // persistent dialogs owned by this file
        public string FilePath { get; private set; }                          // where it came from
        public string Name { get; private set; }                              // its logical name
        public DateTime WriteTimeUTC { get; private set; }                    // last modified time    
        public bool Enabled { get; private set; }                             // if enabled.

        public Encoding FileEncoding {get; private set;}                      // file encoding (auto calc, not saved)

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

        public string ReadFile(string filename, out bool readenable)     // string, empty if no errors
        {
            readenable = false;

            Clear(filename, Path.GetFileNameWithoutExtension(filename));

            try
            {
                var utc8nobom = new UTF8Encoding(false);        // give it the default UTF8 no BOM encoding, it will detect BOM or UCS-2 automatically

                string currenteventgroup = null;

                using (StreamReader sr = new StreamReader(filename, utc8nobom))         // read directly from file.. presume UTF8 no bom
                {
                    WriteTimeUTC = File.GetLastWriteTimeUtc(filename);

                    string firstline = sr.ReadLine();

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

                                readenable = true;
                            }
                            else if (line.StartsWith("PROGRAM", StringComparison.InvariantCultureIgnoreCase))
                            {
                                ActionProgram ap = new ActionProgram();     
                                string err = ap.Read(sr, ref lineno, line.Substring(7).Trim()); // Read it, prename it..

                                if (err.Length > 0)
                                    return Name + " " + err;

                                ProgramList.Add(ap);
                            }
                            else if (line.StartsWith("INCLUDE", StringComparison.InvariantCultureIgnoreCase))
                            {
                                string incfilename = line.Substring(7).Trim();
                                if (!incfilename.Contains("/") && !incfilename.Contains("\\"))
                                    incfilename = Path.Combine(Path.GetDirectoryName(filename), incfilename);

                                ActionProgram ap = new ActionProgram("", incfilename);   // NAME will be filled in by PROGRAM statement in file

                                string err = ap.ReadFile(incfilename);

                                if (err.Length > 0)
                                    return Name + " " + err;

                                ProgramList.Add(ap);
                            }
                            else if (line.StartsWith("EVENT", StringComparison.InvariantCultureIgnoreCase))
                            {
                                Condition c = new Condition();
                                string err = c.Read(line.Substring(5).Trim(), true);
                                if (err.Length > 0)
                                    return Name + " " + lineno + " " + err + Environment.NewLine;
                                else if (c.Action.Length == 0 || c.EventName.Length == 0)
                                    return Name + " " + lineno + " EVENT Missing event name or action" + Environment.NewLine;

                                c.GroupName = currenteventgroup;
                                FileEventList.Add(c);
                                InUseEventList.Add(new Condition(c));        // full clone
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
                            else if (line.StartsWith("//") || line.StartsWith("REM", StringComparison.InvariantCultureIgnoreCase) || line.Length == 0)
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

        public bool WriteFile()
        {
            try
            {
                var utc8nobom = new UTF8Encoding(false); System.Diagnostics.Trace.WriteLine("File " + FilePath + " written in " + FileEncoding.BodyName + " is utf8 no bom " + Equals(utc8nobom,FileEncoding));

                using (StreamWriter sr = new StreamWriter(FilePath, false, FileEncoding))
                {
                    string rootpath = Path.GetDirectoryName(FilePath) + "\\";

                    sr.WriteLine("ACTIONFILE V4");
                    sr.WriteLine();
                    sr.WriteLine("ENABLED " + Enabled);
                    sr.WriteLine();

                    if (InstallationVariables.Count > 0)
                    {
                        sr.WriteLine(InstallationVariables.ToString(prefix: "INSTALL ", separ: Environment.NewLine));
                        sr.WriteLine();
                    }

                    if (FileEventList.Count > 0)
                    {
                        string currenteventgroup = null;

                        for (int i = 0; i < FileEventList.Count; i++)
                        {
                            string evgroup = FileEventList[i].GroupName;
                            if ( evgroup != currenteventgroup )
                            {
                                if ( currenteventgroup != null )
                                    sr.WriteLine("");
                                currenteventgroup = evgroup;
                                sr.WriteLine("GROUP " + currenteventgroup);
                            }

                            sr.WriteLine("EVENT " + FileEventList[i].ToString(includeaction: true));
                        }

                        sr.WriteLine();
                    }

                    if (ProgramList.Count > 0)
                    {
                        for (int i = 0; i < ProgramList.Count; i++)
                        {
                            ActionProgram f = ProgramList.Get(i);

                            sr.WriteLine("//*************************************************************");
                            sr.WriteLine("// " + f.Name);
                            string evl = "";

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

                                    e += ", ";

                                    if (evl.Length>0 && evl.Length + e.Length > 120 )   // if we have text, and adding this on makes it long
                                    {
                                        sr.WriteLine("// Events: " + evl);  // write current out
                                        evl = "";
                                    }

                                    evl += e;
                                }
                            }

                            if (evl.Length > 0)
                            {
                                evl = evl.Substring(0, evl.Length - 2); // remove ,
                                sr.WriteLine("// Events: " + evl);
                            }

                            sr.WriteLine("//*************************************************************");


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

        static public bool SetEnableFlag(string file, bool enable)              // change the enable flag. Read in,write out.
        {                                                                       // true if managed to change it..
            try
            {
                ActionFile f = new ActionFile();

                bool readenable;
                if (f.ReadFile(file, out readenable).Length == 0)        // read it in..
                {
                    f.Enabled = enable;
                    f.WriteFile();                                // write it out.
                  //  System.Diagnostics.Debug.WriteLine("Set Enable " + file + " " + enable );
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        static public Variables ReadVarsAndEnableFromFile(string file, out bool? enable)
        {
            ActionFile f = new ActionFile();
            enable = null;

            bool readenable;
            string res = f.ReadFile(file, out readenable);

            if (res.Length == 0)        // read it in..
            {
                if (readenable)
                    enable = f.Enabled;
                //System.Diagnostics.Debug.WriteLine("Enable vars read " + file + " " + enable);
                return f.InstallationVariables;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Error reading pack " + file + ":" + res);
                return null;
            }
        }

    }
}
