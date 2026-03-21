using Database;
using System;
using TBB.He.TbbLib.Debuger;

namespace MutantContainmentProject.MutanterComponent
{
    class MutanterStatusItems : StatusItems
    {
        public StatusItem Idle;
        public StatusItem Incapacitated;// (瘫痪): 畸变体无法行动。
        public StatusItem Sealed;// (封印): 在完美收容下，行为被抑制。
        public StatusItem Stable;// (稳定): 在正常收容下，表现平静或执行低威胁行为。
        public StatusItem Agitated;// (焦躁): 收容出现问题时，开始表现出攻击性或不安。
        public StatusItem Hostile;// (敌对): 收容失效或达到特定条件时，进入全面攻击模式。
        //public StatusItem SpecialAction;// (特殊行动): 执行与其背景故事或特性相关的独特行为
        public StatusItem AttackLoop;// (正在攻击): 畸变体正在攻击。
        public StatusItem MeleeAttack;// (近战攻击): 畸变体正在使用近战攻击。
        public StatusItem PsychologicalAttack;// (精神攻击): 畸变体正在使用精神攻击。
        public StatusItem ErosionAttack;// (侵蚀攻击): 畸变体正在使用侵蚀攻击。
        public StatusItem SoulAttack;// (灵魂攻击): 畸变体正在使用灵魂攻击。
        public StatusItem BuildingDestruction;// (建筑破坏): 畸变体正在破坏建筑。

        private static MutanterStatusItems _instance;
        public static MutanterStatusItems Instance { get => _instance; }
        public MutanterStatusItems(ResourceSet parent)
        : base("MutanterStatusItems", parent)
        {
            CreateStatusItems();
            _instance = this;
        }
        private StatusItem CreateStatusItem(string id, string prefix, string icon, StatusItem.IconType icon_type, NotificationType notification_type, bool allow_multiples, HashedString render_overlay, bool showWorldIcon = true, int status_overlays = 2)
        {
            return Add(new StatusItem(id, prefix, icon, icon_type, notification_type, allow_multiples, render_overlay, showWorldIcon, status_overlays));
        }

        private StatusItem CreateStatusItem(string id, string name, string tooltip, string icon, StatusItem.IconType icon_type, NotificationType notification_type, bool allow_multiples, HashedString render_overlay, int status_overlays = 2)
        {
            return Add(new StatusItem(id, name, tooltip, icon, icon_type, notification_type, allow_multiples, render_overlay, status_overlays));
        }
        private void CreateStatusItems()
        {
            TbbDebuger.LogDebug($"CreateStatusItems");
            Func<string, object, string> resolveStringCallback = delegate (string str, object data)
            {
                Workable workable = (Workable)data;
                if (workable != null && workable.GetComponent<KSelectable>() != null)
                {
                    str = str.Replace("{Target}", workable.GetComponent<KSelectable>().GetName());
                }

                return str;
            };
            Func<string, object, string> resolveStringCallback2 = delegate (string str, object data)
            {
                Workable workable = (Workable)data;
                if (workable != null)
                {
                    str = str.Replace("{Target}", workable.GetComponent<KSelectable>().GetName());
                    ComplexFabricatorWorkable complexFabricatorWorkable = workable as ComplexFabricatorWorkable;
                    if (complexFabricatorWorkable != null)
                    {
                        ComplexRecipe currentWorkingOrder = complexFabricatorWorkable.CurrentWorkingOrder;
                        if (currentWorkingOrder != null)
                        {
                            str = str.Replace("{Item}", currentWorkingOrder.FirstResult.ProperName());
                        }
                    }
                }

                return str;
            };
            Idle = CreateStatusItem("Idle", "MUTANTERS", "", StatusItem.IconType.Info, NotificationType.Neutral, allow_multiples: false, OverlayModes.None.ID);
            Incapacitated = CreateStatusItem("Incapacitated", "MUTANTERS", "", StatusItem.IconType.Exclamation, NotificationType.BadMinor, allow_multiples: false, OverlayModes.None.ID);

            Sealed = CreateStatusItem("Sealed", "MUTANTERS", "", StatusItem.IconType.Exclamation, NotificationType.Good, allow_multiples: false, OverlayModes.None.ID);

            Stable = CreateStatusItem("Stable", "MUTANTERS", "", StatusItem.IconType.Info, NotificationType.Good, allow_multiples: false, OverlayModes.None.ID);

            Agitated = CreateStatusItem("Agitated", "MUTANTERS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, allow_multiples: false, OverlayModes.None.ID);
            Agitated.AddNotification();

            Hostile = CreateStatusItem("Hostile", "MUTANTERS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, allow_multiples: false, OverlayModes.None.ID);
            Hostile.AddNotification();
            
            AttackLoop = CreateStatusItem("AttackLoop", "MUTANTERS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, allow_multiples: false, OverlayModes.None.ID);
            AttackLoop.AddNotification();
            
            MeleeAttack = CreateStatusItem("MeleeAttack", "MUTANTERS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, allow_multiples: false, OverlayModes.None.ID);
            
            PsychologicalAttack = CreateStatusItem("PsychologicalAttack", "MUTANTERS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, allow_multiples: false, OverlayModes.None.ID);
            
            ErosionAttack = CreateStatusItem("ErosionAttack", "MUTANTERS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, allow_multiples: false, OverlayModes.None.ID);
            
            SoulAttack = CreateStatusItem("SoulAttack", "MUTANTERS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, allow_multiples: false, OverlayModes.None.ID);
            
            BuildingDestruction = CreateStatusItem("BuildingDestruction", "MUTANTERS", "", StatusItem.IconType.Exclamation, NotificationType.Bad, allow_multiples: false, OverlayModes.None.ID);
            BuildingDestruction.AddNotification();
        }
    }
}
