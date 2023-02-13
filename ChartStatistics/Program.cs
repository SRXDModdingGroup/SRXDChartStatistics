using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChartStatistics; 

public static class Program {
    private static readonly object LOCK = new object();
    private static readonly Regex MATCH_PARAM = new Regex(@"(""(.+?)""|(\w+)?)\s?");

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main() {
        Command.AddListener("exit", _ => Application.Exit());
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Task.Run(ConsoleLoop);
        Application.Run(new Form1());
    }

    public static void Execute(Action action) {
        lock (LOCK)
            action.Invoke();
    }

    private static void ConsoleLoop() {
        Console.WriteLine("Type \"help\" for a list of commands");
        Console.WriteLine();
            
        string name = string.Empty;
        string[] args = new string[8];

        do {
            Console.Write("> ");
                
            string line = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(line)) {
                name = string.Empty;
                    
                continue;
            }

            var matches = MATCH_PARAM.Matches(line);

            int i = 0;

            foreach (Match match in matches) {
                string result;
                var groupA = match.Groups[2];
                var groupB = match.Groups[3];

                if (groupA.Success)
                    result = groupA.ToString();
                else if (groupB.Success)
                    result = groupB.ToString();
                else
                    result = string.Empty;

                if (i == 0)
                    name = result;
                else if (i < matches.Count && i - 1 < args.Length)
                    args[i - 1] = result;
                else if (i - 1 < args.Length)
                    args[i] = string.Empty;

                i++;
            }

            string name1 = name;
                
            Execute(() => TryRunCommand(name1, args));
        } while (name != "exit");
    }

    private static void TryRunCommand(string name, string[] args) {
        try {
            Command.InvokeCommand(name, args);
        }
        catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }
}