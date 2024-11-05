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

        [System.Diagnostics.DebuggerDisplay("{ItemName} {LocalFilePath} {DownloadedURI}")]
        public class DownloadItem
        {
            public string ShortDownloadedDescription { get { return DownloadedVars != null && DownloadedVars.Exists("ShortDescription") ? DownloadedVars["ShortDescription"] : ""; } }
            public string LongDownloadedDescription { get { return DownloadedVars != null && DownloadedVars.Exists("LongDescription") ? DownloadedVars["LongDescription"] : ""; } }

            public string DownloadedURI { get; set; }           // where the ACT came from, in download form
            public string DownloadedTemporaryFilePath { get; set; }       // where the temporary act file is stored
            public int[] DownloadedVersion {get;set;}
            public Variables DownloadedVars {get;set;}

            public string LongLocalDescription { get { return LocalVars != null && LocalVars.Exists("LongDescription") ? LocalVars["LongDescription"] : ""; } }
            public string ShortLocalDescription { get { return LocalVars != null && LocalVars.Exists("ShortDescription") ? LocalVars["ShortDescription"] : ""; } }

            public bool LocalPresent {get;set;}           // if scanned locally
            public string LocalFilePath {get;set;}        // if present, where is it? full path including file name
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

        #region Public functions

        // read local act files from filesfolder
        public void ReadLocalFiles(string approotfolder,        // root of install
                                   string filesfolder,      // where we are scanning
                                   string wildcardfilename, // pattern to match
                                   string defaultitemtype,  // type to give in it.ItemType to the scanned files
                                   bool calcsha = true // set normally, but false if you just want to scan files and not 
            )  
        {
            if (!System.IO.Directory.Exists(filesfolder))
                System.IO.Directory.CreateDirectory(filesfolder);

            FileInfo[] allFiles = Directory.EnumerateFiles(filesfolder, wildcardfilename, SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.Name).ToArray();

            foreach (FileInfo f in allFiles)
            {
                try
                {
                    DownloadItem it = new DownloadItem();

                    it.LocalPresent = true;

                    it.ItemName = Path.GetFileNameWithoutExtension(f.FullName);
                    it.ItemType = defaultitemtype;

                    it.LocalFilePath = f.FullName;

                    it.State = ItemState.LocalOnly;
                    it.LocalVars = ActionLanguage.ActionFile.ReadVarsAndEnableFromFile(f.FullName, out bool enable);
                    it.LocalEnable = enable;

                    System.Diagnostics.Debug.WriteLine($"Local File {it.LocalFilePath} Enabled {it.LocalEnable}");

                    if (it.LocalVars != null)       // If we read local vars.. there may not be any there
                    {
                        if (it.LocalVars.Exists("Version"))     
                        {
                            it.LocalVersion = it.LocalVars["Version"].VersionFromString();
                            it.LocalModified = calcsha ? !WriteOrCheckSHAFile(it, it.LocalVars, approotfolder, false) : false;
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
                    }

                    DownloadItems.Add(it);
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Exception read local files");
                }
            }
        }

        // Read files from a dump got from github
        // does after local files reading
        public void ReadInstallFiles(
                                    string downloaduriroot,  // where the files came from , with / at end
                                    string downloadfolder, // where the downloads have been stored
                                    string approotfolder, // root of install
                                    string wildcardfilename, // what files to consider in the download folder
                                    int[] edversion , 
                                    string defaultitemtype,
                                    string progtype)      // program name, which can screen out downloaded files if it has ProgType variable defined
        {
            // all files found in the download folder

            FileInfo[] allFiles = Directory.EnumerateFiles(downloadfolder, wildcardfilename, SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.Name).ToArray();

            foreach (FileInfo f in allFiles)
            {
                try
                {
                    Variables cv = ActionLanguage.ActionFile.ReadVarsAndEnableFromFile(f.FullName, out bool _);

                    if (cv != null)
                    {
                        int[] version;

                        bool allowedpack = !cv.Exists("ProgType") || cv["ProgType"].Contains(progtype);    // if no progtype, or we have progtype and its got a field with it in

                        if (allowedpack && 
                            cv.Exists("LongDescription") && cv.Exists("ShortDescription") &&
                            cv.Exists("Version") && cv.Exists("Location") &&
                            cv.Exists("MinEDVersion") &&
                            (version = cv["Version"].VersionFromString()) != null
                            )
                        {
                            string filename = Path.GetFileName(f.FullName);
                            string installfolder = System.IO.Path.Combine(approotfolder, cv["Location"]);       // store folder given from root of app by Location
                            string localfilepath = System.IO.Path.Combine(installfolder, filename); // local equivalent file path

                            // see if the local scan above found the equivalent item
                            DownloadItem it = DownloadItems.Find(x => x.LocalFilePath.EqualsIIC(localfilepath));

                            if (it != null)     // local exists
                            {
                                it.DownloadedURI = downloaduriroot + filename;
                                it.DownloadedTemporaryFilePath = f.FullName;
                                it.DownloadedVars = cv;
                                it.DownloadedVersion = version;
                                it.State = (it.DownloadedVersion.CompareVersion(it.LocalVersion) > 0) ? ItemState.OutOfDate : ItemState.UpToDate;
                            }
                            else
                            {
                                it = new DownloadItem()     // not locally held, new item
                                {
                                    ItemName = Path.GetFileNameWithoutExtension(f.FullName),
                                    ItemType = cv.Exists("ItemType") ? cv["ItemType"] : defaultitemtype,       // use file description of it, or use default

                                    DownloadedURI = downloaduriroot + filename,
                                    DownloadedTemporaryFilePath = f.FullName,
                                    DownloadedVersion = version,
                                    DownloadedVars = cv,

                                    LocalFilePath = localfilepath,          // set these so it knows where to install..

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


        // Install this item
        public bool InstallFiles(System.Threading.CancellationToken canceltoken, DownloadItem item, string downloadserver, string approotfolder, string tempmovefolder)
        {
            BaseUtils.GitHubClass ghc = new BaseUtils.GitHubClass(downloadserver);

            var folderfiles = CreateFolderList(item.DownloadedVars, canceltoken, downloadserver);

            if ( folderfiles.Count>0)
            {
                foreach (var x in folderfiles) System.Diagnostics.Debug.WriteLine($"Download file {x.DownloadURI} -> {Path.Combine(approotfolder, x.Path, x.Name)}");

                bool res = ghc.DownloadFiles(canceltoken, approotfolder, folderfiles, true, true);      // download, don't use etags, and sync folder
                if (!res)
                {
                    System.Diagnostics.Trace.WriteLine("Install: Download of folders failed");
                    return false;
                }
            }

            var files = CreateFileList(item.DownloadedVars, downloadserver, item.DownloadedURI);

            if (files.Count > 0)
            {
                foreach (var x in files) System.Diagnostics.Debug.WriteLine($"Download file {x.DownloadURI} -> {Path.Combine(approotfolder, x.Path, x.Name)}");

                bool res = ghc.DownloadFiles(canceltoken, approotfolder, files, true);      // download, don't use etags, don't sync folder
                if (!res)
                {
                    System.Diagnostics.Trace.WriteLine("Install: Download of files failed");
                    return false;
                }
            }

            // look for disable other tags

            foreach (string key in item.DownloadedVars.NameEnumuerable.Where(x=>x.StartsWith("DisableOther")))  
            {
                DownloadItem other = DownloadItems.Find(x => x.ItemName.Equals(item.DownloadedVars[key]));  // see if its present (locally)

                if (other != null && other.LocalFilePath != null)       // if there, and locally there
                    SetEnableFlag(other, false, approotfolder);         // don't worry if it fails..
            }

            File.Copy(item.DownloadedTemporaryFilePath, item.LocalFilePath, true);  // copy act file in

            WriteOrCheckSHAFile(item, item.DownloadedVars, approotfolder, true);

            return true;
        }
    
        static public bool DeleteInstall(DownloadItem item, string downloadserver, string approotfolder, string tempmovefolder)
        {
            // we remove these files..
            var files = CreateFileList(item.LocalVars);

            foreach( var file in files)
            {
                string removefile = Path.Combine(approotfolder, file.Path, file.Name);
                System.Diagnostics.Debug.WriteLine($"Remove file {removefile}");
                if (!BaseUtils.FileHelpers.DeleteFileNoError(removefile))      // if failed to delete, do it on next restart
                {
                    // can't delete, tell EDD to delete this next time
                    File.WriteAllText(Path.Combine(tempmovefolder, file.Name + ".txt"), "Delete:" + removefile);        
                }
            }

            var vars = item.LocalVars.NameEnumuerable.Where(x => x.StartsWith("DownloadFolder"));

            foreach (var v in vars)
            {
                string[] commands = item.LocalVars[v].Split(';');

                if (commands.Length == 2)
                {
                    string rootpath = Path.Combine(approotfolder, commands[1]);         // delete the folder and all sub content
                    System.Diagnostics.Debug.WriteLine($"Remove folder {rootpath}");
                    FileHelpers.DeleteDirectoryNoError(rootpath,true);
                }
            }

            BaseUtils.FileHelpers.DeleteFileNoError(item.LocalFilePath);                // delete the local act
            string shafile = Path.Combine(Path.GetDirectoryName(item.LocalFilePath), item.ItemName + ".sha");
            BaseUtils.FileHelpers.DeleteFileNoError(shafile);
            return true;
        }

        // false if could not change the flag
        static public bool SetEnableFlag(DownloadItem item, bool enable, string appfolder)
        {
            if (File.Exists(item.LocalFilePath))    // if its there..
            {
                if (ActionLanguage.ActionFile.SetEnableFlag(item.LocalFilePath, enable))     // if enable flag was changed..
                {
                    if (!item.LocalModified)      // if was not local modified, lets set the SHA so it does not appear local modified just because of the enable
                        WriteOrCheckSHAFile(item, item.LocalVars, appfolder, true);
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Helpers



        // SHA file set (not folder set).
        // true for write, for read its true if the same.. (5/11/24)
        static private bool WriteOrCheckSHAFile(DownloadItem item, Variables installationvars, string approotfolder, bool write)
        {
            approotfolder = Path.GetFullPath(approotfolder);

            // we only sha over these files (not folder files for historical reasons)
            var files = CreateFileList(installationvars);      
            // add on the act file itself
            files.Add(new RemoteFile(Path.GetFileName(item.LocalFilePath), Path.GetDirectoryName(item.LocalFilePath).Substring(approotfolder.Length+1), ""));

            //foreach (var x in files) System.Diagnostics.Debug.WriteLine($"SHA {Path.Combine(approotfolder, x.Path, x.Name)}");

            try
            {
                string shacurrent = BaseUtils.SHA.CalcSha1(files.Select(x => Path.Combine(approotfolder, x.Path, x.Name)).ToArray());   // calculate SHA over all these files

                string shafile = Path.Combine(Path.GetDirectoryName(item.LocalFilePath), item.ItemName + ".sha");

                if (write)
                {
                    using (StreamWriter sr = new StreamWriter(shafile))         // write to file
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
                System.Diagnostics.Trace.WriteLine("Exception in VManager " + ex.Message + " " + ex.StackTrace);
            }

            return false;
        }

        // from a set of install variables, get a folder list remote file set.  Involves a HTTP
        private static List<RemoteFile> CreateFolderList(Variables installvariables, System.Threading.CancellationToken canceltoken, string downloadserver)
        {
            BaseUtils.GitHubClass ghc = new BaseUtils.GitHubClass(downloadserver);

            var vars = installvariables.NameEnumuerable.Where(x => x.StartsWith("DownloadFolder"));
            List<RemoteFile> folderfiles = new List<RemoteFile>();
            foreach (var v in vars)
            {
                string[] commands = installvariables[v].Split(';');

                if (commands.Length == 2)
                {
                    // read the tree from commands[0] on the server, generate download URIs, store paths with the root path from appdata as commands[1]
                    List<RemoteFile> list = ghc.ReadFolderTree(canceltoken, "master", commands[0], commands[1]);
                    if (list != null)
                        folderfiles.AddRange(list);
                }
            }

            return folderfiles;
        }

        // from a set of install variables, get a file list. No HTTP, downloadserver/URI can be null if all your interested in is the local paths
        private static List<RemoteFile> CreateFileList(Variables installvariables, string downloadserver = null, string downloadedactURI = null)
        {
            var vars = installvariables.NameEnumuerable.Where(x => x.StartsWith("OtherFile") || x.StartsWith("DownloadFile"));
            List<RemoteFile> files = new List<RemoteFile>();
            foreach (var v in vars)
            {
                string[] commands = installvariables[v].Split(';');
                if (v.StartsWith("OtherFile") && commands.Length == 2) // older version
                {
                    // this file commands[0] is downloaded by the directory name of item.DownloadedURI with commands[0] as the filename
                    // to be stored in path commands[1] off of approttfolder
                    files.Add(new RemoteFile(commands[0], commands[1], downloadedactURI != null ?  GitHubClass.GetDownloadURIFromRoot(downloadedactURI, commands[0]) : null));
                }
                else if (v.StartsWith("DownloadFile") && commands.Length == 3) // new version allowing source folder to be specified on server
                {
                    // this file commands[0] is downloaded by the the URI from the root server, with commands[1] as the path and commands[0] as the filename
                    // to be stored in path commands[2] off of approttfolder
                    files.Add(new RemoteFile(commands[0], commands[2], downloadserver != null ? GitHubClass.GetDownloadURI(downloadserver, "master", commands[1] + "/" + commands[0]) : null));
                }
            }

            return files;
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


        #endregion

    }
}
