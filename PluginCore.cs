using Decal.Adapter;
using Decal.Adapter.Wrappers;
using MyClasses.MetaViewWrappers;
using Newtonsoft.Json;
using QuestHelper.Models;
using QuestHelper.View;
using System;
using System.Collections.Generic;
using System.IO;
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

    [FriendlyName("QuestHelper")]
    public class PluginCore : PluginBase
    {
        HudView questView;
        Quest questForEdit = null;
        int lastTab = 0;

        // Recurring Events
        Timer AllQuestRedrawTimer { get; set; }
        Timer AutoSaveTimer { get; set; }

        // Views
        AllQuestsView _allQuestsView { get; set; }
        FavoriteQuestsView _favoriteQuestsView { get; set; }
        
        // Data Repositories
        QuestManager _questManager { get; set; } = new QuestManager();
        PlayerData _playerData { get; set; } = new PlayerData();
        QuestFlagRepository qt { get; set; }

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

            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void LoadPlayerData()
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var preferencesFilePath = $"{appdata}\\VehnPlugins\\QuestHelper\\QuestPreferences\\{Core.CharacterFilter.Server}\\{Core.CharacterFilter.AccountName}\\{Core.CharacterFilter.Name}.json";
            //_playerData = _playerDataRepository.Load(Core.CharacterFilter.Server, Core.CharacterFilter.AccountName, Core.CharacterFilter.Name);
            

            if (!File.Exists(preferencesFilePath))
            {
                CreateChildDirectories(preferencesFilePath);
                _playerData = new PlayerData();
                return;
            }

            var file = new StreamReader(preferencesFilePath);

            var preferencesJson = file.ReadToEnd();

            _playerData = JsonConvert.DeserializeObject<PlayerData>(preferencesJson);
            file.Close();
        }

        private void CreateChildDirectories(string preferencesFilePath)
        {
            var lastSlash = preferencesFilePath.LastIndexOf('\\');
            var directory = preferencesFilePath.Substring(0, lastSlash);
            if (!Directory.Exists(directory))
            {
                CreateChildDirectories(directory);
                Directory.CreateDirectory(directory);
            }
        }

        private void AutoSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SaveState();
        }

        private void AllQuestRedrawTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _allQuestsView.RedrawItems();
            _favoriteQuestsView.RedrawItems();
        }


        private void QuestName_Hit(Quest quest, int fromTab)
        {
            ClearEditQuest(false);
            questForEdit = quest;
            lastTab = fromTab;
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

            ((HudTabView)questView["QuestListNotebook"]).CurrentTab = 2;
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
                AllQuestRedrawTimer.Stop();
                AutoSaveTimer.Stop();
                SaveState();
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        private void LoadState()
        {
            LoadPlayerData();
        }



        private void SaveState()
        {
            SaveQuests();
            SavePlayerData();
        }

        private void SavePlayerData()
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var preferencesFilePath = $"{appdata}\\VehnPlugins\\QuestHelper\\QuestPreferences\\{Core.CharacterFilter.Server}\\{Core.CharacterFilter.AccountName}\\{Core.CharacterFilter.Name}.json";

            var text = JsonConvert.SerializeObject(_playerData, Formatting.Indented);
            var f = new StreamWriter(preferencesFilePath);
            f.Write(text);
            f.Close();
        }

        private void SaveQuests()
        {
            //var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            //var questPath = $"{appdata}\\VehnPlugins\\QuestHelper\\Quests\\";
            var questPath = $"C:\\AC\\QuestHelper\\Data";
            foreach (var quest in _questManager.GetQuests())
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
                LoadState();
                _allQuestsView = new AllQuestsView(_questManager, (HudList)questView["QuestList"], qt, _playerData, (q) => QuestName_Hit(q, 1), questView);
                _favoriteQuestsView = new FavoriteQuestsView(_questManager, (HudList)questView["FavoriteList"], qt, _playerData, (q) => QuestName_Hit(q, 0), questView);

                AutoSaveTimer = new Timer();
                AutoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
                AutoSaveTimer.Interval = 10000;
                AutoSaveTimer.Start();

                AllQuestRedrawTimer = new Timer();
                AllQuestRedrawTimer.Elapsed += AllQuestRedrawTimer_Elapsed;
                AllQuestRedrawTimer.Interval = 1000;
                AllQuestRedrawTimer.Start();
            }
            catch (Exception ex) { Util.LogError(ex); }
        }

        [BaseEvent("Logoff", "CharacterFilter")]
        private void CharacterFilter_Logoff(object sender, LogoffEventArgs e)
        {
            try
            {
                AllQuestRedrawTimer.Stop();
                // Unsubscribe to events here, but know that this event is not gauranteed to happen. I've never seen it not fire though.
                // This is not the proper place to free up resources, but... its the easy way. It's not proper because of above statement.
            }
            catch (Exception ex) { Util.LogError(ex); }
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
                ((HudTabView)questView["QuestListNotebook"]).CurrentTab = lastTab;
        }

        [MVControlEvent("EditQuestSave", "Click")]
        void EditQuestSave_Click(object sender, MVControlEventArgs e)
        {
            var updatedQuest = questForEdit != null ? questForEdit : new Quest();
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
            catch (Exception ex)
            {
                Util.WriteToChat($"Failed to save Quest: {ex}");
                return;
            }
            if (questForEdit == null)
            {
                _questManager.Add(updatedQuest);
            }
            ClearEditQuest();
        }

        [MVControlEvent("QuestListRefresh", "Click")]
        void QuestRefresh_Click(object sender, MVControlEventArgs e)
        {
            qt.GetMyQuestsList();
            //RedrawQuests();
            //foreach (var quest in Quests)
            //{
            //    //_allQuestsView.AddQuest(quest);
            //}
            //QuestRedrawTimer_Elapsed(null, null);
        }

        [MVControlEvent("StopTimers", "Click")]
        void Stop_Click(object sender, MVControlEventArgs e)
        {
            AllQuestRedrawTimer.Stop();
            AutoSaveTimer.Stop();
        }

        [MVControlEvent("QuestTick", "Click")]
        void QuestTick_Click(object sender, MVControlEventArgs e)
        {
            _allQuestsView.RedrawItems();
            _favoriteQuestsView.RedrawItems();
        }
    }
}
