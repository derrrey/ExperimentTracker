/**
* 
* Code originally created by Allen Mrazek (alias xEvilReeperx).
* The project "ScienceAlert" is open source at https://bitbucket.org/xEvilReeperx/ksp_sciencealert.
*
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExperimentTracker
{
    public class BiomeHelper : MonoBehaviour
    {
        public static bool GetCurrentBiome(out string biome)
        {
            biome = "N/A";

            if (FlightGlobals.ActiveVessel == null)
                return false;

            string possibleBiome = string.Empty;

            if (GetBiome(FlightGlobals.ActiveVessel.latitude * Mathf.Deg2Rad, FlightGlobals.ActiveVessel.longitude * Mathf.Deg2Rad, out possibleBiome))
            {
                // the biome we got is most likely good
                biome = possibleBiome;
                return true;
            }
            else
            {
                // the biome we got is not very accurate, but we'll take it
                biome = possibleBiome;
                return false;
            }
        }

        private static bool GetBiome(double latRad, double lonRad, out string biome)
        {
            biome = string.Empty;
            var vessel = FlightGlobals.ActiveVessel;

            if (vessel == null || vessel.mainBody.BiomeMap == null)
                return true;

            // vessel.landedAt gets priority since there are some special
            // factors it will take into account for us; specifically, that
            // the vessel is on the launchpad, ksc, etc which are treated
            // as biomes even though they don't exist on the biome map.
            if (!string.IsNullOrEmpty(vessel.landedAt))
            {
                biome = Vessel.GetLandedAtString(vessel.landedAt);
                return true;
            }
            else
            {
                // use the stock function to get an initial possibility
                var possibleBiome = vessel.mainBody.BiomeMap.GetAtt(latRad, lonRad);
                biome = possibleBiome.name;
                return false;
            }
        }
    }
}