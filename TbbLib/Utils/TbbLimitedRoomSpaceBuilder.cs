using System;
using System.Collections.Generic;
using TBB.He.TbbLib.Debuger;

namespace TBB.He.TbbLib.Utils
{
    /// <summary>
    /// 有限空间房间构建器，从一个起始格子开始，探测指定格子数距离内的连通区域。
    /// </summary>
    public static class TbbLimitedRoomSpaceBuilder
    {
        /// <summary>
        /// 构建一个受格子距离限制的连通房间格子集合。
        /// </summary>
        /// <param name="startCell">起始的格子ID。</param>
        /// <param name="maxCellDistance">允许的最大格子距离（曼哈顿距离）。</param>
        /// <returns>构成连通房间的格子坐标的列表。</returns>
        public static List<int> BuildRoom(int startCell, int maxCellDistance)
        {
            if (!Grid.IsValidCell(startCell) || IsCavityBoundary(startCell))
            {
                //TbbDebuger.LogDebug($"LimitedRoomSpaceBuilder: 起始格子 {startCell} 无效或为边界。");
                return new List<int>();
            }

            // 获取起始坐标
            Grid.CellToXY(startCell, out int startX, out int startY);

            var roomCells = new List<int>();
            var visited = new HashSet<int>();
            var queue = new Queue<(int cellId, int x, int y, int distFromStart)>();

            // 将起始信息入队
            queue.Enqueue((startCell, startX, startY, 0));
            visited.Add(startCell);

            // 定义四个方向的偏移量
            var offsets = new (int dx, int dy)[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

            while (queue.Count > 0)
            {
                var (currentCell, currentX, currentY, currentDist) = queue.Dequeue();

                // 将当前格子加入房间
                roomCells.Add(currentCell);

                // 探索四个方向的邻居
                foreach (var (dx, dy) in offsets)
                {
                    // 使用 Grid.OffsetCell 计算邻居格子ID
                    var neighborCell = Grid.OffsetCell(currentCell, dx, dy);

                    if (!Grid.IsValidCell(neighborCell) || visited.Contains(neighborCell))
                        continue;

                    // 计算邻居格子到起始点的新曼哈顿距离 (格子距离)
                    var neighborX = currentX + dx;
                    var neighborY = currentY + dy;
                    var newDistance = Math.Abs(neighborX - startX) + Math.Abs(neighborY - startY);

                    // 如果超出距离限制，则跳过
                    if (newDistance > maxCellDistance)
                        continue;

                    // 如果邻居是边界（固体、地基或门），则跳过
                    if (IsCavityBoundary(neighborCell))
                        continue;

                    // 否则，将邻居信息加入队列和已访问集合
                    queue.Enqueue((neighborCell, neighborX, neighborY, newDistance));
                    visited.Add(neighborCell);
                }
            }

            return roomCells;
        }

        /// <summary>
        /// 判断一个格子是否为腔体（房间）的边界。
        /// </summary>
        /// <param name="cell">要检查的格子ID。</param>
        /// <returns>如果是边界则返回true，否则返回false。</returns>
        private static bool IsCavityBoundary(int cell)
        {
            // 检查 BuildMasks 是否设置了 Solid 或 Foundation 标志
            // 或者检查 HasDoor
            return (Grid.BuildMasks[cell] & (Grid.BuildFlags.Solid | Grid.BuildFlags.Foundation)) != 0 || Grid.HasDoor[cell];
        }
    }
}
