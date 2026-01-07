using Klei.AI;
namespace MutantFarmLab.mutantplants
{
    public static class MutantEffects
    {
        public static readonly string DUAL_HEAD_SYMBIOSIS = "DualHeadSymbiosis";
        public static void RegisterDualHeadSymbiosis() {
            Effect DualHeadSymbiosis = new (
                id: DUAL_HEAD_SYMBIOSIS,
                name: STRINGS.MUTANTS.EFFECTS.DUAL_HEAD_SYMBIOSIS.NAME,
                description: STRINGS.MUTANTS.EFFECTS.DUAL_HEAD_SYMBIOSIS.DESC,
                duration: 0f,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false
            );
            Db.Get().effects.Add(DualHeadSymbiosis);
        }

        public static void RegisterAllEffect()
        {
            RegisterDualHeadSymbiosis();
        }
    }
}
