using HarmonyLib;
using Unity.Collections;
using UnityEngine;

namespace TbbLib.UI
{
    public class TbbRangeVisualizerEffect : MonoBehaviour
    {
        public Color highlightColor = new Color(0f, 1f, 0.8f, 1f);
        private Material material;
        private Camera myCamera;
        private Texture2D OcclusionTex;
        private int LastVisibleTileCount = -1;

        private void Start()
        {
            this.material = new Material(Shader.Find("Klei/PostFX/Range"));
        }

        void OnPostRender()
        {
            TbbRangeVisualizer tbbRangeVis = null;
            Vector2I u = new Vector2I(0, 0);
            if (SelectTool.Instance.selected != null)
            {
                tbbRangeVis = SelectTool.Instance.selected.GetComponent<TbbRangeVisualizer>();
            }
            if (tbbRangeVis == null || tbbRangeVis.targetCells == null || tbbRangeVis.targetCells.Count == 0)
            {
                if (LastVisibleTileCount != 0) LastVisibleTileCount = 0;
                return;
            }

            if (tbbRangeVis.targetCells.Count == 0) return;

            if (tbbRangeVis.highlightColor != null) highlightColor = tbbRangeVis.highlightColor;

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (int cell in tbbRangeVis.targetCells)
            {
                int x, y;
                Grid.CellToXY(cell, out x, out y);
                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }

            int texWidth = maxX - minX + 1;
            int texHeight = maxY - minY + 1;

            if (this.OcclusionTex == null || this.OcclusionTex.width != texWidth || this.OcclusionTex.height != texHeight)
            {
                if (this.OcclusionTex != null) Object.DestroyImmediate(this.OcclusionTex);
                this.OcclusionTex = new Texture2D(texWidth, texHeight, TextureFormat.Alpha8, false);
                this.OcclusionTex.filterMode = FilterMode.Point;
                this.OcclusionTex.wrapMode = TextureWrapMode.Clamp;
            }

            NativeArray<byte> pixelData = this.OcclusionTex.GetPixelData<byte>(0);
            for (int i = 0; i < pixelData.Length; i++)
            {
                pixelData[i] = 0;
            }

            int numVisibleTiles = 0;

            foreach (int cell in tbbRangeVis.targetCells)
            {
                int x, y;
                Grid.CellToXY(cell, out x, out y);

                int relX = x - minX;
                int relY = y - minY;

                if (relX >= 0 && relX < texWidth && relY >= 0 && relY < texHeight)
                {
                    int index = relY * texWidth + relX;
                    pixelData[index] = byte.MaxValue;
                    numVisibleTiles++;
                }
            }

            this.OcclusionTex.Apply(false, false);

            Vector2I vector2I; Vector2I vector2I2;
            this.FindWorldBounds(out vector2I, out vector2I2);

            Vector2I rangeMin = new Vector2I(0, 0);
            Vector2I rangeMax = new Vector2I(texWidth - 1, texHeight - 1);
            Vector2I originOffset = new Vector2I(minX, minY);

            Vector2I worldPosMin = u + originOffset;
            Vector2I worldPosMax = worldPosMin + rangeMax;

            if (this.myCamera == null)
            {
                this.myCamera = GetComponent<Camera>();
                if (this.myCamera == null) return;
            }

            Ray ray = this.myCamera.ViewportPointToRay(Vector3.zero);
            float distance = Mathf.Abs(ray.origin.z / ray.direction.z);
            Vector3 point = ray.GetPoint(distance);
            Vector4 uvOffsetScale;
            uvOffsetScale.x = point.x;
            uvOffsetScale.y = point.y;
            ray = this.myCamera.ViewportPointToRay(Vector3.one);
            distance = Mathf.Abs(ray.origin.z / ray.direction.z);
            point = ray.GetPoint(distance);
            uvOffsetScale.z = point.x - uvOffsetScale.x;
            uvOffsetScale.w = point.y - uvOffsetScale.y;
            this.material.SetVector("_UVOffsetScale", uvOffsetScale);

            Vector4 rangeParams;
            rangeParams.x = (float)worldPosMin.x;
            rangeParams.y = (float)worldPosMin.y;
            rangeParams.z = (float)(worldPosMax.x + 1);
            rangeParams.w = (float)(worldPosMax.y + 1);
            this.material.SetVector("_RangeParams", rangeParams);
            this.material.SetColor("_HighlightColor", this.highlightColor);

            Vector4 occlusionParams;
            occlusionParams.x = 1f / (float)this.OcclusionTex.width;
            occlusionParams.y = 1f / (float)this.OcclusionTex.height;
            occlusionParams.z = 0f;
            occlusionParams.w = 0f;
            this.material.SetVector("_OcclusionParams", occlusionParams);

            this.material.SetTexture("_OcclusionTex", this.OcclusionTex);

            Vector4 worldParams;
            worldParams.x = (float)Grid.WidthInCells;
            worldParams.y = (float)Grid.HeightInCells;
            worldParams.z = 1f / (float)Grid.WidthInCells;
            worldParams.w = 1f / (float)Grid.HeightInCells;
            this.material.SetVector("_WorldParams", worldParams);

            GL.PushMatrix();
            this.material.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(5);
            GL.Color(Color.white);
            GL.Vertex3(0f, 0f, 0f);
            GL.Vertex3(0f, 1f, 0f);
            GL.Vertex3(1f, 0f, 0f);
            GL.Vertex3(1f, 1f, 0f);
            GL.End();
            GL.PopMatrix();

            if (this.LastVisibleTileCount != numVisibleTiles)
            {
                // SoundEvent.PlayOneShot(GlobalAssets.GetSound("RangeVisualization_movement", false), tbbRangeVis.transform.GetPosition(), 1f);
                this.LastVisibleTileCount = numVisibleTiles;
            }
        }
        private void FindWorldBounds(out Vector2I world_min, out Vector2I world_max)
        {
            if (ClusterManager.Instance != null)
            {
                WorldContainer activeWorld = ClusterManager.Instance.activeWorld;
                world_min = activeWorld.WorldOffset;
                world_max = activeWorld.WorldOffset + activeWorld.WorldSize;
                return;
            }
            world_min.x = 0;
            world_min.y = 0;
            world_max.x = Grid.WidthInCells;
            world_max.y = Grid.HeightInCells;
        }
    }
}
