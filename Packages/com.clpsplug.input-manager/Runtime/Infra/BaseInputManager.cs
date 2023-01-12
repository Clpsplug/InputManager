using System;
using System.Collections.Generic;
using System.Linq;
using InputManager.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputManager.Infra
{
    /// <summary>
    /// Common implementations for Input Managers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    /// TBD: This may be a weird practice. This class is used like a "trait" - this class is 'mixed into' other interface implementations to add implementations missing from the said one.
    /// We may be better off making a corresponding interface for this one.
    /// </remarks>
    public abstract class BaseInputManager<T> where T : Enum
    {
        protected readonly bool IsReady = false;
        protected bool IsEnabled = true;
        protected OnRebindDelegate<T> OnRebindDelegates;

        protected readonly KeySetting<T> KeySettings;

        protected readonly Dictionary<T, bool> IsKeyPressed = new Dictionary<T, bool>();

        private InputActionRebindingExtensions.RebindingOperation _currentRebindingOperation;

        private readonly InputActionAsset _inputActionAsset;
        protected readonly List<InputAction> InputActions = new List<InputAction>();

        protected readonly Dictionary<T, float> KeyHoldLengths = new Dictionary<T, float>();

        protected BaseInputManager(KeySetting<T> keySetting)
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
            KeySettings = keySetting;

            _inputActionAsset = Resources.Load<InputActionAsset>($"Input Assets/{KeySettings.inputAssetName}");
            var inputActionMap = _inputActionAsset.FindActionMap($"{KeySettings.inputMapName}", true);

            foreach (var setting in KeySettings.keySettings)
            {
                var inputAction = inputActionMap.FindAction(setting.actionName, true);
                InputActions.Add(inputAction);
            }

            foreach (var key in KeySettings.keySettings)
            {
                IsKeyPressed.Add(key.enumKey, false);
                KeyHoldLengths.Add(key.enumKey, 0f);
            }

            _inputActionAsset.Enable();
        }

        /// <inheritdoc cref="IInputManager{T}.PerformOverride"/>
        public void PerformOverride(Dictionary<T, string> overridePaths)
        {
            if (!KeySettings.overrideable || overridePaths == null) return;
            foreach (var inputAction in InputActions)
            {
                var setting = KeySettings.keySettings.First(kv => kv.actionName == inputAction.name);
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

        /// <inheritdoc cref="IInputManager{T}.RequestRebind"/>
        public void RequestRebind(T target,
            string targetBindingGroup,
            Func<InputActionRebindingExtensions.RebindingOperation, InputActionRebindingExtensions.RebindingOperation>
                operationConfigCallback,
            Action onComplete = null,
            Action onCancel = null
        )
        {
            var keySetting = KeySettings.keySettings.First(kv => Equals(kv.enumKey, target));
            var action = InputActions.First(a => a.name == keySetting.actionName);
            // Save the previous key binding for duplicated keys
            var previousBinding = action.bindings.First(b => b.groups.Split(";").Any(g => g == targetBindingGroup));
            var previousEffectivePath = previousBinding.effectivePath;
            _inputActionAsset.Disable();
            _currentRebindingOperation = operationConfigCallback.Invoke(action.PerformInteractiveRebinding());

            _currentRebindingOperation
                .WithBindingGroup(targetBindingGroup)
                .OnMatchWaitForAnother(0.1f)
                .Start();
            _currentRebindingOperation
                .OnComplete(_ =>
                {
                    _currentRebindingOperation.Dispose();
                    onComplete?.Invoke();
                })
                .OnCancel(_ =>
                {
                    OnRebindDelegates?.Invoke(default, true, "", false, default, default);
                    _currentRebindingOperation.Dispose();
                    _inputActionAsset.Enable();
                    onCancel?.Invoke();
                });
            _currentRebindingOperation
                .OnApplyBinding(
                    (_, path) =>
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
                        var duplicatedAction = InputActions
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
                            OnRebindDelegates?.Invoke(
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
                            OnRebindDelegates?.Invoke(
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

        /// <inheritdoc cref="IInputManager{T}.GetCurrentBindings"/>
        public Dictionary<T, string> GetCurrentBindings()
        {
            var result = new Dictionary<T, string>();

            foreach (var action in InputActions)
            {
                var key = KeySettings.keySettings.First(ks => ks.actionName == action.name).enumKey;
                var effectivePath = action.bindings.First(b => b.groups == "Keyboard").effectivePath;
                result.Add(key,
                    InputControlPath.ToHumanReadableString(effectivePath,
                        InputControlPath.HumanReadableStringOptions.OmitDevice));
            }

            return result;
        }
    }
}