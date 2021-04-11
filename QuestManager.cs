using Newtonsoft.Json;
using QuestHelper.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuestHelper
{
    class QuestManager
    {
        public DateTime LastChanged { get; private set; } = DateTime.MinValue;

        List<Quest> _quests;

        public QuestManager()
        {
            _quests = null;
        }

        public List<Quest> GetQuests()
        {
            if (_quests == null)
            {
                LoadQuests();
            }

            return _quests;
        }

        private void LoadQuests()
        {
            _quests = new List<Quest>();
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
                    _quests.Add(quest);
                file.Close();
            }
            _quests.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        internal void Add(Quest questForEdit)
        {
            _quests.Add(questForEdit);
        }
    }

    
    struct QuestListAccess
    {
        public QuestListAccess(List<Quest> quests, DateTime timeAccessed)
        {
            Quests = quests;
            TimeAccessed = timeAccessed;
        }

        public List<Quest> Quests;
        public DateTime TimeAccessed;
    }
}
