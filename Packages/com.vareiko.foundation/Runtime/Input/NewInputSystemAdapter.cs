using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vareiko.Foundation.Input
{
    public sealed class NewInputSystemAdapter : IInputAdapter, IDisposable
    {
#if ENABLE_INPUT_SYSTEM
        private const string MoveActionName = "Move";
        private const string DashActionName = "Dash";
        private const string PauseActionName = "Pause";
        private const string SubmitActionName = "Submit";
        private const string CancelActionName = "Cancel";

        private readonly IInputRebindStorage _rebindStorage;
        private readonly InputActionMap _actionMap;
        private readonly InputAction _moveAction;
        private readonly InputAction _dashAction;
        private readonly InputAction _pauseAction;
        private readonly InputAction _submitAction;
        private readonly InputAction _cancelAction;
#endif

        [Inject]
        public NewInputSystemAdapter([InjectOptional] IInputRebindStorage rebindStorage = null)
        {
#if ENABLE_INPUT_SYSTEM
            _rebindStorage = rebindStorage;
            _actionMap = new InputActionMap("Foundation");

            _moveAction = _actionMap.AddAction(MoveActionName, InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _moveAction.AddBinding("<Gamepad>/leftStick");
            _moveAction.AddBinding("<Gamepad>/dpad");

            _dashAction = _actionMap.AddAction(DashActionName, InputActionType.Button);
            _dashAction.AddBinding("<Keyboard>/space");
            _dashAction.AddBinding("<Gamepad>/buttonSouth");

            _pauseAction = _actionMap.AddAction(PauseActionName, InputActionType.Button);
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.AddBinding("<Gamepad>/start");

            _submitAction = _actionMap.AddAction(SubmitActionName, InputActionType.Button);
            _submitAction.AddBinding("<Keyboard>/enter");
            _submitAction.AddBinding("<Keyboard>/numpadEnter");
            _submitAction.AddBinding("<Gamepad>/buttonSouth");

            _cancelAction = _actionMap.AddAction(CancelActionName, InputActionType.Button);
            _cancelAction.AddBinding("<Keyboard>/escape");
            _cancelAction.AddBinding("<Mouse>/rightButton");
            _cancelAction.AddBinding("<Gamepad>/buttonEast");

            _actionMap.Enable();
            ImportOverridesJson(_rebindStorage != null ? _rebindStorage.Load() : string.Empty, false);
#endif
        }

        public InputScheme Scheme
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                InputDevice activeDevice = ResolveActiveDevice();
                if (activeDevice is Gamepad)
                {
                    return InputScheme.Gamepad;
                }

                if (activeDevice is Touchscreen)
                {
                    return InputScheme.Touch;
                }

                if (activeDevice is Keyboard || activeDevice is Mouse)
                {
                    return InputScheme.KeyboardMouse;
                }

                if (Keyboard.current != null || Mouse.current != null)
                {
                    return InputScheme.KeyboardMouse;
                }

                if (Gamepad.current != null)
                {
                    return InputScheme.Gamepad;
                }

                if (Touchscreen.current != null)
                {
                    return InputScheme.Touch;
                }
#endif
                return InputScheme.Unknown;
            }
        }

        public bool IsAvailable
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null ||
                       Mouse.current != null ||
                       Gamepad.current != null ||
                       Touchscreen.current != null;
#else
                return false;
#endif
            }
        }

        public Vector2 Move
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _moveAction.ReadValue<Vector2>();
#else
                return Vector2.zero;
#endif
            }
        }

        public bool DashPressedDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _dashAction.WasPressedThisFrame();
#else
                return false;
#endif
            }
        }

        public bool PausePressedDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _pauseAction.WasPressedThisFrame();
#else
                return false;
#endif
            }
        }

        public bool SubmitPressedDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _submitAction.WasPressedThisFrame();
#else
                return false;
#endif
            }
        }

        public bool CancelPressedDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _cancelAction.WasPressedThisFrame();
#else
                return false;
#endif
            }
        }

        public bool SupportsRebinding
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return true;
#else
                return false;
#endif
            }
        }

        public void Dispose()
        {
#if ENABLE_INPUT_SYSTEM
            _actionMap.Disable();
#endif
        }

        public bool TryApplyBindingOverride(string actionName, int bindingIndex, string overridePath)
        {
#if ENABLE_INPUT_SYSTEM
            if (string.IsNullOrWhiteSpace(overridePath))
            {
                return false;
            }

            InputAction action = ResolveAction(actionName);
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                return false;
            }

            action.ApplyBindingOverride(bindingIndex, new InputBinding
            {
                overridePath = overridePath.Trim()
            });

            PersistOverrides();
            return true;
#else
            return false;
#endif
        }

        public bool TryRemoveBindingOverride(string actionName, int bindingIndex)
        {
#if ENABLE_INPUT_SYSTEM
            InputAction action = ResolveAction(actionName);
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                return false;
            }

            action.RemoveBindingOverride(bindingIndex);
            PersistOverrides();
            return true;
#else
            return false;
#endif
        }

        public void ResetAllBindingOverrides()
        {
#if ENABLE_INPUT_SYSTEM
            _actionMap.RemoveAllBindingOverrides();
            PersistOverrides();
#endif
        }

        public string ExportOverridesJson()
        {
#if ENABLE_INPUT_SYSTEM
            BindingOverridesEnvelope envelope = new BindingOverridesEnvelope
            {
                Items = new List<BindingOverrideEntry>(8)
            };

            var actions = _actionMap.actions;
            for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                InputAction action = actions[actionIndex];
                for (int bindingIndex = 0; bindingIndex < action.bindings.Count; bindingIndex++)
                {
                    InputBinding binding = action.bindings[bindingIndex];
                    if (string.IsNullOrEmpty(binding.overridePath) &&
                        string.IsNullOrEmpty(binding.overrideProcessors) &&
                        string.IsNullOrEmpty(binding.overrideInteractions))
                    {
                        continue;
                    }

                    envelope.Items.Add(new BindingOverrideEntry
                    {
                        Action = action.name,
                        BindingId = binding.id.ToString(),
                        OverridePath = binding.overridePath ?? string.Empty,
                        OverrideProcessors = binding.overrideProcessors ?? string.Empty,
                        OverrideInteractions = binding.overrideInteractions ?? string.Empty
                    });
                }
            }

            if (envelope.Items.Count == 0)
            {
                return string.Empty;
            }

            return JsonUtility.ToJson(envelope);
#else
            return string.Empty;
#endif
        }

        public bool ImportOverridesJson(string json, bool persist = true)
        {
#if ENABLE_INPUT_SYSTEM
            _actionMap.RemoveAllBindingOverrides();
            if (string.IsNullOrWhiteSpace(json))
            {
                if (persist)
                {
                    PersistOverrides();
                }

                return true;
            }

            BindingOverridesEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<BindingOverridesEnvelope>(json);
            }
            catch
            {
                return false;
            }

            if (envelope == null || envelope.Items == null || envelope.Items.Count == 0)
            {
                if (persist)
                {
                    PersistOverrides();
                }

                return true;
            }

            int applied = 0;
            for (int i = 0; i < envelope.Items.Count; i++)
            {
                BindingOverrideEntry entry = envelope.Items[i];
                InputAction action = ResolveAction(entry.Action);
                if (action == null)
                {
                    continue;
                }

                int bindingIndex = ResolveBindingIndex(action, entry.BindingId);
                if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                {
                    continue;
                }

                action.ApplyBindingOverride(bindingIndex, new InputBinding
                {
                    overridePath = entry.OverridePath ?? string.Empty,
                    overrideProcessors = entry.OverrideProcessors ?? string.Empty,
                    overrideInteractions = entry.OverrideInteractions ?? string.Empty
                });
                applied++;
            }

            if (persist)
            {
                PersistOverrides();
            }

            return applied > 0 || envelope.Items.Count == 0;
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private InputAction ResolveAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
            {
                return null;
            }

            InputAction found = _actionMap.FindAction(actionName, false);
            if (found != null)
            {
                return found;
            }

            var actions = _actionMap.actions;
            for (int i = 0; i < actions.Count; i++)
            {
                InputAction action = actions[i];
                if (string.Equals(action.name, actionName, StringComparison.OrdinalIgnoreCase))
                {
                    return action;
                }
            }

            return null;
        }

        private static int ResolveBindingIndex(InputAction action, string bindingId)
        {
            if (action == null || string.IsNullOrWhiteSpace(bindingId))
            {
                return -1;
            }

            Guid parsedId;
            if (!Guid.TryParse(bindingId, out parsedId))
            {
                return -1;
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].id == parsedId)
                {
                    return i;
                }
            }

            return -1;
        }

        private InputDevice ResolveActiveDevice()
        {
            InputControl activeControl = _moveAction.activeControl;
            if (activeControl != null)
            {
                return activeControl.device;
            }

            activeControl = _dashAction.activeControl;
            if (activeControl != null)
            {
                return activeControl.device;
            }

            activeControl = _pauseAction.activeControl;
            if (activeControl != null)
            {
                return activeControl.device;
            }

            activeControl = _submitAction.activeControl;
            if (activeControl != null)
            {
                return activeControl.device;
            }

            activeControl = _cancelAction.activeControl;
            if (activeControl != null)
            {
                return activeControl.device;
            }

            return null;
        }

        private void PersistOverrides()
        {
            if (_rebindStorage == null)
            {
                return;
            }

            string json = ExportOverridesJson();
            if (string.IsNullOrWhiteSpace(json))
            {
                _rebindStorage.Clear();
                return;
            }

            _rebindStorage.Save(json);
        }

        [Serializable]
        private sealed class BindingOverridesEnvelope
        {
            public List<BindingOverrideEntry> Items = new List<BindingOverrideEntry>();
        }

        [Serializable]
        private sealed class BindingOverrideEntry
        {
            public string Action;
            public string BindingId;
            public string OverridePath;
            public string OverrideProcessors;
            public string OverrideInteractions;
        }
#endif
    }
}
