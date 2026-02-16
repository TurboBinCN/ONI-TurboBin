using System.Collections.Generic;
using UnityEngine;

namespace TBB.He.TbbLib.UI
{
    public class TbbRangeVisualizer : KMonoBehaviour
    {
        public Color highlightColor = Color.red;
        public List<int> targetCells = new();

        protected override void OnSpawn()
        {
            base.OnSpawn();
            CameraController.Instance.overlayNoDepthCamera.FindOrAddComponent<TbbRangeVisualizerEffect>();
        }
        protected override void OnCleanUp()
        {
            targetCells.Clear();
            base.OnCleanUp();
        }
        public void SetTargetCells(List<int> cells)
        {
            targetCells = new List<int>(cells);
        }
        public void SetHightlightColor(Color color)
        {
            highlightColor = color;
        }
    }
}
