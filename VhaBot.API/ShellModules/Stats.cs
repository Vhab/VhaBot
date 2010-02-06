using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.ShellModules
{
    public class Stats
    {
        private DateTime _startTime;
        public DateTime StartTime { get { return this._startTime; } }
        public TimeSpan Uptime { get { return DateTime.Now - this.StartTime; } }
        public Int64 Counter_Commands;
        public Int64 Counter_PrivateMessages_Sent;
        public Int64 Counter_PrivateMessages_Received;
        public Int64 Counter_ChannelMessages_Sent;
        public Int64 Counter_ChannelMessages_Received;
        public Int64 Counter_PrivateChannelMessages_Sent;
        public Int64 Counter_PrivateChannelMessages_Received;

        public Stats()
        {
            this._startTime = DateTime.Now;
        }

        public Dictionary<string, Int64> SortByValue(Dictionary<string, Int64> dictionary)
        {
            SortedDictionary<Int64, string> sorter = new SortedDictionary<Int64, string>();
            if (dictionary == null)
                return null;

            Int32 i = 0;
            foreach (KeyValuePair<string, Int64> kvp in dictionary)
            {
                sorter.Add((kvp.Value * dictionary.Count) + i, kvp.Key);
                i++;
            }

            Dictionary<string, Int64> sorted = new Dictionary<string, Int64>();
            foreach (KeyValuePair<Int64, string> kvp in sorter)
            {
                sorted.Add(kvp.Value, dictionary[kvp.Value]);
            }

            return sorted;
        }
    }
}
