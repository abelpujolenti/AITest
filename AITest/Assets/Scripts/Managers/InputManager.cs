using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class InputManager : MonoBehaviour
    {
        private static InputManager _instance;

        public static InputManager Instance => _instance;

        private Dictionary<char, Action> _pressKeyboardInputs;
        private Dictionary<KeyCode, Action> _releaseKeyboardInputs;
        private Dictionary<int, Action> _pressMouseButtonsInputs;
        private Dictionary<int, Action> _releaseMouseButtonsInputs;
        private Dictionary<bool, Action> _mouseScrollInputs;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                
                DontDestroyOnLoad(gameObject);
                
                return;
            }
            
            Destroy(gameObject);
        }

        private void Start()
        {
            FillInputDictionaries();
        }

        private void FillInputDictionaries()
        {
            _pressKeyboardInputs = new Dictionary<char, Action>
            {
                { 'w', () => AssertDelegateState(EventsManager.MoveForward) },
                { 'a', () => AssertDelegateState(EventsManager.MoveLeft) },
                { 's', () => AssertDelegateState(EventsManager.MoveBackwards) },
                { 'd', () => AssertDelegateState(EventsManager.MoveRight) },
                { ' ', () => AssertDelegateState(EventsManager.PressJump) }
            };
            
            _releaseKeyboardInputs = new Dictionary<KeyCode, Action>
            {
                { KeyCode.W, () => AssertDelegateState(EventsManager.StopMovingForward) },
                { KeyCode.A, () => AssertDelegateState(EventsManager.StopMovingLeft) },
                { KeyCode.S, () => AssertDelegateState(EventsManager.StopMovingBackwards) },
                { KeyCode.D, () => AssertDelegateState(EventsManager.StopMovingRight) },
                { KeyCode.Space, () => AssertDelegateState(EventsManager.ReleaseJump) }
            };

            _pressMouseButtonsInputs = new Dictionary<int, Action>
            {
                { 0, () => AssertDelegateState(EventsManager.PressMouseButton0) },
                { 1, () => AssertDelegateState(EventsManager.PressMouseButton1) },
                { 2, () => AssertDelegateState(EventsManager.PressMouseButton2) }
            };

            _releaseMouseButtonsInputs = new Dictionary<int, Action>
            {
                { 0, () => AssertDelegateState(EventsManager.ReleaseMouseButton0) },
                { 1, () => AssertDelegateState(EventsManager.ReleaseMouseButton1) },
                { 2, () => AssertDelegateState(EventsManager.ReleaseMouseButton2) }
            };

            _mouseScrollInputs = new Dictionary<bool, Action>
            {
                { true, () => AssertDelegateState(EventsManager.ScrollUp) },
                { false, () => AssertDelegateState(EventsManager.ScrollDown) }
            };
        }

        private void OnGUI()
        {
            Event currentEvent = Event.current;

            if (currentEvent.isScrollWheel)
            {
                _mouseScrollInputs[Input.mouseScrollDelta.y > 0]();
                return;
            }

            if (currentEvent.isMouse)
            {
                if ((int)currentEvent.rawType == 0)
                {
                    _pressMouseButtonsInputs[currentEvent.button]();
                    return;
                }

                _releaseMouseButtonsInputs[currentEvent.button]();
                return;
            }

            if (!currentEvent.isKey)
            {
                return;   
            }

            if ((int)currentEvent.rawType == 4)
            {
                if (currentEvent.character == '\0')
                {
                    return;
                }
                _pressKeyboardInputs[currentEvent.character]();
                return;
            }
            
            _releaseKeyboardInputs[currentEvent.keyCode]();
        }

        private void AssertDelegateState(Action action)
        {
            if (action == null)
            {
                return;
            }

            action();
        }
    }
}
