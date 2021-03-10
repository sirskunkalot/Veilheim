using System;
using System.Collections.Generic;

namespace Veilheim.Util
{

    [Serializable]
    public class RecipeRequirementConfig
    {
        public string item;
        public int amount;
    }

    [Serializable]
    public class RecipeConfig
    {
        public int amount;
        public string craftingStation;
        public int minStationLevel;
        public bool enabled;
        public string repairStation;
        public List<RecipeRequirementConfig> resources = new List<RecipeRequirementConfig>();
    }

}
