namespace MutantContainmentProject.MutanterComponent
{
    public class MutanterSpeciesCatalog : KMonoBehaviour
    {
        private static MutanterSpeciesCatalog Instance;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            MutanterSpeciesCatalog.Instance = this;
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            //this.EnsureOriginalSubSpecies();
            //this.RemoveInvalidMutantPlants();
        }
    }
}
