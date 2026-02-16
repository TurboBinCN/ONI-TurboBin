using Klei.AI;

namespace MutantContainmentProject.Skills
{
    public class MutanterAttributes
    {
        public static string AttributeBraveryID = "Bravery";
        public static string AttributeWillPowerID = "WillPower";
        public static string AttributeSuccessRateID = "SuccessRate";
        public static string AttributeWorkingSpeedID = "WorkingSpeed";
        public static string AttributeAttackSpeedID = "AttackSpeed";
        public static string AttributeMovingSpeedID = "MovingSpeed";
        public static void AttributeBravery(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeBraveryID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_excavation"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }
        public static void AttributeWillPower(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeWillPowerID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_excavation"));

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
        public static void AttributeAttackSpeed(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeAttackSpeedID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_excavation"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }
        public static void AttributeMovingSpeed(Database.Attributes __instance)
        {
            var attribute = __instance.Add(new Attribute(AttributeMovingSpeedID, is_trainable: true, Attribute.Display.Skill, is_profession: true, 0f, null, null, "mod_excavation"));

            attribute.SetFormatter(new StandardAttributeFormatter(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.None));
        }
    }
}
