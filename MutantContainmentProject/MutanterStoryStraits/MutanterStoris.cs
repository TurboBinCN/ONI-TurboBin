using Database;

namespace MutantContainmentProject.MutanterStoryStraits
{
    public class MutanterStoris
    {
        public static void StoryGravitasMutanterFounder(Stories __instance)
        {
            __instance.Add(new Story(nameof(GravitasMutanterFounder), "storytraits/MutanterFounder", 1, 2, 43, "storytraits/mutanter_founder").SetKeepsake("keepsake_mutanterfounder"));
        }
    }
}
