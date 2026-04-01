using TBB.He.TbbLib.Debuger;
using UnityEngine;

namespace MutantContainmentProject.MutanterComponent.VFXController
{
    public class WhiteMistVFXController : KMonoBehaviour
    {
        [Header("白雾配置")]
        public float mistDuration = 5f;
        public float mistRange = 2f;

        private static GameObject FogPrefab;
        private GameObject MistInstance;
        SpriteRenderer SpriteRendererManager;
        private bool isSkillActive = false;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            FogPrefab = MutantContainmentProjectMod.MutantContainmentProject.ModAssetBundle.LoadAsset<GameObject>("TheFixerWhiteFog");
            if (FogPrefab != null) TbbDebuger.LogWarning($"迷雾预制体加载成功");
        }

        protected override void OnCleanUp()
        {
            DeactivateMist();
            base.OnCleanUp();
        }

        public void ActivateMist()
        {
            if (isSkillActive) return;

            TbbDebuger.LogDebug($"激活迷雾 {transform.position}");
            isSkillActive = true;
            CreateMistInstance();
        }

        public void DeactivateMist()
        {
            TbbDebuger.LogDebug($"关闭迷雾 {transform.position}");
            if (MistInstance != null)
            {
                Destroy(MistInstance);
                MistInstance = null;
            }
            isSkillActive = false;
        }

        private void CreateMistInstance()
        {
            if (MistInstance == null)
            {
                TbbDebuger.LogDebug($"创建迷雾实例 {transform.position}");
                Vector3 position = transform.position;
                MistInstance = Util.KInstantiate(FogPrefab);
                if (MistInstance != null)
                {
                    SpriteRendererManager = MistInstance.GetComponent<SpriteRenderer>();
                }
                else
                {
                    TbbDebuger.LogError($"创建迷雾实例失败 {transform.position}");
                    return;
                }
                MistInstance.transform.position = position;
                MistInstance.transform.localScale = new Vector3(5f, 4f, 0);
                MistInstance.SetActive(true);
            }
        }
    }
}
