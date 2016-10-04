using System;

namespace ExperimentTracker
{
    public interface IETExperiment
    {
        void deployExperiment(ModuleScienceExperiment exp);
        void reviewData(ModuleScienceExperiment exp);
        void resetExperiment(ModuleScienceExperiment exp);
        bool hasData(ModuleScienceExperiment exp);
        bool checkExperiment(ModuleScienceExperiment exp, ExperimentSituations expSituation, CelestialBody lastBody, string curBiome);
        Type getType();
    }
}
