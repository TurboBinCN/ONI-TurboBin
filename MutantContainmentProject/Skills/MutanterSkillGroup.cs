using Database;
using Klei.AI;
using STRINGS;

namespace MutantContainmentProject.Skills
{
    public class MutanterSkillGroup : SkillGroup, IListableOption
    {
        public MutanterSkillGroup(string id, string choreGroupID, string name, string icon, string archetype_icon) : base(id, choreGroupID, name, icon, archetype_icon)
        {
        }

        string IListableOption.GetProperName()
        {
            // 优先使用ChoreGroup的ARCHETYPE_NAME
            if (!string.IsNullOrEmpty(choreGroupID))
            {
                ChoreGroup choreGroup = Db.Get().ChoreGroups.Get(choreGroupID);
                if (choreGroup != null)
                {
                    // 尝试获取ARCHETYPE_NAME
                    string archetypeName = (string)Strings.Get($"STRINGS.DUPLICANTS.CHOREGROUPS.{choreGroupID.ToUpper()}.ARCHETYPE_NAME");
                    if (!string.IsNullOrEmpty(archetypeName))
                    {
                        return archetypeName;
                    }
                }
            }
            // 如果没有ARCHETYPE_NAME，使用默认的NAME
            return (string)Strings.Get($"STRINGS.DUPLICANTS.SKILLGROUPS.{Id.ToUpper()}.NAME");
        }
    }
}