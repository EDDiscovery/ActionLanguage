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

using BaseUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ActionLanguage
{
    // this class allows programs to be run, either sync or async..

    public class ActionRun
    {
        public bool AsyncMode { get; set; } = true;         // set this for non async mode -debug only
        public bool Executing { get => executing; }

        #region Implementation

        public ActionRun(ActionCoreController ed, ActionFileList afl)
        {
            restarttick.Interval = 100;
            restarttick.Tick += Tick_Tick;
            actioncontroller = ed;
            actionfilelist = afl;
        }

        // now = true, run it immediately, else run at end of queue.
        // pass in local vars to run with
        // Optionally pass in handles and dialogs in case its a sub prog

        public void Run(bool now, ActionFile actionfile, ActionProgram r, 
                                Variables inputparas, // local vars to pass into run
                                FunctionPersistentData fh = null,           // Function handler in baseutils uses this for peristent data
                                Dictionary<string, ExtendedControls.IConfigurableDialog> dialogs = null,  // set of dialogs
                                bool closeatend = true)
        {
            if (now)
            {
                if (actionprogruncurrent != null)                    // if running, push the current one back onto the queue to be picked up
                    progqueue.Insert(0, actionprogruncurrent);

                // now we run this.. no need to push to stack

                actionprogruncurrent = new ActionProgramRun(actionfile, r, inputparas, this, actioncontroller);   

                // assemble total variable set for running.
                var runningvarset = new Variables(actionprogruncurrent.inputvariables, actioncontroller.Globals, actionfile.FileVariables);

                // prepare runner
                actionprogruncurrent.PrepareToRun(runningvarset,
                                                fh == null ? new FunctionPersistentData() : fh, 
                                                dialogs == null ? new Dictionary<string, ExtendedControls.IConfigurableDialog>() : dialogs, 
                                                closeatend);        // if no filehandles, make them and close at end
            }
            else
            {
                progqueue.Add(new ActionProgramRun(actionfile, r, inputparas, this, actioncontroller));
            }
        }

        public void Execute()  
        {
            if (!executing)        // someone else, asked for us to run.. we don't, as there is a pause, and we wait until the pause completes
            {
                DoExecute();
            }
        }

        public void ResumeAfterPause()          // used when async..
        {
            if (executing) // must be in an execute state
            {
                DoExecute();
            }
        }

        public void DebugTrace(bool ll, string file = null)
        {
            logtologline = ll;
            if (logger != null)
            {
                logger.Dispose();
                logger = null;
            }

            if (file != null)
                logger = new BaseUtils.LogToFile(file);
        }


        private void DoExecute()    // MAIN thread only..     
        {
            executing = true;

            System.Diagnostics.Stopwatch timetaken = new System.Diagnostics.Stopwatch();
            timetaken.Start();

            while( true )
            {
                if (actionprogruncurrent != null)
                {
                    if (actionprogruncurrent.GetErrorList != null)       // any errors pending, handle
                    {
                        actioncontroller.LogLine("Error: " + actionprogruncurrent.GetErrorList + Environment.NewLine + StackTrace() );
                        TerminateToCloseAtEnd();        // terminate up to a close at end entry, which would have started this stack
                    }
                    else if (actionprogruncurrent.IsProgramFinished)        // if current program ran out, cancel it
                    {
                        // this catches a LOOP without a statement at the end..  or a DO without a WHILE at the end..
                        if (actionprogruncurrent.ExecLevel > 0 && actionprogruncurrent.LevelUp(actionprogruncurrent.ExecLevel, null)) // see if we have any pending LOOP (or a DO without a while) and continue..
                            continue;       // errors or movement causes it to go back.. errors will be picked up above

                        TerminateCurrent();
                    }
                }

                while (actionprogruncurrent == null && progqueue.Count > 0)    // if no program,but something in queue
                {
                    actionprogruncurrent = progqueue[0];
                    progqueue.RemoveAt(0);

                    if (actionprogruncurrent.variables != null)      // if not null, its because its just been restarted after a call.. reset globals
                    {
                        actionprogruncurrent.Add(actioncontroller.Globals); // in case they have been updated...
                        actionprogruncurrent.Add(actionprogruncurrent.ActionFile.FileVariables); // in case they have been updated...
                    }
                    else
                    {
                        actionprogruncurrent.PrepareToRun(
                                new Variables(actionprogruncurrent.inputvariables, actioncontroller.Globals, actionprogruncurrent.ActionFile.FileVariables),
                                new FunctionPersistentData(),
                                new Dictionary<string, ExtendedControls.IConfigurableDialog>(), true); // with new file handles and close at end..
                    }

                    if (actionprogruncurrent.IsProgramFinished)          // reject empty programs..
                    {
                        TerminateCurrent();
                        continue;       // and try again
                    }
                }

                if (actionprogruncurrent == null)        // Still nothing, game over
                    break;

                ActionBase ac = actionprogruncurrent.GetNextStep();      // get the step. move PC on.

            //    System.Diagnostics.Debug.WriteLine((Environment.TickCount % 10000).ToString("00000") + " " + timetaken.ElapsedMilliseconds + " @ " + progcurrent.Location + " Lv " + progcurrent.ExecLevel + " e " + (progcurrent.IsExecuteOn ? "1" : "0") + " up " + ac.LevelUp + " " + progcurrent.PushPos + " " + ac.Name);

                if (ac.LevelUp > 0 && actionprogruncurrent.LevelUp(ac.LevelUp, ac) )        // level up..
                {
                    //System.Diagnostics.Debug.WriteLine((Environment.TickCount % 10000).ToString("00000") + " Abort Lv" + progcurrent.ExecLevel + " e " + (progcurrent.IsExecuteOn ? "1" : "0") + " up " + ac.LevelUp + ": " + progcurrent.StepNumber + " " + ac.Name + " " + ac.DisplayedUserData);
                    continue;
                }

                if ( logtologline || logger != null )
                {
                    string t = (Environment.TickCount % 10000).ToString("00000") + " ";
                    string index = string.Concat(Enumerable.Repeat(". ", actionprogruncurrent.ExecLevel));
                    string s =  actionprogruncurrent.GetLastStep().LineNumber.ToString() + (actionprogruncurrent.IsExecuteOn ? "+" : "-") + ":" + index + ac.Name + " " + ac.DisplayedUserData;
                    System.Diagnostics.Debug.WriteLine(t+s);
                    if ( logtologline )
                        actioncontroller.LogLine(t + s);
                    if ( logger!= null )
                        logger.WriteLine(s);
                }

                if (actionprogruncurrent.DoExecute(ac))       // execute is on.. 
                {
                    if (ac.Type == ActionBase.ActionType.Call)     // Call needs to pass info back up thru to us, need a different call
                    {
                        ActionCall acall = ac as ActionCall;
                        string prog;
                        Variables paravars;
                        if (acall.ExecuteCallAction(actionprogruncurrent, out prog, out paravars)) // if execute ok
                        {
                            //System.Diagnostics.Debug.WriteLine("Call " + prog + " with " + paravars.ToString());

                            Tuple<ActionFile, ActionProgram> ap = actionfilelist.FindProgram(prog, actionprogruncurrent.ActionFile);          // find program using this name, prefer this action file first

                            if (ap != null)
                            {
                                Run(true,ap.Item1, ap.Item2, paravars , actionprogruncurrent.Functions.PersistentData,actionprogruncurrent.Dialogs, false);   // run now with these para vars
                            }
                            else
                                actionprogruncurrent.ReportError("Call cannot find " + prog);
                        }
                    }
                    else if (ac.Type == ActionBase.ActionType.Return)     // Return needs to pass info back up thru to us, need a different call
                    {
                        ActionReturn ar = ac as ActionReturn;
                        ActionFile curfile = actionprogruncurrent.ActionFile;
                        string funcname = actionprogruncurrent.Name;

                        string retstr;
                        if ( ar.ExecuteActionReturn(actionprogruncurrent,out retstr) )
                        {
                            TerminateCurrent();

                            // if a new program is queued, but not prepared, and this program returns to finish, make sure we don't
                            // screw up since the variables are not preparred yet - they will be above in PrepareToRun

                            if (progqueue.Count > 0 && progqueue[0].variables != null)        // pass return value if program is there AND its prepared
                                progqueue[0]["ReturnValue"] = retstr;
                            else
                               curfile.ReportClosingReturn?.Invoke(curfile,funcname,retstr);    // or no more queues, send back return to file in case it wants to report it somehow up

                            continue;       // back to top, next action from returned function.
                        }
                    }
                    else if (!ac.ExecuteAction(actionprogruncurrent))      // if execute says, stop, i'm waiting for something
                    {
                        return;             // exit, with executing set true.  ResumeAfterPause will restart it.
                    }
                }

                if (AsyncMode && timetaken.ElapsedMilliseconds > 150)  // no more than ms per go to stop the main thread being blocked
                {
                    System.Diagnostics.Debug.WriteLine((Environment.TickCount % 10000).ToString("00000") + " *** SUSPEND Actions at " + timetaken.ElapsedMilliseconds + " " + actionprogruncurrent?.Name);
                    restarttick.Start();
                    break;
                }
            }

            executing = false;
        }

        private void Tick_Tick(object sender, EventArgs e) // used when async
        {
            restarttick.Stop();
            System.Diagnostics.Debug.WriteLine((Environment.TickCount % 10000).ToString("00000") + " *** RESUME Action Program");
            Execute();
        }


        public void TerminateAll()          // halt everything
        {
            foreach (ActionProgramRun p in progqueue)       // ensure all have a chance to clean up
                p.Terminated();

            actionprogruncurrent = null;
            progqueue.Clear();
            executing = false;
        }

        public string StackTrace()
        {
            string s = "";

            if (actionprogruncurrent != null)        // if we are running..
            {
                s += "At " + actionprogruncurrent.Location + ": " + actionprogruncurrent.GetLastStep().Name;

                if (!actionprogruncurrent.ClosingHandlesAtEnd)       // if we are not a base functions, trace back up
                {
                    foreach (ActionProgramRun p in progqueue)       // ensure all have a chance to clean up
                    {
                        s += Environment.NewLine;
                        s += "At " + p.Location + ": " + p.GetLastStep().Name;
                        if (p.ClosingHandlesAtEnd)
                            break;
                    }
                }
            }

            return s;
        }

        public void TerminateToCloseAtEnd()     // halt up to close at end function
        {
            if ( actionprogruncurrent != null && !actionprogruncurrent.ClosingHandlesAtEnd )     // if its not a top end function
            {
                List<ActionProgramRun> toremove = new List<ActionProgramRun>();
                foreach (ActionProgramRun p in progqueue)       // ensure all have a chance to clean up
                {
                    toremove.Add(p);
                    if (p.ClosingHandlesAtEnd)
                        break;
                }

                foreach (ActionProgramRun apr in toremove)
                {
                    apr.Terminated();
                    progqueue.Remove(apr);
                }
            }

            actionprogruncurrent = null;
            executing = false;
        }

        public void TerminateCurrent()
        {
            if (actionprogruncurrent != null)
            {
                //System.Diagnostics.Debug.WriteLine((Environment.TickCount % 10000).ToString("00000") + " Terminate program " + progcurrent.Name);
                actionprogruncurrent.Terminated();
                actionprogruncurrent = null;
            }
        }

        public void WaitTillFinished(int timeout, bool doevents)           // Could be IN ANOTHER THREAD BEWARE
        {
            MSTicks tm = new MSTicks(timeout);
            while( tm.NotTimedOut )
            {
                if (actionprogruncurrent == null)
                    break;

                if (doevents)
                    Application.DoEvents();     // let the application run
                else
                    System.Threading.Thread.Sleep(20);
            }
        }

        #endregion

        #region Vars

        private BaseUtils.LogToFile logger = null;
        private bool logtologline = false;

        private List<ActionProgramRun> progqueue = new List<ActionProgramRun>();
        private ActionProgramRun actionprogruncurrent = null;

        private ActionCoreController actioncontroller = null;
        private ActionFileList actionfilelist = null;

        private bool executing = false;         // Records is executing
        private Timer restarttick = new Timer();

        #endregion

    }
}
