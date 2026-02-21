using Database;

namespace MutantContainmentProject.MutanterStoryStraits
{
    public class MutanterStoris
    {
        public static readonly string GravitasMutanterFounderID = nameof(GravitasMutanterFounder);
        public static void StoryGravitasMutanterFounder(Stories __instance)
        {
            __instance.Add(new Story(GravitasMutanterFounderID, "storytraits/MutanterFounder", 1, 2, 43, "storytraits/mutanter_founder").SetKeepsake("keepsake_mutanterfounder"));
        }
    }
}
