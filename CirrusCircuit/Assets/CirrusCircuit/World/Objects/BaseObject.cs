﻿//using Cirrus.DH.Actions;
//using Cirrus.DH.Objects.Characters;
//using Cirrus.DH.Objects.Characters.Controls;
using System.Collections.Generic;
using UnityEngine;
using Cirrus.Resources;
using Cirrus.Utils;
using System;
using Cirrus.Circuit.Controls;
using Cirrus.Circuit.Networking;
using Cirrus.Circuit.Cameras;
using UnityEditor;
using MathUtils = Cirrus.Utils.MathUtils;
//using System.Numerics;

namespace Cirrus.Circuit.World.Objects
{
    [Serializable]
    public enum ObjectState
    {
        Unknown,
        Disabled,
        LevelSelect,
        //Entering,
        Falling,
        Idle,
        Moving,
        Sliding,
    }

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
        Breakable
    }

    public abstract partial class BaseObject : MonoBehaviour
    {
        public virtual ObjectType Type => ObjectType.Default;

        [SerializeField]
        public ObjectSession _session;

        [SerializeField]
        protected ColorController _visual;

        [SerializeField]
        private Color[] _fallbackColors;

        [SerializeField]
        public Transform _transform;
        public Transform Transform => _transform;

        public const int StepSize = 1;

        public const float StepSpeed = 0.4f;

        public const float FallSpeed = 0.8f;

        public const float FallDistance = 100f;

        public const float ScaleSpeed = 0.6f;

        public BaseObject _entered = null;

        public BaseObject _visitor = null;

        public Vector3Int _direction;

        public float _pitchAngle = 0;

        public Vector3 _targetPosition;

        public Vector3 _offset;

        public virtual bool IsSlidable => false;

        Vector3Int _previousGridPosition;

        public Vector3Int _gridPosition;

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

        private Timer _exitScaleTimer;

        [SerializeField]
        protected ObjectState _state = ObjectState.Disabled;


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
        protected virtual void Awake()
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

            _exitScaleTimer = new Timer(ExitScaleTime, start: false, repeat: false);
            _exitScaleTimer.OnTimeLimitHandler += OnExitScaleTimeout;

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

            GraphicsUtils.DrawLine(
                Transform.position,
                Transform.position + Transform.forward,
                2f);

            if (this is Characters.Character)
            {
                Handles.Label(
                    Transform.position,
                    _direction.ToString());
            }
#endif
        }

        #endregion

        public void Register(Level level)
        {
            (transform.position, _gridPosition) = level.RegisterObject(this);
            Transform.position = transform.position;
        }

        public virtual void OnRoundEnd()
        {

        }


        // TODO remove
        public void Respond(ObjectSession.CommandResponse res)
        {
            switch (res.Id)
            {
                case ObjectSession.CommandId.LevelSession_IsFallThroughAllowed:


                    break;

                case ObjectSession.CommandId.LevelSession_IsMoveAllowed:

                    break;
            }
        }

        public virtual void Accept(BaseObject source)
        {
            //source.SetState
        }

        public virtual void Disable()
        {
            InitState(ObjectState.Disabled, null);
        }

        public virtual void WaitLevelSelect()
        {
            if (PlayerManager.IsValidPlayerId(ColorId))
            {
                OnNextColorTimeOut();
                _nextColorTimer.Start();

                InitState(ObjectState.LevelSelect, null);
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

        #region Move

        public virtual void Cmd_Move(Move move)
        {
            _session.Cmd_Move(move);
        }

        public virtual void Move(MoveResult result)
        {
            ObjectState state = _state;

            _pitchAngle = 0;

            switch (result.MoveType)
            {
                case MoveType.Direction:
                    _direction = result.Direction;                    
                    return;

                case MoveType.Teleport:
                    _gridPosition = result.Destination;                    
                    _targetPosition = Level.GridToWorld(result.Destination);
                    _targetPosition += result.Offset;
                    Transform.position = _targetPosition;
                    _direction = result.Direction;
                    _pitchAngle = result.PitchAngle;
                    break;

                case MoveType.Moving:

                    _gridPosition = result.Destination;
                    _targetPosition = Level.GridToWorld(result.Destination);
                    _targetPosition += result.Offset;
                    _direction = result.Direction;
                    _pitchAngle = result.PitchAngle;
                    state = ObjectState.Moving;


                    break;

                case MoveType.Falling:

                    _gridPosition = result.Destination;
                    _targetPosition = Level.GridToWorld(result.Destination);
                    _targetPosition += result.Offset;
                    _direction = result.Direction;
                    _pitchAngle = 0;

                    state = ObjectState.Falling;

                    break;


                case MoveType.Sliding:

                    _gridPosition = result.Destination;
                    _targetPosition = Level.GridToWorld(result.Destination);
                    _targetPosition += result.Offset;
                    _direction = result.Direction;
                    _pitchAngle = result.PitchAngle;
                    state = ObjectState.Sliding;
                    break;
            }


            if (result.Moved != null)
            {
                result.Moved.Interact(this);
            }

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

            InitState(
                state,
                this);
            LevelSession.Instance.Move(result);
        }


        public virtual void Move(IEnumerable<MoveResult> results)
        {
            foreach (var result in results)
            {
                if (result == null) continue;
                result.Move.User.Move(result);
            }
        }


        public virtual bool GetMoveResults(
            Move move, 
            out IEnumerable<MoveResult> results)
        {
            results = null;

            if (move.Source != null &&
                move.Type == MoveType.Sliding &&
                !IsSlidable)
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
                        out results);

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
            result = new EnterResult {
                Step = move.Step,
                Entered = this,
                Offset = Vector3.zero,
                Destination = move.Position + move.Step,
                //MoveType = move.Type                
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
                        Entered = _entered,
                        Step = move.Step.SetY(0)
                    },                    
                    out moveResults
                    );
            }

            return true;
        }

        #endregion

        #region Exit

        public virtual void OnExited()
        {
            _exitScaleTimer.Start();
            _targetScale = 0;
        }

        // Ramp exit with offset
        public virtual bool GetExitResult(
            Move move,
            out ExitResult result)
        {
            result = new ExitResult
            {
                Step = move.Step,
                Offset = Vector3.zero
            };

            return true;
        }

        public virtual void Exit(BaseObject visitor)
        {
            if (_visitor == visitor) _visitor = null;
        }


        #endregion

        #region Idle

        public virtual void Cmd_Idle()
        { 
        
            _session.Cmd_Idle();
        }


        // TODO play some anim
        public virtual void Idle()
        {
            InitState(ObjectState.Idle, null);
        }


        #endregion



        #region Land

        public virtual void Cmd_Slide()
        {
            _session.Cmd_Slide();
        }


        #endregion



        #region Land

        public virtual void Land()
        {
            InitState(ObjectState.Idle, null);
        }
        public virtual void Cmd_Land()
        {
            _session.Cmd_Land();
        }


        #endregion


        #region Fall

        public virtual void Cmd_Fall()
        {
            _session.Cmd_Fall();
        }

        #endregion


        #region FSM

        public virtual void FSM_Awake()
        {
            Disable();
        }

        public virtual void FSM_Start()
        {
            InitState(ObjectState.Disabled, null);
        }


        // Common to all state or subset of states
        public void InitState(ObjectState target, BaseObject source = null)
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
                            //above.Cmd_Fall();
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
                case ObjectState.LevelSelect:
                    break;

                case ObjectState.Moving:
                case ObjectState.Falling:
                case ObjectState.Idle:
                case ObjectState.Disabled:
                case ObjectState.Sliding:

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
                    return;
                case ObjectState.LevelSelect:
                    return;

                case ObjectState.Idle:                
                    return;

                case ObjectState.Sliding:
                case ObjectState.Falling:
                case ObjectState.Moving:

                    if (_hasArrived) break;

                    if (VectorUtils.IsCloseEnough(
                        Transform.position,
                        _targetPosition))
                    {
                        _hasArrived = true;

                        if (_entered == null)
                        {
                            if (LevelSession.Get(
                                _gridPosition + Vector3Int.down,
                                out BaseObject obj))
                            {
                                if (_state == ObjectState.Falling) Cmd_Land();
                                else Cmd_Idle();
                            }
                            else Cmd_Fall();
                        }
                        // If arrived on a slope
                        else if (
                            IsSlidable &&
                            _entered != null &&
                            _entered is Slope &&
                            !((Slope)_entered).IsStaircase)
                        {
                            Cmd_Slide();
                        }
                        else if (LevelSession.Get(         
                            _gridPosition + Vector3Int.down,
                            out BaseObject _))
                        {                            
                            if (_state == ObjectState.Falling) Cmd_Land();
                            else Cmd_Idle();                            
                        }
                    }

                    break;
            }
        }

        #endregion

        #endregion        
    }
}

