using System;
using System.Collections.Generic;

namespace TBB.He.TbbLib.Debuger
{
    public class TbbAnimationDebugger : KMonoBehaviour
    {
        private KBatchedAnimController animController;
        private List<string> animationNames = new List<string>();
        private int currentAnimationIndex = 0;

        // 可在Inspector中设置的动画名称列表
        public List<string> AnimationNames = new List<string>();

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            animController = GetComponent<KBatchedAnimController>();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            TbbDebuger.LogDebug("TbbAnimationDebugger OnSpawn()");

            // 如果Inspector中没有设置动画名称，使用默认动画
            if (AnimationNames.Count == 0)
            {
                animationNames.AddRange(new List<string> {
                    "idle",
                    "working",
                    "hit",
                    "run"
                });
            }
            else
            {
                animationNames = AnimationNames;
            }

            LogAnimationFrameCounts();

            // 添加点击事件监听
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                Subscribe(-1503271301, OnSelected);
            }
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                Unsubscribe(-1503271301);
            }
        }

        private void LogAnimationFrameCounts()
        {
            TbbDebuger.LogDebug("=== Tbb动画帧计数 ===");
            foreach (string animName in animationNames)
            {
                try
                {
                    var anim = animController.GetAnim(animName);
                    if (anim != null)
                    {
                        int frameCount = anim.numFrames;
                        float length = anim.totalTime;
                        TbbDebuger.LogDebug($"动画: {animName}, 帧数: {frameCount}, 长度: {length}s");
                    }
                    else
                    {
                        TbbDebuger.LogDebug($"未找到动画: {animName}");
                    }
                }
                catch (Exception e)
                {
                    TbbDebuger.LogError($"检查动画 {animName} 时出错: {e.Message}");
                }
            }
        }

        private void OnSelected(object obj)
        {
            PlayNextAnimation();
        }

        private void PlayNextAnimation()
        {
            if (animController == null)
                return;

            string animName = animationNames[currentAnimationIndex];
            TbbDebuger.LogDebug($"播放动画: {animName}");
            
            try
            {
                animController.Play(animName, KAnim.PlayMode.Loop);
                currentAnimationIndex = (currentAnimationIndex + 1) % animationNames.Count;
            }
            catch (Exception e)
            {
                TbbDebuger.LogError($"播放动画 {animName} 时出错: {e.Message}");
                currentAnimationIndex = (currentAnimationIndex + 1) % animationNames.Count;
            }
        }

        // 公开方法，允许外部调用播放下一个动画
        public void PlayNext()
        {
            PlayNextAnimation();
        }

        // 公开方法，允许外部设置动画列表
        public void SetAnimationNames(List<string> names)
        {
            if (names != null && names.Count > 0)
            {
                animationNames = names;
                currentAnimationIndex = 0;
                LogAnimationFrameCounts();
            }
        }
    }
}