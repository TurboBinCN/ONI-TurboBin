using Klei.AI;

namespace MutantContainmentProject.Skills
{
    public class MutanterAttributes
    {
        public static string AttributeBraveryID = "Bravery";
        public static string AttributeDefenseID = "Defense";
        public static string AttributeDisciplineID = "Discipline";
        public static string AttributeRighteousnessID = "Righteousness";
        public static void AttributeBravery(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeBraveryID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_health"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }
        public static void AttributeDefense(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeDefenseID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "icon_errand_metal_resistance"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }
        public static void AttributeDiscipline(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeDisciplineID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_strength"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }

        public static void AttributeRighteousness(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeRighteousnessID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_stamina"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }
    }
}
