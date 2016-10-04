using System;
using DMagic;
using DMagic.Part_Modules;

namespace ExperimentTracker
{
    class OrbitalScience : IETExperiment
    {
        public bool checkExperiment(ModuleScienceExperiment exp, ExperimentSituations expSituation, CelestialBody lastBody, string curBiome)
        {
            ScienceExperiment sciexp = ResearchAndDevelopment.GetExperiment(exp.experimentID);
            ScienceSubject sub = ResearchAndDevelopment.GetExperimentSubject(sciexp, expSituation, lastBody, curBiome);
            float dmscival = ResearchAndDevelopment.GetScienceValue(sciexp.dataScale * sciexp.baseValue, sub);

            float dmexpds = sciexp.dataScale;
            float dmexpbv = sciexp.baseValue;

            return !exp.Inoperable && !exp.Deployed && DMAPI.experimentCanConduct(exp) && dmscival > 1f;
        }

        public void deployExperiment(ModuleScienceExperiment exp)
        {
            if (DMAPI.experimentCanConduct(exp as IScienceDataContainer))
                DMAPI.deployDMExperiment(exp as IScienceDataContainer);
        }

        public Type getType()
        {
            return typeof(DMModuleScienceAnimate);
        }

        public bool hasData(ModuleScienceExperiment exp)
        {
            return (exp as IScienceDataContainer).GetScienceCount() > 0;
        }

        public void resetExperiment(ModuleScienceExperiment exp)
        {
            (exp as DMModuleScienceAnimate).ResetExperiment();
            if ((exp as DMModuleScienceAnimate).IsDeployed)
                (exp as DMModuleScienceAnimate).retractEvent();
        }

        public void reviewData(ModuleScienceExperiment exp)
        {
            (exp as DMModuleScienceAnimate).ReviewData();
        }
    }
}
