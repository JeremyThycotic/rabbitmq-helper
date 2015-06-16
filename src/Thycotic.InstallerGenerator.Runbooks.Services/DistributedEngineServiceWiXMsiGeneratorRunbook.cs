﻿using System;
using System.Collections.Generic;
using Thycotic.InstallerGenerator.Core.MSI.WiX;
using Thycotic.InstallerGenerator.Core.Steps;
using Thycotic.InstallerGenerator.Runbooks.Services.Ingredients;

namespace Thycotic.InstallerGenerator.Runbooks.Services
{
    /// <summary>
    /// Distributed engine service WiX MSI generator runbook
    /// </summary>
    public class DistributedEngineServiceWiXMsiGeneratorRunbook : WiXMsiGeneratorRunbook
    {
        /// <summary>
        /// The default artifact name
        /// </summary>
        public const string DefaultArtifactName = "Thycotic.DistributedEngine.Service";

        /// <summary>
        /// Gets or sets the engine to server communication settings.
        /// </summary>
        /// <value>
        /// The engine to server communication.
        /// </value>
        public EngineToServerCommunicationSettings EngineToServerCommunicationSettings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedEngineServiceWiXMsiGeneratorRunbook"/> class.
        /// </summary>
        public DistributedEngineServiceWiXMsiGeneratorRunbook()
        {
            Is64Bit = true;
        }

        /// <summary>
        /// Bakes the steps.
        /// </summary>
        /// <exception cref="System.ArgumentException">Engine to server communication ingredients missing.</exception>
        public override void BakeSteps()
        {
            if (EngineToServerCommunicationSettings == null)
            {
                throw new ArgumentException("Engine to server communication ingredients missing.");
            }

            ArtifactName = GetArtifactFileName(DefaultArtifactName, ArtifactNameSuffix, Is64Bit, Version);

            Steps = new IInstallerGeneratorStep[]
            {
                new AppSettingConfigurationChangeStep
                {
                    Name = "App.config changes",
                    ConfigurationFilePath = GetPathToFileInSourcePath(string.Format("{0}.exe.config", DefaultArtifactName)),
                    Settings = new Dictionary<string, string>
                    {
                        {"EngineToServerCommunication.ConnectionString", EngineToServerCommunicationSettings.ConnectionString},
                        {"EngineToServerCommunication.UseSsl", EngineToServerCommunicationSettings.UseSsl},
                        {"EngineToServerCommunication.SiteId", EngineToServerCommunicationSettings.SiteId},
                        {"EngineToServerCommunication.OrganizationId", EngineToServerCommunicationSettings.OrganizationId}
                    }
                },
                new ExternalProcessStep
                {
                    Name = "File harvest (WiX Heat process)",
                    WorkingPath = WorkingPath,
                    ExecutablePath = ToolPaths.GetHeatPath(ApplicationPath),
                    Parameters = string.Format(@"
dir {0}
-nologo
-o output\Autogenerated.wxs 
-ag 
-sfrags 
-suid 
-cg main_component_group 
-t add_service_install.xsl 
-sreg 
-scom 
-srd 
-template fragment 
-dr INSTALLLOCATION", SourcePath)

                },
                new ExternalProcessStep
                {
                    Name = "Compiling (WiX Candle process)",
                    WorkingPath = WorkingPath,
                    ExecutablePath = ToolPaths.GetCandlePath(ApplicationPath),
                    Parameters = string.Format(@"
-fips
-nologo 
-arch x64
-ext WixUtilExtension 
-dInstallerVersion={0} 
-out output\
output\AutoGenerated.wxs Product.wxs", Version)
                },
                new ExternalProcessStep
                {
                    Name = "Linking and binding (WiX Light process)",
                    WorkingPath = WorkingPath,
                    ExecutablePath = ToolPaths.GetLightPath(ApplicationPath),
                    Parameters = string.Format(@"
-nologo
-b {0}
-sval 
-ext WixUIExtension 
-ext WixUtilExtension 
-out {1}
output\AutoGenerated.wixobj output\Product.wixobj", SourcePath, ArtifactName)
                }
            };
        }
    }
}
