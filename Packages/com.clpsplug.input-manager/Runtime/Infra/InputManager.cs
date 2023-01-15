using System;
using System.Collections.Generic;
using System.Linq;
using InputManager.Domain;
using UnityEngine;

namespace InputManager.Infra
{
    /// <summary>
    /// Our trusty input manager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InputManager<T> : BaseInputManager<T>, IInputManager<T> where T : Enum
    {
        private OnKeyDownDelegate<T> _onKeyDownDelegates;
        private OnKeyHoldDelegate<T> _onKeyHoldDelegates;
        private OnKeyHoldFrameRateUnawareDelegate<T> _onKeyHoldFrameRateUnawareDelegates;
        private OnKeyUpDelegate<T> _onKeyUpDelegates;

        private readonly Dictionary<T, int> _keyHoldFrames = new Dictionary<T, int>();

        /// <summary>
        /// Intended Frame Rate. Used in conjunctions with <see cref="BaseInputManager{T}.KeyHoldLengths"/>.
        /// </summary>
        public int TargetFrameRate => 60;

        public InputManager(KeySetting<T> keySetting) : base(keySetting)
        {
            foreach (var key in KeySettings.keySettings)
            {
                _keyHoldFrames.Add(key.enumKey, 0);
            }
        }

        /// <inheritdoc cref="IInputManager{T}.CheckKey"/>
        // ReSharper disable Unity.PerformanceAnalysis
        public void CheckKey()
        {
#if !UNITY_EDITOR
            // If not editor, just return if not enabled.
            if (!_isEnabled) return;
#endif
            foreach (var action in InputActions)
            {
                if (!action.triggered) continue;
#if UNITY_EDITOR
                if (!IsEnabled)
                {
                    Debug.LogWarning("This input manager is disabled.");
                    return;
                }
#endif
                // Intentionally NOT using LINQ's First() method because it GC Alloc-s
                foreach (var ks in KeySettings.keySettings)
                {
                    if (ks.actionName != action.name) continue;
                    if (action.ReadValue<float>() > 0)
                    {
                        IsKeyPressed[ks.enumKey] = true;
                        _onKeyDownDelegates?.Invoke(ks.enumKey);
                    }
                    else
                    {
                        IsKeyPressed[ks.enumKey] = false;
                        _onKeyUpDelegates?.Invoke(ks.enumKey);
                    }
                }
            }

            // Intentionally copying the dictionary to 'freeze' the information
            // for this frame. Disable() can mutate the content while enumerating.
            // FIXME: should probably cache this dictionary somewhere
            foreach (var key in new Dictionary<T, bool>(IsKeyPressed))
            {
                if (_onKeyHoldDelegates == null) continue;
                if (key.Value)
                {
#if UNITY_EDITOR
                    // If editor, log a warning to indicate that the key is picked up.
                    if (!IsEnabled)
                    {
                        Debug.LogWarning("This InputManager is disabled!");
                        KeyHoldLengths[key.Key] = 0f;
                        _keyHoldFrames[key.Key] = 0;
                        return;
                    }
#endif
                    var previousLength = KeyHoldLengths[key.Key];
                    _keyHoldFrames[key.Key] += 1;
                    KeyHoldLengths[key.Key] += Time.deltaTime;
                    var currentFrameCount = Mathf.RoundToInt(KeyHoldLengths[key.Key] / (1.0f / TargetFrameRate));
                    var previousFrameCount = Mathf.RoundToInt(previousLength / (1.0f / TargetFrameRate));
                    _onKeyHoldDelegates?.Invoke(key.Key, currentFrameCount, previousFrameCount);
                    _onKeyHoldFrameRateUnawareDelegates?.Invoke(key.Key, _keyHoldFrames[key.Key]);
                }
                else
                {
                    // We need to reset these HERE
                    KeyHoldLengths[key.Key] = 0f;
                    _keyHoldFrames[key.Key] = 0;
                }
            }
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
            foreach (var key in KeyHoldLengths.Keys.ToList())
            {
                IsKeyPressed[key] = false;
                KeyHoldLengths[key] = 0f;
                _keyHoldFrames[key] = 0;
            }
        }

        /// <inheritdoc cref="IInputManager{T}.AddOnKeyDownDelegate"/>
        public void AddOnKeyDownDelegate(OnKeyDownDelegate<T> d)
        {
            _onKeyDownDelegates += d;
        }

        /// <inheritdoc cref="ICommonInputManagerMethods{T}.ResetOnKeyDownDelegate"/>
        public void ResetOnKeyDownDelegate()
        {
            _onKeyDownDelegates = null;
        }

        /// <inheritdoc cref="IInputManager{T}.AddOnKeyHoldDelegate(InputManager.Domain.OnKeyHoldDelegate{T})"/>
        public void AddOnKeyHoldDelegate(OnKeyHoldDelegate<T> d)
        {
            _onKeyHoldDelegates += d;
        }

        /// <inheritdoc cref="IInputManager{T}.AddOnKeyHoldDelegate(InputManager.Domain.OnKeyHoldFrameRateUnawareDelegate{T})"/>
        public void AddOnKeyHoldDelegate(OnKeyHoldFrameRateUnawareDelegate<T> d)
        {
            _onKeyHoldFrameRateUnawareDelegates += d;
        }

        /// <inheritdoc cref="ICommonInputManagerMethods{T}.ResetOnKeyHoldDelegate"/>
        public void ResetOnKeyHoldDelegate()
        {
            _onKeyHoldDelegates = null;
            _onKeyHoldFrameRateUnawareDelegates = null;
        }

        /// <inheritdoc cref="IInputManager{T}.AddOnKeyUpDelegate"/>
        public void AddOnKeyUpDelegate(OnKeyUpDelegate<T> d)
        {
            _onKeyUpDelegates += d;
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