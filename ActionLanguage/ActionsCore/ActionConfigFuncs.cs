using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioExtensions;
using BaseUtils;

namespace ActionLanguage
{
    public abstract class ActionConfigFuncs
    {
        public interface IPromptFolderSettings
        {
            Environment.SpecialFolder RootFolder { get; set; }
            string Description { get; set; }
            bool ShowNewFolderButton { get; set; }
            string SelectedPath { get; set; }
        }

        public interface IPromptFileSettings
        {
            string InitialDirectory { get; set; }
            string FileName { get; set; }
            string[] FileNames { get; }
            bool CheckFileExists { get; set; }
            bool CheckPathExists { get; set; }
            bool ValidateNames { get; set; }
            string DefaultExt { get; set; }
            string Filter { get; set; }
            int FilterIndex { get; set; }
        }

        public interface IPromptOpenFileSettings : IPromptFileSettings
        {
            bool Multiselect { get; set; }
            System.IO.Stream OpenFile();
        }

        public interface IPromptSaveFileSettings : IPromptFileSettings
        {
            bool CreatePrompt { get; set; }
            bool OverwritePrompt { get; set; }
            System.IO.Stream OpenFile();
        }

        public interface ISpeechSettings
        {
            string SayText { get; }
            bool Wait { get; }
            bool Literal { get; }
            AudioExtensions.AudioQueue.Priority Priority { get; }
            string StartEvent { get; }
            string FinishEvent { get; }
            string VoiceName { get; }
            string Volume { get; }
            string Rate { get; }
            Variables Effects { get; }
        }

        public interface IWaveSettings
        {
            string Path { get; }
            bool Wait { get; }
            AudioQueue.Priority Priority { get; }
            string StartEvent { get; }
            string FinishEvent { get; }
            string Volume { get; }
            Variables Effects { get; }

        }

        public enum DialogResult
        {
            None = 0,
            OK = 1,
            Cancel = 2,
            Abort = 3,
            Retry = 4,
            Ignore = 5,
            Yes = 6,
            No = 7
        }

        public interface IConfigurableForm
        {
            string Add(string instr);
            string Get(string control);
            bool Set(string control, string value);
            void InitCentred(Icon icon, string caption, string lname = null, Object callertag = null);
            void Init(Point pos, Icon icon, string caption, string lname = null, Object callertag = null);
            void Show();
            void Close();
            event Action<string, string, object> Trigger;
            Point Location { get; set; }
            void ReturnResult(DialogResult result);
            DialogResult DialogResult { get; }
        }

        public interface ITimer : IDisposable
        {
            event EventHandler Tick;
            int Interval { get; set; }
            void Start();
            void Stop();
        }

        public interface IProgramEditForm : IDisposable
        {
            event Action<string> EditProgram;
            bool Execute(string t, Icon ic, ActionCoreController cp, string appfolder, List<BaseUtils.TypeHelpers.PropertyNameInfo> vbs, string pfilesetname, ActionProgram prog = null, string[] defprogs = null, string suggestedname = null, bool edittext = false);
            ActionProgram GetProgram();
        }

        public interface IKeySettings
        {
            string KeyList { get; }
            string ProcessSelected { get; }
            int DefaultDelay { get; }

        }

        public abstract string PromptSingleLine(string lab1, string defaultValue1, string caption, Icon ic,
                            bool multiline = false,
                            string tooltip = null,
                            bool cursoratend = false,
                            int widthboxes = 200,
                            int heightboxes = -1);
        public abstract List<string> PromptMultiLine(string caption, Icon ic, string[] lab, string[] def,
                            bool multiline = false,
                            string[] tooltips = null,
                            bool cursoratend = false,
                            int widthboxes = 200,
                            int heightboxes = -1);

        public abstract bool PromptFolder(Action<IPromptFolderSettings> config, out string selectePath);
        public abstract bool PromptOpenFile(Action<IPromptOpenFileSettings> config, out string filename, Action<IPromptOpenFileSettings> post = null);
        public abstract bool PromptSaveFile(Action<IPromptSaveFileSettings> config, out string filename, Action<IPromptSaveFileSettings> post = null);
        public abstract string MessageBox(string text, string caption = null, string buttons = "OK", string icon = "None", Icon windowicon = null);
        public abstract void InfoBox(string title, Icon ic, string info, int[] array = null, float pointsize = -1, Action<Object> acknowledgeaction = null, Object acknowledgedata = null);
        public abstract IConfigurableForm CreateConfigurableForm();
        public abstract bool SetVariables(string t, Icon ic, Variables vbs, Dictionary<string, string> altops = null, bool showatleastoneentry = false, bool showrunatrefreshcheckbox = false, bool setrunatrefreshcheckboxstate = false, bool allowadd = false, bool allownoexpand = false, bool allowmultipleentries = true, Action<Variables, Dictionary<string, string>, bool> resultact = null);
        public abstract ITimer CreateTimer(int interval, EventHandler callback);
        public abstract bool EditProgram(string t, Icon ic, ActionCoreController cp, string appfolder, List<TypeHelpers.PropertyNameInfo> vbs, string pfilesetname, ActionProgram prog = null, string[] defprogs = null, string suggestedname = null, bool edittext = false, Action<string> callback = null, Action<ActionProgram> resultcb = null);
        public abstract bool FilterConditions(List<TypeHelpers.PropertyNameInfo> eventvars, string t, Icon ic, ref ConditionLists jf);
        public abstract bool ConfigureSpeech(AudioQueue qu, SpeechSynthesizer syn, string title, string caption, Icon ic, String text, bool waitcomplete, bool literal, AudioQueue.Priority prio, string startname, string endname, string voicename, string volume, string rate, Variables ef, Action<ISpeechSettings> resultcb);
        public abstract bool ConfigureWave(AudioQueue qu, bool defaultmode, string title, string caption, Icon ic, string defpath, bool waitcomplete, AudioQueue.Priority prio, string startname, string endname, string volume, Variables ef, Action<IWaveSettings> resultcb);
        public abstract string SendKeyToProcess(string keys, int keydelay, int shiftdelay, int updelay, string pname, IAdditionalKeyParser additionalkeyparser = null);
        public abstract bool ConfigureKeys(Icon i, bool showprocess, string separ = " ", string keystring = "", string process = "", int defdelay = 50, bool allowkeysedit = false, List<string> additionalkeys = null, IAdditionalKeyParser parser = null, Action<IKeySettings> resultcb = null);
        public abstract void Yield();
    }
}
