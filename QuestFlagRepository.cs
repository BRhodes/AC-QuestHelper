using Decal.Adapter;
using QuestHelper.Models;
using System;
using System.Collections.Generic;
using System.Text;
using VirindiViewService;
using VirindiViewService.Controls;

namespace QuestHelper
{
    class QuestFlagRepository
    {
        private bool GettingQuests = false;
        public Dictionary<string, QuestFlag> QuestFlags { get; set; } = new Dictionary<string, QuestFlag>();
        private sbyte GetQuestTries = 0;
        private bool GotFirstQuest = false;
        private bool ShouldEat = false;
        DateTime lastHeartbeat = DateTime.MinValue;
        HudButton UIQuestListRefresh;


        public HudView View;
        private readonly CoreManager Core;

        public QuestFlagRepository(CoreManager core, HudView view)
        {
            Core = core;
            View = view;

            UIQuestListRefresh = (HudButton)View["QuestListRefresh"];
            Core.ChatBoxMessage += Current_ChatBoxMessage;
        }
        
        public int WhenIsQuestReady(Quest quest)
        {
            var questReady = 0;
            foreach (var questFlagId in quest.QuestFlags)
            {
                if (QuestFlags.TryGetValue(questFlagId, out var questFlag))
                {
                    var nextAvailable = questFlag.NextAvailable();

                    questReady = Math.Max(questReady, nextAvailable);
                }
            }

            return questReady;
        }



        public void GetMyQuestsList(sbyte tries = 1, bool doCommand = true, bool shouldEat = true)
        {
            if (GettingQuests)
            {
                if (doCommand) Util.WriteToChat("GetMyQuestsList called while it was already running");
                return;
            }
            QuestFlags.Clear();
            GetQuestTries = tries;
            GettingQuests = true;
            GotFirstQuest = false;
            ShouldEat = shouldEat;
            Core.RenderFrame += Core_RenderFrame;
            lastHeartbeat = DateTime.UtcNow;
            UIQuestListRefresh.Text = "Refresh";
            UIQuestListRefresh.Visible = false;
            if (doCommand) RealGetMyQuestList();
        }

        private void Core_RenderFrame(object sender, EventArgs e)
        {
            if (!GettingQuests)
            {
                Core.RenderFrame -= Core_RenderFrame;
                return;
            }
            if (GotFirstQuest)
            {
                if (DateTime.UtcNow - lastHeartbeat > TimeSpan.FromSeconds(1))
                {
                    GettingQuests = false;
                    Core.RenderFrame -= Core_RenderFrame;
                    UIQuestListRefresh.Visible = true;
                    ShouldEat = false;

                    //UpdateQuestDB();
                }
            }
            else
            {
                if (DateTime.UtcNow - lastHeartbeat > TimeSpan.FromSeconds(15))
                {
                    Util.WriteToChat("Timeout (15s) getting quests");
                    GetQuestTries--;
                    lastHeartbeat = DateTime.UtcNow;
                    RealGetMyQuestList();
                }
            }
        }

        private void RealGetMyQuestList()
        {
            if (GetQuestTries < 1)
            {
                Util.WriteToChat("GetMyQuestList failed too many times, retiring");
                GettingQuests = false;
                Core.RenderFrame -= Core_RenderFrame;
                UIQuestListRefresh.Visible = true;
                ShouldEat = false;
                return;
            }
            try
            {
                Core.Actions.InvokeChatParser("/myquests");
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        public void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            try
            {
                if (e.Text.Equals("Quest list is empty.\n") || e.Text.Equals("The command \"myquests\" is not currently enabled on this server.\n"))
                {
                    GettingQuests = false;
                    Core.RenderFrame -= Core_RenderFrame;
                    UIQuestListRefresh.Visible = true;
                    ShouldEat = false;
                    return;
                }

                if (QuestFlag.MyQuestRegex.IsMatch(e.Text))
                {
                    e.Eat = ShouldEat;
                    GotFirstQuest = true;
                    var questFlag = QuestFlag.FromMyQuestsLine(e.Text);

                    if (questFlag != null)
                    {
                        QuestFlags[questFlag.Id] = questFlag;
//                        UpdateQuestFlag(questFlag);
                    }
                    lastHeartbeat = DateTime.UtcNow;
                }
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }

        //private void UpdateQuestFlag(QuestFlag questFlag)
        //{
        //    try
        //    {
        //        HudList UIList = GetUIListForFlagType(questFlag.FlagType);
        //        Regex filter = GetFilter();

        //        if (questFlags.ContainsKey(questFlag.Key))
        //        {
        //            questFlags[questFlag.Key] = questFlag;
        //        }
        //        else
        //        {
        //            questFlags.Add(questFlag.Key, questFlag);
        //        }

        //        if (filter.IsMatch(questFlag.Key) || filter.IsMatch(questFlag.Name))
        //        {
        //            InsertQuestFlagRow(UIList, questFlag);
        //        }
        //    }
        //    catch (Exception ex) { Logger.LogException(ex); }
        //}

        //private HudList GetUIListForFlagType(QuestFlagType flagType)
        //{
        //    switch (flagType)
        //    {
        //        case QuestFlagType.Timed:
        //            return UITimedQuestList;
        //        case QuestFlagType.Once:
        //            return UIOnceQuestList;
        //        case QuestFlagType.KillTask:
        //            return UIKillTaskQuestList;
        //    }

        //    return null;
        //}

        //public enum QuestFlagType
        //{
        //    Once,
        //    Timed,
        //    KillTask
        //}




    }
}
