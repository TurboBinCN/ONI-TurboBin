using Database;
using KSerialization;
using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using MutantContainmentProject.Mutanters;
using MutantContainmentProject.MutanterStoryStraits;
using MutantContainmentProject.FunctionPatches;
using System;
using System.Collections.Generic;
using System.Linq;
using TBB.He.TbbLib.Debuger;
using UnityEngine;
using static MutantContainmentProject.STRINGS;

public class GravitasMutanterFounder : GameStateMachine<GravitasMutanterFounder, GravitasMutanterFounder.Instance, IStateMachineTarget, GravitasMutanterFounder.Def>
{
    public override void InitializeStates(out BaseState default_state)
    {
        default_state = inoperational;
        serializable = SerializeType.ParamsOnly;

        root.Enter(delegate (Instance smi)
        {
            TbbDebuger.LogDebug($"GravitasMutanterFounder SMI Enter Root");
            smi.Initialize();
        }).EventHandler(GameHashes.BuildingActivated, (smi, activated) =>
        {
            if (((Boxed<bool>)activated).value)
            {
                TbbDebuger.LogDebug($"GravitasMutanterFounder:[{((Boxed<bool>)activated).value}]");
                StoryManager.Instance.BeginStoryEvent(Db.Get().Stories.Get(MutanterStoris.GravitasMutanterFounderID));
            }
        });

        inoperational
            .Enter((smi) => { TbbDebuger.LogDebug($"[GravitasMutanterFounder] states:inoperational"); })
            .PlayAnim("off")
            .EventTransition(GameHashes.OperationalChanged, operational.idle, (smi) => smi.GetComponent<Operational>().IsOperational);

        operational
            .Enter((smi) => { TbbDebuger.LogDebug($"[GravitasMutanterFounder] states:operational"); })
            .DefaultState(operational.idle)
            .EventTransition(GameHashes.OperationalChanged, inoperational, (smi) => !smi.GetComponent<Operational>().IsOperational);

        operational.idle
            .PlayAnim("idle", KAnim.PlayMode.Loop)
            .Enter(delegate (Instance smi)
            {
                TbbDebuger.LogDebug($"[GravitasMutanterFounder] states:operational.idle");
                smi.UpdateMeter(); // 更新进度条，显示当前配方的满足数量
                // 重置献祭品检测参数
                smi.sm.sacrificeDetected.Set(false, smi, false);
            })
            .ToggleMainStatusItem(GravitasMutanterFounderBuildingStatusItems.Instance.GravitasMutanterFounderWaiting, null)
            // 检测到献祭品时转换到capture状态
            .ParamTransition(sacrificeDetected, operational.capture, IsTrue)
            // 检查是否正在冷却
            .ParamTransition(cooldownTimer, operational.cooldown, IsGTZero);
        operational.capture
            .PlayAnim("working_capture", KAnim.PlayMode.Once)
            .Enter((smi)=>{
                // 重置献祭品检测参数
                smi.sm.sacrificeDetected.Set(false, smi, false);
                smi.CreatureStore();
                // 更新进度条，显示当前配方的满足数量
                smi.UpdateMeter();
            })
            .EventHandler(GameHashes.AnimQueueComplete, (smi, data) => {
                // 检查激活条件
                if (smi.CheckAndSetUnlockCondition())
                {
                    smi.GoTo(smi.sm.operational.activating.pre);
                }
                else
                {
                    smi.GoTo(smi.sm.operational.idle);
                }
            });
        // Activating Group:从准备激活到畸变体生成
        operational.activating
            .Enter((smi) => { TbbDebuger.LogDebug($"[GravitasMutanterFounder] states:operational.activating"); })
            .DefaultState(operational.activating.pre)
            .ToggleMainStatusItem(GravitasMutanterFounderBuildingStatusItems.Instance.GravitasMutanterFounderWorking, null);

        operational.activating.pre
            .Enter((smi) => { TbbDebuger.LogDebug($"[GravitasMutanterFounder] states:operational.activating.pre"); })
            .PlayAnim("working_pre")
            .OnAnimQueueComplete(operational.activating.loop)
            .Enter(delegate (Instance smi)
            {
                smi.sm.activationTimer.Set(smi.def.activationDuration, smi, false); // 设置激活过程的持续时间
            })
            .Exit(delegate (Instance smi)
            {
                // 激活时间结束后的后续处理（如果需要）
            });

        // Loop Activation: 激活动画循环
        operational.activating.loop
            .Enter((smi) => { TbbDebuger.LogDebug($"[GravitasMutanterFounder] states:operational.activating.loop"); })
            .PlayAnim("working_loop", KAnim.PlayMode.Loop)
            .Update(delegate (Instance smi, float dt)
            {
                smi.sm.activationTimer.DeltaClamp(-dt, 0f, float.MaxValue, smi);
            }, UpdateRate.SIM_1000ms, false)
            .ParamTransition<float>(activationTimer, operational.activating.pst, IsLTEZero);

        // Post Activation: 激活动画结束，生成畸变体和陷阱
        operational.activating.pst
            .Enter((smi) => { TbbDebuger.LogDebug($"[GravitasMutanterFounder] states:operational.activating.pst"); })
            .PlayAnim("working_pst")
            .OnAnimQueueComplete(operational.cooldown) // 激活完成后直接进入冷却
            .Enter(delegate (Instance smi)
            {
                Tag species = smi.SpawnMutants(); // 生成畸变体
                smi.ConsumeSacrificeItems(); // 在这里消耗掉作为献祭的物品

                if (species != Tag.Invalid)
                {
                    smi.gameObject.Trigger(1980521255, null);
                    smi.ShowNotification(species);
                    smi.TryShowCompletedNotification();
                }
                //TODO 需要细化完善， 计算并设置冷却时间，基于激活等级
                float cooldownDuration = smi.GetCooldownDurationForLevel(smi.ActivationLevel);
                smi.sm.cooldownTimer.Set(cooldownDuration, smi, false);
            });

        // 添加冷却状态的进度条状态项
        operational.cooldown
            .Enter((smi) => { TbbDebuger.LogDebug($"[GravitasMutanterFounder] states:operational.cooldown"); })
            .PlayAnim("working_cooldown", KAnim.PlayMode.Loop)
            .Update(delegate (Instance smi, float dt)
            {
                smi.sm.cooldownTimer.DeltaClamp(-dt, 0f, float.MaxValue, smi);
            }, UpdateRate.SIM_1000ms, false)
            .ParamTransition(cooldownTimer, operational.idle, IsLTEZero)
            .ToggleStatusItem(
                BUILDINGS.STATUSITEMS.GRAVITAS_MUTANTER_FOUNDER_COOLDOWN.NAME,
                BUILDINGS.STATUSITEMS.GRAVITAS_MUTANTER_FOUNDER_COOLDOWN.TOOLTIP,
                "", StatusItem.IconType.Info, NotificationType.Neutral, false, default(HashedString), 129022,
                (str, smi) => CooldownProcessing(str, smi),
                (str, smi) => CooldownProcessingTooltip(str, smi),
                Db.Get().StatusItemCategories.Main
            );


    }

    private static string CooldownProcessing(string str, Instance smi)
    {
        return str.Replace("{percent}", GameUtil.GetFormattedPercent((1f - smi.sm.cooldownTimer.Get(smi) / smi.GetCooldownDurationForLevel(smi.ActivationLevel)) * 100f, GameUtil.TimeSlice.None));
    }

    private static string CooldownProcessingTooltip(string str, Instance smi)
    {
        return str.Replace("{timeleft}", GameUtil.GetFormattedTime(smi.sm.cooldownTimer.Get(smi), "F0"));
    }


    public FloatParameter cooldownTimer;

    public FloatParameter activationTimer;

    public BoolParameter sacrificeDetected;

    public IntParameter activationLevel;

    public TargetParameter sacrificeTarget;

    public State inoperational;

    public ActiveStates operational;

    public class Def : BaseDef
    {
        public CellOffset pickupOffset; // 献祭物品拾取位置偏移
        public int numSpeciesToUnlockMorphMode; // 解锁所需的物种数量 (复用原名，含义改为解锁遗迹所需)
        public CellOffset dropOffset; // 生成物掉落位置偏移
        public float activationDuration = 3f; // 激活动画持续时间
        public float baseCooldownDuration = 10f; // 基础冷却时间
        public float highLevelCooldownBonus = 5f; // 高级激活额外冷却时间
        public float trapChance = 0.15f; // 陷阱触发概率
        public int maxMutantsPerSpawn = 2; // 单次最大生成畸变体数量
        public int minMutantsPerSpawn = 1; // 单次最小生成畸变体数量
        public List<Tag> requiredSacrificeTags; // 必需的献祭物品标签列表 (例如 ["Critter"], ["Meat"], ["SpecialItem"])

        // ---献祭配方字典 ---
        public Dictionary<Tag, Dictionary<Tag, int>> sacrificeRecipes = new Dictionary<Tag, Dictionary<Tag, int>> {
            {
                SCP173Config.ID, new Dictionary<Tag, int> {
                    { HatchConfig.ID, 3 }// 需要3个Hatch
                }
            },
            {
                SCP096Config.ID, new Dictionary<Tag, int>
                {
                    { HatchConfig.ID, 1},
                    { PacuConfig.ID, 2}
                }
            },
            {
                SCP662Config.ID, new Dictionary<Tag, int>
                {
                    { HatchConfig.ID, 2},
                    { PacuConfig.ID, 1}
                }
            }
        };

    }

    public class ActivatingStates : State
    {
        public State pre;
        public State loop;
        public State pst;
    }
    public class ActiveStates : State

    {
        public State idle;
        public ActivatingStates activating;
        public State cooldown;
        public State capture;
    }

    public new class Instance : GameInstance
    {
        // 缓存本次激活将使用的配方和目标
        private Tag m_activationTarget;
        private Dictionary<Tag, int> m_usedRecipe;

        public Instance(IStateMachineTarget master, Def def) : base(master, def)
        {
            //献祭物品检测逻辑
            pickupCell = Grid.OffsetCell(Grid.PosToCell(master.gameObject), smi.def.pickupOffset);

            m_partitionEntry = GameScenePartitioner.Instance.Add("GravitasMutanterFounder", gameObject, pickupCell, GameScenePartitioner.Instance.pickupablesChangedLayer, new Action<object>(DetectSacrifice));

            m_largeCreaturePartitionEntry = GameScenePartitioner.Instance.Add("GravitasMutanterFounder.large", gameObject, Grid.CellLeft(pickupCell), GameScenePartitioner.Instance.pickupablesChangedLayer, new Action<object>(DetectLargeCreature));

            m_progressMeter = new MeterController(GetComponent<KBatchedAnimController>(), "m_d", "meter", Meter.Offset.UserSpecified, Grid.SceneLayer.TileFront, Array.Empty<string>());
            // 使用自定义插值函数，实现4帧3格动画的正确映射
            m_progressMeter.interpolateFunction = (percentage, frames) => {
                if (frames <= 1 || percentage <= 0f) return 0f;
                if (percentage >= 1f) return 1f;
                
                // 4帧3格动画的映射：
                // 0-33.33% → 第1-2帧 (0-0.3333)
                // 33.33-66.66% → 第2-3帧 (0.3333-0.6666)
                // 66.66-100% → 第3-4帧 (0.6666-1.0)
                if (percentage < 1f/3f) {
                    return percentage * 3f / (float)frames;
                } else if (percentage < 2f/3f) {
                    return (1f + (percentage - 1f/3f) * 3f) / (float)frames;
                } else {
                    return (2f + (percentage - 2f/3f) * 3f) / (float)frames;
                }
            };


            // 初始化献祭相关的集合
            m_sacrificedSpecies = new HashSet<Tag>();
            m_sacrificeContainer = master.gameObject.GetComponent<Storage>();
            m_sacrificeContainer.allowItemRemoval = false; // 防止外部轻易拿走献祭品
            m_sacrificeContainer.showDescriptor = false;
            m_sacrificeContainer.capacityKg = 2000f; // 设置容量
        }

        public override void StartSM()
        {
            base.StartSM();
            UpdateStatusItems();
            UpdateMeter();

            // 可故事事件或通知初始化
            StoryManager.Instance.ForceCreateStory(Db.Get().Stories.Get(MutanterStoris.GravitasMutanterFounderID), gameObject.GetMyWorldId());

            if (MutanterSpeciesCatalog.Instance.GetMutanterSpeciesCount() >= smi.def.numSpeciesToUnlockMorphMode)
            {
                StoryManager.Instance.BeginStoryEvent(Db.Get().Stories.Get(MutanterStoris.GravitasMutanterFounderID));
            }
            TryShowCompletedNotification();

            onBuildingSelectHandle = Subscribe(-1503271301, new Action<object>(OnBuildingSelect));

            StoryManager.Instance.DiscoverStoryEvent(Db.Get().Stories.Get(MutanterStoris.GravitasMutanterFounderID));
        }
        private void OnBuildingSelect(object obj)
        {
            if (!((Boxed<bool>)obj).value) return;
            if (!m_introPopupSeen) ShowIntroNotification();
            if (m_endNotification == null) return;
            m_endNotification.customClickCallback(m_endNotification.customClickData);
        }
        public void Initialize()
        {
            // 清理上次运行的痕迹（如果有）
            this.ActivationLevel = 0;
            // 如果有上次未完成的冷却，确保参数正确
            if (sm.cooldownTimer.Get(this) <= 0)
            {
                sm.cooldownTimer.Set(0f, this, false);
            }
        }

        private void UpdateStatusItems()
        {
            KSelectable component = gameObject.GetComponent<KSelectable>();
            component.ToggleStatusItem(GravitasMutanterFounderBuildingStatusItems.Instance.GravitasMutanterFounderProgress, !IsUnlocked, this);
            component.ToggleStatusItem(GravitasMutanterFounderBuildingStatusItems.Instance.GravitasMutanterFounderMorphMode, IsUnlocked, this);
            component.ToggleStatusItem(GravitasMutanterFounderBuildingStatusItems.Instance.GravitasMutanterFounderMorphModeLocked, !IsUnlocked, this);
        }

        public void UpdateMeter()
        {
            // 更新进度条，显示当前配方的满足数量
            float progress = 0f;
            
            // 检查是否有激活目标
            if (m_activationTarget != Tag.Invalid && m_usedRecipe != null)
            {
                // 计算当前配方的满足比例
                float totalRequired = 0f;
                float totalAvailable = 0f;
                
                foreach (var ingredient in m_usedRecipe)
                {
                    Tag ingredientTag = ingredient.Key;
                    int requiredCount = ingredient.Value;
                    float availableCount = CountItemsInStorage(ingredientTag);
                    
                    TbbDebuger.LogDebug($"UpdateMeter: ingredient={ingredientTag}, required={requiredCount}, available={availableCount}");
                    
                    totalRequired += requiredCount;
                    totalAvailable += Mathf.Min(availableCount, requiredCount);
                }
                
                TbbDebuger.LogDebug($"UpdateMeter: totalRequired={totalRequired}, totalAvailable={totalAvailable}");
                
                if (totalRequired > 0)
                {
                    progress = totalAvailable / totalRequired;
                }
            }
            else
            {
                // 检查所有配方的进度，显示最接近完成的那个
                float maxProgress = 0f;
                foreach (var recipePair in smi.def.sacrificeRecipes)
                {
                    Dictionary<Tag, int> possibleRecipe = recipePair.Value;
                    float totalRequired = 0f;
                    float totalAvailable = 0f;
                    
                    foreach (var ingredient in possibleRecipe)
                    {
                        Tag ingredientTag = ingredient.Key;
                        int requiredCount = ingredient.Value;
                        float availableCount = CountItemsInStorage(ingredientTag);
                        
                        totalRequired += requiredCount;
                        totalAvailable += Mathf.Min(availableCount, requiredCount);
                    }
                    
                    if (totalRequired > 0)
                    {
                        float recipeProgress = totalAvailable / totalRequired;
                        if (recipeProgress > maxProgress)
                        {
                            maxProgress = recipeProgress;
                        }
                    }
                }
                
                // 如果有配方在进行中，显示配方进度
                if (maxProgress > 0)
                {
                    TbbDebuger.LogDebug($"UpdateMeter: no activation target, max recipe progress={maxProgress}");
                    progress = maxProgress;
                }
                else
                {
                    // 否则显示已献祭的不同物种数量
                    TbbDebuger.LogDebug($"UpdateMeter: no activation target, species count={m_sacrificedSpecies.Count}, required={smi.def.numSpeciesToUnlockMorphMode}");
                    progress = Mathf.Clamp01(m_sacrificedSpecies.Count / (float)smi.def.numSpeciesToUnlockMorphMode);
                }
            }
            TbbDebuger.LogDebug($"UpdateMeter: final progress=[{Mathf.Clamp01(progress)}]");
            
            m_progressMeter.SetPositionPercent(Mathf.Clamp01(progress));
        }

        public bool IsUnlocked
        {
            get
            {
                return m_sacrificedSpecies.Count >= base.smi.def.numSpeciesToUnlockMorphMode;
            }
        }

        public int ActivationLevel { get; private set; } = 0;
        private void DetectLargeCreature(object obj)
        {
            Pickupable pickupable = obj as Pickupable;
            if (pickupable == null)
            {
                return;
            }
            if (pickupable.GetComponent<KCollider2D>().bounds.size.x > 1.5f)
            {
                DetectSacrifice(obj);
            }
        }
        private void DetectSacrifice(object obj)
        {
            Pickupable pickupable = obj as Pickupable;
            if (pickupable == null || !IsSacrificeValid(pickupable.KPrefabID))
            {
                return;
            }

            // 检查是否在正确的状态下接收献祭
            List<GameObject> list = new();
            if (smi.IsInsideState(smi.sm.operational.idle) && (m_sacrificeContainer.Find(pickupable.gameObject.PrefabID(), list) == null || !list.Contains(pickupable.gameObject)))
            {
                // 存储献祭品引用，供capture状态使用
                m_currentSacrifice = pickupable;
                // 设置献祭品检测参数
                smi.sm.sacrificeDetected.Set(true, smi, false);
            }
        }
        
        private Pickupable m_currentSacrifice;
        
        public void CreatureStore() {
            if (m_currentSacrifice == null)
                return;
                
            TbbDebuger.LogDebug($"[GravitasMutanterFounder] Store [{m_currentSacrifice.gameObject.GetInstanceID()}]");
            // 尝试将物品放入献祭容器
            if (m_sacrificeContainer.Store(m_currentSacrifice.gameObject, false, false, true, false))
            {
                // 记录新物种
                var creatureBrain = m_currentSacrifice.GetComponent<CreatureBrain>();
                if (creatureBrain != null)
                {
                    m_sacrificedSpecies.Add(creatureBrain.species);
                }

                UpdateStatusItems();
                // 检查激活条件，更新m_activationTarget和m_usedRecipe
                CheckAndSetUnlockCondition();
                UpdateMeter();
            }
            
            // 清空当前献祭品引用
            m_currentSacrifice = null;
        }
        public bool IsSacrificeValid(KPrefabID kpid)
        {
            foreach (var recipePair in base.smi.def.sacrificeRecipes)
            {
                var recipe = recipePair.Value;
                foreach (var ingredient in recipe.Keys)
                {
                    if (kpid.HasTag(ingredient))
                    {
                        return true; // 如果物品是任何一个配方的一部分，则有效
                    }
                }
            }
            return false; // 否则无效
        }
        // --- 配方检查 ---
        private bool CanActivateAndSpawn(out Tag target, out Dictionary<Tag, int> recipe)
        {
            TbbDebuger.LogDebug($"[GravitasMutanterFounder] 开始检查激活条件...当前牺牲容器物品数量: {m_sacrificeContainer.items.Count}");
            target = Tag.Invalid;
            recipe = null;

            // 遍历所有可用的配方
            foreach (var recipePair in smi.def.sacrificeRecipes)
            {
                Tag possibleTarget = recipePair.Key;
                Dictionary<Tag, int> possibleRecipe = recipePair.Value;

                // 检查当前库存是否满足此配方
                bool hasEnough = true;
                foreach (var ingredient in possibleRecipe)
                {
                    Tag ingredientTag = ingredient.Key;
                    int requiredCount = ingredient.Value;

                    float currentCount = CountItemsInStorage(ingredientTag);
                    if (currentCount < requiredCount)
                    {
                        hasEnough = false;
                        break; // 当前配方不满足，跳出内层循环检查下一个
                    }
                }
                if (hasEnough)
                {
                    target = possibleTarget;
                    recipe = possibleRecipe;
                    return true; // 成功找到
                }

            }

            // 所有配方都不满足
            return false;
        }
        // --- 统计存储中某种标签的物品数量 ---
        private float CountItemsInStorage(Tag tag)
        {
            float count = 0;
            foreach (var item in m_sacrificeContainer.items)
            {
                var kpid = item.GetComponent<KPrefabID>();
                if (kpid != null && kpid.HasTag(tag))
                {
                    // 如果物品有StackSize组件，则加上其堆叠数量
                    var pickupable = item.GetComponent<Pickupable>();
                    count += (pickupable != null ? pickupable.TotalAmount : 1);
                }
            }
            return count;
        }
        public bool CheckAndSetUnlockCondition()
        {
            // 检查是否有足够的物品满足任意一个配方
            bool conditionMet = CanActivateAndSpawn(out m_activationTarget, out m_usedRecipe);
            if (conditionMet)
            {
                // 添加 null 检查，防止在 m_usedRecipe 为 null 时调用 .Select()
                string recipeDetails = m_usedRecipe != null
                    ? string.Join(", ", m_usedRecipe.Select(kvp => $"{kvp.Key}:{kvp.Value}"))
                    : "没有匹配的配方或者发生错误";
                TbbDebuger.LogDebug($"[GravitasMutanterFounder] 解锁0conditionmet! 准备生成{m_activationTarget}配方: {recipeDetails}");
            }
            return conditionMet;
        }

        public void ConsumeSacrificeItems()
        {
            if (m_usedRecipe == null)
            {
                TbbDebuger.LogWarning("[GravitasMutanterFounder] 准备消耗祭品但是没有配方");
                return;
            }

            foreach (var ingredient in m_usedRecipe)
            {
                Tag ingredientTag = ingredient.Key;
                int requiredCount = ingredient.Value;

                float consumedSoFar = 0;
                List<GameObject> itemsToRemove = new List<GameObject>();

                foreach (var itemGO in m_sacrificeContainer.items)
                {
                    if (consumedSoFar >= requiredCount) break;

                    var kpid = itemGO.GetComponent<KPrefabID>();
                    if (kpid != null && kpid.HasTag(ingredientTag))
                    {
                        var pickupable = itemGO.GetComponent<Pickupable>();
                        float availableInThisStack = pickupable != null ? pickupable.TotalAmount : 1;

                        float toTakeFromThisStack = Mathf.Min(requiredCount - consumedSoFar, availableInThisStack);

                        if (toTakeFromThisStack == availableInThisStack)
                        {
                            // 整个堆栈都要被消耗
                            itemsToRemove.Add(itemGO);
                        }
                        else
                        {
                            // 只消耗堆栈的一部分
                            pickupable.TotalAmount -= toTakeFromThisStack;
                        }
                        consumedSoFar += toTakeFromThisStack;
                    }
                }

                // 从存储中移除被完全消耗的物品
                foreach (var itemGO in itemsToRemove)
                {
                    m_sacrificeContainer.Drop(itemGO, true); // Drop会将其从存储中移除
                    itemGO.DeleteObject();
                }
            }

            // 清空缓存的配方信息
            m_usedRecipe = null;
            m_activationTarget = Tag.Invalid;
            
            // 清空已献祭的物种集合，确保进度条归零
            m_sacrificedSpecies.Clear();
            
            // 调用UpdateMeter，确保进度条归零
            UpdateMeter();
        }

        public float GetCooldownDurationForLevel(int level)
        {
            float baseCooldown = base.smi.def.baseCooldownDuration;
            float bonusCooldown = (level > 1) ? base.smi.def.highLevelCooldownBonus : 0f;
            return baseCooldown + bonusCooldown;
        }

        public Tag SpawnMutants()
        {
            if (m_activationTarget == Tag.Invalid)
            {
                TbbDebuger.LogWarning($"[GravitasMutanterFounder]没有目标畸变体被激活");
                return Tag.Invalid;
            }

            // 检查畸变体是否已经存在
            if (MutanterSpeciesCatalog.Instance.IsMutanterSpeciesExists(m_activationTarget))
            {
                TbbDebuger.LogWarning($"[GravitasMutanterFounder] 畸变体 {m_activationTarget} 已经存在，无法生成");
                ShowMutanterExistsNotification(m_activationTarget);
                // 清空献祭容器
                //if (m_sacrificeContainer != null)
                //{
                //    m_sacrificeContainer.DropAll(offset:smi.def.dropOffset.ToVector3());
                //}
                return Tag.Invalid;
            }

            // 获取随机生成位置
            Vector3 spawnPos = GetRandomSpawnPosition(m_activationTarget);
            if (spawnPos == Vector3.zero)
            {
                // 如果没有找到合适位置，使用默认位置
                spawnPos = Grid.CellToPosCBC(Grid.PosToCell(smi), Grid.SceneLayer.Creatures) + smi.def.dropOffset.ToVector3();
                TbbDebuger.LogWarning($"[GravitasMutanterFounder] 没有找到合适的随机位置，使用默认位置: {spawnPos}");
            }

            //int numToSpawn = UnityEngine.Random.Range(smi.def.minMutantsPerSpawn, smi.def.maxMutantsPerSpawn + 1);
            int numToSpawn = 1;
            for (int i = 0; i < numToSpawn; i++)
            {
                GameObject mutantGO = Util.KInstantiate(Assets.GetPrefab(m_activationTarget), spawnPos);
                if (mutantGO != null)
                {
                    mutantGO.SetActive(true);
                    TbbDebuger.LogDebug($"[GravitasMutanterFounder] 生成畸变体： {m_activationTarget} @ [{spawnPos}]");
                    
                    // 注册新生成的畸变体
                    MutanterSpeciesCatalog.Instance.RegisterMutanterSpecies(m_activationTarget);
                    
                    // 执行迷雾揭开和镜头操作
                    //StartRevealSequence(mutantGO, spawnPos);
                    
                    return m_activationTarget;
                }
            }
            return Tag.Invalid;
        }
        
        private Vector3 GetRandomSpawnPosition(Tag mutantTag)
        {
            // 获取畸变体预制体以确定大小
            GameObject prefab = Assets.GetPrefab(mutantTag);
            if (prefab == null)
            {
                TbbDebuger.LogWarning($"[GravitasMutanterFounder] 无法找到畸变体预制体: {mutantTag}");
                return Vector3.zero;
            }
            
            // 估算畸变体大小（默认2x2格子）
            int width = 2;
            int height = 2;

            // 尝试从碰撞体获取实际大小
            KBoxCollider2D collider = prefab.GetComponent<KBoxCollider2D>();
            if (collider != null)
            {
                width = Mathf.CeilToInt(collider.size.x);
                height = Mathf.CeilToInt(collider.size.y);
            }
            
            // 获取当前世界ID
            int worldID = gameObject.GetMyWorldId();
            
            // 尝试最多100次寻找合适位置
            int maxAttempts = 100;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 在世界范围内随机选择一个起始格子
                int x = UnityEngine.Random.Range(0, Grid.WidthInCells);
                int y = UnityEngine.Random.Range(0, Grid.HeightInCells);
                
                // 检查该位置是否在当前世界
                int cell = Grid.XYToCell(x, y);
                if (Grid.IsValidCell(cell) && Grid.WorldIdx[cell] == worldID)
                {
                    // 检查该区域是否满足无液体和无固体的条件
                    if (IsAreaSuitableForSpawn(x, y, width, height))
                    {
                        // 返回该位置的中心坐标
                        return Grid.CellToPosCBC(cell, Grid.SceneLayer.Creatures);
                    }
                }
            }
            
            // 没有找到合适位置
            return Vector3.zero;
        }
        
        private bool IsAreaSuitableForSpawn(int startX, int startY, int width, int height)
        {
            // 检查指定区域内的所有格子
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    int cell = Grid.XYToCell(x, y);
                    
                    // 检查格子是否有效
                    if (!Grid.IsValidCell(cell))
                    {
                        return false;
                    }
                    
                    // 检查是否有固体
                    if (Grid.Solid[cell])
                    {
                        return false;
                    }
                    
                    // 检查是否有液体
                    if (Grid.IsLiquid(cell))
                    {
                        return false;
                    }
                    
                    // 检查是否有其他物体
                    if (Grid.Objects[cell, (int)ObjectLayer.Pickupables] != null)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        private void StartRevealSequence(GameObject mutantGO, Vector3 spawnPos)
        {
            // 揭开迷雾
            Vector2I cellPos = Grid.PosToXY(spawnPos);
            GridVisibility.Reveal(cellPos.x, cellPos.y, 8, 1f);
            
            // 镜头操作 - 拉近到生成位置
            CameraController.Instance.DisableUserCameraControl = true;
            CameraController.Instance.SetOverrideZoomSpeed(0.6f);
            
            // 设置相机目标位置和缩放
            float initialOrthographicSize = 15f;
            float finalOrthographicSize = 6f;
            
            CameraController.Instance.SetTargetPos(spawnPos, initialOrthographicSize, false);
            
            // 使用GameScheduler实现平滑的镜头缩放和淡入效果
            StartRevealSequenceWithScheduler(spawnPos, finalOrthographicSize);
        }
        
        private void StartRevealSequenceWithScheduler(Vector3 targetPos, float finalZoom)
        {
            // 第一阶段：淡入效果
            CameraController.Instance.FadeInColor(Color.black, callback:() => {
                // 淡入完成后，镜头拉近
                CameraController.Instance.SetTargetPos(targetPos, finalZoom, false);
                
                // 第二阶段：等待3秒展示生成的畸变体
                GameScheduler.Instance.Schedule("RevealSequence_Wait", 3f, _ => {
                    // 第三阶段：镜头拉远
                    CameraController.Instance.SetTargetPos(targetPos, 15f, true);
                    
                    // 第四阶段：等待1秒后恢复控制
                    GameScheduler.Instance.Schedule("RevealSequence_Complete", 1f, __ => {
                        // 恢复相机控制
                        CameraController.Instance.DisableUserCameraControl = false;
                        CameraController.Instance.SetOverrideZoomSpeed(1f);
                    });
                });
            });
        }

        public void ShowIntroNotification()
        {
            Game.Instance.unlocks.Unlock(GravitasMutanterFounderConfig.INITIAL_LORE_UNLOCK_ID, true);
            m_introPopupSeen = true;
            EventInfoScreen.ShowPopup(EventInfoDataHelper_GenerateStoryTraitData_Patches.GenerateStoryTraitDataWithAnim(CODEX.STORY_TRAITS.MUTANTER_FOUNDER.BEGIN_POPUP.NAME, CODEX.STORY_TRAITS.MUTANTER_FOUNDER.BEGIN_POPUP.DESCRIPTION, CODEX.STORY_TRAITS.CLOSE_BUTTON, "gravitas_mutanter_founder_kanim", EventInfoDataHelper.PopupType.BEGIN, "event_active"));
        }
        public void TryShowCompletedNotification()
        {
            if (MutanterSpeciesCatalog.Instance.GetMutanterSpeciesCount() < smi.def.numSpeciesToUnlockMorphMode || IsMorphMode)
                return;

            eventInfo = EventInfoDataHelper_GenerateStoryTraitData_Patches.GenerateStoryTraitDataWithAnim((string)CODEX.STORY_TRAITS.MUTANTER_FOUNDER.END_POPUP.NAME, (string)CODEX.STORY_TRAITS.MUTANTER_FOUNDER.END_POPUP.DESCRIPTION, (string)CODEX.STORY_TRAITS.MUTANTER_FOUNDER.END_POPUP.BUTTON, "gravitas_mutanter_founder_kanim", EventInfoDataHelper.PopupType.COMPLETE, "event_completed");

            //EventInfoScreen.ShowPopup(eventInfo);
            m_endNotification = EventInfoScreen.CreateNotification(eventInfo, new Notification.ClickCallback(UnlockMorphMode));
            gameObject.AddOrGet<Notifier>().Add(m_endNotification);
            gameObject.GetComponent<KSelectable>().AddStatusItem(Db.Get().MiscStatusItems.AttentionRequired, smi);

        }
        public void ShowNotification(Tag species)
        {
            Game.Instance.unlocks.Unlock(GravitasMutanterFounderConfig.LORE_UNLOCK_ID.For(species), false);

            ShowNotificationAndWaitForClick().Then(() => ShowLoreUnlockedPopup(species));

            Promise ShowNotificationAndWaitForClick()
            {
                return new Promise(resolve =>
                {
                    Notification notification1 = new Notification((string)CODEX.STORY_TRAITS.MUTANTER_FOUNDER.UNLOCK_SPECIES_NOTIFICATION.NAME, NotificationType.Event, (notifications, obj) =>
                    {
                        string str = (string)CODEX.STORY_TRAITS.MUTANTER_FOUNDER.UNLOCK_SPECIES_NOTIFICATION.TOOLTIP;
                        foreach (Notification notification2 in notifications)
                        {
                            string tooltipData = notification2.tooltipData as string;
                            str = $"{str}\n • {(string)Strings.Get("STRINGS.CREATURES.FAMILY_PLURAL." + tooltipData)}";
                        }
                        return str;
                    }, species.ToString().ToUpper(), false, custom_click_callback: obj => resolve(), clear_on_click: true);
                    gameObject.AddOrGet<Notifier>().Add(notification1);
                });
            }
        }
        public static void ShowLoreUnlockedPopup(Tag species)
        {
            InfoDialogScreen infoDialogScreen =
                LoreBearer.ShowPopupDialog().SetHeader(CODEX.STORY_TRAITS.MUTANTER_FOUNDER.UNLOCK_SPECIES_POPUP.NAME).AddDefaultOK(false);

            bool flag = CodexCache.GetEntryForLock(GravitasMutanterFounderConfig.LORE_UNLOCK_ID.For(species)) != null;
            Option<string> bodyContentForSpeciesTag = GravitasMutanterFounderConfig.GetBodyContentForSpeciesTag(species);

            if (flag && bodyContentForSpeciesTag.HasValue)
            {
                infoDialogScreen.AddPlainText(bodyContentForSpeciesTag.Value).AddOption(CODEX.STORY_TRAITS.MUTANTER_FOUNDER.UNLOCK_SPECIES_POPUP.VIEW_IN_CODEX, LoreBearerUtil.OpenCodexByEntryID(GravitasMutanterFounderConfig.CODEX_ENTRY_ID), false);
                return;
            }
            infoDialogScreen.AddPlainText(GravitasMutanterFounderConfig.GetBodyContentForUnknownSpecies());
        }
        
        public void ShowMutanterExistsNotification(Tag species)
        {
            // 显示畸变体已存在的通知
            Notification notification = new Notification(
                BUILDINGS.NOTIFICATIONS.GRAVITASMUTANTERFOUNDER.NAME,
                NotificationType.Bad,
                (notifications, obj) => string.Format(BUILDINGS.NOTIFICATIONS.GRAVITASMUTANTERFOUNDER.TOOLTIP, species.ToString()), 
                species.ToString().ToUpper(), 
                false, 
                clear_on_click: true
            );
            gameObject.AddOrGet<Notifier>().Add(notification);
        }
        public void UnlockMorphMode(object _)
        {
            if (m_morphModeUnlocked) return;

            Game.Instance.unlocks.Unlock(GravitasMutanterFounderConfig.COMPLETED_LORE_UNLOCK_ID, true);

            if (m_endNotification != null)
            {
                gameObject.AddOrGet<Notifier>().Remove(m_endNotification);
            }
            EventInfoScreen.ShowPopup(eventInfo);
            m_morphModeUnlocked = true;
            UpdateStatusItems();
            ClearEndNotification();
            Vector3 target = Grid.CellToPosCCC(Grid.OffsetCell(Grid.PosToCell(smi), new CellOffset(0, 2)), Grid.SceneLayer.Ore);
            StoryManager.Instance.CompleteStoryEvent(Db.Get().Stories.Get(MutanterStoris.GravitasMutanterFounderID), gameObject.GetComponent<MonoBehaviour>(), new FocusTargetSequence.Data
            {
                WorldId = smi.GetMyWorldId(),
                OrthographicSize = 6f,
                TargetSize = 6f,
                Target = target,
                PopupData = eventInfo,
                CompleteCB = new System.Action(OnStorySequenceComplete),
                CanCompleteCB = null
            });
        }
        private void OnStorySequenceComplete()
        {
            Vector3 keepsakeSpawnPosition = Grid.CellToPosCCC(Grid.OffsetCell(Grid.PosToCell(smi), new CellOffset(-1, 1)), Grid.SceneLayer.Ore);
            StoryManager.Instance.CompleteStoryEvent(Db.Get().Stories.Get(MutanterStoris.GravitasMutanterFounderID), keepsakeSpawnPosition);
            eventInfo = null;
        }
        public void ClearEndNotification()
        {
            gameObject.GetComponent<KSelectable>().RemoveStatusItem(Db.Get().MiscStatusItems.AttentionRequired, false);
            if (m_endNotification != null)
            {
                gameObject.AddOrGet<Notifier>().Remove(this.m_endNotification);
            }
            m_endNotification = null;
        }

        protected override void OnCleanUp()
        {
            GameScenePartitioner.Instance.Free(ref m_partitionEntry);
        }
        public bool IsMorphMode
        {
            get
            {
                return m_morphModeUnlocked;
            }
        }

        public int pickupCell;

        [MyCmpGet]
        private Operational m_operational;

        private MeterController m_progressMeter;
        private HandleVector<int>.Handle m_largeCreaturePartitionEntry;
        private HandleVector<int>.Handle m_partitionEntry;

        [Serialize]
        public HashSet<Tag> m_sacrificedSpecies;

        [MyCmpReq]
        private Storage m_sacrificeContainer;

        private int onBuildingSelectHandle;

        [Serialize]
        private bool m_introPopupSeen;
        private Notification m_endNotification;
        [Serialize]
        private bool m_morphModeUnlocked;
        private EventInfoData eventInfo;
    }
    public class GravitasMutanterFounderBuildingStatusItems
    {
        public StatusItem GravitasMutanterFounderWaiting;

        public StatusItem GravitasMutanterFounderProgress;

        public StatusItem GravitasMutanterFounderMorphModeLocked;

        public StatusItem GravitasMutanterFounderMorphMode;

        public StatusItem GravitasMutanterFounderWorking;

        private static GravitasMutanterFounderBuildingStatusItems _instance;
        public static GravitasMutanterFounderBuildingStatusItems Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GravitasMutanterFounderBuildingStatusItems();
                }
                return _instance;
            }
        }
        public void CreateStatusItems(BuildingStatusItems buildingStatusItems)
        {
            GravitasMutanterFounderWaiting = buildingStatusItems.Add(new StatusItem("GravitasMutanterFounderWaiting", "BUILDINGS", "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, true, 129022, null));

            GravitasMutanterFounderProgress = buildingStatusItems.Add(new StatusItem("GravitasMutanterFounderProgress", "BUILDINGS", "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, true, 129022, null));
            GravitasMutanterFounderProgress.resolveStringCallback = delegate (string str, object data)
            {
                Instance instance = (Instance)data;
                return string.Format(str, instance.m_sacrificedSpecies.Count, instance.def.numSpeciesToUnlockMorphMode);
            };
            GravitasMutanterFounderProgress.resolveTooltipCallback = delegate (string str, object data)
            {
                Instance instance = (Instance)data;
                if (instance.m_sacrificedSpecies.Count == 0)
                {
                    str = str + "\n • " + BUILDINGS.STATUSITEMS.GRAVITASMUTANTERFOUNDERPROGRESS.NO_DATA;
                }
                else
                {
                    foreach (Tag tag in instance.m_sacrificedSpecies)
                    {
                        str = str + "\n • " + Strings.Get("STRINGS.CREATURES.FAMILY_PLURAL." + tag.ToString().ToUpper());
                    }
                }
                return str;
            };

            GravitasMutanterFounderMorphModeLocked = buildingStatusItems.Add(new StatusItem("GravitasMutanterFounderMorphModeLocked", "BUILDINGS", "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, true, 129022));

            GravitasMutanterFounderMorphMode = buildingStatusItems.Add(new StatusItem("GravitasMutanterFounderMorphMode", "BUILDINGS", "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, true, 129022));

            GravitasMutanterFounderWorking = buildingStatusItems.Add(new StatusItem("GravitasMutanterFounderWorking", "BUILDINGS", "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, true, 129022));
        }
    }
}