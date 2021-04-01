using Decal.Adapter;
using Decal.Adapter.Wrappers;
using MyClasses.MetaViewWrappers;
using Newtonsoft.Json;
using QuestHelper.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using VirindiViewService;
using VirindiViewService.Controls;

namespace QuestHelper
{
    //Attaches events from core
    [WireUpBaseEvents]

    //View (UI) handling
    [MVView("QuestHelper.mainView.xml")]
    [MVWireUpControlEvents]

    // FriendlyName is the name that will show up in the plugins list of the decal agent (the one in windows, not in-game)
    // View is the path to the xml file that contains info on how to draw our in-game plugin. The xml contains the name and icon our plugin shows in-game.
    // The view here is SamplePlugin.mainView.xml because our projects default namespace is SamplePlugin, and the file name is mainView.xml.
    // The other key here is that mainView.xml must be included as an embeded resource. If its not, your plugin will not show up in-game.
    [FriendlyName("QuestHelper")]
    public class PluginCore : PluginBase
    {
        static readonly ACImage _routeStart = new ACImage(0x69e8);
        static readonly ACImage _priorityUp = new ACImage(0x28FC);
        static readonly ACImage _priorityDown = new ACImage(0x28FD);

        List<Quest> Quests = new List<Quest>();

        Dictionary<string, Quest> questDic;
        SortedList<double, string> QuestPriority = new SortedList<double, string>();

        QuestFlagRepository qt;
        HudView questView;
        Quest questForEdit = null;

        public Timer QuestRedrawTimer { get; private set; }
        public Timer AutoSaveTimer { get; private set; }

        /// <summary>
        /// This is called when the plugin is started up. This happens only once.
        /// </summary>
        protected override void Startup()
        {
            try
            {
                // This initializes our static Globals class with references to the key objects your plugin will use, Host and Core.
                // The OOP way would be to pass Host and Core to your objects, but this is easier.
                Globals.Init("QuestHelper", Host, Core);

                //Initialize the view.
                MVWireupHelper.WireupStart(this, Host);
                var views = HudView.GetAllViews();
                foreach (var view in views)
                {
                    if (view.Title == "Quest Helper")
                    {
                        qt = new QuestFlagRepository(Core, view);
                        questView = view;
                    }
                }
                //Quests = CodeLoadQuests();

                //Core.CharacterFilter.AccountName;
                //Core.CharacterFilter.Name;
                //Quests.AddRange(LoadQuests());
                LoadState();
                AutoSaveTimer = new Timer();
                AutoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
                AutoSaveTimer.Interval = 10000;
                AutoSaveTimer.Start();
                QuestRedrawTimer = new Timer();
                QuestRedrawTimer.Elapsed += QuestRedrawTimer_Elapsed;
                QuestRedrawTimer.Interval = 30;
                QuestRedrawTimer.Start();

            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void AutoSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SaveState();
        }

        private void QuestRedrawTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            RedrawQuests();
        }

        private void RedrawQuests()
        {
            try
            {
                if (questView.Visible)
                {
                    var UiQuestList = (HudList)questView["QuestList"];
                    var sp = UiQuestList.ScrollPosition;
                    UiQuestList.ClearRows();

                    for (int x = 0; x < Quests.Count; x++)
                    {
                        var quest = Quests[x];
                        DrawQuest(UiQuestList, quest, x, Quests.Count - 1);
                    }
                    UiQuestList.ScrollPosition = sp;
                }
            }
            catch (Exception ex) { Logger.LogException(ex); }
        }


        [DllImport("Decal.dll")]
        static extern int DispatchOnChatCommand(ref IntPtr str, [MarshalAs(UnmanagedType.U4)] int target);

        public static bool Decal_DispatchOnChatCommand(string cmd)
        {
            IntPtr bstr = Marshal.StringToBSTR(cmd);

            try
            {
                bool eaten = (DispatchOnChatCommand(ref bstr, 1) & 0x1) > 0;

                return eaten;
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }

        /// <summary>
        /// This will first attempt to send the messages to all plugins. If no plugins set e.Eat to true on the message, it will then simply call InvokeChatParser.
        /// </summary>
        /// <param name="cmd"></param>
        public static void DispatchChatToBoxWithPluginIntercept(string cmd)
        {
            if (!Decal_DispatchOnChatCommand(cmd))
                CoreManager.Current.Actions.InvokeChatParser(cmd);
        }


        private void StartNavigation_ClickHandler(Quest quest, object sender, ControlMouseEventArgs e)
        {
            if (e.EventType != ControlMouseEventArgs.MouseEventType.MouseDown)
                return;

            DispatchChatToBoxWithPluginIntercept($"/vt nav load {quest.Route}");
            Util.WriteToChat("Clicked!");
        }

        private void PriorityChange_ClickHandler(object sender, ControlMouseEventArgs e, int a, int b)
        {
            if (e.EventType != ControlMouseEventArgs.MouseEventType.MouseDown)
                return;

            var holdOver = Quests[b];
            Quests[b] = Quests[a];
            Quests[a] = holdOver;

            var x = int.MaxValue;
            foreach (var quest in Quests)
            {
                quest.Priority = x--;
            }
            RedrawQuests();
        }

        private void QuestName_Hit(object sender, EventArgs e, Quest quest)
        {
            ClearEditQuest(false);
            questForEdit = quest;
            ((HudStaticText)questView["QuestForEdit"]).Text = "New Quest";
            ((HudTextBox)questView["EditQuestName"]).Text = quest.Name;
            ((HudTextBox)questView["EditQuestLevelRequirement"]).Text = quest.LevelRequirement.ToString();
            ((HudTextBox)questView["EditQuestDescription"]).Text = quest.Description;
            ((HudTextBox)questView["EditQuestNotes"]).Text = quest.Notes;
            ((HudTextBox)questView["EditQuestQuestFlags"]).Text = string.Join(",", quest.QuestFlags.ToArray());
            ((HudTextBox)questView["EditQuestRoute"]).Text = quest.Route;
            ((HudTextBox)questView["EditQuestPriority"]).Text = quest.Priority.ToString();
            ((HudTextBox)questView["EditQuestXpReward"]).Text = quest.XpReward.ToString();
            ((HudTextBox)questView["EditQuestLuminanceReward"]).Text = quest.LuminanceReward.ToString();
            ((HudTextBox)questView["EditQuestKeyReward"]).Text = quest.KeyReward;

            ((HudTabView)questView["QuestListNotebook"]).CurrentTab = 1;
        }

        private void DrawQuest(HudList uiQuestList, Quest quest, int current, int last)
        {
            var x = 0;
            HudList.HudListRowAccessor row = uiQuestList.AddRow();
            // Route Button
            var button = (HudButton)row[x++];
            button.OverlayImage = _routeStart;
            button.MouseEvent += (sender, e) => StartNavigation_ClickHandler(quest, sender, e);

            // Quest Name
            var questName = (HudStaticText)row[x++];
            questName.Text = quest.Name;
            questName.Hit += (s, e) => QuestName_Hit(s, e, quest);


            // Time till next available
            ((HudStaticText)row[x]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
            ((HudStaticText)row[x++]).Text = Util.GetFriendlyTimeDifference(qt.WhenIsQuestReady(quest));

            // Xp reward
            ((HudStaticText)row[x]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
            ((HudStaticText)row[x++]).Text = quest.XpRewardIn(Quest.RewardAmount.Millions);

            // Lum Reward
            ((HudStaticText)row[x]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
            ((HudStaticText)row[x++]).Text = quest.LuminanceRewardIn(Quest.RewardAmount.Thousands);

            // Key Reward
            ((HudStaticText)row[x]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
            ((HudStaticText)row[x++]).Text = quest.KeyReward;

            // Spacing
            x++;

            // Priority Up
            var priorityUp = (HudButton)row[x++];
            if (current != 0)
            {
                priorityUp.OverlayImage = _priorityUp;
                priorityUp.MouseEvent += (sender, e) => PriorityChange_ClickHandler(sender, e, current, current - 1);
            }
            else
            {
                priorityUp.Visible = false;
            }

            // Priority Down
            var priorityDown = (HudButton)row[x++];
            if (current < last)
            {
                priorityDown.OverlayImage = _priorityDown;
                priorityDown.MouseEvent += (sender, e) => PriorityChange_ClickHandler(sender, e, current, current + 1);
            }
            else
            {
                priorityDown.Visible = false;
            }
        }


        /// <summary>
        /// This is called when the plugin is shut down. This happens only once.
        /// </summary>
        protected override void Shutdown()
        {
            try
            {
                //Destroy the view.
                MVWireupHelper.WireupEnd(this);
                QuestRedrawTimer.Stop();
                AutoSaveTimer.Stop();
                SaveState();
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void LoadState()
        {
            LoadQuests();
        }

        private void LoadQuests()
        {
            //var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //var questPath = $"{appdata}\\VehnPlugins\\QuestHelper\\Quests\\";
            var questPath = $"C:\\AC\\QuestHelper\\Data";
            if (!Directory.Exists(questPath)) return;
            var questFiles = Directory.GetFiles(questPath);

            foreach (var fileName in questFiles)
            {
                var file = new StreamReader(fileName);

                var questJson = file.ReadToEnd();

                var quest = JsonConvert.DeserializeObject<Quest>(questJson);
                quest.IsDirty = false;
                if (quest.Name != "template")
                    Quests.Add(quest);
                file.Close();
            }
            SortQuests();
        }

        private void SortQuests()
        {
            Quests.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        private void SaveState()
        {
            SaveQuests();
        }

        private void SaveQuests()
        {
            //var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //var questPath = $"{appdata}\\VehnPlugins\\QuestHelper\\Quests\\";
            var questPath = $"C:\\AC\\QuestHelper\\Data";
            foreach (var quest in Quests)
            {
                if (!quest.IsDirty) continue;
                var scrubbedQuestName = quest.Name.Replace(":", "-");
                var text = JsonConvert.SerializeObject(quest, Formatting.Indented);
                var f = new StreamWriter($"{questPath}\\{scrubbedQuestName}.json");
                f.Write(text);
                f.Close();
                quest.IsDirty = false;
            }
        }

        [BaseEvent("LoginComplete", "CharacterFilter")]
        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            try
            {
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("Logoff", "CharacterFilter")]
        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            try
            {
                // Unsubscribe to events here, but know that this event is not gauranteed to happen. I've never seen it not fire though.
                // This is not the proper place to free up resources, but... its the easy way. It's not proper because of above statement.
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [MVControlEvent("QuestListRefresh", "Click")]
        void wButton_Click(object sender, MVControlEventArgs e)
        {



        }

        [MVControlEvent("EditQuestCancel", "Click")]
        void EditQuestCancel_Click(object sender, MVControlEventArgs e)
        {
            ClearEditQuest();
        }

        void ClearEditQuest(bool switchTab = true)
        {
            questForEdit = null;
            ((HudStaticText)questView["QuestForEdit"]).Text = "New Quest";
            ((HudTextBox)questView["EditQuestName"]).Text = "";
            ((HudTextBox)questView["EditQuestLevelRequirement"]).Text = "";
            ((HudTextBox)questView["EditQuestDescription"]).Text = "";
            ((HudTextBox)questView["EditQuestNotes"]).Text = "";
            ((HudTextBox)questView["EditQuestQuestFlags"]).Text = "";
            ((HudTextBox)questView["EditQuestRoute"]).Text = "";
            ((HudTextBox)questView["EditQuestPriority"]).Text = "";
            ((HudTextBox)questView["EditQuestXpReward"]).Text = "";
            ((HudTextBox)questView["EditQuestLuminanceReward"]).Text = "";
            ((HudTextBox)questView["EditQuestKeyReward"]).Text = "";
            if (switchTab)
                ((HudTabView)questView["QuestListNotebook"]).CurrentTab = 0;
        }

        [MVControlEvent("EditQuestSave", "Click")]
        void EditQuestSave_Click(object sender, MVControlEventArgs e)
        {
            var updatedQuest = new Quest();
            var Name = ((HudTextBox)questView["EditQuestName"]).Text;
            var LevelRequirement = ((HudTextBox)questView["EditQuestLevelRequirement"]).Text;
            var Description = ((HudTextBox)questView["EditQuestDescription"]).Text;
            var Notes = ((HudTextBox)questView["EditQuestNotes"]).Text;
            var QuestFlags = ((HudTextBox)questView["EditQuestQuestFlags"]).Text;
            var Route = ((HudTextBox)questView["EditQuestRoute"]).Text;
            var Priority = ((HudTextBox)questView["EditQuestPriority"]).Text;
            var XpReward = ((HudTextBox)questView["EditQuestXpReward"]).Text;
            var LuminanceReward = ((HudTextBox)questView["EditQuestLuminanceReward"]).Text;
            var KeyReward = ((HudTextBox)questView["EditQuestKeyReward"]).Text;

            try
            {
                updatedQuest.Name = Name;
                updatedQuest.LevelRequirement = string.IsNullOrEmpty(LevelRequirement) ? 0 : int.Parse(LevelRequirement);
                updatedQuest.Description = Description;
                updatedQuest.Notes = Notes;
                updatedQuest.QuestFlags = new List<string>(QuestFlags.Split(','));
                updatedQuest.Route = Route;
                updatedQuest.Priority = string.IsNullOrEmpty(Priority) ? 0 : double.Parse(Priority);
                updatedQuest.XpReward = string.IsNullOrEmpty(XpReward) ? 0 : long.Parse(XpReward);
                updatedQuest.LuminanceReward = string.IsNullOrEmpty(LuminanceReward) ? 0 : int.Parse(LuminanceReward);
                updatedQuest.KeyReward = KeyReward;
            }
            catch
            {
                return;
            }
            if (questForEdit != null)
            {
                Quests.RemoveAll(x => x.Name == questForEdit.Name);
            }
            Quests.Add(updatedQuest);
            SortQuests();
            RedrawQuests();
            ClearEditQuest();
        }

        [MVControlEvent("QuestListRefresh", "Click")]
        void QuestRefresh_Click(object sender, MVControlEventArgs e)
        {
            qt.GetMyQuestsList();
            RedrawQuests();
            //QuestRedrawTimer_Elapsed(null, null);
        }
    }
}
