/*
 * Copyright © 2017 - 2020 EDDiscovery development team
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

using BaseUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ActionLanguage
{
    public class ActionFileList
    {
        private List<ActionFile> actionfiles = new List<ActionFile>();

        public List<string> GetFileNames { get { return (from af in actionfiles select af.Name).ToList(); } }

        public IEnumerable<ActionFile> Enumerable { get { return actionfiles; } }

        // normally pack names are case sensitive, except when we are checking it can be written to a file.. then we would want a case insensitive version
        public ActionFile Get(string name, StringComparison c = StringComparison.InvariantCultureIgnoreCase) { return actionfiles.Find(x => x.Name.Equals(name,c)); }

        // find all which match any name in []
        public ActionFile[] Get(string[] name, StringComparison c = StringComparison.InvariantCultureIgnoreCase)
        {
            return (from x in actionfiles where Array.Find(name, (n) => n.Equals(x.Name, c)) != null select x).ToArray();
        }

        public ActionFile[] Get(string[] name, bool enablestate, StringComparison c = StringComparison.InvariantCultureIgnoreCase)
        {
            return (from x in actionfiles where Array.Find(name, (n) => n.Equals(x.Name, c) && x.Enabled == enablestate) != null select x).ToArray();
        }


        public void CreateSet(string s, string appfolder)
        {
            ActionFile af = new ActionFile(appfolder + "\\\\" + s + ".act", s);
            af.WriteFile();
            actionfiles.Add(af);
        }

        public class MatchingSets
        {
            public ActionFile af;                           // file it came from
            public List<Condition> cl;       // list of matching events..
            public List<Condition> passed;   // list of passed events after condition checked.
        }

        // any with this variable defined?
        public bool IsActionVarDefined(string flagstart)
        {
            foreach (ActionFile af in actionfiles)
            {
                if (af.InUseEventList.IsActionVarDefined(flagstart))
                    return true;
            }
            return false;
        }

        // get actions in system matching eventname
        public List<MatchingSets> GetMatchingConditions(string eventname, string flagstart = null)        // flag start is compared with start of actiondata
        {
            List<MatchingSets> apl = new List<MatchingSets>();

            foreach (ActionFile af in actionfiles)
            {
                if (af.Enabled)         // only enabled files are checked
                {
                    List<Condition> events = af.InUseEventList.GetConditionListByEventName(eventname, flagstart);

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

        public int CheckActions(List<ActionFileList.MatchingSets> ale, Object cls, Variables othervars,
                                            Functions se = null)
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

        public Tuple<ActionFile, ActionProgram> FindProgram(string packname, string progname)
        {
            ActionFile f = actionfiles.Find(x => x.Name.Equals(packname));

            if (f != null)
            {
                ActionProgram ap = f.ProgramList.Get(progname);   // get in local program list first

                if (ap != null)
                    return new Tuple<ActionFile, ActionProgram>(f, ap);
            }

            return null;
        }

        public Tuple<ActionFile, ActionProgram> FindProgram(string req, ActionFile preferred = null)        // find a program 
        {
            ActionProgram ap = null;

            string file = null, prog;
            int colon = req.IndexOf("::");

            if (colon != -1)
            {
                file = req.Substring(0, colon);
                prog = req.Substring(colon + 2);
            }
            else
                prog = req;

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

        public string LoadAllActionFiles(string appfolder)              // loads or reloads the actions, dep on if present in list
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

                bool readenable;
                string err = af.ReadFile(f.FullName, out readenable);       // re-read it in.  Note it does not kill the fileaveriables

                if (err.Length == 0)
                {
                    if (indexof == -1)
                    {
                        System.Diagnostics.Trace.WriteLine("Add pack " + af.Name);
                        actionfiles.Add(af);
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine("Update Pack " + af.Name);
                    }
                }
                else
                {
                    errlist += "File " + f.FullName + " failed to load: " + Environment.NewLine + err;
                    if (indexof != -1)
                    {
                        actionfiles.RemoveAt(indexof);          // remove dead packs
                        System.Diagnostics.Trace.WriteLine("Delete Pack " + af.Name);
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


        #region special helpers

        // give back all conditions which match itemname and have a compatible matchtype across all files.. used for key presses/voice input to compile a list of condition data to check for

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

    }
}
