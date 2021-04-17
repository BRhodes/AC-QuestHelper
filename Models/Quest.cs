using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace QuestHelper.Models
{
    class Quest
    {
        [JsonIgnore]
        public bool IsDirty { get; set; }
        private string name;
        private int levelRequirement;
        private string description;
        private string notes;
        private List<string> questFlags;

        private string route;
        private double priority;
        //private List<TimeSpan> RunLengthHistory { get; set; }

        private long xpReward;
        private int luminanceReward;
        private string keyReward;

        public string Name { get { return name; } set { name = value; IsDirty = true; } }
        public int LevelRequirement { get { return levelRequirement; } set { levelRequirement = value; IsDirty = true; } }
        public string Description { get { return description; } set { description = value; IsDirty = true; } }
        public string Notes { get { return notes; } set { notes = value; IsDirty = true; } }
        public List<string> QuestFlags { get { return questFlags; } set { questFlags = value; IsDirty = true; } }

        public string Route { get { return route; } set { route = value; IsDirty = true; } }
        public double Priority { get { return priority; } set { priority = value; IsDirty = true; } }
        //public List<TimeSpan> RunLengthHistory { get { return RunLengthHistory; } set { RunLengthHistory = value; IsDirty = true; } }

        public long XpReward { get { return xpReward; } set { xpReward = value; IsDirty = true; } }
        public int LuminanceReward { get { return luminanceReward; } set { luminanceReward = value; IsDirty = true; } }
        public string KeyReward { get { return keyReward; } set { keyReward = value; IsDirty = true; } }


        public override string ToString()
        {
            return Name;
        }

        public enum RewardAmount
        {
            Ones = 0,
            Thousands = 1000,
            Millions = 1000000
        }
        public string XpRewardIn(RewardAmount rewardAmount)
        {
            return FormatNumber(rewardAmount, XpReward);
        }

        public string LuminanceRewardIn(RewardAmount rewardAmount)
        {
            return FormatNumber(rewardAmount, LuminanceReward);
        }

        private string FormatNumber(RewardAmount displayUnit, long x)
        {
            if (x == 0) return "";
            var d = x / (long)displayUnit;

            var endUnit = "";

            switch (displayUnit)
            {
                case RewardAmount.Ones:
                    endUnit = "";
                    break;
                case RewardAmount.Thousands:
                    endUnit = "k";
                    break;
                case RewardAmount.Millions:
                    endUnit = "m";
                    break;
            }

            return $"{d:n0}{endUnit}";

        }
    }
}
