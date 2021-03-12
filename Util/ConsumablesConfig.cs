using System;
using System.Collections.Generic;

namespace Veilheim.Util
{
    [Serializable]
    public class ConsumableItemConfig
    {
        public string id;
        public string basePrefab;
        public string displayName;
        public string[] icons;
        public string description;
        public int maxStackSize;
        public int food;
        public int foodStamina;
        public int foodRegen;
        public int foodBurnTime;
        public string foodColor;
        public RecipeConfig RecipeConfig = new RecipeConfig();
    }

    [Serializable]
    public class ConsumablesConfig
    {
        public List<ConsumableItemConfig> items = new List<ConsumableItemConfig>();
    }
}
