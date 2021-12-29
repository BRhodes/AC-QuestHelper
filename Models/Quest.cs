using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace QuestHelper.Models
{
    class Quest : Activity
    {
        private List<string> questFlags;

        public List<string> QuestFlags { get { return questFlags; } set { questFlags = value; IsDirty = true; } }
    }
}
