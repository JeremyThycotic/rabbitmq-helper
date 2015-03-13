﻿using TechTalk.SpecFlow;
using Thycotic.DistributedEngine.Configuration;
using Thycotic.Utility.Specflow;

namespace Thycotic.DistributedEngine.Tests.Configuration
{
    [Binding]
    public class IoCConfiguratorSteps
    {
        [Given(@"there exists a substitute object for IIoCConfigurator stored in the scenario as (\w+)")]
        public void GivenThereExistsASubstituteObjectOfTypeStoredInTheScenarioAsIoCConfiguratorTest(string iocConfigurationName)
        {
            this.GetScenarioContext().SetSubstitute<IIoCConfigurator>(iocConfigurationName);
        }

    }
}
