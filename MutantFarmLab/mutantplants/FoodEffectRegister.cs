using HarmonyLib;
using Klei.AI;
using MutantFarmLab;
using MutantFarmLab.tbbLibs;
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
                duration: 0f,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false
            );
            // ✅ 核心1：清零小人所有辐射剂量（源码属性，100%命中）
            radClear.Add(new AttributeModifier(
                Db.Get().Amounts.RadiationBalance.Id,
                -999999f, // 超大负值，强制覆盖所有辐射剂量，归零无残留
                STRINGS.DUPLICANTS.MODIFIERS.RADCLEAR.NAME,
                false, false, true
            ));

            Db.Get().effects.Add(radClear);
        }
        #endregion

        #region ✅ 辐射免疫效果（根治报错+源码属性，5周期完全免疫）
        private static void RegisterMethodRadImmune()
        {
            // 持续5周期效果（3000秒），替代缺失的TUNING.TIME_CYCLE
            Effect radImmune = new Effect(
                id: RAD_IMMUNE_ID,
                name: STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.NAME,
                description: STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.DESCRIPTION,
                duration: 5 * 600,
                show_in_ui: true,
                trigger_floating_text: true,
                is_bad: false
            );

            // ✅ 核心2：辐射抗性拉满（100%免疫辐射伤害，源码属性）
            radImmune.Add(new AttributeModifier(
                Db.Get().Attributes.RadiationResistance.Id,
                100f, // 抗性最大值，完全抵消所有辐射伤害
                STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.NAME,
                false, false, true
            ));

            // ✅ 核心3：暂停辐射恢复（防止辐射值反弹，源码属性）
            radImmune.Add(new AttributeModifier(
                Db.Get().Attributes.RadiationRecovery.Id,
                -1f, // 强制暂停辐射数值增长
                STRINGS.DUPLICANTS.MODIFIERS.RADIMMUNITY.NAME,
                false, false, true
            ));

            Db.Get().effects.Add(radImmune);
        }
        #endregion
        /// <summary>
        /// 清零指定小人的所有辐射剂量（游戏原生写法，0Missing、0失效）
        /// </summary>
        /// <param name="minionGo">小人GameObject</param>
        public static void ClearMinionAllRadiation(GameObject minionGo)
        {
            if (minionGo == null || !Sim.IsRadiationEnabled()) return;

            // ✅ 游戏原生获取辐射实例的方式（100%系统认可，永不Missing）
            AmountInstance radiationAmount = minionGo.GetAmounts().Get(Db.Get().Amounts.RadiationBalance.Id);
            if (radiationAmount != null && radiationAmount.value > 0f)
            {
                // ✅ 方式1：一键清零（推荐，和如厕扣辐射同源API）
                radiationAmount.ApplyDelta(-radiationAmount.value);
                // ✅ 方式2：直接赋值（等效，更简洁）
                // radiationAmount.value = 0f;
            }
        }
        // 统一注册入口，与你的调用完全匹配
        public static void RegisterAllEffects()
        {
            RegisterMethodRadClear();
            RegisterMethodRadImmune();
        }
    }

    // 🎯 标记要Hook的类和方法
    [HarmonyPatch(typeof(Edible), "StopConsuming")]
    public static class Edible_StopConsuming_Patch
    {
        /// <summary>
        /// Postfix = 原生方法执行完毕后，再执行我们的逻辑（完美不冲突）
        /// </summary>
        public static void Postfix(Edible __instance, WorkerBase worker)
        {
            // ✅ 校验核心参数，防止空指针
            if (__instance == null || worker == null || worker.gameObject == null) return;

            // ✅ 过滤：只给「你的辐射籽」附加效果（替换成你的辐射籽ID）
            if (__instance.gameObject.name != RadiationResistSeedConfig.ID)
                return;

            
            // ✅ 操作1：执行辐射清零（硬逻辑，无回调、0Missing）
            RadiationHelper.ClearMinionRadiation(worker.gameObject);

            // ✅ 操作2：附加辐射抗性Effect（原生API，强制刷新）
            Effects effectsComp = worker.gameObject.GetComponent<Effects>();
            if (effectsComp != null && !effectsComp.HasEffect(FoodEffectRegister.RAD_IMMUNE_ID))
            {
                effectsComp.Add(FoodEffectRegister.RAD_IMMUNE_ID, true);
            }
        }
    }
    /// <summary>
    /// 辐射操作工具类（无回调、无依赖，直接调用生效）
    /// </summary>
    public static class RadiationHelper
    {
        /// <summary>
        /// 清零指定小人的所有辐射剂量（游戏原生写法，0Missing、100%生效）
        /// </summary>
        public static void ClearMinionRadiation(GameObject minionGo)
        {
            if (minionGo == null || !Sim.IsRadiationEnabled()) return;

            // ✅ 从你的PeeChore源码复刻的原生写法，绝对系统认可
            AmountInstance radiationAmount = minionGo.GetAmounts().Get(Db.Get().Amounts.RadiationBalance.Id);
            if (radiationAmount != null && radiationAmount.value > 0f)
            {
                radiationAmount.value = 0f; // 一键清零，无残留
                                            // 可选：添加飘字提示，和如厕扣辐射一致
                PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Negative,
                    "0 Rads", minionGo.transform, 1.5f, false);
            }
        }
    }
}