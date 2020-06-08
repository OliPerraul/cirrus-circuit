﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Cirrus.Circuit.World.Objects
{
    public class Door : BaseObject
    {

        [Header("----------------------------", order = 0)]
        [Header("Door", order = 1)]
        [Header("----------------------------", order = 2)]
        [SerializeField]
        private Cirrus.UI.ProgressBars.ProgressBar _progressBar;

        public override ObjectType Type => ObjectType.Door;

        public delegate void OnScoreValueAdded(
        Gem gem,
        int playerNumber,
        float score
        );

        public OnScoreValueAdded OnScoreValueAddedHandler;

        private const int ComboRequired = 2;

        [SerializeField]
        private int _comboAmount = 0;

        private Gem _previousGem;

        private float _multiplier = 1;
        
        private const float MultiplierIncrement = 0.5f;

        private Timer _multiplierTimer;
        
        private const float MultiplierTime = 2f;

        private Timer _valueTimer;

        private const float ValueTime = 1.5f;

        private const float ScorePunchScaleAmount = 0.5f;

        private const float ScorePunchScaleTime = 1f;

        [SerializeField]
        private UnityEngine.UI.Text _textMultiplier;

        [SerializeField]
        private UnityEngine.UI.Text _textValue;

        private const float PunchScaleAmount = 1f;

        private const float PunchScaleTime = 1f;
  
        IEnumerator PunchScaleCoroutine()
        {
            iTween.Stop(_visual.Parent.gameObject);
            _visual.Parent.gameObject.transform.localScale = new Vector3(1, 1, 1);

            yield return new WaitForSeconds(0.01f);

            iTween.PunchScale(_visual.Parent.gameObject,
                new Vector3(PunchScaleAmount, PunchScaleAmount, PunchScaleAmount),
                PunchScaleTime);

            yield return null;
        }

        public override void Awake()
        {
            base.Awake();

            _multiplierTimer = new Timer(MultiplierTime, start: false, repeat: false);
            _multiplierTimer.OnTimeLimitHandler += OnMultiplierTimeOut;


            _valueTimer = new Timer(ValueTime, start: false, repeat: false);
            _valueTimer.OnTimeLimitHandler += OnValueTimeOut;

            Multiplier = 1;

            Value = 0;
            OnValueTimeOut();

        }

        public override void Update()
        {
            base.Update();

            _progressBar.SetValue(1 - (_multiplierTimer.Time / MultiplierTime));
        }


        public override ReturnType GetMoveResults(
            Move move, 
            out IEnumerable<MoveResult> res,
            bool isRecursiveCall=false,
            bool lockResults = false)
        {
            res = null;
            return ReturnType.Failed;
        }

        #region Exit

        #endregion



        #region Enter

        public override ReturnType GetEnterResults(
            Move move, 
            out EnterResult enterResult, 
            out IEnumerable<MoveResult> results)
        {
            results = new MoveResult[0];
            enterResult = null;
            if (move.User is Characters.Character) return ReturnType.Failed;            

            return base.GetEnterResults(
                move, 
                out enterResult, 
                out results);
        }

        public override void Enter(BaseObject entered)            
        {
            base.Enter(entered);

            if (entered != null)
            {
                EnterVisitor(this);
            }
        }
        public override void EnterVisitor(BaseObject visitor)
        {
            switch (visitor.Type)
            {
                case ObjectType.Gem:
                    iTween.Init(_visual.Parent.gameObject);
                    iTween.Stop(_visual.Parent.gameObject);
                    _visual.Parent.gameObject.transform.localScale = new Vector3(1, 1, 1);
                    StartCoroutine(PunchScaleCoroutine());
                    OnGemEntered(visitor as Gem);
                    visitor._targetScale = 0;
                    break;

                case ObjectType.Character:
                    break;
                default:
                    break;
            }
        }


        #endregion


        IEnumerator PunchValueCoroutine()
        {
            iTween.Stop(_textValue.gameObject);
            _textValue.gameObject.transform.localScale = new Vector3(1, 1, 1);

            yield return new WaitForSeconds(0.01f);

            iTween.PunchScale(_textValue.gameObject,
                new Vector3(ScorePunchScaleAmount, ScorePunchScaleAmount, ScorePunchScaleAmount),
                ScorePunchScaleTime);


            yield return null;
        }

        IEnumerator PunchMultiplierCoroutine()
        {
            iTween.Stop(_textMultiplier.gameObject);
            _textMultiplier.gameObject.transform.localScale = new Vector3(1, 1, 1);

            yield return new WaitForSeconds(0.01f);

            iTween.PunchScale(_textMultiplier.gameObject,
                new Vector3(ScorePunchScaleAmount, ScorePunchScaleAmount, ScorePunchScaleAmount),
                ScorePunchScaleTime);


            yield return null;
        }

        public void OnGemEntered(Gem gem)
        {
            if (_previousGem != null &&
                gem.ColorId == ColorId &&
                gem.Type == _previousGem.Type)
            {
                _multiplierTimer.Start();
                _comboAmount++;
            }
            else
            {
                _comboAmount = 0;
                Multiplier = 1;
                _multiplierTimer.Stop();
            }

            iTween.Init(_textValue.gameObject); //TODO move elsewhere
            iTween.Stop(_textValue.gameObject);
            _textValue.gameObject.transform.localScale = new Vector3(1, 1, 1);

            Value = gem.ColorId == ColorId ? gem._value * _multiplier : -gem._value;

            _valueTimer.Start();

            StartCoroutine(PunchValueCoroutine());

            OnScoreValueAddedHandler?.Invoke(gem, ColorId, Value);

            _previousGem = gem;

            if (_comboAmount % ComboRequired == ComboRequired -1)
            {
                Multiplier += MultiplierIncrement;

                iTween.Init(_textMultiplier.gameObject); //TODO move elsewhere
                iTween.Stop(_textMultiplier.gameObject);
                _textMultiplier.gameObject.transform.localScale = new Vector3(1, 1, 1);

                StartCoroutine(PunchMultiplierCoroutine());
            }
        }



        public void OnMultiplierTimeOut()
        {
            _comboAmount = 0;
            _previousGem = null;
            Multiplier = 1;
        }

        private void OnValueTimeOut()
        {
            if(_textValue)
            _textValue.text = "";
        }


        public float Multiplier
        {
            get => _multiplier;

            set
            {
                _multiplier = value;

                if (_multiplier <= 1)
                {
                    if(_textMultiplier)
                    _textMultiplier.text = "";

                    if(_progressBar)
                        _progressBar.gameObject.SetActive(false);
                }
                else
                {
                    if(_textMultiplier)
                    _textMultiplier.text = " x" + _multiplier.ToString() + "";

                    if (_progressBar)
                        _progressBar.gameObject.SetActive(true);
                }
            }

        }

        private float _value;

        public float Value
        {
            get => _value;            

            set
            {
                _value = value;
  
                if (_textValue)
                {
                    if (_value < 0)
                    {
                        _textValue.text = _value.ToString();
                        _textValue.color = Color.black;
                    }
                    else
                    {
                        _textValue.text =  " +" + _value.ToString();
                        _textValue.color = Color;
                    }
                       
                }
                
                
            }

        }


    }
}