﻿using UnityEngine;
using System.Collections;

using Cirrus.Circuit.World.Objects.Characters;
using Cirrus.Utils;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

using Cirrus.Circuit.Networking;

namespace Cirrus.Circuit.UI
{
    public class CharacterSelectSlot : NetworkBehaviour
    {
        [SerializeField]        
        private Image _imageTemplate;

        [SerializeField]
        private List<Image> _portraits;

        [SerializeField]        
        private RectTransform _rect;

        [SerializeField]
        private RectTransform _maskRect;

        [SerializeField]
        [SyncVar]
        private float _offset = -512;

        [SerializeField]
        [SyncVar]
        private float _portraitHeight = 256;

        [SyncVar]
        [SerializeField] float _totalHeight = 0;

        [SerializeField]
        [SyncVar]
        private float _bound = 0;

        [SerializeField]        
        private GameObject _selection;

        [SerializeField]        
        private Text _statusText;

        [SerializeField]        
        private Text _up;

        [SerializeField]        
        private Text _down;

        [SerializeField]
        [SyncVar]
        private float _speed = 0.5f;

        [SerializeField]
        [SyncVar]
        private float _selectPunchScale = 0.5f;

        [SerializeField]
        [SyncVar]
        private float _selectPunchScaleTime = 1f;

        [SerializeField]
        [SyncVar]
        private Vector3 _startPosition;

        [SerializeField]
        [SyncVar]
        private Vector3 _targetPosition;

        [SerializeField]
        private Transform _characterSpotlightAnchor;

        [SerializeField]
        [SyncVar]
        private float _characterSpotlightSize = 10f;

        [SerializeField]
        [SyncVar]
        private float _characterSpotlightRotateSpeed = 20f;


        [SerializeField]
        [SyncVar]
        private int _selectedIndex = 0;

        [SerializeField]
        [SyncVar]
        private float _disabledArrowAlpha = 0.35f;

        [System.Serializable]
        public enum State
        {
            Closed,
            Ready,
            Selecting,
        }

        [SerializeField]
        [SyncVar]
        private State _state = State.Closed;

        [SerializeField]
        private CameraManager _camera;

        private void OnValidate()
        {
            if (_camera == null) _camera = FindObjectOfType<CameraManager>();
            if (_rect == null) _rect = _selection.GetComponent<RectTransform>();

        }

        public void Awake()
        {
            if (_imageTemplate == null) DebugUtils.Assert(false, "Portrait template is null");
            else _imageTemplate.gameObject.SetActive(true);

            _portraits = new List<Image>();
            foreach (var res in CharacterLibrary.Instance.Characters)
            {
                if (res == null) continue;

                var portrait = _imageTemplate.gameObject.Create(_imageTemplate.transform.parent)?.GetComponent<Image>();
                if (portrait != null)
                {
                    portrait.sprite = res.Portrait;
                    _portraits.Add(portrait);
                }
            }

            if (_imageTemplate != null)
            {
                _portraitHeight = _portraits[0].GetComponent<LayoutElement>().preferredHeight;
                _totalHeight = _portraitHeight * _portraits.Count;
                _offset = 0;
                _imageTemplate.gameObject.SetActive(false);
            }
            //_bound = (_portraitHeight * _images.Count) / 2;
        }

        public void OnEnable()
        {
            _startPosition = Vector3.up * (_portraitHeight/2);
            _targetPosition = _startPosition;
        }

        public virtual void Start()
        {
            
        }

        public void FixedUpdate()
        {
            _rect.localPosition = Vector3.Lerp(_rect.localPosition, _targetPosition, _speed);

            if (_characterSpotlightAnchor.childCount != 0)
                _characterSpotlightAnchor.GetChild(0)
                    .Rotate(Vector3.up * Time.deltaTime * _characterSpotlightRotateSpeed);
        }

        public override void OnStartClient()
        {
            base.OnStartLocalPlayer();
            Local_TrySetState(_state);
            Cmd_Scroll(true);
        }
       

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            Cmd_TrySetState(State.Selecting);
        }


        public void Cmd_TrySetState(State target)
        {            
            CommandClient.Instance.Cmd_CharacterSelectSlot_SetState(gameObject, target);
        }

        public void Local_TrySetState(State target)
        {
            switch (target)
            {
                case State.Closed:
                    if (_state != State.Closed)
                        GameSession.Instance.CharacterSelectOpenCount =
                            GameSession.Instance.CharacterSelectOpenCount == 0 ?
                            0 :
                            GameSession.Instance.CharacterSelectOpenCount - 1;

                    _up.gameObject.SetActive(false);
                    _down.gameObject.SetActive(false);
                    _maskRect.gameObject.SetActive(false);
                    _statusText.text = "Press A to join";
                    break;

                case State.Selecting:
                    if (_state == State.Closed)
                        GameSession.Instance.CharacterSelectOpenCount =
                            GameSession.Instance.CharacterSelectOpenCount >= Controls.PlayerManager.PlayerMax ?
                                Controls.PlayerManager.PlayerMax :
                                GameSession.Instance.CharacterSelectOpenCount + 1;

                    if (_state == State.Ready)
                        GameSession.Instance.CharacterSelectReadyCount--;

                    if (_characterSpotlightAnchor.transform.childCount != 0)
                        Destroy(_characterSpotlightAnchor.GetChild(0).gameObject);

                    _up.gameObject.SetActive(true);
                    _down.gameObject.SetActive(true);
                    _maskRect.gameObject.SetActive(true);
                    _statusText.text = "";
                    break;

                case State.Ready:
                    if (_state != State.Ready) GameSession.Instance.CharacterSelectReadyCount++;

                    Vector3 position =
                    CameraManager.Instance.Camera.ScreenToWorldPoint(
                        _characterSpotlightAnchor.position);

                    // TODO maybe select locked character??
                    CharacterAsset resource = CharacterLibrary.Instance.Characters[_selectedIndex];
                    Character character =
                        resource.Create(
                            _characterSpotlightAnchor,
                            Quaternion.Euler(new Vector3(0, 180, 0)));

                    character.transform.localScale = new Vector3(
                        _characterSpotlightSize,
                        _characterSpotlightSize,
                        _characterSpotlightSize);

                    _up.gameObject.SetActive(false);
                    _down.gameObject.SetActive(false);
                    _maskRect.gameObject.SetActive(false);
                    _statusText.text = "Ready";
                    break;
            }

            _state = target;
        }

        [ClientRpc]
        public void Rpc_TrySetState(State target)
        {
            Local_TrySetState(target);
        }
        
        public IEnumerator PunchScale(bool previous)
        {
            iTween.Stop(_up.gameObject);
            iTween.Stop(_down.gameObject);

            _up.transform.localScale = new Vector3(1, 1, 1);
            _down.transform.localScale = new Vector3(1, 1, 1);

            yield return new WaitForSeconds(0.01f);

            if (previous)
            {
                iTween.PunchScale(
                    _up.gameObject,
                    new Vector3(_selectPunchScale,
                    _selectPunchScale, 0),
                    _selectPunchScaleTime);
            }
            else
            {
                iTween.PunchScale(
                    _down.gameObject,
                    new Vector3(_selectPunchScale,
                        _selectPunchScale, 0),
                        _selectPunchScaleTime);
            }
        }
        
        [ClientRpc]
        public void Rpc_Scroll(bool up)
        {
            _selectedIndex = up ? _selectedIndex - 1 : _selectedIndex + 1;
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, CharacterLibrary.Instance.Characters.Length - 1);
            _offset = up ? _offset - _portraitHeight : _offset + _portraitHeight;
            _offset = Mathf.Clamp(_offset, 0, _totalHeight - _portraitHeight);
            _targetPosition = _startPosition + Vector3.up * _offset;

            if (_selectedIndex == 0)
            {
                _up.color = _up.color.SetA(_disabledArrowAlpha);
                if (!up) StartCoroutine(PunchScale(false));
            }
            else if (_selectedIndex == CharacterLibrary.Instance.Characters.Length - 1)
            {
                if (up) StartCoroutine(PunchScale(true));
                _down.color = _down.color.SetA(_disabledArrowAlpha);
            }
            else
            {
                _up.color = _up.color.SetA(1f);
                _down.color = _down.color.SetA(1f);

                if (up) StartCoroutine(PunchScale(true));
                else StartCoroutine(PunchScale(false));
            }
        }

        public void Cmd_Scroll(bool up)
        {            
            if (!hasAuthority) return;

            CommandClient.Instance.Cmd_CharacterSelectSlot_Scroll(gameObject, up);
        }

        public void HandleAction0()
        {
            switch (_state)
            {
                case State.Selecting:
                    break;

                case State.Ready:
                    CharacterSelect.Instance.TrySetState(CharacterSelect.State.Select);
                    Cmd_TrySetState(State.Selecting);
                    break;

                case State.Closed:
                    break;
            }
        }

        public void HandleAction1(params object[] args)
        {
            switch (_state)
            {
                case State.Selecting:
                    Controls.Player player = (Controls.Player) args[0];
                    Debug.Log("Assigned id: " + CharacterLibrary.Instance.Characters[_selectedIndex].Id);
                    player._session.CharacterId = CharacterLibrary.Instance.Characters[_selectedIndex].Id;
                    Cmd_TrySetState(State.Ready);
                    break;                    

                case State.Ready:
                    // Try to change the state of the select screen, not the slot
                    CharacterSelect.Instance.TrySetState(CharacterSelect.State.Ready);
                    break;

                case State.Closed:
                    break;
            }            
        }              
    }
}
