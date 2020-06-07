﻿//using Cirrus.DH.Actions;
//using Cirrus.DH.Objects.Characters;
//using Cirrus.DH.Objects.Characters.Controls;
using System.Collections.Generic;
using UnityEngine;
using Cirrus.Resources;
using Cirrus;
using System;
using Cirrus.Circuit.Controls;
using Cirrus.Circuit.Networking;
using Cirrus.Circuit.Cameras;
using UnityEditor;
using MathUtils = Cirrus.MathUtils;
using System.Linq;
using System.Threading;
//using System.Numerics;

namespace Cirrus.Circuit.World.Objects
{
    [Serializable]
    public enum ObjectState
    {
        Unknown,
        Disabled,
        LevelSelect,
        CharacterSelect,
        Falling,
        Idle,
        Moving,
        Sliding,
        Climbing
    }

    [Serializable]
    public enum ObjectAction
    {
        Unknown,
        Land,
        Emote0,
        Emote1,
        Emote2
    }

    [Serializable]
    public enum ObjectType
    {
        None,
        Default,
        Character,
        CharacterPlaceholder,
        Gem,
        Door,
        Portal,
        Solid,
        Slope,
        Ladder,
        ConveyerBelt
    }

    public abstract partial class BaseObject : MonoBehaviour
    {
        public virtual ObjectType Type => ObjectType.Default;

        [SerializeField]
        public ObjectSession _session;

        [SerializeField]
        protected ColorController _visual;

        [SerializeField]
        public Transform _transform;
        public Transform Transform => _transform;

        public const int StepSize = 1;

        public const float StepSpeed = 0.4f;

        public const float FallSpeed = 0.8f;

        public const float FallDistance = 100f;

        public const float ScaleSpeed = 0.6f;

        public const float ClimbFallTime = 0.5f;

        public Timer _climbFallTimer;

        public BaseObject _entered = null;

        public BaseObject _visitor = null;

        public Vector3Int _direction;

        public float _pitchAngle = 0;

        public Vector3 _targetPosition;

        public Vector3 _offset;

        public virtual bool IsNetworked => true;

        public virtual bool IsSlidable => false;

        public virtual bool IsStable => true;

        Vector3Int _previousGridPosition;

        public Vector3Int _gridPosition;

        public Vector3Int Forward => Transform.forward.normalized.ToVector3Int();

        [HideInInspector]
        [SerializeField]
        public int _rotationIndex = 0;

        public int RotationIndex
        {
            get => _rotationIndex;
            set
            {
                _rotationIndex = value;
                _rotationIndex = MathUtils.Wrap(_rotationIndex, 0, 4);

                Transform.rotation = Quaternion.AngleAxis(_rotationIndex.IndexToAngle(), Vector3.up);
            }
        }

        public float _targetScale = 1;

        public int ColorId
        {
            set => _colorId = (Number)value;
            get => (int)_colorId;
        }

        [SerializeField]
        private Number _colorId;

        [SerializeField]
        protected Color _color;

        public virtual Color Color
        {
            get => _color;

            set
            {
                _color = value;

                if (_visual != null) _visual.Color = _color;
            }
        }

        [SerializeField]
        protected Color _nextColor;

        private Timer _nextColorTimer;

        private const float NextColorTime = 2;

        protected const float NextColorSpeed = 0.05f;

        [SerializeField]
        protected int _nextColorIndex = 0;

        public string Name => transform.name;

        [SerializeField]
        public Level Level => LevelSession.Instance.Level;

        public LevelSession LevelSession => LevelSession.Instance;

        protected bool _hasArrived = false;

        [SerializeField]
        private const float ExitScaleTime = 0.01f;

        private Timer _exitPortalTimer;

        [SerializeField]
        protected ObjectState _state = ObjectState.Disabled;

        protected bool _preserveInputDirection = false;

        private Mutex _applyResultmutex = new Mutex();


        #region Unity Engine

        public virtual void OnValidate()
        {
            if (PlayerManager.Instance != null)
            {
                Color = PlayerManager
                    .Instance
                    .GetColor(ColorId);

                _nextColor = Color;
            }
        }

        // TODO: will not be called on disabled level
        public virtual void Awake()
        {
            if (PlayerManager.IsValidPlayerId(ColorId))
            {
                _nextColorIndex = ColorId;
                _nextColorTimer = new Timer(NextColorTime, start: false, repeat: true);
                _nextColorTimer.OnTimeLimitHandler += OnNextColorTimeOut;
            }
            else
            {
                _visual.MakeMaterialsUnique();

                Color = PlayerManager
                    .Instance
                    .GetColor(ColorId);
            }

            _exitPortalTimer = new Timer(ExitScaleTime, start: false, repeat: false);
            _exitPortalTimer.OnTimeLimitHandler += OnExitScaleTimeout;

            _climbFallTimer = new Timer(ClimbFallTime, start: false, repeat: false);
            _climbFallTimer.OnTimeLimitHandler += OnClimbFallTimeout;

            _direction = Transform.forward.ToVector3Int();
            _targetPosition = Transform.position;
            _targetScale = 1f;

            FSM_Awake();
        }

        // TODO remove

        public virtual void Start()
        {
            FSM_Start();
        }

        public virtual void OnEnable()
        {

        }

        public virtual void FixedUpdate()
        {
            FSM_FixedUpdate();
        }

        public virtual void Update()
        {
            FSM_Update();
        }

        public virtual void OnDrawGizmos()
        {
#if UNITY_EDITOR            

            try
            {
                GraphicsUtils.DrawLine(
                    Transform.position,
                    Transform.position + Transform.forward,
                    2f);
            }
            catch { }

            //if (this is Characters.Character)
            //{
            //    Handles.Label(
            //        Transform.position,
            //        _direction.ToString());
            //}
#endif
        }

        #endregion

        public void Register(Level level)
        {
            (transform.position, _gridPosition) = level.RegisterObject(this);
            Transform.position = transform.position;
        }

        public bool Register(Level level, Vector3Int pos)
        {
            if (level.RegisterObject(this, pos))
            {
                _gridPosition = pos;
                transform.position = level.GridToWorld(pos);
                Transform.position = transform.position;
                return true;
            }

            return false;
        }

        public virtual void OnRoundEnd()
        {

        }

        public virtual void Server_BecomeUnstable()
        {
            if (_entered == null)
            {
                if (LevelSession.Get(
                    _gridPosition + Vector3Int.down,
                    out BaseObject obj))
                {
                    if (obj.IsStable)
                    {
                        if (_state == ObjectState.Falling) Cmd_PerformAction(ObjectAction.Land);

                        else Cmd_FSM_SetState(ObjectState.Idle);
                    }
                    else Server_Fall();
                }
                else Server_Fall();
            }
            // If arrived on a slope
            else if (
                IsSlidable &&
                _entered != null &&
                _entered is Slope &&
                !((Slope)_entered).IsStaircase)
            {
                Server_Slide();
            }
            else if (LevelSession.Get(
                _gridPosition + Vector3Int.down,
                out BaseObject _))
            {
                if (_state == ObjectState.Falling) Cmd_PerformAction(ObjectAction.Land);
                else Cmd_FSM_SetState(ObjectState.Idle);
            }
        }


        public virtual void Accept(BaseObject source)
        {
            //source.SetState
        }

        public virtual void Disable()
        {
            FSM_SetState(ObjectState.Disabled, null);
        }

        public virtual void WaitLevelSelect()
        {
            if (PlayerManager.IsValidPlayerId(ColorId))
            {
                OnNextColorTimeOut();
                _nextColorTimer.Start();

                FSM_SetState(ObjectState.LevelSelect, null);
            }
        }


        public void OnNextColorTimeOut()
        {
            if (_state != ObjectState.LevelSelect) return;

            _nextColorIndex = _nextColorIndex + 1;
            _nextColorIndex = MathUtils.Wrap(_nextColorIndex, 0, GameSession.Instance.PlayerCount);
            _nextColor = PlayerManager.Instance.GetColor(_nextColorIndex);
        }

        public void OnExitScaleTimeout()
        {
            _targetScale = 1;
            _targetPosition -= _offset;
        }


        #region Interact

        public virtual void Interact(BaseObject source)
        {
            if (!PlayerManager.IsValidPlayerId(ColorId))
            {
                ColorId = source.ColorId;
                Color = source.Color;
            }
        }

        // TODO : OnMoved
        public virtual void Cmd_Interact(BaseObject source)
        {
            _session.Cmd_Interact(source);
        }

        #endregion

        #region Perform Action

        public void Cmd_PerformAction(ObjectAction action)
        {
            _session.Cmd_PerformAction(action);
        }

        public virtual void PerformAction(ObjectAction action)
        {
            switch (action)
            {
                case ObjectAction.Land:
                    FSM_SetState(ObjectState.Idle);
                    break;
            }
        }

        #endregion

        #region Move

        public virtual void Cmd_Move(Move move)
        {
            _session.Cmd_Move(move);
        }

        public virtual bool Server_Move(Move move)
        {
            if (LevelSession.GetMoveResults(
                move,
                out IEnumerable<MoveResult> results,
                false,
                move.Type.IsLocking()))
            {
                LevelSession.Instance.ApplyMoveResults(results);                    

                return true;
            }

            return false;
        }

        public virtual void ApplyMoveResult(MoveResult result)
        {
            _applyResultmutex.WaitOne();

            do
            {

                ObjectState state = _state;

                _pitchAngle = 0;

                if (result.MoveType == MoveType.Direction)
                {
                    _direction = result.Direction;
                    break;
                }
                else if (result.MoveType == MoveType.Moving)
                {
                    // Clear move position
                    int idx = VectorUtils.ToIndex(result.Move.Position, 20, 20);
                     _gridPosition = result.Destination;
                    _targetPosition = Level.GridToWorld(result.Destination);
                    _targetPosition += result.Offset;
                    _direction = result.Direction;
                    _pitchAngle = result.PitchAngle;
                    state = ObjectState.Moving;
                }
                else if (result.MoveType == MoveType.Climbing)
                {
                    _gridPosition = result.Destination;
                    _targetPosition = Level.GridToWorld(result.Destination);
                    _targetPosition += result.Offset;
                    _direction = result.Direction;
                    _pitchAngle = result.PitchAngle;
                    state = ObjectState.Climbing;
                }
                else if (result.MoveType == MoveType.Teleport)
                {
                    _gridPosition = result.Destination;
                    _targetPosition = Level.GridToWorld(result.Destination);
                    _targetPosition += result.Offset;
                    Transform.position = _targetPosition;
                    _pitchAngle = result.PitchAngle;
                }
                else if (result.MoveType == MoveType.UsingPortal)
                {
                    // TODO preserve input direction timer
                    // TODO Timer restarted inside Cmd move
                    _preserveInputDirection = true;
                    _gridPosition = result.Destination;
                    _targetPosition = Level.GridToWorld(result.Destination);
                    _offset = result.Offset;
                    _targetPosition += _offset;
                    _targetScale = result.Scale;
                    Transform.position = Level.GridToWorld(result.Position);
                    _direction = result.Direction;
                    _pitchAngle = result.PitchAngle;
                    _exitPortalTimer.Start();
                    state = ObjectState.Moving;
                }
                else if (result.MoveType == MoveType.Falling)
                {
                    _gridPosition = result.Destination;
                    _targetPosition = Level.GridToWorld(result.Destination);
                    _targetPosition += result.Offset;
                    _pitchAngle = 0;
                    state = ObjectState.Falling;
                }
                else if (result.MoveType == MoveType.Sliding)
                {
                    _gridPosition = result.Destination;
                    _targetPosition = Level.GridToWorld(result.Destination);
                    _targetPosition += result.Offset;
                    _direction = result.Direction;
                    _pitchAngle = result.PitchAngle;
                    state = ObjectState.Sliding;
                }

                if (result.Moved != null) result.Moved.Interact(this);

                if (
                    result.Move.Position != result.Destination &&
                    _entered != null)
                {
                    _entered.Exit(this);
                    _entered = null;
                }

                if (result.Entered != null)
                {
                    _entered = result.Entered;
                    result.Entered.Enter(this);
                }

                FSM_SetState(
                    state,
                    this);
            }
            while (false);

            _applyResultmutex.ReleaseMutex();
        }

        public virtual bool GetMoveResults(
            Move move,
            out IEnumerable<MoveResult> results,
            bool isRecursiveCall=false,
            bool lockResults = true)
        {
             results = null;

            if (move.Source != null &&
                move.Type == MoveType.Sliding)
            {
                return false;
            }

            switch (move.Type)
            {
                case MoveType.Sliding:
                case MoveType.Moving:
                case MoveType.Teleport:
                case MoveType.Falling:
                    return LevelSession.GetMoveResults(
                        move,
                        out results,
                        isRecursiveCall,
                        lockResults);

                case MoveType.Direction:
                    results = new List<MoveResult>();
                    ((List<MoveResult>)results).Add(new MoveResult
                    {
                        MoveType = MoveType.Direction,
                        Move = move,
                        Destination = move.Position,
                        Offset = Vector3.zero,
                        Entered = null,
                        Moved = null,
                        Direction = move.Step.SetY(0)
                    });
                    return true;
                default: return false;
            }
        }


        #endregion

        #region Enter

        public virtual void Enter(BaseObject visitor)
        {
            _visitor = visitor;
        }

        public virtual bool GetEnterResults(
            Move move,
            out EnterResult result,
            out IEnumerable<MoveResult> moveResults)
        {
            result = new EnterResult
            {
                Step = move.Step,
                Entered = this,
                Offset = Vector3.zero,
                Destination = move.Position + move.Step,
                MoveType = move.Type,
                Position = move.Position,
                Scale = 1
            };

            moveResults = new MoveResult[0];

            if (_visitor != null)
            {
                result.Moved = _visitor;
                return _visitor.GetMoveResults(
                    new Move
                    {
                        Position = _visitor._gridPosition,
                        Source = move.User,
                        Type = move.Type,
                        User = _visitor,
                        Entered = _visitor._entered,
                        Step = move.Step.SetY(0)
                    },
                    out moveResults,
                    isRecursiveCall: true
                    );
            }

            return true;
        }

        #endregion

        #region Exit

        //public virtual void OnExited()
        //{
        //    _exitScaleTimer.Start();
        //    _targetScale = 0;
        //}

        // Ramp exit with offset
        public virtual bool GetExitResult(
            Move move,
            out ExitResult result,
            out IEnumerable<MoveResult> moveResults)
        {
            moveResults = new MoveResult[0];
            result = new ExitResult
            {
                Step = move.Step,
                Offset = Vector3.zero,
                Destination = move.Position + move.Step,
                Entered = null,
                Moved = null,
                Position = move.Position
            };

            return true;
        }

        public virtual void Exit(BaseObject visitor)
        {
            if (_visitor == visitor) _visitor = null;
        }


        #endregion


        #region Land

        public virtual bool Server_Slide()
        {
            if (!CustomNetworkManager.IsServer) return false;

            if (_entered == null) return false;

            Move move =
                new Move
                {
                    Type = MoveType.Sliding,
                    User = this,
                    Position = _gridPosition,
                    Step = -_entered._direction,
                    Entered = _entered,
                };

            return Server_Move(move);

        }


        #endregion


        #region Fall

        public virtual bool Server_Fall()
        {
            if (!CustomNetworkManager.IsServer) return false;

            return Server_Move(new Move
            {
                Type = MoveType.Falling,
                User = this,
                Position = _gridPosition,
                Step = Vector3Int.down,
                Entered = _entered
            });            
        }

        public virtual void OnClimbFallTimeout()
        {
            if (!CustomNetworkManager.IsServer) return;

            Server_BecomeUnstable();
        }

        #endregion


        #region FSM

        public virtual void FSM_Awake()
        {
            //Disable();
        }

        public virtual void FSM_Start()
        {
            //InitState(ObjectState.Disabled, null);
        }

        public void Cmd_FSM_SetState(ObjectState state)
        {
            _session.Cmd_FSM_SetState(state);
        }

        public virtual void FSM_SetState(
            ObjectState target,
            BaseObject source = null)
        {
            _state = target;

            switch (_state)
            {
                case ObjectState.Falling:
                case ObjectState.Moving:
                case ObjectState.Sliding:
                    _hasArrived = false;
                    break;
                default:
                    _hasArrived = true;
                    break;
            }

            BaseObject above;

            // TODO previous grid pos may not be upd ?
            if (source == null)
            {
                // Determine if object above to make it fall
                switch (target)
                {
                    case ObjectState.Moving:
                    case ObjectState.Falling:
                    case ObjectState.Sliding:

                        if (LevelSession.Get(
                            _previousGridPosition + Vector3Int.up,
                            out above))
                        {
                            above.Server_Fall();
                        }

                        _state = target;
                        break;
                }
            }

        }


        #region Update

        public virtual void FSM_FixedUpdate()
        {
            switch (_state)
            {
                case ObjectState.CharacterSelect:
                case ObjectState.LevelSelect:
                    break;

                case ObjectState.Moving:
                case ObjectState.Falling:
                case ObjectState.Idle:
                case ObjectState.Disabled:
                case ObjectState.Sliding:
                case ObjectState.Climbing:

                    if (_direction != Vector3Int.zero)
                    {
                        Transform.rotation = Quaternion.LookRotation(
                            Quaternion.AngleAxis(
                                _pitchAngle,
                                Vector3.Cross(_direction, Vector3.up).normalized) * _direction,
                            Vector3.up);
                    }
                    break;

            }


            switch (_state)
            {
                case ObjectState.Disabled:
                    break;

                case ObjectState.LevelSelect:

                    if (ColorId < PlayerManager.PlayerMax)
                    {
                        Color = Color.Lerp(_color, _nextColor, NextColorSpeed);
                    }

                    break;

                case ObjectState.Falling:
                case ObjectState.Idle:
                case ObjectState.Moving:
                case ObjectState.Sliding:
                case ObjectState.Climbing:

                    Transform.position = Vector3.Lerp(
                        Transform.position,
                        _targetPosition,
                        StepSpeed);

                    float scale =
                        Mathf.Lerp(
                            Transform.localScale.x,
                            _targetScale,
                            ScaleSpeed);

                    Transform.localScale =
                        new Vector3(
                            scale,
                            scale,
                            scale);

                    break;
            }
        }

        public virtual void FSM_Update()
        {
            switch (_state)
            {

                case ObjectState.Disabled:
                    return;

                case ObjectState.CharacterSelect:
                    return;

                case ObjectState.LevelSelect:
                    return;

                case ObjectState.Idle:
                    return;

                case ObjectState.Climbing:

                    if (_hasArrived) break;

                    if (VectorUtils.IsCloseEnough(
                        Transform.position,
                        _targetPosition))
                    {
                        _hasArrived = true;

                        if (!CustomNetworkManager.IsServer) break;

                        _climbFallTimer.Start();
                    }

                    break;

                case ObjectState.Sliding:
                case ObjectState.Falling:
                case ObjectState.Moving:

                    if (_hasArrived) break;

                    if (!CustomNetworkManager.IsServer) break;

                    if (VectorUtils.IsCloseEnough(
                        Transform.position,
                        _targetPosition))
                    {
                        _hasArrived = true;

                        Server_BecomeUnstable();
                    }

                    break;
            }
        }

        #endregion

        #endregion
    }
}

