using Klei.AI;

namespace MutantContainmentProject.Skills
{
    public class MutanterAttributes
    {
        public static string AttributeBraveryID = "Bravery";
        public static string AttributeMentalResistanceID = "MentalResistance";
        public static string AttributeSuccessRateID = "SuccessRate";
        public static string AttributeWorkingSpeedID = "WorkingSpeed";
        public static string AttributeAttackDamageID = "AttackDamage";
        public static void AttributeBravery(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeBraveryID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_excavation"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }
        public static void AttributeMentalResistance(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeMentalResistanceID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_excavation"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }
        public static void AttributeSuccessRate(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeSuccessRateID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_excavation"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }
        public static void AttributeWorkingSpeed(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeWorkingSpeedID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_excavation"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }

        public static void AttributeAttackDamage(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeAttackDamageID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_excavation"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }
    }
}
