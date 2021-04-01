using QuestHelper;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace QuestHelper.Models
{
    class QuestFlag
    {
        public enum Type
        {
            KillTask,
            Once,
            Timed
        }
        public static readonly Regex MyQuestRegex = new Regex(@"(?<key>\S+) \- (?<solves>\d+) solves \((?<completedOn>\d{0,11})\)""?((?<description>.*)"" (?<maxSolves>.*) (?<repeatTime>\d{0,11}))?.*$");
        public static readonly Regex KillTaskRegex = new Regex(@"killtask|killcount|slayerquest|totalgolem.*dead|(kills$)");

        public string Id = "";
        public string Description = "";
        public int Solves = 0;
        public int MaxSolves = 0;
        public DateTime CompletedOn = DateTime.MinValue;
        public TimeSpan RepeatTime = TimeSpan.FromSeconds(0);

        public Type FlagType
        {
            get
            {
                if (KillTaskRegex.IsMatch(Id))
                {
                    return Type.KillTask;
                }
                else if (MaxSolves == 1 && Solves <= 1)
                {
                    return Type.Once;
                }
                else
                {
                    return Type.Timed;
                }
            }
        }

        public static QuestFlag FromMyQuestsLine(string line)
        {
            try
            {
                var questFlag = new QuestFlag();
                Match match = MyQuestRegex.Match(line);

                if (match.Success)
                {
                    questFlag.Id = match.Groups["key"].Value.ToLower();
                    questFlag.Description = match.Groups["description"].Value;

                    int.TryParse(match.Groups["solves"].Value, out questFlag.Solves);
                    int.TryParse(match.Groups["maxSolves"].Value, out questFlag.MaxSolves);

                    double completedOn = 0;
                    if (double.TryParse(match.Groups["completedOn"].Value, out completedOn))
                    {
                        questFlag.CompletedOn = Util.UnixTimeStampToDateTime(completedOn);

                        double repeatTime = 0;
                        if (double.TryParse(match.Groups["repeatTime"].Value, out repeatTime))
                        {
                            questFlag.RepeatTime = TimeSpan.FromSeconds(repeatTime);
                        }
                    }

                    return questFlag;
                }
                else
                {
                    Logger.Error("Unable to parse myquests line: " + line);
                    return null;
                }
            }
            catch (Exception ex) { Logger.LogException(ex); }

            return null;
        }

        internal bool IsReady()
        {
            var difference = CompletedOn + RepeatTime - DateTime.UtcNow;

            if (difference.TotalSeconds > 0)
            {
                return false;
            }
            else
            {
                return FlagType != Type.Once;
            }
        }

        internal int NextAvailable()
        {
            var difference = CompletedOn + RepeatTime - DateTime.UtcNow;

            if (difference.TotalSeconds > 0)
            {
                return (int)Math.Round(difference.TotalSeconds);
            }
            else
            {
                return 0;
            }
        }

        internal string NextAvailableString()
        {
            var difference = CompletedOn + RepeatTime - DateTime.UtcNow;

            if (difference.TotalSeconds > 0)
            {
                return Util.GetFriendlyTimeDifference(difference);
            }
            else
            {
                return "ready";
            }
        }

        public new string ToString()
        {
            return $"{Id}: {Description} CompletedOn:{CompletedOn} Solves:{Solves} MaxSolves:{MaxSolves} RepeatTime:{Util.GetFriendlyTimeDifference(RepeatTime)}";
        }
    }
}
