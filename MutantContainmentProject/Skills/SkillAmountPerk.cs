using Klei.AI;
using STRINGS;
using System;

namespace MutantContainmentProject.Skills
{
    public class SkillAmountPerk : Database.SkillPerk
    {
        public AttributeModifier modifier;

        public SkillAmountPerk(
            string id,
            string amountId,
            float modifierBonus,
            string modifierDesc,
            bool modifierCanStack = false)
            : base(id, "", null, null, identity => { }, null, false)
        {
            SkillAmountPerk skillAmountPerk = this;
            Klei.AI.Amount amount = Db.Get().Amounts.Get(amountId);
            this.modifier = new AttributeModifier(amount.maxAttribute.Id, modifierBonus, modifierDesc);
            string formattedModifier = "";
            if (amount.Id == "HitPoints")
            {
                formattedModifier = $"{(this.modifier.Value> 0?"+":"-")}{this.modifier.Value.ToString()}";
            }
            else if (amount.displayer != null)
            {
                formattedModifier = amount.displayer.Formatter.GetFormattedModifier(this.modifier);
            }
            else
            {
                formattedModifier = this.modifier.GetFormattedString();
            }
            this.Name = string.Format((string)UI.ROLES_SCREEN.PERKS.ATTRIBUTE_EFFECT_FMT, (object)formattedModifier, (object)amount.Name);

            this.OnApply = (Action<MinionResume>)(identity =>
            {
                if (!modifierCanStack && identity.GetAttributes().Get(skillAmountPerk.modifier.AttributeId).Modifiers.FindIndex((Predicate<AttributeModifier>)(mod => mod == skillAmountPerk.modifier)) != -1)
                    return;
                identity.GetAttributes().Add(skillAmountPerk.modifier);
            });

            this.OnRemove = (Action<MinionResume>)(identity => identity.GetAttributes().Remove(skillAmountPerk.modifier));
        }
    }
}