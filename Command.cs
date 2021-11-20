using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChartStatistics {
    public static class Command {
        private static readonly CommandInternal[] COMMANDS = {
            new CommandInternal("help", "Gets additional info about a command",
                new ArgDescription("command", "The name of the command")),
            new CommandInternal("exit", "Exits the program"),
            new CommandInternal("load", "Loads a chart into the chart viewer",
                new ArgDescription("path", "The file name (with or without extension) or full path of the chart to load")),
            new CommandInternal("show", "Shows data for a specified metric",
                new ArgDescription("metric", "The name of the metric to show")),
            new CommandInternal("path", "Displays a movement path through the notes in the chart",
                new ArgDescription("type", "The type of path to show", 
                    "exact: The precise path through the notes",
                    "simplified: A simplified path with less movement",
                    "none: Do not show a path"))
        };
        
        private static readonly Dictionary<string, CommandInternal> COMMANDS_DICT = COMMANDS.ToDictionary(command => command.Name, command => command);

        private class CommandInternal {
            public string Name { get; }
        
            public string Description { get; }
            
            public ArgDescription[] Args { get; }
        
            public Action<string[]> OnInvoked;

            public CommandInternal(string name, string description, params ArgDescription[] args) {
                Name = name;
                Description = description;
                Args = args;
            }
        }
        
        private class ArgDescription {
            public string Name { get; }
            
            public string Description { get; }
            
            public string[] PossibleValues { get; set; }

            public ArgDescription(string name, string description, params string[] possibleValues) {
                Name = name;
                Description = description;
                PossibleValues = possibleValues;
            }
        }

        static Command() {
            AddListener("help", args => Help(args[0]));
            SetPossibleValues("help", 0, COMMANDS.Select(command => $"{command.Name}: {command.Description}").ToArray());
        }

        public static void InvokeCommand(string commandName, params string[] args) {
            if (!COMMANDS_DICT.TryGetValue(commandName, out var command)) {
                Console.WriteLine("This is not a valid command. Type \"help\" for a list of commands");
                
                return;
            }
            
            command.OnInvoked?.Invoke(args);
        }

        public static void AddListener(string commandName, Action<string[]> action) {
            if (!COMMANDS_DICT.TryGetValue(commandName, out var command))
                return;

            command.OnInvoked += action;
        }

        public static void SetPossibleValues(string commandName, int argIndex, params string[] possibleValues) {
            if (!COMMANDS_DICT.TryGetValue(commandName, out var command))
                return;

            command.Args[argIndex].PossibleValues = possibleValues;
        }

        private static void Help(string commandName) {
            if (string.IsNullOrWhiteSpace(commandName)) {
                Console.WriteLine();
                Console.WriteLine("Available commands:");

                foreach (var command in COMMANDS)
                    Console.WriteLine($"\t{command.Name}: {command.Description}");
                
                Console.WriteLine("Type \"help [command]\" for more info about a command");
                Console.WriteLine();
            }
            else {
                if (!COMMANDS_DICT.TryGetValue(commandName, out var command))
                    return;

                var builder = new StringBuilder(command.Name);

                foreach (var arg in command.Args)
                    builder.Append($" [{arg.Name}]");

                Console.WriteLine();
                Console.WriteLine(builder.ToString());
                Console.WriteLine(command.Description);

                foreach (var arg in command.Args) {
                    Console.WriteLine($"\t{arg.Name}: {arg.Description}");

                    foreach (string value in arg.PossibleValues)
                        Console.WriteLine($"\t\t{value}");
                }

                Console.WriteLine();
            }
        }
    }
}