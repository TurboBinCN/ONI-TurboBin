using HarmonyLib;
using KMod;
using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using MutantContainmentProject.MutanterEffect;
using MutantContainmentProject.MutanterStoryStraits;
using MutantContainmentProject.MutanterTraits;
using MutantContainmentProject.Patches;
using MutantContainmentProject.Room;
using MutantContainmentProject.SideScreen;
using MutantContainmentProject.Skills;
using PeterHan.PLib.Core;
using System.IO;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Module;
using TBB.He.TbbLib.SingleToneInstance;
using TBB.He.TbbLib.UI;
using TBB.He.TbbLib.Utils;
using UnityEngine;
using static GravitasMutanterFounder;

namespace MutantContainmentProject
{
    public class MutantContainmentProjectMod
    {
        public class MutantContainmentProject : UserMod2
        {
            public override void OnLoad(Harmony harmony)
            {
                base.OnLoad(harmony);

                TbbDebuger.GlobalLogLevel = TbbDebuger.LogLevel.Debug;

                PUtil.InitLibrary();

                TbbAssetsUtils.Initialize(mod, harmony);

                AssetBundle assetBundle = TbbAssetBundle.LoadAssetBundle("", "mutant_containment_project", Path.Combine(mod.ContentPath, "assets/assetsBundle"));

                TbbAssets.Initialize(mod, harmony)
                    .AddSprite(("gravitas_mutanter_founder_image"), assetBundle)
                    .AddSprite(("gravitas_mutanter_founder_icon"), assetBundle)
                    .AddSprite(("skillbadge_role_bravery1"), assetBundle)
                    .AddSprite(("skillbadge_role_bravery2"), assetBundle)
                    .AddSprite(("skillbadge_role_bravery3"), assetBundle)
                    .AddSprite(("skillbadge_role_discipline1"), assetBundle)
                    .AddSprite(("skillbadge_role_discipline2"), assetBundle)
                    .AddSprite(("skillbadge_role_discipline3"), assetBundle)
                    .AddSprite(("skillbadge_role_metal_resistance1"), assetBundle)
                    .AddSprite(("skillbadge_role_metal_resistance2"), assetBundle)
                    .AddSprite(("skillbadge_role_metal_resistance3"), assetBundle)
                    .AddSprite(("skillbadge_role_righteousness1"), assetBundle)
                    .AddSprite(("skillbadge_role_righteousness2"), assetBundle)
                    .AddSprite(("skillbadge_role_righteousness3"), assetBundle)
                    .AddSprite(("icon_errand_righteousness"), assetBundle)
                    .AddSprite(("icon_errand_discipline"), assetBundle)
                    .AddSprite(("icon_errand_metal_resistance"), assetBundle)
                    .AddSprite(("icon_errand_bravery"), assetBundle)
                    .AddSprite(("icon_archetype_bravery"), assetBundle)
                    .AddSprite(("icon_archetype_metal_resistance"), assetBundle)
                    .AddSprite(("icon_archetype_discipline"), assetBundle)
                    .AddSprite(("icon_archetype_righteousness"), assetBundle)
                    .AddSprite(("icon_benneng"), assetBundle)
                    .AddSprite(("icon_dongcha"), assetBundle)
                    .AddSprite(("icon_goutong"), assetBundle)
                    .AddSprite(("icon_yapo"), assetBundle);

                //语言本地化
                TbbLocalization.Initialize(mod, harmony)
                    .RegisterLoad(typeof(STRINGS))
                    .RegisterAddStrings(typeof(STRINGS.UI))
                    .RegisterAddStrings(typeof(STRINGS.MISC))
                    .RegisterAddStrings(typeof(STRINGS.ROOMS))
                    .RegisterAddStrings(typeof(STRINGS.CODEX))
                    .RegisterAddStrings(typeof(STRINGS.MUTANTERS))
                    .RegisterAddStrings(typeof(STRINGS.CREATURES))
                    .RegisterAddStrings(typeof(STRINGS.DUPLICANTS))
                    .RegisterAddStrings(typeof(STRINGS.SKILLS))
                    .RegisterAddStrings(typeof(STRINGS.ENTITY))
                    .RegisterAddStrings(typeof(STRINGS.EFFECTS))
                    .RegisterAddStrings(typeof(STRINGS.BUILDINGS))
                    .RegisterAddStrings(typeof(STRINGS.SECURE_ACTION));
                //故事特质
                TbbStoryTraits.Initialize(mod, harmony)
                    .ADD("MutanterFounder");
                //故事
                TbbStories.Initialize(mod, harmony)
                    .Add(MutanterStoris.StoryGravitasMutanterFounder);
                //百科词条
                TbbCodexEntries.Initialize(mod, harmony)
                    .ADD("SCP173", "Creatures")
                    .ADD("SCP096", "Creatures")
                    .ADD("SCP662", "IndustrialIngredients")
                    .ADD("SCP662_1", "Creatures")
                    .ADD("SCP049", "Creatures")
                    .ADD("SCP049_2", "Creatures")
                    .ADD("SCP939", "Creatures");
                //房间
                TbbRoom.Initialize(mod, harmony)
                    .Add(ContainmentCharmberRoom.Register);
                //特性
                TbbTraits.Initialize(mod, harmony)
                    .Add(MutanterTraitDb.PsychologicalTrait);
                //Effect
                TbbEffect.Initialize(mod, harmony)
                    .Add(MutanterEffects.MutanterContainedEffect)
                    .Add(MutanterEffects.MutanterWilledEffect)
                    .Add(MutanterEffects.MutanterChaseEffect)
                    .Add(MutanterEffects.MutanterAttackRestrictedEffect)
                    .Add(MutanterEffects.MutanterAttackEnhancedEffect)
                    .Add(MutanterEffects.SCP939AmnesiaEffect);
                //小人属性
                TbbDuplicantsAttributes.Initialize(mod, harmony)
                    .Add(MutanterAttributes.AttributeBravery, MutanterAttributes.AttributeBraveryID)
                    .Add(MutanterAttributes.AttributeDefense, MutanterAttributes.AttributeDefenseID)
                    .Add(MutanterAttributes.AttributeDiscipline, MutanterAttributes.AttributeDisciplineID)
                    .Add(MutanterAttributes.AttributeRighteousness, MutanterAttributes.AttributeRighteousnessID);
                
                //属性转换器
                TbbAttributeConverters.Initialize(mod,harmony)
                    .Add(MutanterAttributeConverters.RegisterAttributeConverters);
                //ChoreGroups
                TbbChoreGroups.Initialize(mod, harmony)
                    .Add(MutanterChoreGroups.ChoreGroupContain)
                    .Add(MutanterChoreGroups.ChoreGroupBravery)
                    .Add(MutanterChoreGroups.ChoreGroupDefense)
                    .Add(MutanterChoreGroups.ChoreGroupDiscipline)
                    .Add(MutanterChoreGroups.ChoreGroupRighteousness);
                //ChoreType
                TbbChoreTypes.Initialize(mod, harmony)
                    .Add(MutanterChoreTypes.ChoreTypeContain)
                    .Add(MutanterChoreTypes.ChoreTypeDefense)
                    .Add(MutanterChoreTypes.ChoreTypeDiscipline)
                    .Add(MutanterChoreTypes.ChoreTypeRighteousness);
                //SkillGroup
                TbbSkillGroups.Initialize(mod, harmony)
                    .Add(MutanterSkillGroups.SkillGroupBravery)
                    .Add(MutanterSkillGroups.SkillGroupDefense)
                    .Add(MutanterSkillGroups.SkillGroupDiscipline)
                    .Add(MutanterSkillGroups.SkillGroupRighteousness);
                //SkillPerk 技能特性
                TbbSkillPerks.Initialize(mod, harmony)
                    .Add(MutanterSkillPerks.SkillPerkContain)
                    .Add(MutanterSkillPerks.SkillDefensePower)
                    .Add(MutanterSkillPerks.SkillDisciplinePower)
                    .Add(MutanterSkillPerks.SkillRighteousnessPower);
                //技能
                TbbSkills.Initialize(mod, harmony)
                    .Add(MutanterSkills.SkillBraveryI)
                    .Add(MutanterSkills.SkillBraveryII)
                    .Add(MutanterSkills.SkillBraveryIII)
                    .Add(MutanterSkills.SkillDefenseI)
                    .Add(MutanterSkills.SkillDefenseII)
                    .Add(MutanterSkills.SkillDefenseIII)
                    .Add(MutanterSkills.SkillDisciplineI)
                    .Add(MutanterSkills.SkillDisciplineII)
                    .Add(MutanterSkills.SkillDisciplineIII)
                    .Add(MutanterSkills.SkillRighteousnessI)
                    .Add(MutanterSkills.SkillRighteousnessII)
                    .Add(MutanterSkills.SkillRighteousnessIII);

                //UI布局
                TbbSideScreen.Initialize(mod, harmony)
                    .CopyAndCreate<GeneticAnalysisStationSideScreen, ContainmentMonitorSideScreen>();
                //StatusItems
                TbbStatusItems.Initialize(mod, harmony)
                    .Add<MutanterStatusItems>();
                //建筑
                TbbBuilding.Initialize(mod, harmony)
                    .ToAdvanced()
                    .PlanAndTech(TbbTypes.PlanMenuCategory.Stations, TbbTypes.PlanMenuSubcategory.Farming, TbbTypes.Technology.Food.Bioengineering)
                    .AddBuilding(ContainmentMonitorStationConfig.ID)
                    .PlanAndTech(TbbTypes.PlanMenuCategory.Stations, TbbTypes.PlanMenuSubcategory.Farming, TbbTypes.Technology.Food.Bioengineering)
                    .AddBuilding(ContainmentTileConfig.ID)
                    .PlanAndTech(TbbTypes.PlanMenuCategory.Stations, TbbTypes.PlanMenuSubcategory.Farming, TbbTypes.Technology.Food.Bioengineering)
                    .AddBuilding(GlobalErosionPlanelConfig.ID);
                //建筑StatusItems
                TbbBuildingStatusItems.Initialize(mod, harmony)
                    .Add(GravitasMutanterFounderBuildingStatusItems.Instance.CreateStatusItems)
                    .Add(ContainmentMonitorBuildingStatusItems.Instance.CreateStatusItems);
                //全局单例
                TbbSingleTone.Initialize(mod, harmony)
                    .Add<MutanterSpeciesCatalog>()
                    .Add<GlobalErosionManager>();

            }
        }
    }
}
