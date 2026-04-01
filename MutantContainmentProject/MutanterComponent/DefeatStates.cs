using UnityEngine;

namespace MutantContainmentProject.MutanterComponent
{
    // 状态机定义
    public class DefeatStates : GameStateMachine<DefeatStates, DefeatStates.Instance, IStateMachineTarget, DefeatStates.Def>
    {
        public override void InitializeStates(out BaseState default_state)
        {
            default_state = root;
            root
                .Enter("InitializeDefeatState", (Instance smi) => smi.InitializeDefeatState())
                .EventTransition(GameHashes.HealthChanged, check_health, null)
                .GoTo(check_health);

            check_health
                .Enter("CheckHealthAndSetFlag", (Instance smi) => smi.CheckHealthAndSetFlag())
                .GoTo(root);
        }

        public State root;
        public State check_health;

        public class Def : BaseDef
        {
            public bool allowDefeatHandling = true;
        }

        public new class Instance : GameInstance
        {
            private Movable _movable;
            [MyCmpReq]
            private Capturable _capturable;
            [MyCmpReq]
            private Health _health;
            [MyCmpReq]
            private Navigator _navigator;
            [MyCmpReq]
            private FactionAlignment _factionAlignment;
            [MyCmpReq]
            private Baggable _baggable;
            [SerializeField]
            private float _or_speed = 0;
            [SerializeField]
            private bool _defeated = false;

            public Instance(IStateMachineTarget master, Def def) : base(master, def)
            {
                if (_capturable == null) _capturable = gameObject.AddOrGet<Capturable>();
                if (_health == null) _health = gameObject.GetComponent<Health>();
                _capturable.allowCapture = false;

                if (_movable != null) UnityEngine.Object.Destroy(_movable);
                _or_speed = _navigator.defaultSpeed;
                Subscribe(856640610, OnStoreHandler);
            }

            private void OnStoreHandler(object obj)
            {
                //回复畸变体血量
                if (_health != null)
                {
                    _health.hitPoints = _health.maxHitPoints;
                    _health.OnHealthChanged(_health.maxHitPoints);
                }
                if (_navigator != null) _navigator.defaultSpeed = _or_speed;
                _defeated = true;

                CavityInfo cavityForCell = Game.Instance.roomProber.GetCavityForCell(Grid.PosToCell(_navigator));
                Game.Instance.roomProber.UpdateRoom(cavityForCell);

            }

            public void InitializeDefeatState()
            {
                CheckHealthAndSetFlag();

            }

            public void CheckHealthAndSetFlag()
            {
                bool isDefeated = _health != null && _health.hitPoints <= 0;
                //if(_health.hitPoints == _health.maxHitPoints && _movable != null) UnityEngine.Object.Destroy(_movable);
                bool canBeHandled = smi.def.allowDefeatHandling && isDefeated && !_defeated;
                if (canBeHandled && _health != null && _capturable != null && _navigator != null
                    && _baggable != null && !_baggable.wrangled)
                {
                    //TODO 血量为0时，移除可被攻击的标记，最好添加捕捉标记
                    if (Components.PlayerTargeted.Items.Contains(_factionAlignment)) _factionAlignment.SetPlayerTargeted(false);
                    if (!_capturable.IsMarkedForCapture)
                    {
                        _capturable.MarkForCapture(true);
                        _capturable.allowCapture = true;
                    }
                    if (_movable == null) _movable = gameObject.AddOrGet<Movable>();
                    _navigator.defaultSpeed = 0;
                }


            }
        }
    }
}