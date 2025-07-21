/*
 * Copyright 2025-2025 EDDiscovery development team
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
using System.IO;
using System.Windows.Forms;
using BaseUtils;

namespace ActionLanguage.Manager
{
    /// <summary>
    /// Class encodes knowledge of particular structure to load files from git specific to EDD
    /// </summary>
    public class GitActionFiles
    {
        public string ActionFileWildCard { get; set; } = "*.act";
        public string InfFileWildCard { get; set; } = "*.inf";
        public string AppRootFolder { get; set; }

        public VersioningManager VersioningManager { get; set; } = new VersioningManager();

        // create the class with approotfolder.
        // If tempdatafolder is present we can prepare to download from git
        // if prepropulatefolder is present we check for an appropriate zip file in that folder and prepop
        public GitActionFiles(string approotfolder, string tempdatafolder = null, string prepopulatefolder = null) 
        { 
            this.AppRootFolder = approotfolder;

            if (tempdatafolder != null)
            {
                downloadactfolder = CreateFolder(tempdatafolder, "act", prepopulatefolder);
                downloadaddonfolder = CreateFolder(tempdatafolder, "addonfiles", prepopulatefolder);
                downloadactdebugfolder = CreateFolder(tempdatafolder, "Debug", prepopulatefolder);
                downloadacttestversionsfolder = CreateFolder(tempdatafolder, "TestVersions", prepopulatefolder);
            }
        }

        // Create the folder for delivery of the files and check to see if a prepopulated zip file exists with those files in
        private string CreateFolder(string tempdatafolder, string partialpath, string prepopulatefolder)
        {
            string folder = System.IO.Path.Combine(tempdatafolder, partialpath);
            FileHelpers.CreateDirectoryNoError(folder);

            // we can prepopulate the folder from a zip file called 'default'<partialpath>'files.zip'

            if (prepopulatefolder != null)
            {
                string prepropname = System.IO.Path.Combine(prepopulatefolder, "default" + partialpath + "files.zip");
            
                // folder should be empty (first time)
                if (System.IO.File.Exists(prepropname) && Directory.GetFiles(folder).Length == 0)
                {
                    try
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(prepropname, folder);

                        // github downloads using \n, so we need to make sure its \n to keep the SHA happy between these files and the ones on the server
                        foreach ( var f in Directory.GetFiles(folder) )
                        {
                            FileHelpers.ChangeLineEndings(f, f, outlf: "\n");       
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AddOnManager Zip failed {prepropname} -> {folder} {ex}");
                    }
                }
            }

            return folder;
        }

        public void ReadLocalFolder(string folder, string otherfilewildcard, string type)
        {
            VersioningManager.ReadLocalFiles(AppRootFolder, folder, otherfilewildcard, type);
        }

        public void ReadDownloadedFolder(string githuburl, Version edversion, string progtype)
        {
            System.Diagnostics.Debug.WriteLine("GitActionFiles Reading download folder");

            VersioningManager.ReadInstallFiles(BaseUtils.GitHubClass.GetDownloadURI(githuburl, "master", "ActionFiles/V1/"), downloadactfolder, AppRootFolder, ActionFileWildCard, edversion, "Action File", progtype);
            VersioningManager.ReadInstallFiles(BaseUtils.GitHubClass.GetDownloadURI(githuburl, "master", "ActionFiles/V1/"), downloadaddonfolder, AppRootFolder, InfFileWildCard, edversion, "Other File", progtype);
#if DEBUG
            VersioningManager.ReadInstallFiles(BaseUtils.GitHubClass.GetDownloadURI(githuburl, "master", "ActionFiles/Debug/"), downloadactdebugfolder, AppRootFolder, ActionFileWildCard, edversion, "Action File", progtype);
#endif
            VersioningManager.ReadInstallFiles(BaseUtils.GitHubClass.GetDownloadURI(githuburl, "master", "ActionFiles/TestVersions/"), downloadacttestversionsfolder, AppRootFolder, ActionFileWildCard, edversion, "Action File", progtype);

            System.Diagnostics.Debug.WriteLine("GitActionFiles Reading download finished");
        }

        // in a task, download from github, and callback when complete with good/bad flag
        public void DownloadFromGit(System.Threading.CancellationTokenSource canceldownload,  string githuburl, Action<bool> callback)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                BaseUtils.GitHubClass ghc = new BaseUtils.GitHubClass(githuburl); // EDDiscovery.Properties.Resources.URLGithubDataDownload

                System.Diagnostics.Debug.WriteLine("GitActionFiles Checking github");

                // we don't use .etag and we synchronise the folder removing anything not in github

                //System.Threading.Thread.Sleep(5000);

                var res = ghc.DownloadFolder(canceldownload.Token, downloadactfolder, "ActionFiles/V1", ActionFileWildCard, true, true);

#if false       // not in use
                res = (res != null && !canceldownload.IsCancellationRequested) ? ghc.DownloadFolder(canceldownload.Token, downloadaddonfolder, "AddonFiles/V1", InfFileWildCard, true, true) : null;
#endif
#if DEBUG
                res = (res != null && !canceldownload.IsCancellationRequested) ? ghc.DownloadFolder(canceldownload.Token, downloadactdebugfolder, "ActionFiles/Debug", ActionFileWildCard, true, true) : null;
#endif
#if false       // not in use
                res = (res != null && !canceldownload.IsCancellationRequested) ? ghc.DownloadFolder(canceldownload.Token, downloadacttestversionsfolder, "ActionFiles/TestVersions", ActionFileWildCard, true, true, true) : null;
#endif
                if (canceldownload.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("GitActionFiles Exit due to cancel 2");
                    res = null;
                }

                System.Diagnostics.Debug.WriteLine("GitActionFiles Finished checking github");
                callback.Invoke(res!=null);
            });
        }

        private string downloadactfolder;
        private string downloadaddonfolder;
        private string downloadactdebugfolder;
        private string downloadacttestversionsfolder;

    }
}
