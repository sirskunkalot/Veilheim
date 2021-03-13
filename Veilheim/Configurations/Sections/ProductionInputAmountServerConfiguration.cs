// Veilheim

namespace Veilheim.Configurations.Sections
{
    [ConfigurationSection("Production input amounts")]
    public class ProductionInputAmountServerConfiguration : ServerSyncConfig<ProductionInputAmountServerConfiguration>
    {
        [Configuration("Max windmill barley amount", ActivationTime.Immediately)]
        public int windmillBarleyAmount { get; set; } = 50;

        [Configuration("Max wood amount for kiln", ActivationTime.Immediately)]
        public int kilnWoodAmount { get; set; } = 25;

        [Configuration("Max coal amount for furnace", ActivationTime.Immediately)]
        public int furnaceCoalAmount { get; set; } = 20;

        [Configuration("Max ore amount for furnace", ActivationTime.Immediately)]
        public int furnaceOreAmount { get; set; } = 10;

        [Configuration("Max coal amount for blast furnace", ActivationTime.Immediately)]
        public int blastfurnaceCoalAmount { get; set; } = 20;

        [Configuration("Max coal amount for furnace", ActivationTime.Immediately)]
        public int blastfurnaceOreAmount { get; set; } = 10;

        [Configuration("Max flachs for spinning wheel", ActivationTime.Immediately)]
        public int spinningWheelFlachsAmount { get; set; } = 40;
    }
}