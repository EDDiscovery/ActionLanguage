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
        public GitActionFiles(string approotfolder, string tempdatafolder = null) 
        { 
            this.AppRootFolder = approotfolder;

            if (tempdatafolder != null)
            {

                downloadactfolder = System.IO.Path.Combine(tempdatafolder, "act");
                FileHelpers.CreateDirectoryNoError(downloadactfolder);

                downloadaddonfolder = System.IO.Path.Combine(tempdatafolder, "addonfiles");
                FileHelpers.CreateDirectoryNoError(downloadaddonfolder);

#if DEBUG
                downloadactdebugfolder = System.IO.Path.Combine(tempdatafolder, "Debug");
                FileHelpers.CreateDirectoryNoError(downloadactdebugfolder);
#endif
                downloadacttestversionsfolder = System.IO.Path.Combine(tempdatafolder, "TestVersions");
                FileHelpers.CreateDirectoryNoError(downloadacttestversionsfolder);
            }
        }

        public void ReadLocalFolder(string folder, string otherfilewildcard, string type)
        {
            VersioningManager.ReadLocalFiles(AppRootFolder, folder, otherfilewildcard, type);
        }

        public void ReadDownloadedFolder(string githuburl, int[] edversion, string progtype)
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

        // in a task, download from github, and callback when complete
        public void DownloadFromGit(System.Threading.CancellationTokenSource canceldownload,  string githuburl, Action callback)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                BaseUtils.GitHubClass ghc = new BaseUtils.GitHubClass(githuburl); // EDDiscovery.Properties.Resources.URLGithubDataDownload

                System.Diagnostics.Debug.WriteLine("GitActionFiles Checking github");

                // we don't use .etag and we synchronise the folder removing anything not in github

                //System.Threading.Thread.Sleep(5000);

                ghc.DownloadFolder(canceldownload.Token, downloadactfolder, "ActionFiles/V1", ActionFileWildCard, true, true);
                if (canceldownload.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("GitActionFiles Exit due to cancel 1");
                    return;
                }

                //System.Threading.Thread.Sleep(1000);

                ghc.DownloadFolder(canceldownload.Token, downloadaddonfolder, "AddonFiles/V1", InfFileWildCard, true, true);
                if (canceldownload.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("GitActionFiles Exit due to cancel 2");
                    return;
                }

                //System.Threading.Thread.Sleep(1000);
#if DEBUG

                ghc.DownloadFolder(canceldownload.Token, downloadactdebugfolder, "ActionFiles/Debug", ActionFileWildCard, true, true);
                if (canceldownload.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("GitActionFiles Exit due to cancel 3");
                    return;
                }
#endif
                //System.Threading.Thread.Sleep(1000);

                ghc.DownloadFolder(canceldownload.Token, downloadacttestversionsfolder, "ActionFiles/TestVersions", ActionFileWildCard, true, true);
                if (canceldownload.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("GitActionFiles Exit due to cancel 4");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("GitActionFiles Finished checking github");
                callback.Invoke();
            });
        }

        private string downloadactfolder;
        private string downloadaddonfolder;
#if DEBUG
        private string downloadactdebugfolder;
#endif
        private string downloadacttestversionsfolder;

    }
}
