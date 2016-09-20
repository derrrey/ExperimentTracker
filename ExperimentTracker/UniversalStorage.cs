using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExperimentTracker
{
    class UniversalStorage : IETExperiment
    {
        public bool checkExperiment(ModuleScienceExperiment exp, ExperimentSituations expSituation, CelestialBody lastBody, string curBiome)
        {
            throw new NotImplementedException();
        }

        public void deployExperiment(ModuleScienceExperiment exp)
        {
            throw new NotImplementedException();
        }

        public bool hasData(ModuleScienceExperiment exp)
        {
            throw new NotImplementedException();
        }

        public void resetExperiment(ModuleScienceExperiment exp)
        {
            throw new NotImplementedException();
        }

        public void reviewData(ModuleScienceExperiment exp)
        {
            throw new NotImplementedException();
        }
    }
}
