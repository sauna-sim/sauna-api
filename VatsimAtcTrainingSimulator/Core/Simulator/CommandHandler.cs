using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator
{
    public static class CommandHandler
    {
        private static Dictionary<string, IAircraftCommand> Commands = new Dictionary<string, IAircraftCommand>();

        static CommandHandler()
        {
            // Get types
            List<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IAircraftCommand).IsAssignableFrom(p) && p.GetInterfaces().Contains(typeof(IAircraftCommand)))
                .ToList();

            foreach (Type type in types)
            {
                IAircraftCommand cmd = (IAircraftCommand)Activator.CreateInstance(type);
                Commands.Add(cmd.CommandName, cmd);
            }
        }

        public static List<string> HandleCommand(string commandName, VatsimClientPilot aircraft, List<string> args, Action<string> logger)
        {
            // Get command
            if (Commands.TryGetValue(commandName, out IAircraftCommand cmd))
            {
                cmd.Logger = logger;
                args = cmd.HandleCommand(aircraft, args);
                cmd.Logger = null;
            }
            else
            {
                logger($"ERROR: Command {commandName} not valid!");
            }

            return args;
        }
    }
}
