// Veilheim
// a Valheim mod
// 
// File:    Configuration.cs
// Project: Veilheim

using Veilheim.Configurations.Sections;

namespace Veilheim.Configurations
{
    public partial class Configuration
    {
        public MapConfiguration Map { get; set; }
        public MapServerConfiguration MapServer { get; set; }
        public ProductionInputAmountServerConfiguration ProductionInputAmounts { get; set; }
    }
}