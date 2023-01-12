#if UNITASK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using InputManager.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace InputManager.Infra
{
    /// <summary>
    /// Input Manager without frame lock.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FrameUnlockedInputManager<T> : BaseInputManager<T>, IFrameUnlockedInputManager<T> where T : Enum
    {
        private bool _isListeningForActions;
        private OnKeyDownFrameUnlockedDelegate<T> _onKeyDownDelegates;
        private OnKeyHoldFrameUnlockedDelegate<T> _onKeyHoldDelegates;
        private OnKeyUpFrameUnlockedDelegate<T> _onKeyUpDelegates;

        private readonly Dictionary<T, double> _lastKeyDownTimestamp;

        private Dictionary<KeyEnumPair<T>, InputActionTrace> _keySettingsActionTraceRelation;

        private const float Threshold = 0.2f;

        private Dictionary<InputAction, InputActionTrace> _traces;

        public FrameUnlockedInputManager(KeySetting<T> keySetting) : base(keySetting)
        {
            _lastKeyDownTimestamp = keySetting.keySettings.Select(ks => ks.enumKey)
                .ToDictionary(key => key, _ => 0.0);
        }

        /// <inheritdoc cref="IFrameUnlockedInputManager{T}.SetPollingFrequency"/>
        public void SetPollingFrequency(int freq)
        {
            InputSystem.pollingFrequency = freq;
        }

        /// <inheritdoc cref="IFrameUnlockedInputManager{T}.CheckKey"/>
        public async UniTask CheckKey(Func<bool> enabledCondition, CancellationToken token)
        {
            try
            {
                if (token == default || token == CancellationToken.None)
                {
                    throw new InvalidOperationException(
                        "Please do NOT pass default or CancellationToken.None as the cancellation token.\n"
                        + "Providing one is a MUST because you will not be able to stop this key checking process otherwise."
                    );
                }

                if (_isListeningForActions)
                {
                    throw new InvalidOperationException(
                        "This input manager is already listening to actions! Duplicate CheckKey attempts are prohibited."
                    );
                }


                _isListeningForActions = true;
                _keySettingsActionTraceRelation = KeySettings.keySettings.ToDictionary(
                    ks => ks,
                    ks =>
                    {
                        var trace = new InputActionTrace();
                        trace.SubscribeTo(InputActions.First(ia => ia.name == ks.actionName));
                        return trace;
                    });

                while (true)
                {
                    if (!IsReady)
                    {
                        Debug.Log("Not ready, please wait");
                        continue;
                    }
#if !UNITY_EDITOR
                    // Editorでない場合はそのままreturn
                    if (!IsEnabled) return;
#else
                    if (!IsEnabled)
                    {
                        Debug.Log("This input manager is disabled.");
                        continue;
                    }
#endif
                    foreach (var ksai in _keySettingsActionTraceRelation)
                    {
                        var key = ksai.Key;
                        var trace = ksai.Value;
                        foreach (var action in trace)
                        {
                            var val = action.ReadValue<float>();
                            if (enabledCondition())
                            {
                                if (val > Threshold && !IsKeyPressed[key.enumKey])
                                {
                                    IsKeyPressed[ksai.Key.enumKey] = true;
                                    _onKeyDownDelegates?.Invoke(
                                        key.enumKey,
                                        action.time,
                                        Time.realtimeSinceStartupAsDouble
                                    );
                                    _lastKeyDownTimestamp[ksai.Key.enumKey] = action.time;
                                }

                                if (val <= Threshold && IsKeyPressed[key.enumKey])
                                {
                                    IsKeyPressed[key.enumKey] = false;
                                    _onKeyUpDelegates?.Invoke(
                                        key.enumKey,
                                        action.time,
                                        Time.realtimeSinceStartupAsDouble
                                    );
                                }
                            }
                        }

                        trace.Clear();
                    }

                    // Intentionally copying the dictionary to 'freeze' the information
                    // for this frame. Disable() can mutate the content while enumerating.
                    foreach (var key in new Dictionary<T, bool>(IsKeyPressed))
                    {
                        if (_onKeyHoldDelegates != null)
                        {
                            if (key.Value)
                            {
#if UNITY_EDITOR
                                // Editorの場合、反応を示すために警告
                                if (!IsEnabled)
                                {
                                    Debug.LogWarning("This InputManager is disabled!");
                                    return;
                                }
#endif
                                _onKeyHoldDelegates?.Invoke(
                                    key.Key, _lastKeyDownTimestamp[key.Key], Time.realtimeSinceStartupAsDouble
                                );
                            }
                        }
                    }

                    await UniTask.Yield(token);
                }
            }
            finally
            {
                foreach (var trace in _keySettingsActionTraceRelation.Values)
                {
                    trace.UnsubscribeFromAll();
                    trace.Dispose();
                }

                _isListeningForActions = false;
            }
        }

        /// <inheritdoc cref="IFrameUnlockedInputManager{T}.AddOnKeyDownDelegate"/>
        public void AddOnKeyDownDelegate(OnKeyDownFrameUnlockedDelegate<T> d)
        {
            _onKeyDownDelegates += d;
        }

        /// <inheritdoc cref="IFrameUnlockedInputManager{T}.AddOnKeyHoldDelegate"/>
        public void AddOnKeyHoldDelegate(OnKeyHoldFrameUnlockedDelegate<T> d)
        {
            _onKeyHoldDelegates += d;
        }

        /// <inheritdoc cref="IFrameUnlockedInputManager{T}.AddOnKeyUpDelegate"/>
        public void AddOnKeyUpDelegate(OnKeyUpFrameUnlockedDelegate<T> d)
        {
            _onKeyUpDelegates += d;
        }

        /// <inheritdoc cref="ICommonInputManagerMethods{T}.Enable"/>
        public void Enable()
        {
            IsEnabled = true;
        }

        /// <inheritdoc cref="ICommonInputManagerMethods{T}.Disable"/>
        public void Disable()
        {
            IsEnabled = false;
        }

        /// <inheritdoc cref="ICommonInputManagerMethods{T}.ResetOnKeyDownDelegate"/> 
        public void ResetOnKeyDownDelegate()
        {
            _onKeyDownDelegates = null;
        }

        /// <inheritdoc cref="ICommonInputManagerMethods{T}.ResetOnKeyHoldDelegate"/>
        public void ResetOnKeyHoldDelegate()
        {
            _onKeyHoldDelegates = null;
        }

        /// <inheritdoc cref="ICommonInputManagerMethods{T}.ResetOnKeyUpDelegate"/>
        public void ResetOnKeyUpDelegate()
        {
            _onKeyUpDelegates = null;
        }

        /// <inheritdoc cref="ICommonInputManagerMethods{T}.AddOnRebindDelegate"/>
        public void AddOnRebindDelegate(OnRebindDelegate<T> d)
        {
            OnRebindDelegates += d;
        }

        /// <inheritdoc cref="ICommonInputManagerMethods{T}.ResetOnRebindDelegate"/>
        public void ResetOnRebindDelegate()
        {
            OnRebindDelegates = null;
        }
    }
}
#endif