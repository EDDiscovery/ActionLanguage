/*
 * Copyright 2017 EDDiscovery development team
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
using AudioExtensions;

namespace ActionLanguage
{
    public class ActionSay : ActionBase
    {
        public override bool AllowDirectEditingOfUserData { get { return true; } }    // and allow editing?

        public static string globalvarspeechvolume = "SpeechVolume";
        public static string globalvarspeechrate = "SpeechRate";
        public static string globalvarspeechvoice = "SpeechVoice";
        public static string globalvarspeecheffects = "SpeechEffects";
        public static string globalvarspeechculture = "SpeechCulture";
        public static string globalvarspeechpriority = "SpeechPriority";
        public static string globalvarspeechdisable = "SpeechDisable";

        static string volumename = "Volume";
        static string voicename = "Voice";
        static string ratename = "Rate";
        static string waitname = "Wait";
        static string priorityname = "Priority";
        static string startname = "StartEvent";
        static string finishname = "FinishEvent";
        static string culturename = "Culture";
        static string literalname = "Literal";
        static string dontspeakname = "DontSpeak";
        static string prefixsound = "PrefixSound";
        static string postfixsound = "PostfixSound";
        static string mixsound = "MixSound";
        static string queuelimit = "QueueLimit";
        static string paravar = "Para";

        static public bool FromString(string s, out string saying, out Variables vars)
        {
            vars = new Variables();

            StringParser p = new StringParser(s);
            saying = p.NextQuotedWord(", ");        // stop at space or comma..

            if (saying != null && (p.IsEOL || (p.IsCharMoveOn(',') && vars.FromString(p, Variables.FromMode.MultiEntryComma))))   // normalise variable names (true)
                return true;

            saying = "";
            return false;
        }

        static public string ToString(string saying, Variables cond)
        {
            if (cond.Count > 0)
                return saying.QuoteString(comma: true) + ", " + cond.ToString();
            else
                return saying.QuoteString(comma: true);
        }

        public override string VerifyActionCorrect()
        {
            string saying;
            Variables vars;
            return FromString(userdata, out saying, out vars) ? null : "Say command line not in correct format";
        }

        static public string Menu(Control parent, string userdata, ActionCoreController cp)
        {
            string saying;
            Variables vars;
            FromString(userdata, out saying, out vars);

            ExtendedAudioForms.SpeechConfigure cfg = new ExtendedAudioForms.SpeechConfigure();
            cfg.Init(false,true,false, false, cp.AudioQueueSpeech, cp.SpeechSynthesizer,
                        null, cp.Icon,
                        saying,
                        vars.Exists(waitname), vars.Exists(literalname),
                        AudioQueue.GetPriority(vars.GetString(priorityname, "Normal")),
                        vars.GetString(startname, ""),
                        vars.GetString(finishname, ""),
                        vars.GetString(voicename, "Default"),
                        vars.GetString(volumename, "Default"),
                        vars.GetString(ratename, "Default"),
                        vars
                        );

            if (cfg.ShowDialog(parent.FindForm()) == DialogResult.OK)
            {
                Variables cond = new Variables(cfg.Effects);// add on any effects variables (and may add in some previous variables, since we did not purge
                cond.SetOrRemove(cfg.Wait, waitname, "1");
                cond.SetOrRemove(cfg.Literal, literalname, "1");
                cond.SetOrRemove(cfg.Priority != AudioQueue.Priority.Normal, priorityname, cfg.Priority.ToString());
                cond.SetOrRemove(cfg.StartEvent.Length > 0, startname, cfg.StartEvent);
                cond.SetOrRemove(cfg.StartEvent.Length > 0, finishname, cfg.FinishEvent);
                cond.SetOrRemove(!cfg.VoiceName.Equals("Default", StringComparison.InvariantCultureIgnoreCase), voicename, cfg.VoiceName);
                cond.SetOrRemove(!cfg.Volume.Equals("Default", StringComparison.InvariantCultureIgnoreCase), volumename, cfg.Volume);
                cond.SetOrRemove(!cfg.Rate.Equals("Default", StringComparison.InvariantCultureIgnoreCase), ratename, cfg.Rate);

                return ToString(cfg.SayText, cond);
            }

            return null;
        }


        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<BaseUtils.TypeHelpers.PropertyNameInfo> eventvars)
        {
            string ud = Menu(parent, userdata, cp);

            if (ud != null)
            {
                userdata = ud;
                return true;
            }
            else
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
            if (FromString(userdata, out string say, out Variables statementvars))
            {
                string ctrl = ap.VarExist("SpeechDebug") ? ap["SpeechDebug"] : "";

                Variables vars = ap.Functions.ExpandVars(statementvars, out string errlist);

                if (ctrl.Contains("SayLine"))
                {
                    ap.ActionController.LogLine("Say Command: " + userdata);
                    ap.ActionController.LogLine("Say Vars: " + vars.ToString(separ: Environment.NewLine));
                    System.Diagnostics.Debug.WriteLine("Say Vars: " + vars.ToString(separ: Environment.NewLine));
                }

                if (errlist == null)
                {
                    while ( vars.Exists(paravar))       // if we have Para=
                    {
                        string plist = vars[paravar];
                        vars.Delete(paravar);
                        Variables pv = new Variables();
                        pv.FromString(plist, Variables.FromMode.MultiEntryComma);
                        vars.Add(pv);
                    }

                    bool wait = vars.GetInt(waitname, 0) != 0;

                    string prior = (vars.Exists(priorityname) && vars[priorityname].Length > 0) ? vars[priorityname] : (ap.VarExist(globalvarspeechpriority) ? ap[globalvarspeechpriority] : "Normal");
                    AudioQueue.Priority priority = AudioQueue.GetPriority(prior);

                    string start = vars.GetString(startname, checklen: true);
                    string finish = vars.GetString(finishname, checklen: true);
                    string voice = (vars.Exists(voicename) && vars[voicename].Length > 0) ? vars[voicename] : (ap.VarExist(globalvarspeechvoice) ? ap[globalvarspeechvoice] : "Default");

                    int vol = vars.GetInt(volumename, -999);
                    if (vol == -999)
                        vol = ap.variables.GetInt(globalvarspeechvolume, 60);

                    int rate = vars.GetInt(ratename, -999);
                    if (rate == -999)
                        rate = ap.variables.GetInt(globalvarspeechrate, 0);

                    int queuelimitms = vars.GetInt(queuelimit, 0);

                    string culture = (vars.Exists(culturename) && vars[culturename].Length > 0) ? vars[culturename] : (ap.VarExist(globalvarspeechculture) ? ap[globalvarspeechculture] : "Default");

                    bool literal = vars.GetInt(literalname, 0) != 0;
                    bool dontspeak = vars.Exists(dontspeakname) ? (vars.GetInt(dontspeakname, 0) != 0) : (ap.VarExist(globalvarspeechdisable) ? ap[globalvarspeechdisable].InvariantParseInt(0) != 0 : false);

                    string prefixsoundpath = vars.GetString(prefixsound, checklen: true);
                    string postfixsoundpath = vars.GetString(postfixsound, checklen: true);
                    string mixsoundpath = vars.GetString(mixsound, checklen: true);

                    Variables globalsettings = ap.VarExist(globalvarspeecheffects) ? new Variables(ap[globalvarspeecheffects], Variables.FromMode.MultiEntryComma) : null;

                    // work out the settings. local vars could have NoEffect (so none) or MergeEffect (Global then vars)
                    // or if no local vars use 
                    // apply the NoEffects, MergeEffects and NoGlobalEffect flags as well as the effect variables

                    SoundEffectSettings ses = SoundEffectSettings.Create(globalsettings, vars);        

                    if (queuelimitms > 0)
                    {
                        int queue = ap.ActionController.AudioQueueSpeech.InQueuems();

                        if (queue >= queuelimitms)
                        {
                            ap["SaySaid"] = "!LIMIT";
                            System.Diagnostics.Debug.WriteLine("Abort say due to queue being at " + queue);
                            return true;
                        }
                    }

                    string expsay;
                    if (ap.Functions.ExpandString(say, out expsay) != Functions.ExpandResult.Failed)
                    {
                        System.Diagnostics.Debug.WriteLine($"{Environment.TickCount} Say wait {wait}, vol {vol}, rate {rate}, queue {queuelimitms}, priority {priority}, culture {culture}, literal {literal}, dontspeak {dontspeak} , prefix {prefixsoundpath}, postfix {postfixsoundpath}, mix {mixsoundpath} starte {start}, finishe {finish} , voice {voice}, text {expsay}");
                        //System.Diagnostics.Debug.WriteLine($"..Say variables: {vars.ToString(separ: Environment.NewLine, prefix:"  ")}");
                        //System.Diagnostics.Debug.WriteLine($"..Say effect variables: {ses?.Values.ToString(separ: Environment.NewLine, prefix:"  ")}");

                        Random rnd = FunctionHandlers.GetRandom();

                        if (!literal)
                        {
                            expsay = expsay.PickOneOfGroups(rnd);       // expand grouping if not literal
                        }

                        ap["SaySaid"] = expsay;

                        if (ctrl.Contains("Global"))
                        {
                            ap.ActionController.SetPeristentGlobal("GlobalSaySaid", expsay);
                        }

                        if (ctrl.Contains("Print") && expsay.HasChars())
                        {
                            ap.ActionController.LogLine("Say: " + expsay);
                        }

                        if (ctrl.Contains("Mute"))
                        {
                            return true;
                        }

                        if (ctrl.Contains("DontSpeak"))
                        {
                            expsay = "";
                        }

                        if (dontspeak)
                            expsay = "";

                        AudioQueue.AudioSample mix = null, prefix = null, postfix = null;

                        if (mixsoundpath != null)
                        {
                            mix = ap.ActionController.AudioQueueSpeech.Generate(mixsoundpath);

                            if (mix == null)
                            {
                                ap.ReportError("Say could not create mix audio, check audio file format is supported and effects settings");
                                return true;
                            }
                        }

                        if (prefixsoundpath != null)
                        {
                            prefix = ap.ActionController.AudioQueueSpeech.Generate(prefixsoundpath);

                            if (prefix == null)
                            {
                                ap.ReportError("Say could not create prefix audio, check audio file format is supported and effects settings");
                                return true;
                            }

                        }

                        if (postfixsoundpath != null)
                        {
                            postfix = ap.ActionController.AudioQueueSpeech.Generate(postfixsoundpath);

                            if (postfix == null)
                            {
                                ap.ReportError("Say could not create postfix audio, check audio file format is supported and effects settings");
                                return true;
                            }
                        }

                        // we entrust it to a Speach Queue (New Dec 20) as the synth takes an inordinate time to generate speech, it then calls back

                        ap.ActionController.SpeechSynthesizer.SpeakQueue(expsay, culture, voice, rate, (memstream) =>
                        {
                            // in a thread, invoke on UI thread to complete action, since these objects are owned by that thread

                            ap.ActionController.Form.Invoke((MethodInvoker)delegate
                            {
                                System.Diagnostics.Debug.Assert(Application.MessageLoop);       // double check!

                                AudioQueue.AudioSample audio = ap.ActionController.AudioQueueSpeech.Generate(memstream, ses, true);

                                if (audio != null)
                                {
                                    if (mix != null)
                                        audio = ap.ActionController.AudioQueueSpeech.Mix(audio, mix);     // audio in MIX format

                                    if (audio != null && prefix != null)
                                        audio = ap.ActionController.AudioQueueSpeech.Append(prefix, audio);        // audio in AUDIO format.

                                    if (audio != null && postfix != null)
                                        audio = ap.ActionController.AudioQueueSpeech.Append(audio, postfix);         // Audio in P format

                                    if (audio != null)      // just double checking nothing fails above
                                    {
                                        if (start != null)
                                        {
                                            audio.sampleStartTag = new AudioEvent { apr = ap, eventname = start, ev = ActionEvent.onSayStarted };
                                            audio.sampleStartEvent += Audio_sampleEvent;
                                        }

                                        if (wait || finish != null)       // if waiting, or finish call
                                        {
                                            audio.sampleOverTag = new AudioEvent() { apr = ap, wait = wait, eventname = finish, ev = ActionEvent.onSayFinished };
                                            audio.sampleOverEvent += Audio_sampleEvent;
                                        }

                                        ap.ActionController.AudioQueueSpeech.Submit(audio, vol, priority);
                                    }
                                }
                            });
                        });

                        return !wait;
                    }
                    else
                        ap.ReportError(expsay);
                }
                else
                    ap.ReportError(errlist);
            }
            else
                ap.ReportError("Say command line not in correct format");

            return true;
        }

        private void Audio_sampleEvent(AudioQueue sender, object tag)
        {
            AudioEvent af = tag as AudioEvent;

            if (af.eventname != null && af.eventname.Length > 0)
                af.apr.ActionController.ActionRun(af.ev, new Variables("EventName", af.eventname), now: false);    // queue at end an event

            if (af.wait)
                af.apr.ResumeAfterPause();
        }
    }
}

