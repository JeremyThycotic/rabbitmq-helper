﻿using System;
using System.Linq;
using Thycotic.CLI;
using Thycotic.Logging;

namespace Thycotic.RabbitMq.Helper.Installation
{
    internal class SetRabbitMqBaseEnvironmentalVariableCommand : ConsoleCommandBase
    {
        public const string RabbitMqBaseEnvironmentalVariableName = "RABBITMQ_BASE";

        private readonly ILogWriter _log = Log.Get(typeof (SetRabbitMqBaseEnvironmentalVariableCommand));

        public override string Name
        {
            get { return "setRabbitMqBaseEnvironmentalVariable"; }
        }

        public override string Area
        {
            get { return "Installation"; }
        }

        public override string Description
        {
            get { return "Sets the RABBITMQ_BASE environmental variable"; }
        }

        public SetRabbitMqBaseEnvironmentalVariableCommand()
        {

            Action = parameters =>
            {
                var targets = new[]
                {
                    EnvironmentVariableTarget.Machine,
                    EnvironmentVariableTarget.Process
                };

                targets.ToList().ForEach(t =>
                    Environment.SetEnvironmentVariable(RabbitMqBaseEnvironmentalVariableName,
                        InstallationConstants.RabbitMq.ConfigurationPath, t));
                


                return 0;
            };
        }
    }
}