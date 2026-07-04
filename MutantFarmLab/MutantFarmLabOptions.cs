using PeterHan.PLib.Options;

namespace MutantFarmLab
{
    [ConfigFile]
    [RestartRequired]
    public sealed class MutantFarmLabOptions
    {
        [Option("STRINGS.UI.OPTIONS.OPTION_DUAL_HEAD_NAME", "STRINGS.UI.OPTIONS.OPTION_DUAL_HEAD_DESC", "STRINGS.UI.OPTIONS.GROUP_MUTANT_PLANT")]
        public bool EnableDualHeadMutation { get; set; } = true;

        [Option("STRINGS.UI.OPTIONS.OPTION_ACTINO_NAME", "STRINGS.UI.OPTIONS.OPTION_ACTINO_DESC", "STRINGS.UI.OPTIONS.GROUP_MUTANT_PLANT")]
        public bool EnableActinobacteriaMutation { get; set; } = true;

        [Option("STRINGS.UI.OPTIONS.OPTION_OILENRICH_NAME", "STRINGS.UI.OPTIONS.OPTION_OILENRICH_DESC", "STRINGS.UI.OPTIONS.GROUP_MUTANT_PLANT")]
        public bool EnableOilEnrichMutation { get; set; } = true;

        [Option("STRINGS.UI.OPTIONS.OPTION_RADSEED_NAME", "STRINGS.UI.OPTIONS.OPTION_RADSEED_DESC", "STRINGS.UI.OPTIONS.GROUP_MUTANT_PLANT")]
        public bool EnableRadiationResistMutation { get; set; } = true;
    }
}
