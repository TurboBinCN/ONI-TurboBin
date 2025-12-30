using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MutantFarmLab
{
    public static class STRINGS
    {
        public static class BUILDINGS
        {
            public static class PREABS
            {
                public static class MUTANTFARMLAB{
                    public static string NAME = "Mutant farm lab";
                    public static string EFFECT = "";
                    public static string DESC = "";
                }
            }
        }
        public static class UI
        {
            public static class UISIDESCREENS
            {
                public static class MUTANTFARMLAB
                {
                    public static string TITLE ="title";
                    public static string NONE_DISCOVERED = "No seeds";
                    public static string SELECT_SEEDS = "Select seeds";
                    public static string SEED_FORBIDDEN = "Forbidden seeds";
                    public static string SEED_ALLOWED = "Allowed seeds";
                    public static string SEED_NO_MUTANTS = "seed has no subpieces";
                    public static string FILTER_CATEGORY = "Seeds To Delivery";
                }
            }
            public static class NOTIFICATIONS
            {
                public static class MUTANTFARM_NEED_MATERIALS
                {
                    public static LocString NAME = "变异条件不足";
                    public static LocString TOOLTIP = "需要满足：设备通电 + 有效种子入库 + 高能粒子充足(≥10)";
                }
            }
        }
    }
}
