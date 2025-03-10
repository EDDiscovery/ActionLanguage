/*
 * Copyright © 2017-2025 EDDiscovery development team
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

namespace ActionLanguage
{
    internal enum EDTx
    {
        Warning, // Warning
        Today, // Today
        t24h, // 24h
        t7days, // 7 days
        All, // All
        OK, // OK
        Cancel, // Cancel
        Delete, // Delete
        Campaign, // Campaign
        NoScan, // No Scan
        GameTime, // Game Time
        Time, // Time
        NoData, // No Data
        None, // None
        on, // on
        Off, // Off
        Unknown, // Unknown
        Information, // Information
        NoPos, // No Pos

        AddOnManagerForm_buttonExtGlobals,   // Control 'Globals'

        AddOnManagerForm_AddOnTitle, // Add-On Manager
        AddOnManagerForm_EditTitle, // Edit Add-Ons
        AddOnManagerForm_Locallymodified, // Locally modified
        AddOnManagerForm_UptoDate, // Up to Date
        AddOnManagerForm_LocalOnly, // Local Only
        AddOnManagerForm_Newversion, // New version
        AddOnManagerForm_Newer, // Newer EDD required
        AddOnManagerForm_Old, // Too old for EDD
        AddOnManagerForm_Modwarn, // Modified locally, do you wish to overwrite the changes
        AddOnManagerForm_Failed, // Add-on failed to update. Check files for read only status
        AddOnManagerForm_DeleteWarn, // Do you really want to delete {0}
        AddOnManagerForm_Type, // Type
        AddOnManagerForm_Name, // Name
        AddOnManagerForm_Version, // Version
        AddOnManagerForm_Description, // Description
        AddOnManagerForm_Status, // Status
        AddOnManagerForm_Action, // Action
        AddOnManagerForm_Delete, // Delete
        AddOnManagerForm_Enabled, // Enabled
        AddOnManagerForm_Install, // Install
        AddOnManagerForm_Update, // Update
        AddOnManagerForm_Edit, // Edit

    }

    internal static class EDTranslatorExtensions
    {
        static public string T(this string s, EDTx value)              // use the enum.  This was invented before the shift to all Enums of feb 22
        {
            return s.TxID(value);
        }
    }
}
