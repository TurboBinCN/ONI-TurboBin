using Database;
using Epic.OnlineServices.Stats;
using FMOD;
using HarmonyLib;
using Klei.AI;
using MutantFarmLab.tbbLibs;
using PeterHan.PLib.Core;
using STRINGS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using static STRINGS.BUILDING.STATUSITEMS;

namespace MutantFarmLab
{
    /// <summary>
    /// 植物-种子信息实体（适配Klei游戏）
    /// </summary>
    public class PlantSeedInfo
    {
        public Tag PlantID { get; set; }          // 核心：输入的植物ID
        public string PlantName { get; set; }
        public Tag speciesID { get; set; }
        public Tag SeedID { get; set; } // 推导的种子ID
        public string SeedName { get; set; } // 本地化种子名称
        public string SeedPropName { get; set; }
        public bool IsSeedValid { get; set; } = true; // 信息是否有效
    }

    /// <summary>
    /// 植物种子管理器（✅ 对接原生PlantMutations库 + 原生随机变异逻辑）
    /// 100%复用游戏官方变异体系，零硬编码、零兼容问题
    /// </summary>
    public static class PlantSeedManager
    {
        // 缓存：植物ID → 种子信息（原有核心缓存）
        private static readonly Dictionary<Tag, PlantSeedInfo> _plantToSeedCache = new();

        // ✅ 全局缓存：游戏原生变异库实例（仅初始化一次，提升性能）
        private static PlantMutations _nativePlantMutations;
        private static PlantMutations NativePlantMutations
        {
            get
            {
                if (_nativePlantMutations == null)
                    _nativePlantMutations = Db.Get().PlantMutations;
                return _nativePlantMutations;
            }
        }

        #region ✅ 可配置项（轻量化，仅保留核心开关，贴合原生规则）
        /// <summary>
        /// 单次变异数量（原生默认1，推荐1，符合游戏设定）
        /// </summary>
        private const int RANDOM_MUTATION_COUNT = 1;
        #endregion

        public static void ForbiddenAllSeeds(Action<Tag, bool> setForbidden)
        {
            if (setForbidden == null || Assets.GetPrefabsWithTag(GameTags.Seed) == null)
                return;

            foreach (var prefab in Assets.GetPrefabsWithTag(GameTags.Seed))
            {
                if (prefab == null) continue;

                var seedProducer = prefab.GetComponent<SeedProducer>();
                if (seedProducer == null || seedProducer.seedInfo.IsNullOrDestroyed() || seedProducer.seedInfo.seedId == null)
                    continue;
                Tag validSeedTag = seedProducer.seedInfo.seedId;

                if (validSeedTag.IsValid)
                    setForbidden(validSeedTag, true);
            }
            PUtil.LogDebug("[PlantSeedManager] 全种子禁用完成 → 所有种子已加入黑名单");
        }

        #region ========== 原有逻辑：完整保留 植物<->种子 资源映射/缓存 ==========
        public static void InitPlantSeedMapping()
        {
            PlantSubSpeciesCatalog catalog = PlantSubSpeciesCatalog.Instance;
            var discoveredSubspeciesBySpecies = AccessTools.Field(typeof(PlantSubSpeciesCatalog), "discoveredSubspeciesBySpecies").GetValue(catalog);

            if (discoveredSubspeciesBySpecies != null)
            {
                foreach (var (tag, infos) in (Dictionary<Tag, List<PlantSubSpeciesCatalog.SubSpeciesInfo>>)discoveredSubspeciesBySpecies)
                {
                    PUtil.LogDebug($"[PlantSeedManager] 遍历植物Tag:{tag}");
                    
                    foreach (var list in infos)
                    {
                        var plantName = list.ID.ToString();
                        PUtil.LogDebug($"[PlantSeedManager] |__ID:{list.ID} | speciesID:{list.speciesID}");

                        if (Assets.GetPrefab(list.speciesID) is GameObject plantPrefab)
                        {
                            plantName = plantPrefab.name;
                            PUtil.LogDebug($"[PlantSeedManager] |__植物名称:{plantPrefab.name} | 本地化名称:{plantPrefab.GetProperName()}");
                            if (plantPrefab.GetComponent<MutantPlant>() == null) break;
                        }
                        Tag seedID = null;
                        string seedName = null;
                        string seedPropName = null;
                        if (Assets.GetPrefab($"{list.speciesID}") is GameObject plantSpeciesPrefab)
                        {
                            seedID = plantSpeciesPrefab.GetComponent<SeedProducer>()?.seedInfo.seedId;

                            if (Assets.GetPrefab($"{seedID}") is GameObject plantSeedPrefab)
                            {
                                PUtil.LogDebug($"[PlantSeedManager] |__种子预制体ID:{plantSeedPrefab.PrefabID()}");
                                PUtil.LogDebug($"[PlantSeedManager]    |__Seed ID:{seedID}");
                                PUtil.LogDebug($"[PlantSeedManager]    |__种子名称:{plantSeedPrefab.name} | 本地化名称:{plantSeedPrefab.GetProperName()}");

                                seedName = plantSeedPrefab.name;
                                seedPropName = plantSeedPrefab.GetProperName();
                            }
                        }
                        if (seedID != null && !_plantToSeedCache.ContainsKey(tag))
                        {
                            _plantToSeedCache.Add(list.speciesID, new PlantSeedInfo
                            {
                                PlantID = list.ID,
                                PlantName = plantName,
                                speciesID = list.speciesID, // 核心：物种ID作为缓存key
                                SeedID = seedID,
                                SeedName = seedName,
                                SeedPropName = seedPropName
                            });
                            PUtil.LogDebug($"[PlantSeedManager] 缓存映射成功：{tag} → {seedID}");
                        }
                    }
                }
            }
            PUtil.LogDebug($"[PlantSeedManager] 初始化完成，共缓存 {_plantToSeedCache.Count} 组植物-种子映射关系");
        }

        public static bool ContainsTag(Tag tag)
        {
            return _plantToSeedCache.ContainsKey(tag);
        }

        public static List<PlantSeedInfo> GetAllPlantSeedInfos()
        {
            return new List<PlantSeedInfo>(_plantToSeedCache.Values);
        }
        #endregion

        #region ========== 核心改造：对接原生PlantMutations库 实现随机变异 ==========
        public static GameObject GenerateMutantSubspeciesSeed(GameObject sourceSeed, Vector3 spawnPos, Storage storage, bool forceMutate = false)
        {
            if (sourceSeed == null || !sourceSeed.activeInHierarchy)
            {
                PUtil.LogError("[PlantSeedManager] 生成失败：源种子无效/已销毁");
                return null;
            }
            
            var sourceMutant = sourceSeed.GetComponent<MutantPlant>();
            var sourcePrimary = sourceSeed.GetComponent<PrimaryElement>();
            var sourcePickable = sourceSeed.GetComponent<Pickupable>();

            if (sourceMutant == null || sourcePrimary == null || sourcePickable == null)
            {
                PUtil.LogError("[PlantSeedManager] 生成失败：源种子缺少核心组件（MutantPlant/SeedProducer/PrimaryElement/Pickable）");
                return null;
            }

            if (string.IsNullOrEmpty(sourceMutant.SpeciesID.ToString()) || !Assets.GetPrefab(sourceMutant.SpeciesID))
            {
                PUtil.LogError($"[PlantSeedManager] 生成失败：源种子物种ID无效 → {sourceMutant.SpeciesID}");
                return null;
            }

            try
            {
                bool isNeedMutate = forceMutate;
                if (!forceMutate && sourceMutant.IsOriginal)
                {
                    isNeedMutate = RollNativeMutationChance(sourceSeed);
                    PUtil.LogDebug($"[PlantSeedManager] 原生概率判定结果：{(isNeedMutate ? "触发新变异" : "不触发变异，复制原有状态")}");
                }

                GameObject mutantSeed = GameUtil.KInstantiate(original: sourceSeed, position: spawnPos, sceneLayer: Grid.SceneLayer.Front);
                PUtil.LogDebug($"[PlantSeedManager] 变异种子初始化位置: {mutantSeed.transform.GetPosition()} - spawnPos:{spawnPos}");
                SyncSeedBaseProperties(sourceSeed, mutantSeed);

                var targetMutant = mutantSeed.GetComponent<MutantPlant>();
                if (targetMutant == null)
                {
                    PUtil.LogError("[PlantSeedManager] 生成失败：新种子无MutantPlant组件");
                    UnityEngine.Object.DestroyImmediate(mutantSeed);
                    return null;
                }

                if (isNeedMutate && sourceMutant != null && sourceMutant.IsOriginal)
                {
                    // ✅ 核心改造：调用【原生库随机变异】方法，完全对接官方逻辑
                    ApplyNativeLibraryRandomMutation(sourceMutant, targetMutant);
                }
                else
                {
                    if (sourceMutant != null)
                        sourceMutant.CopyMutationsTo(targetMutant);
                    PUtil.LogDebug($"[PlantSeedManager] 变异状态处理完成 → {targetMutant.SubSpeciesID}");
                }

                RegisterUndiscoveredSubspecies(targetMutant);
                FinalizeMutantSeed(mutantSeed, spawnPos, storage);

                PUtil.LogDebug($"[PlantSeedManager] 变异种子生成成功 ✅ → 亚种ID：{targetMutant.SubSpeciesID}（未发现状态）");
                return mutantSeed;
            }
            catch (Exception ex)
            {
                PUtil.LogError($"[PlantSeedManager] 生成变异种子异常：{ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private static bool RollNativeMutationChance(GameObject seed)
        {
            var maxRadiationAttr = Db.Get().PlantAttributes.MaxRadiationThreshold.Lookup(seed);
            if (maxRadiationAttr == null)
            {
                PUtil.LogWarning("[PlantSeedManager] 无法获取植物辐射阈值，默认变异概率0");
                return false;
            }

            int cell = Grid.PosToCell(seed);
            float currentRad = Grid.IsValidCell(cell) ? Grid.Radiation[cell] : 0f;
            float maxRad = maxRadiationAttr.GetTotalValue();
            float mutateChance = Mathf.Clamp01(currentRad / maxRad) * 0.8f;
            bool rollSuccess = UnityEngine.Random.value < mutateChance;

            PUtil.LogDebug($"[PlantSeedManager] 辐射概率计算：当前辐射={currentRad:F1} | 阈值={maxRad:F1} | 概率={mutateChance:P2} | 判定={rollSuccess}");
            return rollSuccess;
        }

        private static void SyncSeedBaseProperties(GameObject source, GameObject target)
        {
            var srcPrimary = source.GetComponent<PrimaryElement>();
            var tarPrimary = target.GetComponent<PrimaryElement>();
            var tarPickable = target.GetComponent<Pickupable>();

            tarPrimary.Temperature = srcPrimary.Temperature;
            tarPrimary.Units = srcPrimary.Units;
            tarPrimary.ElementID = srcPrimary.ElementID;

            tarPickable.enabled = true;
            tarPickable.tag = source.tag;
        }

        #region ✅ 核心新增：对接原生PlantMutations库（官方标准随机变异）
        /// <summary>
        /// 核心方法：调用游戏原生API，实现【官方合规的随机变异】
        /// ✅ 自动过滤不适配当前植物的变异 → 严格遵循官方规则
        /// ✅ 无硬编码、100%兼容官方新增/删减变异类型
        /// </summary>
        private static void ApplyNativeLibraryRandomMutation(MutantPlant source, MutantPlant target)
        {
            if (source == null || target == null || string.IsNullOrEmpty(source.SpeciesID.ToString()))
            {
                PUtil.LogError("[PlantSeedManager] 原生随机变异失败：核心参数为空");
                return;
            }

            // 1. 获取当前植物的预制体ID（用于原生API筛选适配变异）
            string plantPrefabID = source.SpeciesID.ToString();
            if (string.IsNullOrEmpty(plantPrefabID))
            {
                PUtil.LogError("[PlantSeedManager] 原生随机变异失败：植物预制体ID为空");
                return;
            }

            // 2. 存储随机抽取的变异ID列表
            List<string> randomMutationIDs = new List<string>();

            // 3. 循环抽取指定数量的合规变异（自动去重、自动适配）
            for (int i = 0; i < RANDOM_MUTATION_COUNT; i++)
            {
                // ✅ 调用原生核心API：获取当前植物「合规的随机变异实例」
                PlantMutation randomMutation = NativePlantMutations.GetRandomMutation(plantPrefabID);
                if (randomMutation == null)
                {
                    PUtil.LogWarning($"[PlantSeedManager] 原生随机变异警告：植物[{plantPrefabID}]无适配的变异类型，终止抽取");
                    break;
                }

                string mutationID = randomMutation.Id;
                // 去重：避免同一植物重复抽取同一变异
                if (!randomMutationIDs.Contains(mutationID))
                {
                    randomMutationIDs.Add(mutationID);
                    PUtil.LogDebug($"[PlantSeedManager] ✅ 抽取到原生合规变异 → ID:{mutationID} | 名称:{randomMutation.Name}");
                }
                else
                {
                    PUtil.LogDebug($"[PlantSeedManager] 原生随机变异去重：已抽取[{mutationID}]，跳过");
                    i--; // 重新抽取一次
                }
            }

            // 4. 校验抽取结果，调用原生API生成亚种
            if (randomMutationIDs.Count == 0)
            {
                PUtil.LogError("[PlantSeedManager] 原生随机变异失败：未抽取到任何合规变异ID");
                return;
            }

            try
            {
                // ✅ 调用原生亚种生成API，传入随机变异ID列表
                target.SetSubSpecies(randomMutationIDs);
                PUtil.LogDebug($"[PlantSeedManager] ✅ 原生亚种生成成功 → 亚种ID：{target.SubSpeciesID} | 变异组合：{string.Join("+", randomMutationIDs)}");
            }
            catch (Exception ex)
            {
                PUtil.LogWarning($"[PlantSeedManager] 原生亚种生成容错 → {ex.Message}");
            }
        }
        #endregion

        private static void RegisterUndiscoveredSubspecies(MutantPlant mutantPlant)
        {
            if (mutantPlant == null) return;
            PlantSubSpeciesCatalog.SubSpeciesInfo subInfo = mutantPlant.GetSubSpeciesInfo();
            if (subInfo == null)
            {
                PUtil.LogError("[PlantSeedManager] 亚种信息为空，注册失败");
                return;
            }
            PlantSubSpeciesCatalog.Instance.DiscoverSubSpecies(subInfo, mutantPlant);
            PUtil.LogDebug($"[PlantSeedManager] ✅ 注册未发现变异亚种 → {subInfo.speciesID.ToString()}");
        }

        private static void FinalizeMutantSeed(GameObject seed, Vector3 spawnPos, Storage storage = null, bool dropByStorage = true)
        {
            seed.AddTag(GameTags.MutatedSeed);
            seed.SetActive(true);
            seed.gameObject.SetActive(true);
            seed.layer = (int)Grid.SceneLayer.Front;
            seed.transform.SetParent(null);

            TbbDebuger.PrintGameObjectFullInfo(seed);
            if (dropByStorage && storage != null)
            {
                var seedPickable = seed.GetComponent<Pickupable>();
                if (seedPickable != null)
                {
                    storage.Store(seed);
                    PUtil.LogDebug("[PlantSeedManager] 变异种子已存入建筑仓储");
                }

                GameObject droppedSeed = null;
                if (seedPickable != null)
                {
                    droppedSeed = storage.Drop(seed);
                    if (droppedSeed != null)
                    {
                        droppedSeed.transform.SetPosition(spawnPos);
                    }
                }
            }
            else
            {
                seed.transform.SetPosition(spawnPos);
            }

            seed.Trigger(1623392196, null);
            seed.Trigger(-1736624145, seed);
            PUtil.LogDebug($"[PlantSeedManager] ✅ 种子激活成功，掉落位置：{spawnPos}");
        }
        #endregion
        #region 种子有效性判定（供Workable/StatesInstance调用）
        /// <summary>
        /// 全局唯一种子校验入口 → 所有模块统一调用此方法
        /// 复用_plantToSeedCache缓存，自动兼容冰霜小麦/小吃豆/气囊芦荟
        /// </summary>
        public static bool IsSeedValidForMutation(GameObject seedObj)
        {
            if (seedObj == null || !seedObj.activeInHierarchy) return false;
            // 1. 过滤已变异种子（全局规则：禁止重复变异）
            if (seedObj.HasTag(GameTags.MutatedSeed)) return false;

            // 2. 获取种子关联的植物Tag（从MutantPlant组件取，100%兼容所有自定义种子）
            var mutantPlantComp = seedObj.GetComponent<MutantPlant>();
            if (mutantPlantComp == null || string.IsNullOrEmpty(mutantPlantComp.SpeciesID.ToString()))
                return false;

            Tag plantSpeciesTag = mutantPlantComp.SpeciesID;
            // 3. 核心判定：缓存中存在该植物Tag → 即为有效可变异种子
            bool isInCache = ContainsTag(plantSpeciesTag);
            PUtil.LogDebug($"[PlantSeedManager] 种子校验结果：物种[{plantSpeciesTag}] → 缓存存在={isInCache}");
            return isInCache;
        }

        /// <summary>
        /// 重载：批量校验仓储内种子（简化外部调用）
        /// </summary>
        public static bool HasValidMutationSeed(Storage seedStorage)
        {
            if (seedStorage == null || seedStorage.items.Count == 0) return false;
            return seedStorage.items.Any(IsSeedValidForMutation);
        }
        #endregion
    }
}