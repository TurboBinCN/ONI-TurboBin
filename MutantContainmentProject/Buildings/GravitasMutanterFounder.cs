using Database;
using KSerialization;
using MutantContainmentProject.Buildings;
using MutantContainmentProject.MutanterComponent;
using MutantContainmentProject.Mutanters;
using MutantContainmentProject.MutanterStoryStraits;
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
                smi.UpdateMeter(); // 更新进度条，显示已收集的物种
            })
            .ToggleMainStatusItem(GravitasMutanterFounderBuildingStatusItems.Instance.GravitasMutanterFounderWaiting, null)
            .ParamTransition(unlockConditionMet, operational.activating.pre, (Instance smi, bool met) =>
            {
                TbbDebuger.LogDebug($"[GravitasMutanterFounder] states:operational.idle unlockConditionMet");
                return met;
            })
            // 检查是否正在冷却
            .ParamTransition(cooldownTimer, operational.cooldown, IsGTZero);

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
                smi.sm.unlockConditionMet.Set(false, smi, false);
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

    public BoolParameter unlockConditionMet;

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
        public Dictionary<Tag, Dictionary<Tag, int>> sacrificeRecipes = new() {
            {
                SCP173Config.ID, new Dictionary<Tag, int> {
                    { HatchConfig.ID, 3 }// 需要3个Hatch
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

            m_progressMeter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.UserSpecified, Grid.SceneLayer.TileFront, Array.Empty<string>());


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
            if (!((Boxed<bool>)obj).value)
            {
                return;
            }
            if (!m_introPopupSeen)
            {
                ShowIntroNotification();
            }
            if (m_endNotification != null)
            {
                m_endNotification.customClickCallback(m_endNotification.customClickData);
            }
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
            // 更新进度条，显示已献祭的不同物种数量 / 总需求数量
            m_progressMeter.SetPositionPercent(Mathf.Clamp01(m_sacrificedSpecies.Count / (float)smi.def.numSpeciesToUnlockMorphMode));
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
                TbbDebuger.LogDebug($"[GravitasMutanterFounder] Store [{pickupable.gameObject.GetInstanceID()}]");
                // 尝试将物品放入献祭容器
                if (m_sacrificeContainer.Store(pickupable.gameObject, false, false, true, false))
                {
                    // 记录新物种
                    var creatureBrain = pickupable.GetComponent<CreatureBrain>();
                    if (creatureBrain != null)
                    {
                        m_sacrificedSpecies.Add(creatureBrain.species);
                    }

                    UpdateStatusItems();
                    UpdateMeter();

                    // 检查开启条件是否满足
                    CheckAndSetUnlockCondition();
                }
            }
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
        private void CheckAndSetUnlockCondition()
        {
            // 检查是否有足够的物品满足任意一个配方
            bool conditionMet = CanActivateAndSpawn(out m_activationTarget, out m_usedRecipe);
            sm.unlockConditionMet.Set(conditionMet, this, false);
            if (conditionMet)
            {
                // 添加 null 检查，防止在 m_usedRecipe 为 null 时调用 .Select()
                string recipeDetails = m_usedRecipe != null
                    ? string.Join(", ", m_usedRecipe.Select(kvp => $"{kvp.Key}:{kvp.Value}"))
                    : "没有匹配的配方或者发生错误";
                TbbDebuger.LogDebug($"[GravitasMutanterFounder] 解锁0conditionmet! 准备生成{m_activationTarget}配方: {recipeDetails}");
            }
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

            //TODO 生成畸变体的位置应该是随机的，需要修改
            Vector3 spawnPos = Grid.CellToPosCBC(Grid.PosToCell(smi), Grid.SceneLayer.Creatures) + smi.def.dropOffset.ToVector3();

            //int numToSpawn = UnityEngine.Random.Range(smi.def.minMutantsPerSpawn, smi.def.maxMutantsPerSpawn + 1);
            int numToSpawn = 1;
            for (int i = 0; i < numToSpawn; i++)
            {
                GameObject mutantGO = Util.KInstantiate(Assets.GetPrefab(m_activationTarget), spawnPos);
                if (mutantGO != null)
                {
                    mutantGO.SetActive(true);
                    TbbDebuger.LogDebug($"[GravitasMutanterFounder] 生成畸变体： {m_activationTarget} @ [{spawnPos}]");
                    //TODO 生成畸变体后的动画等 清空FOW&Camera Fade In

                    //FocusTargetSequence.Start(mutantGO.GetComponent<MonoBehaviour>(), new FocusTargetSequence.Data
                    //{
                    //    WorldId = mutantGO.GetMyWorldId(),
                    //    OrthographicSize = 6f,
                    //    TargetSize = 6f,
                    //    Target = spawnPos,
                    //    PopupData = eventInfo,
                    //    CompleteCB = new System.Action(OnStorySequenceComplete),
                    //    CanCompleteCB = null
                    //});
                    return m_activationTarget;
                }
            }
            return Tag.Invalid;
        }

        public void ShowIntroNotification()
        {
            Game.Instance.unlocks.Unlock(GravitasMutanterFounderConfig.INITIAL_LORE_UNLOCK_ID, true);
            m_introPopupSeen = true;
            EventInfoScreen.ShowPopup(EventInfoDataHelper.GenerateStoryTraitData(CODEX.STORY_TRAITS.MUTANTER_FOUNDER.BEGIN_POPUP.NAME, CODEX.STORY_TRAITS.MUTANTER_FOUNDER.BEGIN_POPUP.DESCRIPTION, CODEX.STORY_TRAITS.CLOSE_BUTTON, "crittermanipulatoractivate_kanim", EventInfoDataHelper.PopupType.BEGIN, null, null, null));
        }
        public void TryShowCompletedNotification()
        {
            if (MutanterSpeciesCatalog.Instance.GetMutanterSpeciesCount() < smi.def.numSpeciesToUnlockMorphMode || IsMorphMode)
                return;

            //TODO 需要完成显示信息界面
            eventInfo = EventInfoDataHelper.GenerateStoryTraitData((string)CODEX.STORY_TRAITS.MUTANTER_FOUNDER.END_POPUP.NAME, (string)CODEX.STORY_TRAITS.MUTANTER_FOUNDER.END_POPUP.DESCRIPTION, (string)CODEX.STORY_TRAITS.MUTANTER_FOUNDER.END_POPUP.BUTTON, "crittermanipulatormorphmode_kanim", EventInfoDataHelper.PopupType.COMPLETE);

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
        public void UnlockMorphMode(object _)
        {
            if (m_morphModeUnlocked) return;

            Game.Instance.unlocks.Unlock(GravitasMutanterFounderConfig.COMPLETED_LORE_UNLOCK_ID, true);

            if (m_endNotification != null)
            {
                gameObject.AddOrGet<Notifier>().Remove(m_endNotification);
            }
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
            //TODO 
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