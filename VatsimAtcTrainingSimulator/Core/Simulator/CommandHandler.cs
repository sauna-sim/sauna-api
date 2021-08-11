using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VatsimAtcTrainingSimulator.Core.Simulator
{
    public static class CommandHandler
    {
        private static Queue<IAircraftCommand> commandQueue = new Queue<IAircraftCommand>();
        private static object commandQueueLock = new object();
        private static bool processingCommand = false;

        private static void ProcessNextCommand()
        {
            if (processingCommand)
            {
                return;
            }

            processingCommand = true;
            while (commandQueue.Count > 0)
            {
                IAircraftCommand cmd;
                lock (commandQueueLock)
                {
                    cmd = commandQueue.Dequeue();
                }

                // Generate random delay
                int delay = new Random().Next(0, 3000);
                Thread.Sleep(delay);

                cmd.ExecuteCommand();
            }
            processingCommand = false;
        }

        public static List<string> HandleCommand(string commandName, VatsimClientPilot aircraft, List<string> args, Action<string> logger)
        {
            string cmdNameNormalized = commandName.ToLower();
            IAircraftCommand cmd = null;

            // Get Command
            switch (cmdNameNormalized)
            {
                case "pause":
                case "p":
                    aircraft.Paused = true;
                    break;
                case "unpause":
                case "up":
                    aircraft.Paused = false;
                    break;
                case "remove":
                case "delete":
                case "del":
                    _ = aircraft.Disconnect();
                    break;
                case "fh":
                    cmd = new FlyHeadingCommand();
                    break;
                case "tl":
                    cmd = new TurnLeftHeadingCommand();
                    break;
                case "tr":
                    cmd = new TurnRightHeadingCommand();
                    break;
                case "speed":
                case "spd":
                    cmd = new SpeedCommand();
                    break;
                case "alt":
                case "cm":
                case "dm":
                case "climb":
                case "des":
                case "descend":
                    cmd = new AltitudeCommand();
                    break;
                default:
                    logger($"ERROR: Command {commandName} not valid!");
                    return args;
            }

            if (cmd != null)
            {
                // Get new args after processing command
                cmd.Aircraft = aircraft;
                cmd.Logger = logger;

                // Make sure command is valid before running.
                if (cmd.HandleCommand(ref args))
                {
                    // Add to Queue
                    lock (commandQueueLock)
                    {
                        commandQueue.Enqueue(cmd);
                    }

                    // Launch thread to execute queue
                    Thread t = new Thread(ProcessNextCommand);
                    t.Start();
                }
            }

            // Return args
            return args;
        }
    }
}
