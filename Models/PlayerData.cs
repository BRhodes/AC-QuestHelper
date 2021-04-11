using System.Collections.Generic;

namespace QuestHelper.Models
{
    class PlayerData
    {
        public string CharacterName;
        public Dictionary<string, QuestPreferences> _questPreferences { get; set; } = new Dictionary<string, QuestPreferences>();

        public QuestPreferences GetQuestPreference(string questName)
        {
            if (!_questPreferences.TryGetValue(questName, out var questPreferences))
            {
                questPreferences = new QuestPreferences();
                _questPreferences[questName] = questPreferences;
            }

            return questPreferences;
        }
    }

    internal class QuestPreferences
    {
        public bool IsFavorite { get; set; } = false;
        public bool IsAccountFavorite { get; set; } = false;
    }
}
