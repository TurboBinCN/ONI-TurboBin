using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    public class MoveImmediately : KMonoBehaviour
    {
        private KBatchedAnimController animController;
        private Navigator navigator;
        private Navigator NavigatorCom => navigator ??= GetComponent<Navigator>();

        protected override void OnSpawn()
        {
            base.OnSpawn();
            animController = GetComponent<KBatchedAnimController>();
        }
        public void TeleportTo(Vector3 targetPosition)
        {
            Vector3 targetPos = Grid.CellToPos(Grid.PosToCell(targetPosition), CellAlignment.Bottom, Grid.SceneLayer.Creatures);
            if (animController != null)
            {
                try
                {
                    NavigatorCom?.smi.GoTo(NavigatorCom?.smi.sm.normal.moving);
                    animController.Play("move_immediately", KAnim.PlayMode.Once, 1f, 0f);
                    KAnimControllerBase.KAnimEvent animCompleteHandler = null;
                    animCompleteHandler = (_) => {
                        transform.position = targetPos;
                        NavigatorCom?.SetCurrentNavType(NavType.Floor);
                        NavigatorCom?.Stop(arrived_at_destination: true, true);
                        animController.onAnimComplete -= animCompleteHandler;
                    };
                    animController.onAnimComplete += animCompleteHandler;
                }
                catch
                {
                    transform.position = targetPos;
                    NavigatorCom?.SetCurrentNavType(NavType.Floor);
                    NavigatorCom?.Stop(arrived_at_destination: true, true); 
                }
            }
            else
            {
                transform.position = targetPosition;
                NavigatorCom?.Stop(arrived_at_destination: true, true);
                NavigatorCom?.SetCurrentNavType(NavType.Floor);
            }
        }
    }
}