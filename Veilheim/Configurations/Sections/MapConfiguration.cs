namespace Veilheim.Configurations.Sections
{
    [ConfigurationSection("Map settings")]
    public class MapConfiguration : BaseConfig<MapConfiguration>
    {
        [Configuration("Show portals automatically on map\nIf it does not activate automatically, just rename a existing portal.", ActivationTime.AfterRelog)]
        public bool showPortalsOnMap { get; set; } = false;

    }
}