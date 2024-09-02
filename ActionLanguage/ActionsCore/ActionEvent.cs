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

using System.Collections.Generic;

namespace ActionLanguage
{
    [System.Diagnostics.DebuggerDisplay("Event {TriggerName} {TriggerType} {UIClass}")]
    public class ActionEvent
    {
        public string TriggerName { get; protected set; }             // define an event, a name, a trigger type.  associate for editing with a uiclass
        public string TriggerType { get; protected set; }
        public string UIClass { get; protected set; }
        public List<BaseUtils.TypeHelpers.PropertyNameInfo> Properties { get; protected set; }      // list of associated properties of this event, other than trigger name/type

        // create an action event.  Normally you use one of the built in onXXX events, but you could create it dynamically
        public ActionEvent(string triggername, string triggertype, string uiclass, List<BaseUtils.TypeHelpers.PropertyNameInfo> properties)
        {
            TriggerName = triggername; TriggerType = triggertype; UIClass = uiclass; Properties = properties;
        }

        // protected as you use the statics below to get to it

        protected static List<ActionEvent> events = new List<ActionEvent>()
        {
            new ActionEvent("onStartup", "ProgramEvent", "Program", null ),
            new ActionEvent("onPostStartup","ProgramEvent",  "Program", null ),
            new ActionEvent("onNonModalDialog", "UserUIEvent", "UI",
                new List<BaseUtils.TypeHelpers.PropertyNameInfo>()
                {
                    new BaseUtils.TypeHelpers.PropertyNameInfo("Dialog", "Name of dialog", BaseUtils.ConditionEntry.MatchType.Equals, "Event Variable"),
                    new BaseUtils.TypeHelpers.PropertyNameInfo("Control", "Name of control causing the event", BaseUtils.ConditionEntry.MatchType.Equals, "Event Variable"),
                }
                ),
            new ActionEvent("onPlayStarted","ProgramEvent",  "Audio",
                new List<BaseUtils.TypeHelpers.PropertyNameInfo>()
                {
                    new BaseUtils.TypeHelpers.PropertyNameInfo("EventName", "Event name associated with the play start event", BaseUtils.ConditionEntry.MatchType.Equals, "Event Variable"),
                }
                ),
            new ActionEvent("onPlayFinished","ProgramEvent",  "Audio",
                new List<BaseUtils.TypeHelpers.PropertyNameInfo>()
                {
                    new BaseUtils.TypeHelpers.PropertyNameInfo("EventName", "Event name associated with the play finish event", BaseUtils.ConditionEntry.MatchType.Equals, "Event Variable"),
                }
                ),
            new ActionEvent("onSayStarted", "ProgramEvent", "Audio",
                new List<BaseUtils.TypeHelpers.PropertyNameInfo>()
                {
                    new BaseUtils.TypeHelpers.PropertyNameInfo("EventName", "Event name associated with the say start event", BaseUtils.ConditionEntry.MatchType.Equals, "Event Variable"),
                }
                ), //5
            new ActionEvent("onSayFinished","ProgramEvent",  "Audio",
                new List<BaseUtils.TypeHelpers.PropertyNameInfo>()
                {
                    new BaseUtils.TypeHelpers.PropertyNameInfo("EventName", "Event name associated with the say end event", BaseUtils.ConditionEntry.MatchType.Equals, "Event Variable"),
                }
                ),
        };

        public static ActionEvent onStartup { get { return events[0]; } }
        public static ActionEvent onPostStartup { get { return events[1]; } }
        public static ActionEvent onNonModalDialog { get { return events[2]; } }
        public static ActionEvent onPlayStarted { get { return events[3]; } }
        public static ActionEvent onPlayFinished { get { return events[4]; } }
        public static ActionEvent onSayStarted { get { return events[5]; } }
        public static ActionEvent onSayFinished { get { return events[6]; } }
    }

}
