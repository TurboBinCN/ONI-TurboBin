using KSerialization;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GravitasMutanterFounder : GameStateMachine<GravitasMutanterFounder, GravitasMutanterFounder.Instance, IStateMachineTarget, GravitasMutanterFounder.Def>
{
    public override void InitializeStates(out BaseState default_state)
    {
        default_state = this.inoperational;
        base.serializable = StateMachine.SerializeType.ParamsOnly;

        root.Enter(delegate (Instance smi)
        {
            smi.Initialize();
        });

        inoperational.PlayAnim("off")
            .EventTransition(GameHashes.OperationalChanged, operational.idle, (Instance smi) => smi.GetComponent<Operational>().IsOperational);

        // Operational Group: 主要工作状态组
        operational.DefaultState(operational.idle)
            .EventTransition(GameHashes.OperationalChanged, this.inoperational, (Instance smi) => !smi.GetComponent<Operational>().IsOperational);

        // Idle State: 等待献祭和检查开启条件
        operational.idle.PlayAnim("idle", KAnim.PlayMode.Loop)
            .Enter(delegate (Instance smi)
            {
                smi.UpdateMeter(); // 更新进度条，显示已收集的物种
            })
            .ToggleMainStatusItem(Db.Get().BuildingStatusItems.CreatureManipulatorWaiting, null) // 使用相似的状态项
                                                                                                 // 检查是否满足开启条件 (物种数量 + 物品等级)
            .ParamTransition<bool>(unlockConditionMet, operational.activating.pre, (Instance smi, bool met) => met)
            // 检查是否正在冷却
            .ParamTransition(cooldownTimer, operational.cooldown, IsGTZero);

        // Activating Group: 从准备激活到实际激活的过程
        operational.activating.DefaultState(operational.activating.pre)
            .ToggleMainStatusItem(Db.Get().BuildingStatusItems.CreatureManipulatorWorking, null); // 使用相似的工作状态项

        // Pre Activation: 动画前奏，消耗献祭物品
        operational.activating.pre.PlayAnim("working_pre")
            .OnAnimQueueComplete(operational.activating.loop)
            .Enter(delegate (Instance smi)
            {
                smi.ConsumeSacrificeItems(); // 在这里消耗掉作为献祭的物品
                smi.SetActivationLevel(); // 根据消耗的物品设置本次激活的等级
                smi.sm.activationTimer.Set(smi.def.activationDuration, smi, false); // 设置激活过程的持续时间
            })
            .Exit(delegate (Instance smi)
            {
                // 激活时间结束后的后续处理（如果需要）
            });

        // Loop Activation: 激活动画循环
        operational.activating.loop.PlayAnim("working_loop", KAnim.PlayMode.Loop)
            .Update(delegate (Instance smi, float dt)
            {
                smi.sm.activationTimer.DeltaClamp(-dt, 0f, float.MaxValue, smi);
            }, UpdateRate.SIM_1000ms, false)
            .ParamTransition<float>(activationTimer, operational.activating.pst, IsLTEZero);

        // Post Activation: 激活动画结束，生成畸变体和陷阱
        operational.activating.pst.PlayAnim("working_pst")
            .OnAnimQueueComplete(operational.cooldown) // 激活完成后直接进入冷却
            .Enter(delegate (Instance smi)
            {
                smi.SpawnMutants(); // 生成畸变体
                smi.TryTriggerTrap(); // 尝试触发陷阱
                // 计算并设置冷却时间，基于激活等级
                float cooldownDuration = smi.GetCooldownDurationForLevel(smi.ActivationLevel);
                smi.sm.cooldownTimer.Set(cooldownDuration, smi, false);
            });

        // Cooldown State: 冷却期间，无法再次激活
        State cooldownState = operational.cooldown
            .PlayAnim("working_cooldown", KAnim.PlayMode.Loop)
            .Update(delegate (Instance smi, float dt)
            {
                smi.sm.cooldownTimer.DeltaClamp(-dt, 0f, float.MaxValue, smi);
            }, UpdateRate.SIM_1000ms, false)
            .ParamTransition(cooldownTimer, operational.idle, IsLTEZero);

        // 添加冷却状态的进度条状态项
        string cooldownName = "GRAVITAS_MUTANTER_FOUNDER_COOLDOWN"; // 替换为实际的STRINGS键
        string cooldownTooltip = "GRAVITAS_MUTANTER_FOUNDER_COOLDOWN_TOOLTIP"; // 替换为实际的STRINGS键
        string icon = "";
        StatusItem.IconType icon_type = StatusItem.IconType.Info;
        NotificationType notification_type = NotificationType.Neutral;
        bool allow_multiples = false;
        StatusItemCategory main = Db.Get().StatusItemCategories.Main;
        Func<string, Instance, string> resolve_string_callback = new Func<string, Instance, string>(CooldownProcessing);
        Func<string, Instance, string> resolve_tooltip_callback = new Func<string, Instance, string>(CooldownProcessingTooltip);
        cooldownState.ToggleStatusItem(cooldownName, cooldownTooltip, icon, icon_type, notification_type, allow_multiples, default(HashedString), 129022, resolve_string_callback, resolve_tooltip_callback, main);
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

    // public StateMachine<GravitasMutanterFounder, GravitasMutanterFounder.Instance, IStateMachineTarget, GravitasMutanterFounder.Def>.ObjectParameter sacrificeContainer; 

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
        public Instance(IStateMachineTarget master, Def def) : base(master, def)
        {
            this.pickupCell = Grid.OffsetCell(Grid.PosToCell(master.gameObject), base.smi.def.pickupOffset);
            this.m_partitionEntry = GameScenePartitioner.Instance.Add("GravitasMutanterFounder", base.gameObject, this.pickupCell, GameScenePartitioner.Instance.pickupablesChangedLayer, new Action<object>(this.DetectSacrifice));
            this.m_progressMeter = new MeterController(base.GetComponent<KBatchedAnimController>(), "meter_target", "meter", Meter.Offset.UserSpecified, Grid.SceneLayer.TileFront, Array.Empty<string>());

            // 初始化献祭相关的集合
            this.m_sacrificedSpecies = new HashSet<Tag>();
            this.m_sacrificeContainer = master.gameObject.GetComponent<Storage>();
            m_sacrificeContainer.allowItemRemoval = false; // 防止外部轻易拿走献祭品
            m_sacrificeContainer.showDescriptor = false;
            m_sacrificeContainer.storageFilters = def.requiredSacrificeTags; // 只接受指定类型的献祭
            m_sacrificeContainer.capacityKg = 2000f; // 设置容量
        }

        public override void StartSM()
        {
            base.StartSM();
            this.UpdateStatusItems();
            this.UpdateMeter();
            // 可能的故事事件或通知初始化
            // StoryManager.Instance.ForceCreateStory(Db.Get().Stories.MutanterFounder, base.gameObject.GetMyWorldId());
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
            KSelectable component = base.gameObject.GetComponent<KSelectable>();
            // 根据是否解锁（物种数达到要求）显示不同状态项
            component.ToggleStatusItem(Db.Get().BuildingStatusItems.CreatureManipulatorProgress, !this.IsUnlocked, this); // 复用进度条
            component.ToggleStatusItem(Db.Get().BuildingStatusItems.CreatureManipulatorMorphMode, this.IsUnlocked, this); // 复用解锁后状态
            component.ToggleStatusItem(Db.Get().BuildingStatusItems.CreatureManipulatorMorphModeLocked, !this.IsUnlocked, this); // 复用锁定状态
        }

        public void UpdateMeter()
        {
            // 更新进度条，显示已献祭的不同物种数量 / 总需求数量
            m_progressMeter.SetPositionPercent(Mathf.Clamp01((float)m_sacrificedSpecies.Count / (float)base.smi.def.numSpeciesToUnlockMorphMode));
        }

        public bool IsUnlocked
        {
            get
            {
                return m_sacrificedSpecies.Count >= base.smi.def.numSpeciesToUnlockMorphMode;
            }
        }

        public bool HasSacrificeItems
        {
            get
            {
                return m_sacrificeContainer.items.Count > 0;
            }
        }

        public int ActivationLevel { get; private set; } = 0;

        private void DetectSacrifice(object obj)
        {
            Pickupable pickupable = obj as Pickupable;
            if (pickupable == null || !this.IsSacrificeValid(pickupable.KPrefabID))
            {
                return;
            }

            // 检查是否在正确的状态下接收献祭
            if (base.smi.IsInsideState(base.smi.sm.operational.idle))
            {
                // 尝试将物品放入献祭容器
                if (m_sacrificeContainer.Store(pickupable.gameObject, false, false, true, false))
                {
                    // 记录新物种
                    var creatureBrain = pickupable.GetComponent<CreatureBrain>();
                    if (creatureBrain != null)
                    {
                        m_sacrificedSpecies.Add(creatureBrain.species);
                    }
                    // 如果是其他类型物品，可以根据其标签或预制件ID记录

                    this.UpdateStatusItems();
                    this.UpdateMeter();

                    // 检查开启条件是否满足
                    this.CheckAndSetUnlockCondition();
                }
            }
        }

        public bool IsSacrificeValid(KPrefabID kpid)
        {
            // 检查物品标签是否在必需列表中
            foreach (var tag in base.smi.def.requiredSacrificeTags)
            {
                if (kpid.HasTag(tag))
                {
                    return true;
                }
            }
            return false;
        }

        private void CheckAndSetUnlockCondition()
        {
            bool conditionMet = this.IsUnlocked && this.HasSacrificeItems;
            sm.unlockConditionMet.Set(conditionMet, this, false);
        }

        public void ConsumeSacrificeItems()
        {
            // 一次性清空献祭容器，模拟消耗
            List<GameObject> itemsToConsume = new List<GameObject>(m_sacrificeContainer.items);
            foreach (var item in itemsToConsume)
            {
                item.DeleteObject();
            }
            // 清空记录的物种集合，因为献祭已经发生 (如果需要持久化物种解锁状态，应单独保存)
            // this.m_sacrificedSpecies.Clear(); 
        }

        public void SetActivationLevel()
        {
            // 简单示例：根据消耗的物品数量或特定物品类型来设置等级
            // 这里可以更复杂，比如分析消耗物品的 "power" 或 "tier"
            int consumedCount = m_sacrificeContainer.items.Count; // 实际上容器已清空，可能需要缓存
            // 临时示例：如果消耗了超过一定数量的物品，视为高级开启
            this.ActivationLevel = consumedCount > 2 ? 2 : 1; // Level 1 or 2
        }

        public float GetCooldownDurationForLevel(int level)
        {
            float baseCooldown = base.smi.def.baseCooldownDuration;
            float bonusCooldown = (level > 1) ? base.smi.def.highLevelCooldownBonus : 0f;
            return baseCooldown + bonusCooldown;
        }

        public void SpawnMutants()
        {
            int numToSpawn = UnityEngine.Random.Range(base.smi.def.minMutantsPerSpawn, base.smi.def.maxMutantsPerSpawn + 1);
            Vector3 spawnPos = Grid.CellToPosCBC(Grid.PosToCell(base.smi), Grid.SceneLayer.Creatures) + base.smi.def.dropOffset.ToVector3();

            for (int i = 0; i < numToSpawn; i++)
            {
                // TODO: 选择要生成的具体畸变体 prefab
                Tag mutantPrefabTag = TagManager.Create("MutantPrefab"); // 替换为实际的 prefab 标签
                GameObject mutantGO = Util.KInstantiate(Assets.GetPrefab(mutantPrefabTag), spawnPos);
                if (mutantGO != null)
                {
                    mutantGO.SetActive(true);
                    // 触发生成事件或动画
                }
            }
        }

        public void TryTriggerTrap()
        {
            float chance = base.smi.def.trapChance;
            if (UnityEngine.Random.value < chance)
            {
                TriggerSpecificTrap();
            }
        }
        //private void Scan(Tag species)
        //{
        //    if (this.ScannedSpecies.Add(species))
        //    {
        //        base.gameObject.Trigger(1980521255, null);
        //        this.UpdateStatusItems();
        //        this.UpdateMeter();
        //        this.ShowCritterScannedNotification(species);
        //    }
        //    this.TryShowCompletedNotification();
        //}
        //public void ShowCritterScannedNotification(Tag species)
        //{
        //    Game.Instance.unlocks.Unlock(GravitasCreatureManipulatorConfig.CRITTER_LORE_UNLOCK_ID.For(species), false);
        //    ShowCritterScannedNotificationAndWaitForClick().Then((System.Action)(() => GravitasCreatureManipulator.Instance.ShowLoreUnlockedPopup(species)));

        //    Promise ShowCritterScannedNotificationAndWaitForClick()
        //    {
        //        return new Promise((System.Action<System.Action>)(resolve =>
        //        {
        //            Notification notification1 = new Notification((string)CODEX.STORY_TRAITS.CRITTER_MANIPULATOR.UNLOCK_SPECIES_NOTIFICATION.NAME, NotificationType.Event, (Func<List<Notification>, object, string>)((notifications, obj) =>
        //            {
        //                string str = (string)CODEX.STORY_TRAITS.CRITTER_MANIPULATOR.UNLOCK_SPECIES_NOTIFICATION.TOOLTIP;
        //                foreach (Notification notification2 in notifications)
        //                {
        //                    string tooltipData = notification2.tooltipData as string;
        //                    str = $"{str}\n • {(string)Strings.Get("STRINGS.CREATURES.FAMILY_PLURAL." + tooltipData)}";
        //                }
        //                return str;
        //            }), (object)species.ToString().ToUpper(), false, custom_click_callback: (Notification.ClickCallback)(obj => resolve()), clear_on_click: true);
        //            this.gameObject.AddOrGet<Notifier>().Add(notification1);
        //        }));
        //    }
        //}
        //public void TryShowCompletedNotification()
        //{
        //    if (this.ScannedSpecies.Count < this.smi.def.numSpeciesToUnlockMorphMode || this.IsMorphMode)
        //        return;
        //    this.eventInfo = EventInfoDataHelper.GenerateStoryTraitData((string)CODEX.STORY_TRAITS.CRITTER_MANIPULATOR.END_POPUP.NAME, (string)CODEX.STORY_TRAITS.CRITTER_MANIPULATOR.END_POPUP.DESCRIPTION, (string)CODEX.STORY_TRAITS.CRITTER_MANIPULATOR.END_POPUP.BUTTON, "crittermanipulatormorphmode_kanim", EventInfoDataHelper.PopupType.COMPLETE);
        //    this.m_endNotification = EventInfoScreen.CreateNotification(this.eventInfo, new Notification.ClickCallback(this.UnlockMorphMode));
        //    this.gameObject.AddOrGet<Notifier>().Add(this.m_endNotification);
        //    this.gameObject.GetComponent<KSelectable>().AddStatusItem(Db.Get().MiscStatusItems.AttentionRequired, (object)this.smi);
        //}

        private void TriggerSpecificTrap()
        {
            // TODO: 实现具体的陷阱效果
            // 例如，释放毒气、瞬间高温区域、播放特效动画等
            // SimUtil.DamageArea(...);
            // ElementLoader.FindElementByHash(SimHashes.ToxicSand).CreateDiseaseInCell(...);
            Debug.Log($"[GravitasMutanterFounder] Trap triggered at {base.gameObject.name}!");
        }

        protected override void OnCleanUp()
        {
            GameScenePartitioner.Instance.Free(ref this.m_partitionEntry);
        }

        public int pickupCell;

        [MyCmpGet]
        private Operational m_operational;

        private MeterController m_progressMeter;

        private HandleVector<int>.Handle m_partitionEntry;

        [Serialize]
        private HashSet<Tag> m_sacrificedSpecies;

        [MyCmpReq]
        private Storage m_sacrificeContainer;
    }
}