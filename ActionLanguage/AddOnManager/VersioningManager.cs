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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseUtils;

namespace ActionLanguage.Manager
{
    public class VersioningManager
    {
        public List<DownloadItem> DownloadItems { private set; get; } = new List<DownloadItem>();
        public Dictionary<string, string> InstallDeinstallAtStartupList { get; set; } = new Dictionary<string, string>();

        public DownloadItem Find(string itemname)
        {
            return DownloadItems.Find(x=>x.ItemName.Equals(itemname,System.StringComparison.InvariantCulture));
        }

        public VersioningManager()
        {
        }

        // read local act files from filesfolder
        public void ReadLocalFiles(string approotfolder,    // root of install
                                   string actionfilesfolder,      // where we are scanning
                                   string wildcardfilename, // pattern to match
                                   string defaultitemtype  // type to give in it.ItemType to the scanned files
            )  
        {
            if (!System.IO.Directory.Exists(actionfilesfolder))
                System.IO.Directory.CreateDirectory(actionfilesfolder);

            FileInfo[] allFiles = Directory.EnumerateFiles(actionfilesfolder, wildcardfilename, SearchOption.TopDirectoryOnly).Select(f => new FileInfo(f)).OrderBy(p => p.Name).ToArray();

            foreach (FileInfo f in allFiles)
            {
                DownloadItem it = new DownloadItem();
                if ( it.ReadLocalItem(f.FullName, approotfolder, defaultitemtype) )     // if read successfully
                {
                    if (InstallDeinstallAtStartupList.TryGetValue(it.ItemName, out string setting))
                        it.State = setting == "-" ? DownloadItem.ItemState.ToBeRemoved : DownloadItem.ItemState.ToBeInstalled;

                    DownloadItems.Add(it);
                }
            }
        }

        // Read files from a dump got from github
        // does after local files reading
        public void ReadInstallFiles(   string downloaduriroot,  // where the files came from , with / at end
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
                // if successfully read the action file from the server copy

                if (ActionLanguage.ActionFile.ReadVarsAndEnableFromFile(f.FullName, out Variables cv, out bool _))
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
                        string localfilepath = System.IO.Path.Combine(installfolder, filename);             // local equivalent file path

                        // see if the local scan above found the equivalent item
                        DownloadItem it = DownloadItems.Find(x => x.InstallFilePath.EqualsIIC(localfilepath));

                        if (it != null)     // local exists
                        {
                            it.DownloadedURI = downloaduriroot + filename;
                            it.DownloadedTemporaryFilePath = f.FullName;
                            it.DownloadedVars = cv;
                            it.DownloadedVersion = version;
                            it.State = (it.DownloadedVersion.CompareVersion(it.LocalVersion) > 0) ? DownloadItem.ItemState.OutOfDate : DownloadItem.ItemState.UpToDate;
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

                                InstallFilePath = localfilepath,          // set these so it knows where to install..

                                State = DownloadItem.ItemState.NotPresent,
                            };

                            DownloadItems.Add(it);
                        }

                        int[] minedversion = cv["MinEDVersion"].VersionFromString();        // may be null if robert has screwed up the vnumber

                        if ( minedversion != null && minedversion.CompareVersion(edversion) > 0)     // if midedversion > edversion can't install
                            it.State = DownloadItem.ItemState.EDOutOfDate;

                        if ( cv.Exists("MaxEDInstallVersion"))      
                        {
                            int[] maxedinstallversion = cv["MaxEDInstallVersion"].VersionFromString();

                            if (maxedinstallversion.CompareVersion(edversion) <= 0) // if maxedinstallversion 
                                it.State = DownloadItem.ItemState.EDTooOld;
                        }

                        if (InstallDeinstallAtStartupList.TryGetValue(it.ItemName, out string setting))
                            it.State = setting == "-" ? DownloadItem.ItemState.ToBeRemoved : DownloadItem.ItemState.ToBeInstalled;
                    }
                }
            }
        }

        public void Sort()
        {
            DownloadItems.Sort(delegate (DownloadItem x, DownloadItem y) { return x.ItemName.CompareTo(y.ItemName); });
        }

    }
}
