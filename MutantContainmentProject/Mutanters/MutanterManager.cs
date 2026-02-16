using MutantContainmentProject.MutanterComponent;
using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.Mutanters
{
    public class MutanterManager
    {
        public void SpawnMutanter(string speciesID, Vector3 position)
        {
            // 这里可以添加生成畸变体的逻辑
            // 例如，使用 Assets.GetPrefab() 获取预制件，然后实例化它
            GameObject prefab = Assets.GetPrefab(speciesID);
            if (prefab != null)
            {
                GameObject instance = GameUtil.KInstantiate(prefab, position, Grid.SceneLayer.Creatures);
                MutanterSpeciesCatalog.Instance.RegisterMutanterSpecies(instance.PrefabID());
            }
            else
            {
                TbbDebuger.LogDebug($"无法找到畸变体预制件: {speciesID}");
            }
        }
    }
}
