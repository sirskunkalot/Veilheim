// Veilheim
// a Valheim mod
// 
// File:    ConsumablesConfig.cs
// Project: Veilheim

using System;
using System.Collections.Generic;

namespace Veilheim.Util
{
    [Serializable]
    public class ConsumableItemConfig
    {
        public string basePrefab;
        public string description;
        public string displayName;
        public int food;
        public int foodBurnTime;
        public string foodColor;
        public int foodRegen;
        public int foodStamina;
        public string[] icons;
        public string id;
        public int maxStackSize;
        public RecipeConfig RecipeConfig = new RecipeConfig();
    }

    [Serializable]
    public class ConsumablesConfig
    {
        public List<ConsumableItemConfig> items = new List<ConsumableItemConfig>();
    }
}