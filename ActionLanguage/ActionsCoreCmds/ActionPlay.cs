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
using System.Windows.Forms;
using AudioExtensions;
using BaseUtils;

namespace ActionLanguage
{
    public class ActionPlay : ActionBase
    {
        public override bool AllowDirectEditingOfUserData { get { return true; } }    // and allow editing?

        public static string globalvarplayvolume = "WaveVolume";
        public static string globalvarplayeffects = "WaveEffects";

        static string volumename = "Volume";
        static string waitname = "Wait";
        static string priorityname = "Priority";
        static string startname = "StartEvent";
        static string finishname = "FinishEvent";

        public bool FromString(string s, out string path, out Variables vars)
        {
            vars = new Variables();

            if (s.IndexOfAny(",\"'".ToCharArray()) == -1)
            {
                path = s;
                return true;
            }
            else
            {
                StringParser p = new StringParser(s);
                path = p.NextQuotedWord(", ");        // stop at space or comma..

                if (path != null && (p.IsEOL || (p.IsCharMoveOn(',') && vars.FromString(p, Variables.FromMode.MultiEntryComma))))   // normalise variable names (true)
                    return true;

                path = "";
                return false;
            }
        }

        public string ToString(string path, Variables cond)
        {
            if (cond.Count > 0)
                return path.QuoteString(comma: true) + ", " + cond.ToString();
            else
                return path.QuoteString(comma: true);
        }

        public override string VerifyActionCorrect()
        {
            string path;
            Variables vars;
            return FromString(userdata, out path, out vars) ? null : "Play command line not in correct format";
        }

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp , List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            string path;
            Variables vars;
            FromString(userdata, out path, out vars);

            ExtendedAudioForms.WaveConfigureDialog cfg = new ExtendedAudioForms.WaveConfigureDialog();
            cfg.Init(false, cp.AudioQueueWave, null, cp.Icon,
                        path,
                        vars.Exists(waitname),
                        AudioQueue.GetPriority(vars.GetString(priorityname, "Normal")),
                        vars.GetString(startname, ""),
                        vars.GetString(finishname, ""),
                        vars.GetString(volumename, "Default"),
                        vars);

            if (cfg.ShowDialog(parent) == DialogResult.OK)
            {
                Variables cond = new Variables(cfg.Effects);// add on any effects variables (and may add in some previous variables, since we did not purge)
                cond.SetOrRemove(cfg.Wait, waitname, "1");
                cond.SetOrRemove(cfg.Priority != AudioQueue.Priority.Normal, priorityname, cfg.Priority.ToString());
                cond.SetOrRemove(cfg.StartEvent.Length > 0, startname, cfg.StartEvent);
                cond.SetOrRemove(cfg.StartEvent.Length > 0, finishname, cfg.FinishEvent);
                cond.SetOrRemove(!cfg.Volume.Equals("Default", StringComparison.InvariantCultureIgnoreCase), volumename, cfg.Volume);
                userdata = ToString(cfg.Path, cond);
                return true;
            }

            return false;
        }

        class AudioEvent
        {
            public ActionProgramRun apr;
            public bool wait;
            public ActionEvent ev;
            public string eventname;
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            string pathunexpanded;
            Variables statementvars;
            if (FromString(userdata, out pathunexpanded, out statementvars))
            {
                string errlist = null;
                Variables vars = ap.Functions.ExpandVars(statementvars,out errlist);

                if (errlist == null)
                {
                    string path;
                    if (ap.Functions.ExpandString(pathunexpanded, out path) != Functions.ExpandResult.Failed)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            bool wait = vars.GetInt(waitname, 0) != 0;
                            AudioQueue.Priority priority = AudioQueue.GetPriority(vars.GetString(priorityname, "Normal"));
                            string start = vars.GetString(startname);
                            string finish = vars.GetString(finishname);

                            int vol = vars.GetInt(volumename, -999);
                            if (vol == -999)
                                vol = ap.variables.GetInt(globalvarplayvolume, 60);

                            Variables globalsettings = ap.VarExist(globalvarplayeffects) ? new Variables(ap[globalvarplayeffects], Variables.FromMode.MultiEntryComma) : null;
                            SoundEffectSettings ses = SoundEffectSettings.Set(globalsettings, vars);        // work out the settings
                            
                            AudioQueue.AudioSample audio = ap.ActionController.AudioQueueWave.Generate(path, ses);

                            if (audio != null)
                            {
                                if (start != null && start.Length > 0)
                                {
                                    audio.sampleStartTag = new AudioEvent { apr = ap, eventname = start, ev = ActionEvent.onPlayStarted };
                                    audio.sampleStartEvent += Audio_sampleEvent;

                                }
                                if (wait || (finish != null && finish.Length > 0))       // if waiting, or finish call
                                {
                                    audio.sampleOverTag = new AudioEvent() { apr = ap, wait = wait, eventname = finish, ev = ActionEvent.onPlayFinished };
                                    audio.sampleOverEvent += Audio_sampleEvent;
                                }

                                ap.ActionController.AudioQueueWave.Submit(audio, vol, priority);
                                return !wait;       //False if wait, meaning terminate and wait for it to complete, true otherwise, continue
                            }
                            else
                                ap.ReportError("Play could not create audio, check audio file format is supported and effects settings");
                        }
                        else
                            ap.ReportError("Play could not find file " + path);
                    }
                    else
                        ap.ReportError(path);
                }
                else
                    ap.ReportError(errlist);
            }
            else
                ap.ReportError("Play command line not in correct format");

            return true;
        }

        private void Audio_sampleEvent(AudioQueue sender, object tag)
        {
            AudioEvent af = tag as AudioEvent;

            if (af.eventname != null && af.eventname.Length>0)
                af.apr.ActionController.ActionRun(af.ev, new Variables("EventName", af.eventname), now: false);    // queue at end an event

            if (af.wait)
                af.apr.ResumeAfterPause();
        }
    }
}
