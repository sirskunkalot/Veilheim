// Veilheim
// a Valheim mod
// 
// File:    RecipeConfig.cs
// Project: Veilheim

using System;
using System.Collections.Generic;

namespace Veilheim.Util
{
    [Serializable]
    public class RecipeRequirementConfig
    {
        public int amount;
        public string item;
    }

    [Serializable]
    public class RecipeConfig
    {
        public int amount;
        public string craftingStation;
        public bool enabled;
        public int minStationLevel;
        public string repairStation;
        public List<RecipeRequirementConfig> resources = new List<RecipeRequirementConfig>();
    }
}