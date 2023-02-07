/*
 * Copyright © 2017-2023 EDDiscovery development team
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
using BaseUtils;

namespace ActionLanguage.Manager
{
    public class VersioningManager
    {
        public enum ItemState
        {
            None,
            LocalOnly,
            EDOutOfDate,
            NotPresent,
            UpToDate,
            OutOfDate,
            EDTooOld,
        }

        public class DownloadItem
        {
            public bool HasDownloadedCopy { get { return DownloadedFilename != null;  } }
            public string ShortDownloadedDescription { get { return DownloadedVars != null && DownloadedVars.Exists("ShortDescription") ? DownloadedVars["ShortDescription"] : ""; } }
            public string LongDownloadedDescription { get { return DownloadedVars != null && DownloadedVars.Exists("LongDescription") ? DownloadedVars["LongDescription"] : ""; } }

            public string DownloadedPath {get;set;}           // where its stored on disk to be installed from
            public string DownloadedFilename {get;set;}       // filename
            public int[] DownloadedVersion {get;set;}
            public Variables DownloadedVars {get;set;}
            public string DownloadedServer {get;set;}         // where to get any additional files from
            public string DownloadedServerPath {get;set;}     // and its path

            public string LongLocalDescription { get { return LocalVars != null && LocalVars.Exists("LongDescription") ? LocalVars["LongDescription"] : ""; } }
            public string ShortLocalDescription { get { return LocalVars != null && LocalVars.Exists("ShortDescription") ? LocalVars["ShortDescription"] : ""; } }

            public bool LocalPresent {get;set;}             // if scanned locally
            public string LocalFilename {get;set;}        // always set
            public string LocalPath {get;set;}            // always set
            public int[] LocalVersion {get;set;}          // may be null if file does not have version
            public bool LocalModified {get;set;}          // if local file exists, sha comparison
            public Variables LocalVars {get;set;}         //  null, or set if local has variables
            public bool? LocalEnable {get;set;}           // null, or set if local has variables and a Enable flag
            public bool LocalNotEditable {get;set;}       // set if NotEditable variable is true
            public bool LocalNotDisableable {get;set;}    // set if NotDisablable variable is true
            public ItemState State {get;set;}

            public string ItemName {get;set;}               // filename no extension
            public string ItemType {get;set;}
        };

        public List<DownloadItem> DownloadItems { private set; get; } = new List<DownloadItem>();

        public VersioningManager()
        {
        }

        public void ReadLocalFiles(string appfolder, string subfolder, string filename , string defaultitemtype)       // DONE FIRST
        {
            string installfolder = System.IO.Path.Combine(appfolder, subfolder);
            if (!System.IO.Directory.Exists(installfolder))
                System.IO.Directory.CreateDirectory(installfolder);

            FileInfo[] allFiles = Directory.EnumerateFiles(installfolder, filename, SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.Name).ToArray();

            foreach (FileInfo f in allFiles)
            {
                try
                {
                    DownloadItem it = new DownloadItem();

                    it.LocalPresent = true;

                    it.ItemName = Path.GetFileNameWithoutExtension(f.FullName);
                    it.ItemType = defaultitemtype;

                    it.LocalFilename = f.FullName;
                    it.LocalPath = installfolder;

                    it.State = ItemState.LocalOnly;
                    it.LocalVars = ReadVarsFromFile(f.FullName , out bool? le);
                    it.LocalEnable = le;

                    if (it.LocalVars != null)       // always reads some vars as long as file is there..
                    {
                        if (it.LocalVars.Exists("Version"))     
                        {
                            it.LocalVersion = it.LocalVars["Version"].VersionFromString();
                            it.LocalModified = !WriteOrCheckSHAFile(it, it.LocalVars, appfolder, false);
                        }
                        else
                        {
                            it.LocalVersion = new int[] { 0, 0, 0, 0 };
                            it.LocalModified = true;
                        }

                        if (it.LocalVars.Exists("ItemType"))
                            it.ItemType = it.LocalVars["ItemType"];     // allow file to override name

                        if (it.LocalVars.Equals("NotEditable","True"))
                            it.LocalNotEditable = true;

                        if (it.LocalVars.Equals("NotDisableable","True"))
                            it.LocalNotDisableable = true;

                        foreach (string key in it.LocalVars.NameEnumuerable)  // these first, they are not the controller files
                        {
                            if (key.StartsWith("OtherFile"))
                            {
                                string[] parts = it.LocalVars[key].Split(';');
                                string o = Path.Combine(new string[] { appfolder, parts[1], parts[0] });
                            }
                        }
                    }

                    DownloadItems.Add(it);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Exception read local files");
                }
            }
        }

        public void ReadInstallFiles(string serverlocation , string serverpath, string folder, string appfolder, string filename, int[] edversion , string defaultitemtype)
        {
            FileInfo[] allFiles = Directory.EnumerateFiles(folder, filename, SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.Name).ToArray();

            foreach (FileInfo f in allFiles)
            {
                try
                {
                    bool? enabled;   // don't care about this in remote files
                    Variables cv = ReadVarsFromFile(f.FullName,out enabled);

                    if (cv != null)
                    {
                        int[] version;

                        if (cv.Exists("LongDescription") && cv.Exists("ShortDescription") &&
                            cv.Exists("Version") && cv.Exists("Location") &&
                            cv.Exists("MinEDVersion") &&
                            (version = cv["Version"].VersionFromString()) != null
                            )
                        {
                            string installfolder = System.IO.Path.Combine(appfolder, cv["Location"]);
                            string localfilename = System.IO.Path.Combine(installfolder, Path.GetFileName(f.FullName));

                            DownloadItem it = DownloadItems.Find(x => x.LocalFilename.Equals(localfilename, StringComparison.InvariantCultureIgnoreCase));

                            if (it != null)     // local exists
                            {
                                it.DownloadedPath = folder;
                                it.DownloadedFilename = f.FullName;
                                it.DownloadedVars = cv;
                                it.DownloadedVersion = version;
                                it.DownloadedServer = serverlocation;
                                it.DownloadedServerPath = serverpath;

                                it.State = (it.DownloadedVersion.CompareVersion(it.LocalVersion) > 0) ? ItemState.OutOfDate : ItemState.UpToDate;
                            }
                            else
                            {
                                it = new DownloadItem()
                                {
                                    ItemName = Path.GetFileNameWithoutExtension(f.FullName),
                                    ItemType = cv.Exists("ItemType") ? cv["ItemType"] : defaultitemtype,       // use file description of it, or use default

                                    DownloadedPath = folder,
                                    DownloadedFilename = f.FullName,
                                    DownloadedVersion = version,
                                    DownloadedVars = cv,
                                    DownloadedServer = serverlocation,
                                    DownloadedServerPath = serverpath,

                                    LocalFilename = localfilename,          // set these so it knows where to install..
                                    LocalPath = installfolder,

                                    State = ItemState.NotPresent,
                                };

                                DownloadItems.Add(it);
                            }

                            int[] minedversion = cv["MinEDVersion"].VersionFromString();        // may be null if robert has screwed up the vnumber

                            if ( minedversion != null && minedversion.CompareVersion(edversion) > 0)     // if midedversion > edversion can't install
                                it.State = ItemState.EDOutOfDate;

                            if ( cv.Exists("MaxEDInstallVersion"))      
                            {
                                int[] maxedinstallversion = cv["MaxEDInstallVersion"].VersionFromString();

                                if (maxedinstallversion.CompareVersion(edversion) <= 0) // if maxedinstallversion 
                                    it.State = ItemState.EDTooOld;
                            }

                        }
                    }
                }
                catch { }
            }
        }

        private Variables ReadVarsFromFile(string file, out bool? enable)
        {
            return ActionLanguage.ActionFile.ReadVarsAndEnableFromFile(file, out enable);      // note other files share the actionfile Enabled and INSTALL format.. not the other bits
        }

        static public bool SetEnableFlag(DownloadItem item, bool enable, string appfolder)      // false if could not change the flag
        {
            if (File.Exists(item.LocalFilename))    // if its there..
            { 
                if (ActionLanguage.ActionFile.SetEnableFlag(item.LocalFilename, enable))     // if enable flag was changed..
                {
                    if (!item.LocalModified)      // if was not local modified, lets set the SHA so it does not appear local modified just because of the enable
                        WriteOrCheckSHAFile(item, item.LocalVars, appfolder, true);

                    return true;
                }
            }

            return false;
        }

        public bool InstallFiles(DownloadItem item, string appfolder, string tempmovefolder)
        {
            try
            {
                List<string[]> downloads = (from k in item.DownloadedVars.NameEnumuerable where k.StartsWith("OtherFile") select item.DownloadedVars[k].Split(';')).ToList();

                if (downloads.Count > 0)        // we have downloads..
                {
                    List<string> files = (from a in downloads where a.Length == 2 select a[0]).ToList();        // split them apart and get file names

                    BaseUtils.GitHubClass ghc = new BaseUtils.GitHubClass(item.DownloadedServer);

                    string tempfolder = Path.GetTempPath();

                    if (ghc.Download(tempfolder, item.DownloadedServerPath, files))     // download to temp folder..
                    {
                        foreach (string[] entry in downloads)                           // copy in
                        {
                            if (entry.Length == 2)
                            {
                                string folder = Path.Combine(appfolder, entry[1]);
                                if (!Directory.Exists(folder))      // ensure the folder exists
                                    BaseUtils.FileHelpers.CreateDirectoryNoError(folder);
                                string outfile = Path.Combine(folder, entry[0]);
                                string source = Path.Combine(tempfolder, entry[0]);
                                System.Diagnostics.Debug.WriteLine("Downloaded and installed " + outfile);

                                if (!BaseUtils.FileHelpers.TryCopy(source, outfile, true))  // if failed to copy, try it on next restart
                                {
                                    string s = Path.Combine(tempmovefolder, entry[0]);
                                    if (BaseUtils.FileHelpers.TryCopy(source, s, true))
                                    {
                                        File.WriteAllText(Path.Combine(tempmovefolder, entry[0] + ".txt"), "Copy:" + s + ":To:" + outfile);        // can't delete, tell EDD to delete this next time
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (string key in item.DownloadedVars.NameEnumuerable)  // these first, they are not the controller files
                {
                    if (key.StartsWith("DisableOther"))
                    {
                        DownloadItem other = DownloadItems.Find(x => x.ItemName.Equals(item.DownloadedVars[key]));

                        if (other != null && other.LocalFilename != null)
                            SetEnableFlag(other, false, appfolder); // don't worry if it fails..
                    }
                }

                File.Copy(item.DownloadedFilename, item.LocalFilename, true);

                WriteOrCheckSHAFile(item, item.DownloadedVars, appfolder, true);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception " + ex);
            }

            return false;
        }
    
        static public bool DeleteInstall(DownloadItem item, string appfolder, string tempmovefolder)
        {
            foreach (string key in item.LocalVars.NameEnumuerable)  // these first, they are not the controller files
            {
                if (key.StartsWith("OtherFile"))
                {
                    string[] parts = item.LocalVars[key].Split(';');
                    string o = Path.Combine(new string[] { appfolder, parts[1], parts[0] });
                    if ( !BaseUtils.FileHelpers.DeleteFileNoError(o) )      // if failed to delete, do it on next restart
                    {
                        File.WriteAllText(Path.Combine(tempmovefolder, parts[0] + ".txt"), "Delete:" + o);        // can't delete, tell EDD to delete this next time
                    }
                }
            }

            BaseUtils.FileHelpers.DeleteFileNoError(item.LocalFilename);
            string shafile = Path.Combine(item.LocalPath, item.ItemName + ".sha");
            BaseUtils.FileHelpers.DeleteFileNoError(shafile);
            return true;
        }

        // true for write, for read its true if the same..

        static bool WriteOrCheckSHAFile(DownloadItem it, Variables vars, string appfolder, bool write)
        {
            try
            {
                List<string> filelist = new List<string>() { it.LocalFilename };

                foreach (string key in vars.NameEnumuerable)  // these first, they are not the controller files
                {
                    if (key.StartsWith("OtherFile"))
                    {
                        string[] parts = vars[key].Split(';');
                        string o = Path.Combine(new string[] { appfolder, parts[1], parts[0] });
                        if ( File.Exists(o))
                            filelist.Add(o);
                        else
                            System.Diagnostics.Debug.WriteLine("Missing action pack other file " + o);      // ignore it 
                    }
                }

                string shacurrent = BaseUtils.SHA.CalcSha1(filelist.ToArray());

                string shafile = Path.Combine(it.LocalPath, it.ItemName + ".sha");

                if (write)
                {
                    using (StreamWriter sr = new StreamWriter(shafile))         // read directly from file..
                    {
                        sr.Write(shacurrent);
                    }

                    return true;
                }
                else
                {
                    if (File.Exists(shafile))       // if there is no SHA file, its local, prob under dev, so its false.  SHA is only written by install
                    {
                        using (StreamReader sr = new StreamReader(shafile))         // read directly from file..
                        {
                            string shastored = sr.ReadToEnd();
                            sr.Close();

                            if (shastored.Equals(shacurrent))
                                return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception in VManager " + ex.Message + " " + ex.StackTrace);
            }

            return false;
        }

        class SortIt : IComparer<DownloadItem>
        {
            public int Compare(DownloadItem our, DownloadItem other)
            {
                return our.ItemName.CompareTo(other.ItemName);
            }
        }

        public void Sort()
        {
            DownloadItems.Sort(new SortIt());
        }

    }
}
