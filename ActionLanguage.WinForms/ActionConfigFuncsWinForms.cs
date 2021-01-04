using AudioExtensions;
using BaseUtils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ActionLanguage
{
    public class ActionConfigFuncsWinForms : ActionConfigFuncs
    {
        public class PromptFolderSettings : IPromptFolderSettings, IDisposable
        {
            public System.Windows.Forms.FolderBrowserDialog Dialog = new System.Windows.Forms.FolderBrowserDialog();
            public Environment.SpecialFolder RootFolder { get { return Dialog.RootFolder; } set { Dialog.RootFolder = value; } }
            public string Description { get { return Dialog.Description; } set { Dialog.Description = value; } }
            public bool ShowNewFolderButton { get { return Dialog.ShowNewFolderButton; } set { Dialog.ShowNewFolderButton = value; } }
            public string SelectedPath { get { return Dialog.SelectedPath; } set { Dialog.SelectedPath = value; } }

            public System.Windows.Forms.DialogResult Show(System.Windows.Forms.Form parent)
            {
                return Dialog.ShowDialog(parent);
            }

            public void Dispose()
            {
                Dialog?.Dispose();
                Dialog = null;
            }
        }

        public class PromptOpenFileSettings : IPromptOpenFileSettings, IDisposable
        {
            public System.Windows.Forms.OpenFileDialog Dialog { get; } = new System.Windows.Forms.OpenFileDialog();
            public string InitialDirectory { get { return Dialog.InitialDirectory; } set { Dialog.InitialDirectory = value; } }
            public string FileName { get { return Dialog.FileName; } set { Dialog.FileName = value; } }
            public string[] FileNames { get { return Dialog.FileNames; } }
            public bool CheckFileExists { get { return Dialog.CheckFileExists; } set { Dialog.CheckFileExists = value; } }
            public bool CheckPathExists { get { return Dialog.CheckPathExists; } set { Dialog.CheckPathExists = value; } }
            public bool ValidateNames { get { return Dialog.ValidateNames; } set { Dialog.ValidateNames = value; } }
            public bool Multiselect { get { return Dialog.Multiselect; } set { Dialog.Multiselect = value; } }
            public string DefaultExt { get { return Dialog.DefaultExt; } set { Dialog.DefaultExt = value; } }
            public string Filter { get { return Dialog.Filter; } set { Dialog.Filter = value; } }
            public int FilterIndex { get { return Dialog.FilterIndex; } set { Dialog.FilterIndex = value; } }
            public System.IO.Stream OpenFile()
            {
                return Dialog.OpenFile();
            }

            public System.Windows.Forms.DialogResult Show(System.Windows.Forms.Form parent)
            {
                return Dialog.ShowDialog(parent);
            }

            public void Dispose()
            {
                Dialog.Dispose();
            }
        }

        public class PromptSaveFileSettings : IPromptSaveFileSettings, IDisposable
        {
            public System.Windows.Forms.SaveFileDialog Dialog { get; } = new System.Windows.Forms.SaveFileDialog();
            public string InitialDirectory { get { return Dialog.InitialDirectory; } set { Dialog.InitialDirectory = value; } }
            public string FileName { get { return Dialog.FileName; } set { Dialog.FileName = value; } }
            public string[] FileNames { get { return Dialog.FileNames; } }
            public bool CheckFileExists { get { return Dialog.CheckFileExists; } set { Dialog.CheckFileExists = value; } }
            public bool CheckPathExists { get { return Dialog.CheckPathExists; } set { Dialog.CheckPathExists = value; } }
            public bool ValidateNames { get { return Dialog.ValidateNames; } set { Dialog.ValidateNames = value; } }
            public bool CreatePrompt { get { return Dialog.CreatePrompt; } set { Dialog.CreatePrompt = value; } }
            public bool OverwritePrompt { get { return Dialog.OverwritePrompt; } set { Dialog.OverwritePrompt = value; } }
            public string DefaultExt { get { return Dialog.DefaultExt; } set { Dialog.DefaultExt = value; } }
            public string Filter { get { return Dialog.Filter; } set { Dialog.Filter = value; } }
            public int FilterIndex { get { return Dialog.FilterIndex; } set { Dialog.FilterIndex = value; } }
            public System.IO.Stream OpenFile()
            {
                return Dialog.OpenFile();
            }

            public System.Windows.Forms.DialogResult Show(System.Windows.Forms.Form parent)
            {
                return Dialog.ShowDialog(parent);
            }

            public void Dispose()
            {
                Dialog.Dispose();
            }
        }

        public class ConfigurableForm : IConfigurableForm, IDisposable
        {
            public ExtendedControls.ConfigurableForm Form { get; set; } = new ExtendedControls.ConfigurableForm();
            public System.Windows.Forms.Form ParentForm { get; set; }
            public Point Location { get { return Form.Location; } set { Form.Location = value; } }

            public event Action<string, string, object> Trigger { add { Form.Trigger += value; } remove { Form.Trigger -= value; } }
            public DialogResult DialogResult { get { return (DialogResult)Form.DialogResult; } }

            public string Add(string instr)
            {
                return Form.Add(instr);
            }

            public string Get(string control)
            {
                return Form.Get(control);
            }

            public bool Set(string control, string value)
            {
                return Form.Set(control, value);
            }

            public void Dispose()
            {
                Form.Dispose();
            }

            public void InitCentred(Icon icon, string caption, string lname = null, Object callertag = null, AutoScaleMode asm = AutoScaleMode.Font, bool closeicon = false)
            {
                Form.InitCentred(ParentForm, new Size(1, 1), new Size(50000, 50000), icon, caption, lname, callertag, (System.Windows.Forms.AutoScaleMode)asm, closeicon);
            }

            public void Init(Point pos, Icon icon, string caption, string lname = null, Object callertag = null, AutoScaleMode asm = AutoScaleMode.Font, bool closeicon = false)
            {
                Form.Init(new Size(1, 1), new Size(50000, 50000), pos, icon, caption, lname, callertag, (System.Windows.Forms.AutoScaleMode)asm, closeicon);
            }

            public void Show()
            {
                Form.Show(ParentForm);
            }

            public void Close()
            {
                Form.Close();
            }

            public void ReturnResult(DialogResult result)
            {
                Form.ReturnResult((System.Windows.Forms.DialogResult)result);
            }
        }

        public class Timer : ITimer
        {
            public System.Windows.Forms.Timer TimerInstance { get; } = new System.Windows.Forms.Timer();
            public int Interval { get { return TimerInstance.Interval; } set { TimerInstance.Interval = value; } }

            public event EventHandler Tick { add { TimerInstance.Tick += value; } remove { TimerInstance.Tick -= value; } }

            public void Dispose()
            {
                TimerInstance.Dispose();
            }

            public void Start()
            {
                TimerInstance.Start();
            }

            public void Stop()
            {
                TimerInstance.Stop();
            }
        }

        public class ProgramEditForm : IProgramEditForm
        {
            public ActionProgramEditForm Form { get; } = new ActionProgramEditForm();
            public System.Windows.Forms.Form ParentForm { get; set; }

            public event Action<string> EditProgram { add { Form.EditProgram += new ActionProgramEditForm.EditProgramFunc(value); } remove { Form.EditProgram -= new ActionProgramEditForm.EditProgramFunc(value); } }

            public void Dispose()
            {
                Form.Dispose();
            }

            public bool Execute(string t, Icon ic, ActionCoreController cp, string appfolder, List<TypeHelpers.PropertyNameInfo> vbs, string pfilesetname, ActionProgram prog = null, string[] defprogs = null, string suggestedname = null, bool edittext = false)
            {
                Form.Init(t, ic, cp, appfolder, vbs, pfilesetname, prog, defprogs, suggestedname, edittext);
                return Form.ShowDialog(ParentForm) == System.Windows.Forms.DialogResult.OK;
            }

            public ActionProgram GetProgram()
            {
                return Form.GetProgram();
            }
        }

        public class SpeechSettings : ISpeechSettings, IDisposable
        {
            public ExtendedAudioForms.SpeechConfigure Form { get; } = new ExtendedAudioForms.SpeechConfigure();
            public System.Windows.Forms.Form ParentForm { get; set; }

            public string SayText => throw new NotImplementedException();

            public bool Wait => throw new NotImplementedException();

            public bool Literal => throw new NotImplementedException();

            public AudioQueue.Priority Priority => throw new NotImplementedException();

            public string StartEvent => throw new NotImplementedException();

            public string FinishEvent => throw new NotImplementedException();

            public string VoiceName => throw new NotImplementedException();

            public string Volume => throw new NotImplementedException();

            public string Rate => throw new NotImplementedException();

            public Variables Effects => throw new NotImplementedException();

            public System.Windows.Forms.DialogResult ShowDialog(AudioQueue qu, AudioExtensions.SpeechSynthesizer syn, string title, string caption, Icon ic, String text, bool waitcomplete, bool literal, AudioExtensions.AudioQueue.Priority prio, string startname, string endname, string voicename, string volume, string rate, Variables ef)
            {
                Form.Init(qu, syn, title, caption, ic, text, waitcomplete, literal, prio, startname, endname, voicename, volume, rate, ef);
                return Form.ShowDialog(ParentForm);
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }

        public class WaveSettings : IWaveSettings, IDisposable
        {
            public ExtendedAudioForms.WaveConfigureDialog Form { get; } = new ExtendedAudioForms.WaveConfigureDialog();
            public System.Windows.Forms.Form ParentForm { get; set; }
            public string Path => throw new NotImplementedException();

            public bool Wait => throw new NotImplementedException();

            public AudioQueue.Priority Priority => throw new NotImplementedException();

            public string StartEvent => throw new NotImplementedException();

            public string FinishEvent => throw new NotImplementedException();

            public string Volume => throw new NotImplementedException();

            public Variables Effects => throw new NotImplementedException();

            public System.Windows.Forms.DialogResult Execute(AudioQueue qu, bool defaultmode, string title, string caption, Icon ic, string defpath, bool waitcomplete, AudioQueue.Priority prio, string startname, string endname, string volume, Variables ef)
            {
                Form.Init(qu, defaultmode, title, caption, ic, defpath, waitcomplete, prio, startname, endname, volume, ef);
                return Form.ShowDialog(ParentForm);
            }

            public void Dispose()
            {
                Form.Dispose();
            }
        }

        public class KeySettings : IKeySettings, IDisposable
        {
            public ExtendedControls.KeyForm Form { get; } = new ExtendedControls.KeyForm();
            public System.Windows.Forms.Form ParentForm { get; set; }

            public string KeyList => throw new NotImplementedException();

            public string ProcessSelected => throw new NotImplementedException();

            public int DefaultDelay => throw new NotImplementedException();

            public void Dispose()
            {
                Form.Dispose();
            }

            public System.Windows.Forms.DialogResult Execute(Icon i, bool showprocess, string separ = " ", string keystring = "", string process = "", int defdelay = 50, bool allowkeysedit = false, List<string> additionalkeys = null, IAdditionalKeyParser parser = null)
            {
                Form.Init(i, showprocess, separ, keystring, process, defdelay, allowkeysedit, additionalkeys, parser);
                return Form.ShowDialog(ParentForm);
            }
        }


        protected System.Windows.Forms.Form ParentForm { get; set; }
        public ActionConfigFuncsWinForms(System.Windows.Forms.Form parent)
        {
            ParentForm = parent;
        }

        public override string PromptSingleLine(string lab1, string defaultValue1, string caption, Icon ic, bool multiline = false, string tooltip = null, bool cursoratend = false, int widthboxes = 200, int heightboxes = -1)
        {
            return ExtendedControls.PromptSingleLine.ShowDialog(ParentForm, lab1, defaultValue1, caption, ic, multiline, tooltip, cursoratend, widthboxes, heightboxes);
        }

        public override List<string> PromptMultiLine(string caption, Icon ic, string[] lab, string[] def, bool multiline = false, string[] tooltips = null, bool cursoratend = false, int widthboxes = 200, int heightboxes = -1)
        {
            return ExtendedControls.PromptMultiLine.ShowDialog(ParentForm, caption, ic, lab, def, multiline, tooltips, cursoratend, widthboxes, heightboxes);
        }

        public override bool PromptFolder(Action<IPromptFolderSettings> config, out string selectedPath)
        {
            using (var dialog = new PromptFolderSettings())
            {
                config(dialog);

                if (dialog.Show(this.ParentForm) == System.Windows.Forms.DialogResult.OK)
                {
                    selectedPath = dialog.SelectedPath;
                    return true;
                }
                else
                {
                    selectedPath = null;
                    return false;
                }
            }
        }

        public override bool PromptOpenFile(Action<IPromptOpenFileSettings> config, out string filename, Action<IPromptOpenFileSettings> post = null)
        {
            using (var dialog = new PromptOpenFileSettings())
            {
                config(dialog);

                if (dialog.Show(ParentForm) == System.Windows.Forms.DialogResult.OK)
                {
                    post(dialog);
                    filename = dialog.FileName;
                    return true;
                }
                else
                {
                    filename = null;
                    return false;
                }
            }
        }

        public override bool PromptSaveFile(Action<IPromptSaveFileSettings> config, out string filename, Action<IPromptSaveFileSettings> post = null)
        {
            using (var dialog = new PromptSaveFileSettings())
            {
                config(dialog);

                if (dialog.Show(ParentForm) == System.Windows.Forms.DialogResult.OK)
                {
                    post(dialog);
                    filename = dialog.FileName;
                    return true;
                }
                else
                {
                    filename = null;
                    return false;
                }
            }
        }

        public override string MessageBox(string text, string caption = null, string buttons = null, string icon = null, Icon windowicon = null)
        {
            var but = System.Windows.Forms.MessageBoxButtons.OK;
            var ic = System.Windows.Forms.MessageBoxIcon.None;

            if (buttons != null && !Enum.TryParse(buttons, true, out but))
            {
                throw new InvalidOperationException("MessageBox button type not recognised");
            }
            if (icon != null && !Enum.TryParse(icon, true, out ic))
            {
                throw new InvalidOperationException("MessageBox icon type not recognised");
            }

            var res = ExtendedControls.MessageBoxTheme.Show(ParentForm, text, caption, but, ic);

            return res.ToString();
        }

        public override void InfoBox(string title, Icon ic, string info, int[] array = null, float pointsize = -1, Action<object> acknowledgeaction = null, object acknowledgedata = null)
        {
            var infoform = new ExtendedControls.InfoForm();
            infoform.Info(title, ic, info, array, pointsize, acknowledgeaction, acknowledgedata);
            infoform.Show(ParentForm);
        }

        public override IConfigurableForm CreateConfigurableForm()
        {
            return new ConfigurableForm
            {
                ParentForm = ParentForm
            };
        }

        public override bool SetVariables(string t, Icon ic, Variables vbs, Dictionary<string, string> altops = null, bool showatleastoneentry = false, bool showrunatrefreshcheckbox = false, bool setrunatrefreshcheckboxstate = false, bool allowadd = false, bool allownoexpand = false, bool allowmultipleentries = true, Action<Variables, Dictionary<string, string>> resultact = null)
        {
            var form = new ExtendedConditionsForms.VariablesForm();
            form.Init(t, ic, vbs, altops, showatleastoneentry, showrunatrefreshcheckbox, allowadd, allownoexpand, allowmultipleentries);
            var result = form.ShowDialog(ParentForm);

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                resultact?.Invoke(form.result, form.result_altops);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override ITimer CreateTimer(int interval, EventHandler callback)
        {
            var timer = new Timer();
            timer.Interval = interval;
            timer.Tick += callback;
            return timer;
        }

        public override bool EditProgram(string t, Icon ic, ActionCoreController cp, string appfolder, List<TypeHelpers.PropertyNameInfo> vbs, string pfilesetname, ActionProgram prog = null, string[] defprogs = null, string suggestedname = null, bool edittext = false, Action<string> callback = null, Action<ActionProgram> resultcb = null)
        {
            using (var form = new ProgramEditForm { ParentForm = ParentForm })
            {
                if (callback != null)
                {
                    form.EditProgram += callback;
                }

                if (form.Execute(t, ic, cp, appfolder, vbs, pfilesetname, prog, defprogs, suggestedname, edittext))
                {
                    resultcb?.Invoke(form.GetProgram());
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override bool FilterConditions(List<TypeHelpers.PropertyNameInfo> eventvars, string t, Icon ic, ref ConditionLists jf)
        {
            using (var form = new ExtendedConditionsForms.ConditionFilterForm())
            {
                form.VariableNames = eventvars;
                form.InitCondition(t, ic, jf);
                if (form.ShowDialog(ParentForm) == System.Windows.Forms.DialogResult.OK)
                {
                    jf = form.Result;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public override bool ConfigureSpeech(AudioQueue qu, SpeechSynthesizer syn, string title, string caption, Icon ic, String text, bool waitcomplete, bool literal, AudioQueue.Priority prio, string startname, string endname, string voicename, string volume, string rate, Variables ef, Action<ISpeechSettings> resultcb)
        {
            using (var form = new SpeechSettings { ParentForm = ParentForm })
            {
                if (form.ShowDialog(qu, syn, title, caption, ic, text, waitcomplete, literal, prio, startname, endname, voicename, volume, rate, ef) == System.Windows.Forms.DialogResult.OK)
                {
                    resultcb?.Invoke(form);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override string SendKeyToProcess(string keys, int keydelay, int shiftdelay, int updelay, string pname, IAdditionalKeyParser additionalkeyparser = null)
        {
            throw new NotImplementedException();
        }

        public override bool ConfigureKeys(Icon i, bool showprocess, string separ = " ", string keystring = "", string process = "", int defdelay = 50, bool allowkeysedit = false, List<string> additionalkeys = null, IAdditionalKeyParser parser = null, Action<IKeySettings> resultcb = null)
        {
            using (var form = new KeySettings { ParentForm = ParentForm })
            {
                if (form.Execute(i, showprocess, separ, keystring, process, defdelay, allowkeysedit, additionalkeys, parser) == System.Windows.Forms.DialogResult.OK)
                {
                    resultcb?.Invoke(form);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override bool ConfigureWave(AudioQueue qu, bool defaultmode, string title, string caption, Icon ic, string defpath, bool waitcomplete, AudioQueue.Priority prio, string startname, string endname, string volume, Variables ef, Action<IWaveSettings> resultcb)
        {
            using (var form = new WaveSettings { ParentForm = ParentForm })
            {
                if (form.Execute(qu, defaultmode, title, caption, ic, defpath, waitcomplete, prio, startname, endname, volume, ef) == System.Windows.Forms.DialogResult.OK)
                {
                    resultcb?.Invoke(form);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override void Yield()
        {
            Application.DoEvents();
        }

        public override void Invoke(Action act)
        {
            ParentForm.Invoke(act);
        }
    }
}
