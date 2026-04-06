using Database;
using HarmonyLib;
using UnityEngine;

namespace MutantContainmentProject.ArmorSystem
{
    public class ArmorPiecesUnlocker : KMonoBehaviour
    {
        private static ArmorPiecesUnlocker _instance;
        public static ArmorPiecesUnlocker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<ArmorPiecesUnlocker>();
                    if (_instance == null)
                    {
                        GameObject go = new("ArmorManager");
                        _instance = go.AddComponent<ArmorPiecesUnlocker>();
                    }
                }
                return _instance;

            }
        }
        override protected void OnSpawn()
        {
            base.OnSpawn();
            _instance = this;
        }
        public void UnlockArmorPiece(ArmorPiece armorPiece)
        {
            //TODO 使用Game.Instance.unlocks 来根据进程解锁
        }
        public bool IsPermitUnlocked(PermitResource permit)
        {
            if (permit != null && ArmorBlueprintProvider.ArmorPieceIds.Contains(permit.Id))
            {
                // 默认解锁我们的自定义服装
                return true;
                //TODO 使用Game.Instance.unlocks 来根据进程解锁
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(PermitItems), "IsPermitUnlocked")]
    public class PermitItemsIsPermitUnlockedPatch
    {
        public static void Postfix(PermitResource permit, ref bool __result)
        {
            __result = false;
            if (permit != null && ArmorPiecesUnlocker.Instance.IsPermitUnlocked(permit))
            {
                __result = true;
            }
        }
    }
}
