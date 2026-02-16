namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterCreature : KMonoBehaviour
    {
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            // 这里可以添加其他初始化逻辑
            // 比如注册事件监听器等
        }
    }
}
