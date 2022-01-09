using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace FileRetention
{
    public class Program
    {
        private static List<string> _crontabLines;
        private static List<CrontabEntry> _crontabEntries;

        public static void Main(string[] args)
        {
            // Read commandline-arguments

            var filePath = string.Empty;
            var daysRemaining = string.Empty;
            var specifiedDate = string.Empty;
            var isDirectory = false;
            var startInteractiveMode = false;

            if (args.Length == 0)
                WriteHelpText();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h")
                    WriteHelpText();

                if (args[i] == "-d" && args.Length >= i + 1)
                    DeleteNow(args[i + 1]);

                if (args[i] == "-f" && i + 1 <= args.Length)
                    filePath = args[i + 1];

                if (args[i] == "-r" && i + 1 <= args.Length)
                    daysRemaining = args[i + 1];

                if (args[i] == "-s" && i + 1 <= args.Length)
                    specifiedDate = args[i + 1];

                if (args[i] == "-t")
                    isDirectory = true;

                if (args[i] == "-i")
                    startInteractiveMode = true;
            }

            var deletionDate = GetDateOfDeletion(daysRemaining, specifiedDate);

            // If the date of deletion is more in the future than one year, this tool is unable to handle this case
            if (IsMoreThanOneYearAway(deletionDate))
            {
                Console.WriteLine("The date that you specified for deletion is either in the past or more than one year in the future.");
                Console.WriteLine("This tool cannot handle the request.");
                Console.WriteLine("Exiting.");
                Environment.Exit(1);
            }

            if (startInteractiveMode && filePath.Equals(string.Empty))
            {
                Console.WriteLine("You need to specify a file- or folder-path with -f combined with -i parameter.");
                Console.WriteLine("Exiting.");
                Environment.Exit(1);
            }

            LoadCrontabFileEntries();

            var existingEntry = _crontabEntries.Find(e => e.FileName == filePath);

            if (startInteractiveMode)
            {
                // Pick file and start interactive mode

                if (existingEntry == null)
                    existingEntry = new CrontabEntry() { DeletionDate = deletionDate };

                existingEntry.FileName = filePath;
                var modifiedEntry = InteractiveMode(existingEntry);
                filePath = modifiedEntry.FileName;
                specifiedDate = modifiedEntry.DeletionDate.ToString();
                deletionDate = modifiedEntry.DeletionDate;
                existingEntry = modifiedEntry;
            }

            Console.WriteLine($"FilePath: {filePath}");
            Console.WriteLine($"Days remaining: {((int)new TimeSpan(deletionDate.Subtract(DateTime.Now).Ticks).TotalDays)}");
            Console.WriteLine($"Day specified: {specifiedDate}");
            Console.WriteLine($"Deletion-date: {deletionDate.Date}");


            Console.ReadLine();
        }

        private static CrontabEntry InteractiveMode(CrontabEntry entry)
        {
            var finished = false;
            entry.IsDirectory = new DirectoryInfo(entry.FileName).Exists;

            while (!finished)
            {
                Console.Clear();
                Console.WriteLine("**************************************");
                Console.WriteLine("***File-Retention: Interactive Mode***");
                Console.WriteLine("**************************************");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($"File-Path:\t{entry.FileName}");
                Console.WriteLine($"Is Directory:\t{entry.IsDirectory.ToString()}");
                Console.WriteLine($"Deletion-Date:\t{entry.DeletionDate}");
                Console.WriteLine($"Executing user:\t{entry.Who}");
                Console.WriteLine();
                Console.WriteLine($"Remaining days:\t{((int)new TimeSpan(entry.DeletionDate.Subtract(DateTime.Now).Ticks).TotalDays)}");
                Console.WriteLine();
                Console.WriteLine("Please specify what you want to adjust.");
                Console.WriteLine("d = Is Directory, s = Deletion-Date, u = Executing user, f = save & exit");
                var input = Console.ReadKey(false);
                var key = input.Key;

                switch (key)
                {
                    case ConsoleKey.F:
                        finished = true;
                        break;

                    case ConsoleKey.D:
                        entry.IsDirectory = !entry.IsDirectory;
                        break;

                    case ConsoleKey.U:
                        entry.Who = GetUser(entry.Who);
                        break;

                    case ConsoleKey.S:
                        entry.DeletionDate = GetDate(entry.DeletionDate);
                        break;
                }
            }

            return entry;
        }

        private static DateTime GetDate(DateTime currentDate)
        {
            Console.Clear();
            Console.WriteLine("**************************************");
            Console.WriteLine("***File-Retention: Interactive Mode***");
            Console.WriteLine("**************************************");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Current Date of Deletion:\t{currentDate}");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Please enter the new date as dd/mm/yyyy");
            Console.WriteLine("or press return to abort.");
            var input = Console.ReadLine();

            if (input.Equals(string.Empty))
                return currentDate;

            var newDate = new DateTime(0);
            DateTime.TryParse(input, out newDate);

            if (newDate.Ticks < 1)
            {
                Console.WriteLine("ERROR: Cannot read date. Specify as dd/mm/yyyy");
                Console.ReadLine();
                GetDate(currentDate);
            }

            return newDate;
        }

        private static string GetUser(string currentUser)
        {
            Console.Clear();
            Console.WriteLine("**************************************");
            Console.WriteLine("***File-Retention: Interactive Mode***");
            Console.WriteLine("**************************************");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Current user:\t{currentUser}");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Please enter the new user-name and press return");
            Console.WriteLine("or press return without entering a name for aborting.");
            var input = Console.ReadLine();

            if (input.Equals(string.Empty))
                return currentUser;

            return input;
        }

        private static void LoadCrontabFileEntries()
        {
            if (_crontabLines == null)
                _crontabLines = new List<string>();

            _crontabLines.Clear();

            using (var fs = new FileStream("/etc/crontab", FileMode.Open))
            {
                using (var sr = new StreamReader(fs))
                {
                    do
                    {
                        _crontabLines.Add(sr.ReadLine());
                    }while (!sr.EndOfStream);
                }
            }

            if (_crontabEntries == null)
                _crontabEntries = new List<CrontabEntry>();

            _crontabEntries.Clear();

            foreach (var line in _crontabLines)
            {
                if (line.Trim().StartsWith("#"))
                    continue;

                if (!line.Contains($"mono {Assembly.GetExecutingAssembly()}"))
                    continue;

                var entry = new CrontabEntry(line);
                _crontabEntries.Add(entry);
            }
        }

        private static bool IsMoreThanOneYearAway(DateTime date)
        {
            if (DateTime.Now.CompareTo(date) < 0)
                return false;

            if (DateTime.Now.AddYears(1).CompareTo(date) > 0)
                return false;

            return true;
        }

        private static DateTime GetDateOfDeletion(string daysRemaining, string specifiedDate)
        {
            double daysRemainingNumeric = 0;
            var dsRemainingDate = DateTime.MinValue;
            var specifiedDateAsDate = DateTime.MinValue;

            if (daysRemaining != string.Empty)
            {
                if (!double.TryParse(daysRemaining, out daysRemainingNumeric))
                {
                    Console.WriteLine("-r parameter was not given in a correct numeric fashion.");
                    Console.WriteLine("Exiting.");
                    Environment.Exit(1);
                }
                dsRemainingDate = DateTime.Now.AddDays(daysRemainingNumeric);
            }

            if (specifiedDate != string.Empty)
            {
                specifiedDateAsDate = DateTime.Parse(specifiedDate);
            }

            // If both are set it's fine as long as they point to the same date
            if (daysRemaining != String.Empty && specifiedDate != String.Empty)
            {
                if (dsRemainingDate.Date != specifiedDateAsDate.Date)
                {
                    Console.WriteLine($"-r parameter and -s parameter point to a different date. {dsRemainingDate.Date} vs {specifiedDateAsDate.Date}");
                    Console.WriteLine("Specify just one of them.");
                    Console.WriteLine("Exiting.");
                    Environment.Exit(1);
                }
            }

            if (dsRemainingDate != DateTime.MinValue)
                return dsRemainingDate;
            else
                return specifiedDateAsDate;
        }

        public static void DeleteNow(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"File {path} does not exist!");
                Console.WriteLine($"Exiting");
                Environment.Exit(1);
            }

            var fileInfo = new FileInfo(path);
            var directoryInfo = fileInfo.Directory;
            fileInfo.Delete();

            DeleteEmptyRootDirectories(directoryInfo);

            Environment.Exit(0);
        }

        public static void DeleteEmptyRootDirectories(DirectoryInfo directory)
        {
            var filesCount = directory.GetFiles().Length;
            var directoryCount = directory.GetDirectories().Length;

            if (filesCount == 0 && directoryCount == 0)
            {
                Console.WriteLine($"Deleting directory {directory.FullName}");

                var parentDirectory = directory.Parent;
                directory.Delete(true);
                DeleteEmptyRootDirectories(parentDirectory);
            }
        }

        public static void WriteHelpText()
        {
            Console.WriteLine("FileRetention: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.WriteLine();
            Console.WriteLine("FileRetention is a program that lets you mark files and folders");
            Console.WriteLine("for future deletion.");
            Console.WriteLine("It's purpose is to give users an amount of time before");
            Console.WriteLine("an otherwise useless (huge) file is deleted.");
            Console.WriteLine();
            Console.WriteLine("#### Options ####");
            Console.WriteLine();
            Console.WriteLine("-h\t\tDisplays this help-text.");
            Console.WriteLine("-i\t\tStarts an interactive mode. A file must be set (-f) anyways.");
            Console.WriteLine("-d\t\tDeletes a certain file NOW!");
            Console.WriteLine("-r\t\tSpecifies the days remaining until deletion.");
            Console.WriteLine("-f\t\tSpecifies the file to delete after -r days or on the date -s.");
            Console.WriteLine("-t\t\tSpecifies if the selected file -f is a directory. ANY SUB-DIRECTORIES WILL BE DELETED AS WELL!!!");
            Console.WriteLine("-s\t\tSpecifies a certain date on which the file gets deleted. Specify mm/dd/yyyy.");

            Environment.Exit(0);
        }
    }
}
