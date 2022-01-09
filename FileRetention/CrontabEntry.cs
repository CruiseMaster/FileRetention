using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileRetention
{
    public class CrontabEntry
    {
        public string FileName { get; set; }
        public string CrontabLine { get; private set; }
        public DateTime DeletionDate { get; set; }
        public string Who { get; set; }
        public bool IsDirectory { get; set; }

        public CrontabEntry()
        {}

        public CrontabEntry(string crontabLine)
        {
            CrontabLine = crontabLine;
            EvaluateCrontabLine();
        }

        public void EvaluateCrontabLine()
        {
            string line = CrontabLine;

            string[] tabSplit = line.Split('\t');
            var minute = tabSplit[0];
            var hour = tabSplit[1];
            var day = tabSplit[2];
            var month = tabSplit[3];
            var wDay = tabSplit[4];
            var who = tabSplit[5];
            var cmd = tabSplit[6];

            int monthNumeric = 0;
            int dayNumeric = 0;
            Int32.TryParse(month, out monthNumeric);
            Int32.TryParse(day, out dayNumeric);

            if (monthNumeric > DateTime.Now.Month || (monthNumeric == DateTime.Now.Month && dayNumeric > DateTime.Now.Day))
                DeletionDate = DateTime.Parse($"{month}/{day}/{DateTime.Now.Year + 1}");
            else
                DeletionDate = DateTime.Parse($"{month}/{day}/{DateTime.Now.Year}");

            Who = who;

            string[] spaceSplit = cmd.Split(' ');

            for (int i = 0; i < spaceSplit.Length; i++)
            {
                if (spaceSplit[i] == "-f" && spaceSplit.Length > i + 1)
                    FileName = spaceSplit[i + 1];

                if (spaceSplit[i] == "-t")
                    IsDirectory = true;
            }
        }
    }
}
