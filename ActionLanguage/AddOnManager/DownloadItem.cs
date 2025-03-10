/*
 * Copyright 2017-2025 EDDiscovery development team
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
using System.Windows.Forms;
using BaseUtils;

namespace ActionLanguage.Manager
{
    [System.Diagnostics.DebuggerDisplay("{ItemName} {InstallFilePath} {DownloadedURI}")]
    public class DownloadItem
    {
        public string ItemName { get; set; }               // filename no extension
        public string ItemType { get; set; }
        public string InstallFilePath { get; set; }        // where it will, or is, installed (may not be there if remote)

        public enum ItemState
        {
            None,
            LocalOnly,
            EDOutOfDate,
            NotPresent,
            UpToDate,
            OutOfDate,
            EDTooOld,
            ToBeInstalled,
            ToBeRemoved,
        }
        public ItemState State { get; set; }


        // Download info
        public string ShortDownloadedDescription { get { return DownloadedVars != null && DownloadedVars.Exists("ShortDescription") ? DownloadedVars["ShortDescription"] : ""; } }
        public string LongDownloadedDescription { get { return DownloadedVars != null && DownloadedVars.Exists("LongDescription") ? DownloadedVars["LongDescription"] : ""; } }

        public string DownloadedURI { get; set; }           // where the ACT came from, in download form
        public string DownloadedTemporaryFilePath { get; set; }       // where the temporary act file is stored
        public int[] DownloadedVersion { get; set; }
        public Variables DownloadedVars { get; set; }

        // Local
        public string LongLocalDescription { get { return LocalVars != null && LocalVars.Exists("LongDescription") ? LocalVars["LongDescription"] : ""; } }
        public string ShortLocalDescription { get { return LocalVars != null && LocalVars.Exists("ShortDescription") ? LocalVars["ShortDescription"] : ""; } }

        public bool LocalPresent { get; set; }           // if scanned locally
        public int[] LocalVersion { get; set; }          // may be null if file does not have version
        public bool LocalModified { get; set; }          // if local file exists, sha comparison
        public Variables LocalVars { get; set; }         // null, or set if local has variables
        public bool? LocalEnable { get; set; }           // null, or set if local has variables and a Enable flag
        public bool LocalNotEditable { get; set; }       // set if NotEditable variable is true
        public bool LocalNotDisableable { get; set; }    // set if NotDisablable variable is true


       public bool ReadLocalItem(string pathname, string approotfolder, string defaultitemtype)
        {
            if (ActionFile.ReadVarsAndEnableFromFile(pathname, out Variables v, out bool enable))
            {
                ItemName = Path.GetFileNameWithoutExtension(pathname);
                ItemType = defaultitemtype;

                LocalPresent = true;
                LocalVars = v;
                LocalEnable = enable;
                InstallFilePath = pathname;
                State = ItemState.LocalOnly;

                System.Diagnostics.Debug.WriteLine($"Local File {InstallFilePath} Enabled {LocalEnable}");

                if (LocalVars != null)       // If we read local vars.. there may not be any there
                {
                    if (LocalVars.Exists("Version"))
                    {
                        LocalVersion = LocalVars["Version"].VersionFromString();
                        LocalModified = WriteOrCheckSHAFile(approotfolder, false) == false;
                    }
                    else
                    {
                        LocalVersion = new int[] { 0, 0, 0, 0 };
                        LocalModified = true;
                    }

                    if (LocalVars.Exists("ItemType"))
                        ItemType = LocalVars["ItemType"];     // allow file to override name

                    LocalNotEditable = LocalVars.Equals("NotEditable", "True");
                    LocalNotDisableable = LocalVars.Equals("NotDisableable", "True");
                }
                else
                    LocalNotEditable = LocalNotDisableable = false;

                return true;
            }
            else
                return false;
        }

        // from downloaded info, copy to local

        public bool Install(Form fm, System.Threading.CancellationToken canceltoken, string downloadserver, string approotfolder)
        {
            if (Install(canceltoken, downloadserver, approotfolder))
            {
                ExtendedControls.MessageBoxTheme.Show(fm, ItemName + " Add-on updated", "Action Pack", MessageBoxButtons.OK, MessageBoxIcon.Information, fontscaling: 1.8f);
                return true;
            }
            else
                ExtendedControls.MessageBoxTheme.Show(fm, ItemName + " " + "Add-on failed to update. Check files for read only status".T(EDTx.AddOnManagerForm_Failed), "Warning".T(EDTx.Warning), 
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);

            return false;
        }

        // delete local (if present) and then install new downloaded version

        public bool Install(System.Threading.CancellationToken canceltoken, string downloadserver, string approotfolder)
        {
            bool overwritefolders = DownloadedVars.Equals("OverwriteFolders", "True");      // if set, overwrite, don't delete/replace folders

            BaseUtils.GitHubClass ghc = new BaseUtils.GitHubClass(downloadserver);

            var folderfiles = CreateFolderList(canceltoken, DownloadedVars, downloadserver);

            if (LocalPresent)        // if local is there, remove it first. If overwritefolders is present, then don't delete these folders.
            {
                Remove(approotfolder, overwritefolders ? folderfiles : null);
            }

            if (folderfiles.Count > 0)
            {
                foreach (var x in folderfiles) System.Diagnostics.Debug.WriteLine($"Install Download folder file {x.DownloadURI} -> {Path.Combine(approotfolder, x.Path, x.Name)}");

                bool res = ghc.DownloadFiles(canceltoken, approotfolder, folderfiles, true, true);      // download, don't use etags, and sync folder
                if (!res)
                {
                    System.Diagnostics.Trace.WriteLine("Install: Download of folders failed");
                    return false;
                }
            }

            var files = CreateFileList(DownloadedVars, DownloadedURI, downloadserver);

            if (files.Count > 0)
            {
                foreach (var x in files) System.Diagnostics.Debug.WriteLine($"Install Download file {x.DownloadURI} -> {Path.Combine(approotfolder, x.Path, x.Name)}");

                bool res = ghc.DownloadFiles(canceltoken, approotfolder, files, true);      // download, don't use etags, don't sync folder
                if (!res)
                {
                    System.Diagnostics.Trace.WriteLine("Install: Download of files failed");
                    return false;
                }
            }

            string actfolderinstall = Path.GetDirectoryName(InstallFilePath); // where we will store, get folder..

            // go thru disables..
            foreach (string key in DownloadedVars.NameEnumuerable.Where(x => x.StartsWith("DisableOther")))
            {
                string otheract = Path.Combine(actfolderinstall, DownloadedVars[key] + ".act");
                if (File.Exists(otheract))
                {
                    // we have a file in the same folder, so try and disable it
                    DownloadItem other = new DownloadItem();
                    if (other.ReadLocalItem(otheract, approotfolder, "ACT"))      // if its there and read okay
                    {
                        other.SetLocalCopyEnableFlag(false, approotfolder);         // don't worry if it fails..
                    }
                }
            }

            File.Copy(DownloadedTemporaryFilePath, InstallFilePath, true);  // copy act file in

            LocalVars = DownloadedVars;     // now switch to downloaded vars so WriteOrCheck works
            LocalPresent = true;
            WriteOrCheckSHAFile(approotfolder, true);       // just enough vars to get this to work..   

            ReadLocalItem(InstallFilePath, approotfolder, "Action File");  // then read it like its a new file. Just use default Action File For now

            State = DownloadItem.ItemState.UpToDate;    // and since we just downloaded it, then its up to date

            return true;
        }

        public bool Remove(Form fm, string approotfolder)
        {
            if (LocalPresent)        // if local is there, remove it
            {
                if (Remove(approotfolder))
                {
                    ExtendedControls.MessageBoxTheme.Show(fm, ItemName + " Add-on removed", "Action Pack", MessageBoxButtons.OK, MessageBoxIcon.Information, fontscaling: 1.8f);
                }
                else
                {
                    ExtendedControls.MessageBoxTheme.Show(fm, ItemName + " " + "Add-on failed to delete. Check files for read only status", "Warning".T(EDTx.Warning), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        // delete the install, act, all OtherFiles, and optionally folders
        // don't delete remote folders mentioned because they are installed and OverwriteFolders=True
        public bool Remove(string approotfolder, List<RemoteFile> remotefolders = null )
        {
            System.Diagnostics.Debug.Assert(LocalPresent);

            // we remove these files..
            var files = CreateFileList(LocalVars);      // don't worry about URI filling in

            foreach (var file in files)
            {
                string removefile = Path.Combine(approotfolder, file.Path, file.Name);
                System.Diagnostics.Debug.WriteLine($"DeInstall Remove file {removefile}");
                if (!FileHelpers.DeleteFileNoError(removefile))     
                    return false;
            }

            var vars = LocalVars.NameEnumuerable.Where(x => x.StartsWith("DownloadFolder"));

            foreach (var v in vars)
            {
                string[] commands = LocalVars[v].Split(';');
                if (commands.Length == 2)
                {
                    string rootpath = Path.Combine(approotfolder, commands[1]);         // delete the folder and all sub content

                    if (remotefolders?.Find(x => x.Path.EqualsIIC(commands[1])) == null)    // if remotefolders is set, and we can't find it, then delete
                    {
                        System.Diagnostics.Debug.WriteLine($"DeInstall Remove folder {rootpath}");
                        if (!FileHelpers.DeleteDirectoryNoError(rootpath, true))
                            return false;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DeInstall Do not remove folder due to overwrite flag {rootpath}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"DeInstall Remove act {InstallFilePath} and {ItemName + ".sha"}");

            // remove the act file
            if (!FileHelpers.DeleteFileNoError(InstallFilePath))
                return false;

            string shafile = Path.Combine(Path.GetDirectoryName(InstallFilePath), ItemName + ".sha");
            FileHelpers.DeleteFileNoError(shafile); // don't care if this fails

            LocalPresent = false;
            LocalVersion = null;
            LocalVersion = null;
            LocalEnable = null;
            LocalModified = false;
            State = DownloadItem.ItemState.NotPresent;

            return true;
        }

  
        // needs Local info set up
        public bool SetLocalCopyEnableFlag(bool enable, string approotfolder)
        {
            System.Diagnostics.Debug.Assert(LocalPresent);

            if (ActionFile.SetEnableFlag(InstallFilePath, enable))     // if enable flag was changed..
            {
                System.Diagnostics.Debug.WriteLine($"Actionpack Enable/Disable {InstallFilePath} modified to {enable}");
                if (!LocalModified)      // if was not local modified, lets set the SHA so it does not appear local modified just because of the enable
                    WriteOrCheckSHAFile(approotfolder, true);
            }

            return true;
        }

        // needs Local info set up
        // SHA file set (not folder set).
        // true for write, for read its true if the same.. (5/11/24)
        public bool WriteOrCheckSHAFile(string approotfolder, bool write)
        {
            System.Diagnostics.Debug.Assert(LocalPresent);

            // we only sha over these files (not folder files for historical reasons)
            var files = CreateFileList(LocalVars);
            
            // add on the act file itself
            files.Add(new RemoteFile(Path.GetFileName(InstallFilePath), Path.GetDirectoryName(InstallFilePath).Substring(approotfolder.Length + 1), ""));

            //foreach (var x in files) System.Diagnostics.Debug.WriteLine($"SHA {Path.Combine(approotfolder, x.Path, x.Name)}");

            try
            {
                string shacurrent = BaseUtils.SHA.CalcSha1(files.Select(x => Path.Combine(approotfolder, x.Path, x.Name)).ToArray());   // calculate SHA over all these files

                string shafile = Path.Combine(Path.GetDirectoryName(InstallFilePath), ItemName + ".sha");

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

        // if no external files, its just a act, simple install
        public bool IsComplexInstall()
        {
            return ComplexFileSet(DownloadedVars);
        }

        // if no external files, its just a act, simple delete
        public bool IsComplexDelete()
        {
            return LocalPresent ? ComplexFileSet(LocalVars) : false;
        }

        static private bool ComplexFileSet(Variables vars)
        {
            var fileset = CreateFileList(vars);
            if (fileset.Where(x => !x.Name.EndsWith(".mp3")).Count() > 0)       // any files other than .mp3 in the filelist is complex
                return true;
            var varfolders = vars.NameEnumuerable.Where(x => x.StartsWith("DownloadFolder"));
            if (varfolders.Count() > 0)
                return true;
            return vars.NameEnumuerable.Where(x => x.StartsWith("RemoveOther")).Count() > 0;
        }

        // Protect against local only. List of RemoveOther
        public List<string> RemoveOtherPacksList()
        {
            return DownloadedVars.NameEnumuerable.Where(x => x.StartsWith("RemoveOther")).Select(x => DownloadedVars[x]).ToList();
        }

        // Protect against local only. List of NotCompatibleWith
        public List<string> NotCompatibleWithList()
        {
            return DownloadedVars.NameEnumuerable.Where(x => x.StartsWith("NotCompatibleWith")).Select(x => DownloadedVars[x]).ToList();
        }

        // go to github and read folder trees from the variables list
        static private List<RemoteFile> CreateFolderList(System.Threading.CancellationToken canceltoken, Variables installvars, string downloadserver)
        {
            BaseUtils.GitHubClass ghc = new BaseUtils.GitHubClass(downloadserver);

            List<RemoteFile> folderfiles = new List<RemoteFile>();

            var vars = installvars.NameEnumuerable.Where(x => x.StartsWith("DownloadFolder"));
            foreach (var v in vars)
            {
                string[] commands = installvars[v].Split(';');

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

        // go to github and collate a RemoteFile list from the variables
        static private List<RemoteFile> CreateFileList(Variables installvars, string actionfileURI = null, string downloadserverURI = null)
        {
            List<RemoteFile> files = new List<RemoteFile>();

            var vars = installvars.NameEnumuerable.Where(x => x.StartsWith("OtherFile") || x.StartsWith("DownloadFile"));
            foreach (var v in vars)
            {
                string[] commands = installvars[v].Split(';');
                if (v.StartsWith("OtherFile") && commands.Length == 2) // older version
                {
                    // Otherfile had the file in the same folder as act file, so no path inside github was given
                    // this file commands[0] is downloaded by the directory name of item.DownloadedURI with commands[0] as the filename
                    // to be stored in path commands[1] off of approttfolder
                    files.Add(new RemoteFile(commands[0], commands[1], actionfileURI != null ? GitHubClass.GetDownloadURIFromRoot(actionfileURI, commands[0]) : null));
                }
                else if (v.StartsWith("DownloadFile") && commands.Length == 3) // new version allowing source folder to be specified on server
                {
                    // this version has a github path and github file so we don't need to reference DownloadedURI
                    // this file commands[0] is downloaded by the the URI from the root server, with commands[1] as the path and commands[0] as the filename
                    // to be stored in path commands[2] off of approttfolder
                    files.Add(new RemoteFile(commands[0], commands[2], downloadserverURI != null ? GitHubClass.GetDownloadURI(downloadserverURI, "master", commands[1] + "/" + commands[0]) : null));
                }
            }

            return files;
        }


    };


}
