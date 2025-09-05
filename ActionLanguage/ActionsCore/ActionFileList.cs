/*
 * Copyright 2017 - 2025 EDDiscovery development team
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
using System.IO;
using System.Linq;

namespace ActionLanguage
{
    public class ActionFileList
    {

        #region Get Info about files
        public List<string> GetFileNames { get { return (from af in actionfiles select af.Name).ToList(); } }

        public IEnumerable<ActionFile> Enumerable { get { return actionfiles; } }

        // get name, name can be null
        // normally pack names are case sensitive, except when we are checking it can be written to a file.. then we would want a case insensitive version
        public ActionFile Get(string name, StringComparison c = StringComparison.InvariantCultureIgnoreCase) { return name != null ? actionfiles.Find(x => x.Name.Equals(name,c)) : null; }

        // find all which match any name in [] with optional enable state. names can be null
        public ActionFile[] Get(string[] names, bool? enablestate, StringComparison c = StringComparison.InvariantCultureIgnoreCase)
        {
            return names != null ? actionfiles.Where(x => (!enablestate.HasValue || enablestate.Value == x.Enabled) && Array.Find(names, (n) => n.Equals(x.Name, c)) != null).ToArray() : null;
        }

        // find a program, either just the name, or filename::program
        // give a prefered pack to use if no filename given
        public Tuple<ActionFile, ActionProgram> FindProgram(string fileprogname, ActionFile preferred = null)
        {
            ActionProgram ap = null;

            string file = null, prog = null;
            int colon = fileprogname.IndexOf("::");
            if (colon != -1)
            {
                file = fileprogname.Substring(0, colon);
                prog = fileprogname.Substring(colon + 2);
            }
            else
                prog = fileprogname;

            if (file != null)                             // if file given, only search that
            {
                ActionFile f = actionfiles.Find(x => x.Name.Equals(file));

                if (f != null)      // found file..
                {
                    ap = f.ProgramList.Get(prog);

                    return (ap != null) ? new Tuple<ActionFile, ActionProgram>(f, ap) : null;
                }
            }
            else
            {
                if (preferred != null)          // if no file stated, and we have a preferred
                {
                    ap = preferred.ProgramList.Get(prog);   // get in local program list first

                    if (ap != null)
                        return new Tuple<ActionFile, ActionProgram>(preferred, ap);
                }

                foreach (ActionFile f in actionfiles)
                {
                    ap = f.ProgramList.Get(prog);

                    if (ap != null)         // gotcha
                        return new Tuple<ActionFile, ActionProgram>(f, ap);
                }
            }

            return null;
        }

        #endregion

        #region Add Files

        public void AddNewFile(string s, string appfolder)
        {
            ActionFile af = new ActionFile(appfolder + "\\\\" + s + ".act", s);
            af.WriteFile();
            actionfiles.Add(af);
        }

        #endregion

        #region Event Lists checking Conditions to run

        [System.Diagnostics.DebuggerDisplay("{af.Name} {cl.Count} {passed.Count}")]
        public class MatchingSets
        {
            public ActionFile af;            // file it came from
            public List<Condition> cl;       // list of matching events..
            public List<Condition> passed;   // list of passed events after condition checked.
        }

        // given an event name, on enabled files, find conditions in the In Use Event List which has this eventname set
        // you can screen out events which do not contain the action variable defined by actionvarscontainthisname

        public List<MatchingSets> GetMatchingConditions(string eventname, string actionvarscontainthisname = null)        
        {
            List<MatchingSets> apl = new List<MatchingSets>();

            foreach (ActionFile af in actionfiles)
            {
                if (af.Enabled)         // only enabled files are checked
                {
                    List<Condition> events = af.InUseEventList.GetConditionListByEventName(eventname, actionvarscontainthisname);

                    if (events != null)     // and if we have matching event..
                    {
                        apl.Add(new MatchingSets() { af = af, cl = events });
                    }
                }
            }

            return apl;
        }

        // triage found actions and see which ones are runnable
        // cls = object from which to get any needed values from, can be null
        // if se is set passed enabled string expansion of arguments in condition of event..

        public int CheckActions(List<ActionFileList.MatchingSets> ale, Object cls, Variables othervars, Functions se = null)
        {
            Variables valuesneeded = new Variables();

            if (cls != null)
            {
                foreach (MatchingSets ae in ale)       // for all files
                {
                    foreach (Condition fe in ae.cl)        // find all values needed
                        fe.IndicateValuesNeeded(ref valuesneeded);
                }

                valuesneeded.GetValuesIndicated(cls, null, 5, new string[] { "_", "[" });     // get the values needed for the conditions
            }

            valuesneeded.Add(othervars);

            int progs = 0;

            Functions cf = new Functions(valuesneeded, null);

            foreach (MatchingSets ae in ale)       // for all files
            {
                string errlist = null;
                
                ae.passed = new List<Condition>();

                //System.Diagnostics.Debug.WriteLine("Check `" + ae.af.name + ae.af.actionfieldfilter.ToString() + "`");
                //ActionData.DumpVars(valuesneeded, " Test var:");

                ConditionLists.CheckConditions(ae.cl, valuesneeded, out errlist, out ConditionLists.ErrorClass errclass, ae.passed, cf);   // indicate which ones passed
                progs += ae.passed.Count;
            }

            return progs;
        }
        
        // run programs in matching set
        // now = true run immediately, else defer to current programs
        public void RunActions(bool now, List<ActionFileList.MatchingSets> ale, ActionRun run, Variables inputparas)
        {
            foreach (ActionFileList.MatchingSets ae in ale)          // for every file which passed..
            {
                foreach (Condition fe in ae.passed)          // and every condition..
                {
                    Tuple<ActionFile, ActionProgram> ap = FindProgram(fe.Action, ae.af);          // find program using this name, prefer this action file first

                    if (ap != null)     // program got,
                    {
                        inputparas.Add(fe.ActionVars);
                        run.Run(now, ap.Item1, ap.Item2, inputparas);
                    }
                }
            }
        }

        // Look at action flag in the conditions list of the events, and see if this flag is set in any
        public bool IsActionVarDefined(string flagstart)
        {
            foreach (ActionFile af in actionfiles)
            {
                if (af.InUseEventList.IsActionVarDefined(flagstart))
                    return true;
            }
            return false;
        }

        #endregion

        #region Load

        // loads or reloads the actions, dep on if already loaded
        public string LoadAllActionFiles(string appfolder)              
        {
            if (!Directory.Exists(appfolder))
                Directory.CreateDirectory(appfolder);

            FileInfo[] allFiles = Directory.EnumerateFiles(appfolder, "*.act", SearchOption.AllDirectories).Select(f => new FileInfo(f)).OrderBy(p => p.LastWriteTime).ToArray();

            string errlist = "";

            foreach (FileInfo f in allFiles)
            {
                int indexof = actionfiles.FindIndex(x => x.FilePath.Equals(f.FullName));

                ActionFile af;

                if (indexof == -1)                  // if we don't have it, new it.. else overwrite it
                    af = new ActionFile();
                else
                    af = actionfiles[indexof];      // overwriting keeps any dynamic data action files have

                string err = af.ReadFile(f.FullName, false);       // re-read it in.  Note it does not kill the fileaveriables

                if (err.Length == 0)
                {
                    if (indexof == -1)
                    {
                        System.Diagnostics.Trace.WriteLine($"ActionFileList Loaded pack {af.Name} {af.Enabled}");
                        actionfiles.Add(af);
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"ActionFileList Update pack {af.Name} {af.Enabled}");
                    }
                }
                else
                {
                    errlist += "File " + f.FullName + " failed to load: " + Environment.NewLine + err;
                    if (indexof != -1)
                    {
                        actionfiles.RemoveAt(indexof);          // remove dead packs
                        System.Diagnostics.Trace.WriteLine("ActionFileList Delete Pack " + af.Name);
                    }
                }
            }

            return errlist;
        }

        public bool CheckForActionFilesChange()
        {
            foreach (var af in actionfiles)
            {
                if (!File.Exists(af.FilePath) || File.GetLastWriteTimeUtc(af.FilePath) > af.WriteTimeUTC)
                    return true;
            }

            return false;
        }

        public void CloseDown()         // close any system stuff
        {
            foreach (var af in actionfiles)
                af.CloseDown();
        }

        #endregion

        #region Special helpers

        // give back all conditions which match itemname and have a compatible matchtype across all files..
        // used for key presses/voice input to compile a list of condition data to check for

        public List<Tuple<string, ConditionEntry>> ReturnSpecificConditions(string eventname, string itemname, List<ConditionEntry.MatchType> matchtypes)      // given itemname, give me a list of values it is matched against
        {
            var ret = new List<Tuple<string, ConditionEntry>>();

            foreach (ActionFile f in actionfiles)
            {
                if (f.Enabled)
                {
                    var fr = f.InUseEventList.ReturnSpecificConditions(eventname, itemname, matchtypes);
                    if (fr != null)
                        ret.AddRange(fr);
                }
            }

            return ret;
        }

        #endregion

        #region Vars

        private List<ActionFile> actionfiles = new List<ActionFile>();

        #endregion
    }
}
