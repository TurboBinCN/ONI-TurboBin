using Klei.AI;
using PeterHan.PLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MutantFarmLab.mutantplants
{
    public class RadSeedEatWorkable : Workable
    {
        private Sickness radiationSickness = Db.Get().Sicknesses.RadiationSickness;
        protected override void OnSpawn()
        {
            base.OnSpawn();
            base.SetWorkTime(10f);
            this.showProgressBar = false;
            this.synchronizeAnims = false;
            base.GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Normal,null);
            this.createChore();
        }

        private void createChore()
        {
            new RadSeedEatChore(this);
        }
        protected override void OnCompleteWork(WorkerBase worker)
        {
            PUtil.LogDebug($"RadSeedEatWorkable OnCompleteWork [{worker.name}]");
            Effects effects = worker.GetComponent<Effects>();
            EffectInstance effectInstance = effects.Get(FoodEffectRegister.RAD_IMMUNE_ID);
            if (effectInstance != null)
            {
                effectInstance.timeRemaining = effectInstance.effect.duration;
            }
            else
            {
                effects.Add(FoodEffectRegister.RAD_IMMUNE_ID, true);
            }
            RadiationHelper.ClearMinionRadiation(worker.gameObject);

            Sicknesses sicknesses = worker.GetSicknesses();
            SicknessInstance sicknessInstance = sicknesses.Get(radiationSickness);
            if (sicknessInstance != null)
            {
                Game.Instance.savedInfo.curedDisease = true;
                sicknessInstance.Cure();
            }

            base.gameObject.DeleteObject();
        }
        public bool CanBeTakenBy(GameObject consumer)
        {
            AmountInstance radiationAmount = consumer.gameObject.GetAmounts().Get(Db.Get().Amounts.RadiationBalance.Id);
            if (radiationAmount != null && radiationAmount.value > 80f)
            {
                return true;
            }

            Sicknesses sicknesses = consumer.GetSicknesses();
            foreach (SicknessInstance sicknessInstance in sicknesses)
            {
                if(sicknessInstance.modifier.Id == Db.Get().Sicknesses.RadiationSickness.Id)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
