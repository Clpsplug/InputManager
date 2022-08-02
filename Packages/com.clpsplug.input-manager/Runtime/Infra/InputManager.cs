using System;
using System.Collections.Generic;
using System.Linq;
using InputManager.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputManager.Infra
{
    public class InputManager<T> : IInputManager<T> where T : Enum
    {
        private bool _isEnabled = true;

        private OnKeyDownDelegate<T> _onKeyDownDelegates;
        private OnKeyHoldDelegate<T> _onKeyHoldDelegates;
        private OnKeyUpDelegate<T> _onKeyUpDelegates;
        private OnRebindDelegate<T> _onRebindDelegates;

        private readonly KeySetting<T> _keySettings;

        private readonly Dictionary<T, bool> _isKeyPressed = new Dictionary<T, bool>();

        private readonly Dictionary<T, float> _keyHoldLengths = new Dictionary<T, float>();

        private InputActionRebindingExtensions.RebindingOperation _currentRebindingOperation;

        private readonly InputActionAsset _inputActionAsset;
        private readonly InputActionMap _inputActionMap;
        private readonly List<InputAction> _inputActions = new List<InputAction>();

        public List<InputAction> InputActions => _inputActions;

        /// <summary>
        /// Intended Frame Rate. Used in conjunctions with <see cref="_keyHoldLengths"/>.
        /// </summary>
        public virtual int TargetFrameRate => 60;

        protected InputManager(KeySetting<T> keySetting)
        {
            if (keySetting == null)
            {
                Debug.LogError("InputManager received a null Key Setting! Did you assign it?");
            }

            if (keySetting.IsEmpty())
            {
                Debug.LogWarning("InputManager received an empty key settings... why?");
            }

            // This is where you save the association between the enum and the key paths here
            _keySettings = keySetting;

            _inputActionAsset = Resources.Load<InputActionAsset>($"Input Assets/{_keySettings.inputAssetName}");
            _inputActionMap = _inputActionAsset.FindActionMap($"{_keySettings.inputMapName}", true);

            foreach (var setting in _keySettings.keySettings)
            {
                var inputAction = _inputActionMap.FindAction(setting.actionName, true);
                _inputActions.Add(inputAction);
            }

            foreach (var key in _keySettings.keySettings)
            {
                _isKeyPressed.Add(key.enumKey, false);
                _keyHoldLengths.Add(key.enumKey, 0f);
            }

            _inputActionAsset.Enable();
        }
        
        /// <summary>
        /// Injection occurs after constructor, so we need to rebind after the first binding.
        /// </summary>
        public void PerformOverride(Dictionary<T, string> overridePaths)
        {
            if (!_keySettings.overrideable || overridePaths == null) return;
            foreach (var inputAction in _inputActions)
            {
                var setting = _keySettings.keySettings.First(kv => kv.actionName == inputAction.name);
                var overridePath = overridePaths.First(kv => Equals(kv.Key, setting.enumKey)).Value;
                try
                {
                    inputAction.ApplyBindingOverride(new InputBinding
                    {
                        path = inputAction.bindings.First(b => b.groups == "Keyboard").path,
                        overridePath = overridePath,
                    });
                }
                catch (InvalidOperationException)
                {
                    Debug.LogError(
                        $"Could not find original path! Tried to find path or {inputAction.name}; Bindings are {inputAction.bindings.Aggregate("", (str, elem) => $"{str} {elem.groups}:{elem.path},")}"
                    );
                    throw;
                }
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void CheckKey()
        {
#if !UNITY_EDITOR
            // If not editor, just return if not enabled.
            if (!_isEnabled) return;
#endif
            foreach (var action in _inputActions)
            {
                if (action.triggered)
                {
#if UNITY_EDITOR
                    if (!_isEnabled)
                    {
                        Debug.LogWarning("This input manager is disabled.");
                        return;
                    }
#endif
                    // Intentionally NOT using LINQ's First() method because it GC Alloc-s
                    foreach (var ks in _keySettings.keySettings)
                    {
                        if (ks.actionName == action.name)
                        {
                            if (action.ReadValue<float>() > 0)
                            {
                                _isKeyPressed[ks.enumKey] = true;
                                _onKeyDownDelegates?.Invoke(ks.enumKey);
                            }
                            else
                            {
                                _isKeyPressed[ks.enumKey] = false;
                                _onKeyUpDelegates?.Invoke(ks.enumKey);
                            }
                        }
                    }
                }
            }

            // Intentionally copying the dictionary to 'freeze' the information
            // for this frame. Disable() can mutate the content while enumerating.
            foreach (var key in new Dictionary<T, bool>(_isKeyPressed))
            {
                if (_onKeyHoldDelegates != null)
                {
                    if (key.Value)
                    {
#if UNITY_EDITOR
                        // If editor, log a warning to indicate that the key is picked up.
                        if (!_isEnabled)
                        {
                            Debug.LogWarning("This InputManager is disabled!");
                            _keyHoldLengths[key.Key] = 0f;
                            return;
                        }
#endif
                        var previousLength = _keyHoldLengths[key.Key];
                        _keyHoldLengths[key.Key] += Time.deltaTime;
                        var currentFrameCount = Mathf.RoundToInt(_keyHoldLengths[key.Key] / (1.0f / TargetFrameRate));
                        var previousFrameCount = Mathf.RoundToInt(previousLength / (1.0f / TargetFrameRate));
                        _onKeyHoldDelegates?.Invoke(key.Key, currentFrameCount, previousFrameCount);
                    }
                    else
                    {
                        // You need to reset HERE
                        _keyHoldLengths[key.Key] = 0f;
                    }
                }
            }
        }

        public void Enable()
        {
            _isEnabled = true;
        }

        public void Disable()
        {
            _isEnabled = false;
            foreach (var key in _keyHoldLengths.Keys.ToList())
            {
                _isKeyPressed[key] = false;
                _keyHoldLengths[key] = 0f;
            }
        }

        public void AddOnKeyDownDelegate(OnKeyDownDelegate<T> d)
        {
            _onKeyDownDelegates += d;
        }

        public void ResetOnKeyDownDelegate()
        {
            _onKeyDownDelegates = null;
        }

        public void AddOnKeyHoldDelegate(OnKeyHoldDelegate<T> d)
        {
            _onKeyHoldDelegates += d;
        }

        public void ResetOnKeyHoldDelegate()
        {
            _onKeyHoldDelegates = null;
        }

        public void AddOnKeyUpDelegate(OnKeyUpDelegate<T> d)
        {
            _onKeyUpDelegates += d;
        }

        public void ResetOnKeyUpDelegate()
        {
            _onKeyUpDelegates = null;
        }

        public void RequestRebind(T target,
            Func<InputActionRebindingExtensions.RebindingOperation, InputActionRebindingExtensions.RebindingOperation>
                operationConfigCallback,
            Action onComplete = null,
            Action onCancel = null
        )
        {
            var keySetting = _keySettings.keySettings.First(kv => Equals(kv.enumKey, target));
            var action = _inputActions.First(a => a.name == keySetting.actionName);
            // Save the previous key binding for duplicated keys
            var previousBinding = action.bindings.First(b => b.groups == "Keyboard");
            var previousEffectivePath = previousBinding.effectivePath;
            _inputActionAsset.Disable();
            _currentRebindingOperation = operationConfigCallback.Invoke(action.PerformInteractiveRebinding());

            _currentRebindingOperation
                .OnMatchWaitForAnother(0.1f)
                .Start();
            _currentRebindingOperation
                .OnComplete(op =>
                {
                    _currentRebindingOperation.Dispose();
                    onComplete?.Invoke();
                })
                .OnCancel(op =>
                {
                    _onRebindDelegates?.Invoke(default, true, "", false, default, default);
                    _currentRebindingOperation.Dispose();
                    _inputActionAsset.Enable();
                    onCancel?.Invoke();
                });
            _currentRebindingOperation
                .OnApplyBinding(
                    (cb, path) =>
                    {
                        action.ApplyBindingOverride(new InputBinding
                        {
                            path = previousBinding.path,
                            overridePath = path,
                        });

                        // New Path
                        // Is this path used elsewhere?
                        // Are there any input action whose effective binding path contains this action's new effective binding path?
                        // If effective path is empty, look at the path.
                        var duplicatedAction = _inputActions
                            .FirstOrDefault(
                                a =>
                                    a.name != action.name &&
                                    a.bindings.Any(b =>
                                        b.effectivePath.ToLower() == path.ToLower() ||
                                        (b.effectivePath == string.Empty) && b.path.ToLower() == path.ToLower())
                            );


                        // If there are, then swap the bindings. (ignore itself)
                        if (duplicatedAction != null)
                        {
                            Debug.Log("Duplicate found");
                            duplicatedAction.ApplyBindingOverride(new InputBinding
                            {
                                path = duplicatedAction.bindings.First(b => b.groups == "Keyboard").path,
                                overridePath = previousEffectivePath,
                            });
                            _onRebindDelegates?.Invoke(
                                target,
                                false,
                                InputControlPath.ToHumanReadableString(path,
                                    InputControlPath.HumanReadableStringOptions.OmitDevice),
                                true,
                                duplicatedAction.name,
                                previousEffectivePath
                            );
                        }
                        else
                        {
                            _onRebindDelegates?.Invoke(
                                target,
                                false,
                                InputControlPath.ToHumanReadableString(path,
                                    InputControlPath.HumanReadableStringOptions.OmitDevice),
                                false,
                                default,
                                default
                            );
                        }

                        _inputActionAsset.Enable();
                    }
                );
        }

        public void AddOnRebindDelegate(OnRebindDelegate<T> d)
        {
            _onRebindDelegates += d;
        }

        public void ResetOnRebindDelegate()
        {
            _onRebindDelegates = null;
        }

        public Dictionary<T, string> GetCurrentBindings()
        {
            var result = new Dictionary<T, string>();

            foreach (var action in _inputActions)
            {
                var key = _keySettings.keySettings.First(ks => ks.actionName == action.name).enumKey;
                var effectivePath = action.bindings.First(b => b.groups == "Keyboard").effectivePath;
                result.Add(key,
                    InputControlPath.ToHumanReadableString(effectivePath,
                        InputControlPath.HumanReadableStringOptions.OmitDevice));
            }

            return result;
        }
    }
}