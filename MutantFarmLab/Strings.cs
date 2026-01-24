using Klei;

namespace MutantFarmLab
{
    public static class STRINGS
    {
        #region 建筑文本配置（设备名称/描述/效果）
        public static class BUILDINGS
        {
            public static class PREFABS
            {
                public static class MUTANTFARMLAB
                {
                    public static LocString NAME = "Mutant Farm Lab";
                    public static LocString DESC = "A high-tech lab that consumes radiation particles to mutate seeds into rare variants.";
                    public static LocString EFFECT = "Consumes Radiation Particles to mutate plant seeds.";
                }

                public static class CUSTOMRADIATIONLIGHT
                {
                    public static LocString NAME = "Radiation Light Strip";
                    public static LocString DESC = "Provides radiation for mutated plants using stored particles, adjustable power level.";
                    public static LocString EFFECT = "Emits wide range radiation area by consuming the radioactive particles";
                    public static LocString LOW_PARTICLE_SIGNAL = "Low Particle Signal";
                    public static LocString LOW_PARTICLE_SIGNAL_TOOLTIP = "Triggers when particle storage is below activation threshold.";
                    public static LocString LOGIC_PORT_STORAGE = "Particle Storage";
                    public static LocString LOGIC_PORT_STORAGE_ACTIVE = "Low";
                    public static LocString LOGIC_PORT_STORAGE_INACTIVE = "Full";
                }

                public static class RADIATIONPARTICLEADAPTER
                {
                    public static LocString NAME = "Radiation Particle Adapter";
                    public static LocString DESC = "Attachable accessory for farm tiles, emits radiation to boost mutation rate.";
                    public static LocString EFFECT = "Emits radiation for the farm tiles by consuming the radioactive particles";
                    public static LocString LOGIC_PORT_NAME = "Particle Low Alert";
                    public static LocString LOGIC_PORT_ACTIVE = "Particle Low, Request Refill";
                    public static LocString LOGIC_PORT_INACTIVE = "Particle Sufficient, Working Normal";
                    public static LocString WARNING_NO_FARMTILE = "Unbound Farm Tile";
                    public static LocString WARNING_NO_FARMTILE_TOOLTIP = "Place the first cell of the accessory directly on a farm tile!";
                    public static LocString WARNING_LOW_PARTICLE = "Low Particle Reserves";
                    public static LocString WARNING_LOW_PARTICLE_TOOLTIP = "Radiation paused due to insufficient particles, check your generator.";
                }

                public static class RADIATIONFARMTILE
                {
                    public static LocString NAME = "Radiation Farm Tile";
                    public static LocString DESC = "Can Emits radiation above for the plants on it";
                    public static LocString EFFECT = "Emits radiation for Plants on it by consuming the UraniumOre";
                    public static LocString LOW_URANIUMORE = "Low UraniumOre";
                    public static LocString LOW_URANIUMORE_TOOLTIP = "Low UraniumOre Storage,Can not emmit Radiation.";
                }
            }
        }
        #endregion

        #region UI界面文本配置（面板/提示/任务）
        public static class UI
        {
            public static class UISIDESCREENS
            {
                public static class PLANTERSIDESCREEN
                {
                    public static LocString PLANT = "植株";
                }
                public static class MUTANTFARMLAB
                {
                    public static LocString TITLE = "Seed Mutation";
                    public static LocString NONE_DISCOVERED = "No Seeds Unlocked";
                    public static LocString SELECT_SEEDS = "Select Seeds";
                    public static LocString SEED_FORBIDDEN = "Forbidden Seeds";
                    public static LocString SEED_ALLOWED = "Allowed Seeds";
                    public static LocString SEED_NO_MUTANTS = "No Mutation Branches";
                    public static LocString FILTER_CATEGORY = "Seeds To Deliver";
                }

                public static class SLIDERCONTROL
                {
                    public static LocString TITLE = "Radiation Power Setting";
                    public static LocString DESC = "Adjust power level and particle consumption rate";
                    public static LocString TOOLTIP = "Higher power = stronger radiation = faster particle consumption";
                    public static LocString ANOTHER_PLANT = "Another";
                }
            }

            public static class NOTIFICATIONS
            {
                public static class MUTANTFARM_NEED_MATERIALS
                {
                    public static LocString NAME = "Insufficient Mutation Conditions";
                    public static LocString TOOLTIP = "Requires: Power On + Valid Seeds Stored + Radiation Particles ≥10";
                }
            }

            public static class UNITSUFFIXES
            {
                public static class RADIATION
                {
                    public static LocString SETTING_RAD_LEVEL = "Radiation Level Setting";
                    public static LocString RADLEVEL = "Level";
                }
            }

            public static class TASKS
            {
                public static class CANCEL_REASONS
                {
                    public static LocString STORAGE_FULL = "Storage is full.";
                    public static LocString DESTROYED = "Task Destroyed.";
                }
            }
            public static class STATUSITEMS
            {
                public static class NEEDPARTICLES
                {
                    public static LocString NAME = "Insufficient Radiation Particles";
                    public static LocString TOOLTIP = "The Mutant Farm Lab requires Radiation Particles {0} to mutate per seed.";
                }
                public static class CONSUMERATEURANIUMORE
                {
                    public static LocString NAME = "Uranium Ore Consume Rate: {0}g/s";
                    public static LocString TOOLTIP = "The Radaition Farmtile requires Uranium Ore {0}g/s.";
                }
                public static class CONSUMERATEPARTICLES
                {
                    public static LocString NAME = "HighEnergy Particles Consume Rate: {0}/s";
                    public static LocString TOOLTIP = "Requires Radiation Particles {0}/s";
                }
            }
        }
        #endregion

        #region 物品文本配置（核心：抗辐籽RADSEED，与hjson严格对齐）
        public static class ELEMENT
        {
            public static class RADSEED
            {
                public static LocString NAME = "Rad Seed";
                public static LocString DESC = "Radiation Resistence Seed, eating it can completely clear the body's radiation and gain radiation immunity for a period of time.";
            }
            public static class DULHEAD
            {
                public static LocString NAME = "Dual Head";
                public static LocString DESC = "The plant has evolved a twin-stem structure, hosting two growth cores in a single tile. They share the same growing conditions yet mature independently; while the yield of a single head is slightly reduced, the overall space efficiency is drastically improved. At high quality, the dual cores will bear seeds separately.";
            }
            public static class ACTINO
            {
                public static LocString NAME = "Actinobacteria";
                public static LocString DESC = "A special fungal mutant nourished by radiation, with three core values of \"sterilization, fluorescence, and energy saving\". It can use radiation energy to eliminate germs on itself and around it, emit soft fluorescence to meet the lighting needs of planting, and significantly reduce water and fertilizer consumption, serving as the \"environmental steward\" of the radiation farm; the cost is an extremely long growth cycle and the release of a small amount of radiation outward.";
            }
            public static class OILENRICH{
                public static LocString NAME = "Oil-Enriched";
                public static LocString DESC = "An industrial-grade mutant plant exclusive to radiation farms, whose core function is to efficiently absorb carbon dioxide and convert it into high-value crude oil, serving as the core hub of the \"carbon-oil-energy\" cycle in late-game bases. It relies on a radiation environment to activate the conversion mechanism; when paired with Actinobacteria, it can achieve self-sufficiency in lighting and germ purification. The cost is increased nutrient consumption and extended growth cycle, suitable for the resource cycle needs of large-scale industrial bases.";
            }
        }
        #endregion
        #region ✅ 新增：食物文本节点（解决 ITEMS.FOOD 缺失）
        public static class ITEMS
        {
            public static class FOOD
            {
                public static class RADSEED
                {
                    public static LocString NAME = "Radiation Seed";
                    public static LocString DESC = "A luminous seed that purges radiation and grants radiation immunity when eaten.";
                }
            }
        }
        #endregion

        #region ✅ 新增：效果文本节点（解决 DUPLICANTS.MODIFIERS 缺失）
        public static class DUPLICANTS
        {
            public static class MODIFIERS
            {
                // 辐射清零效果文本
                public static class RADCLEAR
                {
                    public static LocString NAME = "Radiation Purge";
                    public static LocString DESCRIPTION = "All radiation in the body is instantly eliminated.";
                }
                // 辐射免疫效果文本
                public static class RADIMMUNITY
                {
                    public static LocString NAME = "Radiation Immunity";
                    public static LocString DESCRIPTION = "Completely immune to radiation damage for 5 cycles.";
                }
            }
        }
        #endregion
        public static class MUTANTS
        {
            public static class EFFECTS
            {
                public static class DUAL_HEAD_SYMBIOSIS
                {
                    public static LocString NAME = "Dual-Head Symbiosis";
                    public static LocString DESC = "The two plant cores of the Dual-Head share growing conditions; individual head yield is slightly reduced. \n'Dual-Head Symbiosis' benefits: \n50% reduction in additional fertilizer required \nweighted average growth cycle.";
                }
            }
        }
    }
}