using HarmonyLib;
using KMod;
using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using MutantContainmentProject.MutanterEffect;
using MutantContainmentProject.MutanterStoryStraits;
using MutantContainmentProject.MutanterTraits;
using MutantContainmentProject.Room;
using MutantContainmentProject.SideScreen;
using MutantContainmentProject.Skills;
using PeterHan.PLib.Core;
using TBB.He.TbbLib.Debuger;
using TBB.He.TbbLib.Module;
using TBB.He.TbbLib.SingleToneInstance;
using TBB.He.TbbLib.UI;
using TBB.He.TbbLib.Utils;
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
                    .RegisterAddStrings(typeof(STRINGS.CHOREGROUPS))
                    .RegisterAddStrings(typeof(STRINGS.CHORES))
                    .RegisterAddStrings(typeof(STRINGS.SKILLS))
                    .RegisterAddStrings(typeof(STRINGS.SKILLGROUP))
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
                    .ADD("SCP173", "Creatures");
                //房间
                TbbRoom.Initialize(mod, harmony)
                    .Add(ContainmentCharmberRoom.Register);
                //特性
                TbbTraits.Initialize(mod, harmony)
                    .Add(MutanterTraitDb.PsychologicalTrait);
                //Effect
                TbbEffect.Initialize(mod, harmony)
                    .Add(MutanterEffects.MutanterContainedEffect)
                    .Add(MutanterEffects.MutanterWilledEffect);
                //小人属性
                TbbDuplicantsAttributes.Initialize(mod, harmony)
                    .Add(MutanterAttributes.AttributeBravery, MutanterAttributes.AttributeBraveryID)
                    .Add(MutanterAttributes.AttributeWillPower, MutanterAttributes.AttributeWillPowerID)
                    .Add(MutanterAttributes.AttributeSuccessRate, MutanterAttributes.AttributeSuccessRateID)
                    .Add(MutanterAttributes.AttributeWorkingSpeed, MutanterAttributes.AttributeWorkingSpeedID)
                    .Add(MutanterAttributes.AttributeAttackDamage, MutanterAttributes.AttributeAttackDamageID);
                //ChoreGroups
                TbbChoreGroups.Initialize(mod, harmony)
                    .Add(MutanterChoreGroups.ChoreGroupContain)
                    .Add(MutanterChoreGroups.ChoreGroupWillPower)
                    .Add(MutanterChoreGroups.ChoreGroupDiscipline)
                    .Add(MutanterChoreGroups.ChoreGroupRighteousness);
                //ChoreType
                TbbChoreTypes.Initialize(mod, harmony)
                    .Add(MutanterChoreTypes.ChoreTypeContain)
                    .Add(MutanterChoreTypes.ChoreTypeWillPower)
                    .Add(MutanterChoreTypes.ChoreTypeDiscipline)
                    .Add(MutanterChoreTypes.ChoreTypeRighteousness);
                //SkillGroup
                TbbSkillGroups.Initialize(mod, harmony)
                    .Add(MutanterSkillGroups.SkillGroupBravery)
                    .Add(MutanterSkillGroups.SkillGroupWillPower)
                    .Add(MutanterSkillGroups.SkillGroupDiscipline)
                    .Add(MutanterSkillGroups.SkillRighteousness);
                //SkillPerk 技能特性
                TbbSkillPerks.Initialize(mod, harmony)
                    .Add(MutanterSkillPerks.SkillPerkContain)
                    .Add(MutanterSkillPerks.SkillPerkWillPower)
                    .Add(MutanterSkillPerks.SkillSuccessRatePower)
                    .Add(MutanterSkillPerks.SkillWorkingSpeedPower)
                    .Add(MutanterSkillPerks.SkillAttackDamagePower);
                //技能
                TbbSkills.Initialize(mod, harmony)
                    .Add(MutanterSkills.SkillBraveryI)
                    .Add(MutanterSkills.SkillBraveryII)
                    .Add(MutanterSkills.SkillBraveryIII)
                    .Add(MutanterSkills.SkillCautionI)
                    .Add(MutanterSkills.SkillCautionII)
                    .Add(MutanterSkills.SkillCautionIII)
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
                    .AddBuilding(ContainmentTileConfig.ID);
                //建筑StatusItems
                TbbBuildingStatusItems.Initialize(mod, harmony)
                    .Add(GravitasMutanterFounderBuildingStatusItems.Instance.CreateStatusItems);
                //全局单例
                TbbSingleTone.Initialize(mod, harmony)
                    .Add<MutanterSpeciesCatalog>();

            }
        }
    }
}
