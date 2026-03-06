using UnityEngine;
using System.Collections.Generic;
using System;
using TBB.He.TbbLib.Debuger;

namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterAnimationDebugger : KMonoBehaviour
    {
        private KBatchedAnimController animController;
        private List<string> animationNames = new List<string> {
            "floor_floor_2_1",
            "floor_floor_1_1",
            "floor_floor_1_0_pre",
            "floor_floor_1_0_loop",
            "floor_floor_1_0_pst"
        };
        private int currentAnimationIndex = 0;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            animController = GetComponent<KBatchedAnimController>();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            TbbDebuger.LogDebug("MutanterAnimationDebugger OnSpawn()");
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
            TbbDebuger.LogDebug("=== Mutanter Animation Frame Counts ===");
            foreach (string animName in animationNames)
            {
                try
                {
                    var anim = animController.GetAnim(animName);
                    if (anim != null)
                    {
                        int frameCount = anim.numFrames;
                        float length = anim.totalTime;
                        TbbDebuger.LogDebug($"Animation: {animName}, Frames: {frameCount}, Length: {length}s");
                    }
                    else
                    {
                        TbbDebuger.LogDebug($"Animation not found: {animName}");
                    }
                }
                catch (Exception e)
                {
                    TbbDebuger.LogError($"Error checking animation {animName}: {e.Message}");
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
            TbbDebuger.LogDebug($"Playing animation: {animName}");
            
            try
            {
                animController.Play(animName, KAnim.PlayMode.Loop);
                currentAnimationIndex = (currentAnimationIndex + 1) % animationNames.Count;
            }
            catch (Exception e)
            {
                TbbDebuger.LogError($"Error playing animation {animName}: {e.Message}");
                currentAnimationIndex = (currentAnimationIndex + 1) % animationNames.Count;
            }
        }
    }
}