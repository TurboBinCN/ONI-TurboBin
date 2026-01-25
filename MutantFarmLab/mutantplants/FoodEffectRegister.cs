using HarmonyLib;
using Klei.AI;
using PeterHan.PLib.Core;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public static class FoodEffectRegister
    {
        // 效果ID与文本注入严格对齐
        public const string RAD_CLEAR_ID = "RadClear";
        public const string RAD_IMMUNE_ID = "RadImmunity";

        #region ✅ 辐射清零效果（100%生效，源码属性+强制归零）
        private static void RegisterMethodRadClear()
        {
            // 瞬时效果：生效后立即消失，无时长、无UI残留
            Effect radClear = new Effect(
                id: RAD_CLEAR_ID,
                name: STRINGS.DUPLICANTS.MODIFIERS.RADCLEAR.NAME,
                description: STRINGS.DUPLICANTS.MODIFIERS.RADCLEAR.DESCRIPTION,
                duration: 60f,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false
            );

            Db.Get().effects.Add(radClear);
        }
        #endregion

        #region ✅ 辐射免疫效果（5周期完全免疫）
        private static void RegisterMethodRadImmune()
        {
            // 持续5周期效果（3000秒）
            Effect radImmune = new Effect(
                id: RAD_IMMUNE_ID,
                name: STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.NAME,
                description: STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.DESCRIPTION,
                duration: 5 * 600,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false
            );

            //辐射抗性
            radImmune.Add(new AttributeModifier(
                Db.Get().Attributes.RadiationResistance.Id,
                100f, // 抗性最大值，完全抵消所有辐射伤害
                STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.NAME,
                false, false, true
            ));

            //暂停辐射恢复
            radImmune.Add(new AttributeModifier(
                Db.Get().Attributes.RadiationRecovery.Id,
                -1f, // 强制暂停辐射数值增长
                STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.NAME,
                false, false, true
            ));

            Db.Get().effects.Add(radImmune);
        }
        #endregion
        // 统一注册入口，与你的调用完全匹配
        public static void RegisterAllEffects()
        {
            RegisterMethodRadClear();
            RegisterMethodRadImmune();
        }
    }

    /// <summary>
    /// 辐射籽生效分类两种情况
    /// 1. 以下Patch中 直接附加effect，触发在小人吃完辐射籽后
    /// 2. RadSeedEatWorkable中，由Workable创建Chore强制小人服用后，附加Effect
    /// </summary>
    [HarmonyPatch(typeof(Edible), "StopConsuming")]
    public static class Edible_StopConsuming_Patch
    {
        /// <summary>
        /// Postfix = 原生方法执行完毕后，再执行我们的逻辑（完美不冲突）
        /// </summary>
        public static void Postfix(Edible __instance, WorkerBase worker)
        {
            if (__instance == null || worker == null || worker.gameObject == null) return;

            if (__instance.gameObject.name != RadiationResistSeedConfig.ID)
                return;

            PUtil.LogDebug($"Edible topConsuming [{worker.name}]");
            Effects effects = worker.GetComponent<Effects>();
            EffectInstance effectInstance = effects?.Get(FoodEffectRegister.RAD_IMMUNE_ID);
            if (effectInstance != null)
            {
                effectInstance.timeRemaining = effectInstance.effect.duration;
            }
            else
            {
                effects.Add(FoodEffectRegister.RAD_IMMUNE_ID, true);
            }
            RadiationHelper.ClearMinionRadiation(worker.gameObject);

            Sicknesses sicknesses = worker.GetSicknesses();
            SicknessInstance sicknessInstance = sicknesses.Get(Db.Get().Sicknesses.RadiationSickness);
            if (sicknessInstance != null)
            {
                Game.Instance.savedInfo.curedDisease = true;
                sicknessInstance.Cure();
            }
        }
    }
    /// <summary>
    /// 辐射操作工具类（无回调、无依赖，直接调用生效）
    /// </summary>
    public static class RadiationHelper
    {
        /// <summary>
        /// 清零指定小人的所有辐射剂量
        /// </summary>
        public static void ClearMinionRadiation(GameObject minionGo)
        {
            if (minionGo == null || !Sim.IsRadiationEnabled()) return;

            AmountInstance radiationAmount = minionGo.GetAmounts().Get(Db.Get().Amounts.RadiationBalance.Id);
            if (radiationAmount != null && radiationAmount.value > 0f)
            {
                float value = radiationAmount.value;
                radiationAmount.value = 0f; // 一键清零，无残留
                                            // 添加飘字提示
                PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Negative,
                    $"- {value} Rads", minionGo.transform, 1.5f, false);
            }
        }
    }
}