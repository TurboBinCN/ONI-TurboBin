using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    public class MagicHealSpellVFXController : KMonoBehaviour
    {
        private KBatchedAnimController animController;

        override protected void OnSpawn()
        {
            base.OnSpawn();
        }

        public void ActivateVFX(GameObject target = null)
        {
            TbbDebuger.LogDebug($"MagicHealSpellVFXController:激活 {target?.name ?? "无目标"}");
            if (target == null)
                return;

            // 确定播放位置
            Vector3 position = target.transform.position;

            // 创建并播放治疗特效动画
            if (animController != null)
            {
                Util.KDestroyGameObject(animController.gameObject);
            }

            // 使用 FXHelpers 创建动画效果
            animController = FXHelpers.CreateEffect(
                "magic_heal_spell_kanim", // 假设的动画文件名称
                position,
                null,
                false,
                Grid.SceneLayer.Front,
                false
            );

            if (animController != null)
            {
                // 设置自动销毁
                animController.destroyOnAnimComplete = true;
                // 播放动画
                animController.Play((HashedString)"loop", KAnim.PlayMode.Loop);
            }
            else
            {
                TbbDebuger.LogWarning("MagicHealSpellVFXController:初始化失败");
            }
        }

        public void Deactivate()
        {
            if (animController != null)
            {
                Util.KDestroyGameObject(animController.gameObject);
                animController = null;
            }
        }


    }
}