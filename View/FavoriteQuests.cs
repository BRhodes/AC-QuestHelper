using QuestHelper.Models;
using System;
using System.Collections.Generic;
using VirindiViewService;
using VirindiViewService.Controls;

namespace QuestHelper.View
{
    class FavoriteQuestsView : UiListBase<Quest, FavoriteQuestsViewData>
    {
        private static readonly ACImage _startRoute = new ACImage(0x69e8);
        static readonly ACImage _priorityUp = new ACImage(0x28FC);
        static readonly ACImage _priorityDown = new ACImage(0x28FD);

        private QuestFlagRepository _questFlagRepository { get; set; }
        private PlayerData _playerData { get; set; }
        private QuestManager _questManager { get; set; }
        public Action<Quest> _questClickCallback;

        internal enum Column
        {
            Navigation,
            QuestName,
            NextAvailable,
            XpReward,
            LumReward,
            KeyReward,
            Space,
            PriorityUp,
            PriorityDown,
            Favorite
        }

        public FavoriteQuestsView(QuestManager questManager, HudList hudList, QuestFlagRepository questFlagRepository, PlayerData playerData, Action<Quest> thing) : base(hudList)
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

        protected override FavoriteQuestsViewData CreateViewRow(Quest item)
        {
            return new FavoriteQuestsViewData(item, _playerData.GetQuestPreference(item.Name), _questClickCallback);
        }

        override protected void RemoveHandlers(HudList.HudListRowAccessor row, FavoriteQuestsViewData quest)
        {
            // Navigation Button
            var navigation = (HudButton)row[(int)Column.Navigation];
            navigation.MouseEvent -= quest.StartNavigation_ClickHandler;

            // Favorite
            var favorite = (HudCheckBox)row[(int)Column.Favorite];
            favorite.Change -= quest.Favorite_Changed;


            // Priority Up
            var priorityUp = (HudButton)row[(int)Column.PriorityUp];
            priorityUp.MouseEvent -= quest.PriorityUp_ClickHandler;

            // Priority Down
            var priorityDown = (HudButton)row[(int)Column.PriorityDown];
            priorityDown.MouseEvent -= quest.PriorityDown_ClickHandler;

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
            return quests.FindAll(x => _playerData.GetQuestPreference(x.Name).IsFavorite);
        }

        private void AddHandlers(HudList.HudListRowAccessor row, FavoriteQuestsViewData quest)
        {
            // Navigation Button
            var navigation = (HudButton)row[(int)Column.Navigation];
            navigation.MouseEvent += quest.StartNavigation_ClickHandler;

            // Favorite
            var favorite = (HudCheckBox)row[(int)Column.Favorite];
            favorite.Change += quest.Favorite_Changed;

            // Priority Up
            var priorityUp = (HudButton)row[(int)Column.PriorityUp];
            priorityUp.MouseEvent += quest.PriorityUp_ClickHandler;

            // Priority Down
            var priorityDown = (HudButton)row[(int)Column.PriorityDown];
            priorityDown.MouseEvent += quest.PriorityDown_ClickHandler;

            // Quest Name
            var questName = (HudStaticText)row[(int)Column.QuestName];
            questName.Hit += quest.QuestName_Hit;
        }

        protected override void SetupRow(HudList.HudListRowAccessor row, FavoriteQuestsViewData quest)
        {
            // Navigation Button
            var navigation = (HudButton)row[(int)Column.Navigation];
            navigation.OverlayImage = _startRoute;

            // Next Available
            var nextAvailable = (HudStaticText)row[(int)Column.NextAvailable];
            nextAvailable.TextAlignment = WriteTextFormats.Right;

            // Xp reward
            var xpReward = (HudStaticText)row[(int)Column.XpReward];
            xpReward.TextAlignment = WriteTextFormats.Right;

            // Lum Reward
            var lumReward = (HudStaticText)row[(int)Column.LumReward];
            lumReward.TextAlignment = WriteTextFormats.Right;

            // Key Reward
            var keyReward = (HudStaticText)row[(int)Column.KeyReward];
            keyReward.TextAlignment = WriteTextFormats.Right;

            // Priority Up Button
            var priorityUp = (HudButton)row[(int)Column.PriorityUp];
            priorityUp.OverlayImage = _priorityUp;

            // Priority down Button
            var priorityDown = (HudButton)row[(int)Column.PriorityDown];
            priorityDown.OverlayImage = _priorityDown;
            DrawItem(row, quest);
            AddHandlers(row, quest);
        }

        protected override void DrawItem(HudList.HudListRowAccessor row, FavoriteQuestsViewData quest)
        {
            ((HudStaticText)row[(int)Column.QuestName]).Text = quest.Quest.Name;
            ((HudStaticText)row[(int)Column.NextAvailable]).Text = Util.GetFriendlyTimeDifference(_questFlagRepository.WhenIsQuestReady(quest.Quest));
            ((HudStaticText)row[(int)Column.XpReward]).Text = quest.Quest.XpRewardIn(Quest.RewardAmount.Millions);
            ((HudStaticText)row[(int)Column.LumReward]).Text = quest.Quest.LuminanceRewardIn(Quest.RewardAmount.Thousands);
            ((HudStaticText)row[(int)Column.KeyReward]).Text = quest.Quest.KeyReward;
            ((HudCheckBox)row[(int)Column.Favorite]).Checked = quest.Preferences.IsFavorite;
        }
    }

    internal class FavoriteQuestsViewData : IEmbedded<Quest>
    {
        public Quest Quest { get; internal set; }
        public QuestPreferences Preferences { get; internal set; }

        Quest IEmbedded<Quest>.Item => Quest;

        public Action<Quest> EditQuestCallBack;

        public FavoriteQuestsViewData(Quest quest, QuestPreferences preferences, Action<Quest> thing)
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

        internal void PriorityUp_ClickHandler(object sender, ControlMouseEventArgs e)
        {
            if (e.EventType != ControlMouseEventArgs.MouseEventType.MouseDown)
                return;

            Util.WriteToChat($"{Quest.Name} priority increased!");
        }

        internal void PriorityDown_ClickHandler(object sender, ControlMouseEventArgs e)
        {
            if (e.EventType != ControlMouseEventArgs.MouseEventType.MouseDown)
                return;

            Util.WriteToChat($"{Quest.Name} priority decreased!");
        }

        internal void QuestName_Hit(object sender, EventArgs e)
        {
            EditQuestCallBack(Quest);
        }
    }
}
