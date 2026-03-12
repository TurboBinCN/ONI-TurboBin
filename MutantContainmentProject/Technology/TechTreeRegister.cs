using MutantContainmentProject.Buildings;
using System.Collections.Generic;
using static TBB.He.TbbLib.Module.TbbTechTree;

namespace MutantContainmentProject.Technology
{
    public class TechTreeRegister
    {
        public static string MutantContainTechCategoryID = "mutant_containment";

        public static string BasicMutantContainTechID = "mutant_containment_basic";
        public static string AdvancedMutantContainTechID = "mutant_containment_advanced";

        public static TechTreeCategoryInfo RegisterMutantContainTechCategory()
        {
            TechTreeCategoryInfo mutantContainTechCategory = new(
                id: MutantContainTechCategoryID,
                nameKey: "STRINGS.RESEARCH.TREES.TITLE_MUTANT_CONTAINMENT"
            );
            return mutantContainTechCategory;
        }

        public static TechNodeInfo RegisterBasicMutantContainTech()
        {
            TechNodeInfo basicMutantContainTech = new(
                BasicMutantContainTechID,
                STRINGS.RESEARCH.TECHS.MUTANT_CONTAINMENT_BASIC.NAME,
                STRINGS.RESEARCH.TECHS.MUTANT_CONTAINMENT_BASIC.DESC,
                MutantContainTechCategoryID,
                new List<string> { ContainmentMonitorStationConfig.ID, ContainmentTileConfig.ID },
                null,
                new Dictionary<string, float> { { "basic", 100f } },
                searchTermKey: "MUTANTER"
            );
            return basicMutantContainTech;
        }

        public static TechNodeInfo RegisterAdvancedMutantContainTech()
        {
            TechNodeInfo advancedMutantContainTech = new(
                AdvancedMutantContainTechID,
                STRINGS.RESEARCH.TECHS.MUTANT_CONTAINMENT_ADVANCED.NAME,
                STRINGS.RESEARCH.TECHS.MUTANT_CONTAINMENT_ADVANCED.DESC,
                MutantContainTechCategoryID,
                new List<string> { ControlDepartmentConsoleConfig.ID },
                new List<string> { BasicMutantContainTechID },
                new Dictionary<string, float> { { "basic", 200f } },
                searchTermKey: "MUTANTER"
            );
            return advancedMutantContainTech;
        }
    }
}
