﻿using System.Linq;
using Thycotic.DistributedEngine.InteractiveRunner;
using Thycotic.DistributedEngine.InteractiveRunner.ConsoleCommands;
using Thycotic.Logging;

namespace Thycotic.DistributedEngine.CertificateHelper
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Log.Configure();

            var initialCommand = string.Join(" ", args);

            var cli = new CommandLineInterface();

            cli.AddCustomCommand(new ConvertPfxToPemCommand());

            cli.BeginInputLoop(initialCommand);

            return 0;
        }

    }
}
