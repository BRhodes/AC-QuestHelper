using QuestHelper.Models;
using System;
using System.Collections.Generic;
using VirindiViewService;
using VirindiViewService.Controls;

namespace QuestHelper.View
{
    class AllQuestsView : UiListBase<Quest, QuestViewData>
    {
        private static readonly ACImage StartRoute = new ACImage(0x69e8);

        private QuestFlagRepository _questFlagRepository { get; set; }
        private PlayerData _playerData { get; set; }
        private QuestManager _questManager { get; set; }
        public Action<Quest> _questClickCallback;

        internal enum Column
        {
            Navigation,
            QuestName,
            NextAvailable,
            LevelRequirement,
            XpReward,
            LumReward,
            KeyReward,
            Space,
            Favorite,
            AccountFavorite
        }

        public AllQuestsView(QuestManager questManager, HudList hudList, QuestFlagRepository questFlagRepository, PlayerData playerData, Action<Quest> thing, HudView questView) : base(hudList)
        {
            _questManager = questManager;
            _questFlagRepository = questFlagRepository;
            _playerData = playerData;
            _questClickCallback = thing;
        }

        protected override List<Quest> GetItems()
        {
            return _questManager.GetQuests();
        }

        protected override QuestViewData CreateViewRow(Quest item)
        {
            return new QuestViewData(item, _playerData.GetQuestPreference(item.Name), _questClickCallback);
        }

        private void AddHandlers(HudList.HudListRowAccessor row, QuestViewData quest)
        {
            // Navigation Button
            var navigation = (HudButton)row[(int)Column.Navigation];
            navigation.MouseEvent += quest.StartNavigation_ClickHandler;

            // Favorite
            var favorite = (HudCheckBox)row[(int)Column.Favorite];
            favorite.Change += quest.Favorite_Changed;

            // Account Favorite
            var accountFavorite = (HudCheckBox)row[(int)Column.AccountFavorite];
            accountFavorite.Change += quest.AccountFavorite_Changed;

            // Quest Name
            var questName = (HudStaticText)row[(int)Column.QuestName];
            questName.Hit += quest.QuestName_Hit;
        }

        override protected void RemoveHandlers(HudList.HudListRowAccessor row, QuestViewData quest)
        {
            // Navigation Button
            var navigation = (HudButton)row[(int)Column.Navigation];
            navigation.MouseEvent -= quest.StartNavigation_ClickHandler;

            // Favorite
            var favorite = (HudCheckBox)row[(int)Column.Favorite];
            favorite.Change -= quest.Favorite_Changed;

            // Account Favorite
            var accountFavorite = (HudCheckBox)row[(int)Column.AccountFavorite];
            accountFavorite.Change -= quest.AccountFavorite_Changed;

            // Quest Name
            var questName = (HudStaticText)row[(int)Column.QuestName];
            questName.Hit -= quest.QuestName_Hit;
        }


        protected override int Compare(Quest a, Quest b)
        {
            return string.Compare(a.Name, b.Name);
            // a < b return -
            // a == b return 0
            // a > b return +

        }

        protected override List<Quest> Filter(List<Quest> quests)
        {
            return quests;
        }

        // Confirmed Derived Class

        protected override void SetupRow(HudList.HudListRowAccessor row, QuestViewData quest)
        {
            // Navigation Button
            var navigation = (HudButton)row[(int)Column.Navigation];
            navigation.OverlayImage = StartRoute;

            // Next Available
            var nextAvailable = (HudStaticText)row[(int)Column.NextAvailable];
            nextAvailable.TextAlignment = WriteTextFormats.Right;

            // Xp reward
            var levelRequirement = (HudStaticText)row[(int)Column.LevelRequirement];
            levelRequirement.TextAlignment = WriteTextFormats.Right;

            // Xp reward
            var xpReward = (HudStaticText)row[(int)Column.XpReward];
            xpReward.TextAlignment = WriteTextFormats.Right;

            // Lum Reward
            var lumReward = (HudStaticText)row[(int)Column.LumReward];
            lumReward.TextAlignment = WriteTextFormats.Right;

            // Key Reward
            var keyReward = (HudStaticText)row[(int)Column.KeyReward];
            keyReward.TextAlignment = WriteTextFormats.Right;

            DrawItem(row, quest);
            AddHandlers(row, quest);
        }

        protected override void DrawItem(HudList.HudListRowAccessor row, QuestViewData quest)
        {
            ((HudStaticText)row[(int)Column.QuestName]).Text = quest.Quest.Name;
            ((HudStaticText)row[(int)Column.NextAvailable]).Text = Util.GetFriendlyTimeDifference(_questFlagRepository.WhenIsQuestReady(quest.Quest));
            ((HudStaticText)row[(int)Column.LevelRequirement]).Text = quest.Quest.LevelRequirement.ToString();
            ((HudStaticText)row[(int)Column.XpReward]).Text = quest.Quest.XpRewardIn(Quest.RewardAmount.Millions);
            ((HudStaticText)row[(int)Column.LumReward]).Text = quest.Quest.LuminanceRewardIn(Quest.RewardAmount.Thousands);
            ((HudStaticText)row[(int)Column.KeyReward]).Text = quest.Quest.KeyReward;
            ((HudCheckBox)row[(int)Column.Favorite]).Checked = quest.Preferences.IsFavorite;
            ((HudCheckBox)row[(int)Column.AccountFavorite]).Checked = quest.Preferences.IsAccountFavorite;
        }
    }

    internal class QuestViewData :IEmbedded<Quest>
    {
        public Quest Quest { get; internal set; }
        public QuestPreferences Preferences { get; internal set; }
        public Action<Quest> EditQuestCallBack;
        Quest IEmbedded<Quest>.Item => Quest;

        public QuestViewData(Quest quest, QuestPreferences preferences, Action<Quest> thing)
        {
            Quest = quest;
            Preferences = preferences;
            EditQuestCallBack = thing;
        }

        public override string ToString()
        {
            return Quest.Name;
        }

        public void StartNavigation_ClickHandler(object sender, ControlMouseEventArgs e)
        {
            if (e.EventType != ControlMouseEventArgs.MouseEventType.MouseDown)
                return;

            Util.DispatchChatToBoxWithPluginIntercept($"/vt nav load {Quest.Route}");
        }

        internal void Favorite_Changed(object sender, EventArgs e)
        {
            if (!(sender is HudCheckBox checkBox)) return;
            Preferences.IsFavorite = checkBox.Checked;
        }

        internal void AccountFavorite_Changed(object sender, EventArgs e)
        {
            if (!(sender is HudCheckBox checkBox)) return;
            Preferences.IsAccountFavorite = checkBox.Checked;
        }

        internal void QuestName_Hit(object sender, EventArgs e)
        {
            EditQuestCallBack(Quest);
        }
    }
}
