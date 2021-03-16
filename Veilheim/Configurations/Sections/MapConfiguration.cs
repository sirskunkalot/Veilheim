// Veilheim
// a Valheim mod
// 
// File:    MapConfiguration.cs
// Project: Veilheim

namespace Veilheim.Configurations.Sections
{
    [ConfigurationSection("Map settings")]
    public class MapConfiguration : BaseConfig<MapConfiguration>
    {
        [Configuration("Show portals automatically on map.", ActivationTime.AfterRelog)]
        public bool showPortalsOnMap { get; set; } = false;

        [Configuration("Show a list of available portal tags when renaming a portal.", ActivationTime.Immediately)]
        public bool showPortalSelection { get; set; } = false;
    }
}