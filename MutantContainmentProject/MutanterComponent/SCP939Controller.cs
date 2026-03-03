using System.Collections.Generic;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class SCP939Controller : KMonoBehaviour, ISim1000ms
    {
        private float aerosolTimer = 0f;
        private float aerosolInterval = 5f;
        private float soundMimicTimer = 0f;
        private float soundMimicInterval = 10f;

        private EmotionMonitor.StatesInstance emotionMonitorSMI;
        private MutanterAttackSystem attackSystem;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            emotionMonitorSMI = gameObject.GetSMI<EmotionMonitor.StatesInstance>();
            attackSystem = GetComponent<MutanterAttackSystem>();
        }
        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }
        public void Sim1000ms(float dt)
        {
            Update();
        }
        private void Update()
        {
            // 释放记忆消除气雾
            aerosolTimer += Time.deltaTime;
            if (aerosolTimer >= aerosolInterval)
            {
                ReleaseAerosol();
                aerosolTimer = 0f;
            }

            // 声音模仿
            soundMimicTimer += Time.deltaTime;
            if (soundMimicTimer >= soundMimicInterval)
            {
                MimicSound();
                soundMimicTimer = 0f;
            }
        }

        private void ReleaseAerosol()
        {
            //TODO 气体病毒UI
            // 使用EmotionMonitor检测周围的复制人
            List<KPrefabID> threaters = emotionMonitorSMI?.GetThreaters();
            if (threaters != null)
            {
                foreach (KPrefabID threater in threaters)
                {
                    if (threater != null && threater.gameObject != null)
                    {
                        // 通过MutanterAttackSystem执行效果攻击
                        attackSystem?.ExecuteEffectAttack(threater.gameObject, "SCP939Amnesia");

                        // 添加SCP939Amnesia组件到复制人身上，实现周期性入睡
                        if (!threater.gameObject.GetComponent<SCP939Amnesia>())
                        {
                            threater.gameObject.AddComponent<SCP939Amnesia>();
                        }
                    }
                }
            }
        }

        private void MimicSound()
        {
            // 显示思维泡泡
            if (NameDisplayScreen.Instance != null)
            {
                // 使用普通思维泡泡，设置气泡精灵和图标
                NameDisplayScreen.Instance.SetThoughtBubbleDisplay(
                    gameObject,
                    true,
                    "attract", // 这个文本会显示为悬停提示
                    Assets.GetSprite((HashedString)"bubble_conversation"),
                    Assets.GetSprite((HashedString)"crew_state_music")
                );

                StartCoroutine(HideThoughtBubble());
            }

            // 播放声音效果（这里需要根据游戏的音频系统实现）
            // 实际实现中需要调用游戏的音频播放API
        }

        private System.Collections.IEnumerator HideThoughtBubble()
        {
            yield return new WaitForSeconds(60f);
            if (NameDisplayScreen.Instance != null)
            {
                NameDisplayScreen.Instance.SetThoughtBubbleDisplay(
                    gameObject,
                    false,
                    string.Empty,
                    null,
                    null
                );
            }
        }

    }
}