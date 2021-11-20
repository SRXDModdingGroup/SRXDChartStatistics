using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChartStatistics {
    public static class Program {
        private static readonly object LOCK = new object();

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
            
            string[] split;

            do {
                Console.Write("> ");
                
                string line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line)) {
                    split = null;
                    
                    continue;
                }
                
                split = line.Split(' ');

                string name = split[0].ToLowerInvariant();
                string[] args = new string[8];

                for (int i = 0; i < args.Length; i++) {
                    if (i < split.Length - 1)
                        args[i] = split[i + 1];
                    else
                        args[i] = string.Empty;
                }
                
                Execute(() => TryRunCommand(name, args));
            } while (split == null || split[0] != "exit");
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
}