using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace InputManager.Domain
{
    public interface IInputManager<T> where T : Enum
    {
        /// <summary>
        /// You MUST call this every frame or the input will NOT work.
        /// </summary>
        void CheckKey();

        /// <summary>
        /// Enables all the key delegates.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables all the key delegates.
        /// <see cref="CheckKey"/> will still pick up the keys, but it will not trigger key delegates.
        /// </summary>
        void Disable();

        /// <summary>
        /// Performs a batch override over a dictionary.
        /// Probably useful to restore the custom key bind from a save file.
        /// </summary>
        /// <param name="overridePaths"></param>
        void PerformOverride(Dictionary<T, string> overridePaths);

        /// <summary>
        /// Registers a delegate for key down event.
        /// </summary>
        /// <param name="d"></param>
        void AddOnKeyDownDelegate(OnKeyDownDelegate<T> d);

        /// <summary>
        /// Unregisters ALL key down delegates.
        /// </summary>
        void ResetOnKeyDownDelegate();

        /// <summary>
        /// Registers a delegate for key hold event.
        /// </summary>
        /// <param name="d"></param>
        void AddOnKeyHoldDelegate(OnKeyHoldDelegate<T> d);

        /// <summary>
        /// Registers a delegate for key hold event. This delegate is not aware of framerate fluctuations.
        /// </summary>
        /// <param name="d"></param>
        void AddOnKeyHoldDelegate(OnKeyHoldFrameRateUnawareDelegate<T> d);

        /// <summary>
        /// Unregisters ALL key hold delegates.
        /// </summary>
        void ResetOnKeyHoldDelegate();

        /// <summary>
        /// Registers a delegate for key up event.
        /// </summary>
        /// <param name="d"></param>
        void AddOnKeyUpDelegate(OnKeyUpDelegate<T> d);

        /// <summary>
        /// Unregisters ALL key up delegates.
        /// </summary>
        void ResetOnKeyUpDelegate();

        /// <summary>
        /// Registers a delegate for key rebound event.
        /// </summary>
        /// <param name="d"></param>
        void AddOnRebindDelegate(OnRebindDelegate<T> d);

        /// <summary>
        /// Unregisters ALL key rebound delegates.
        /// </summary>
        void ResetOnRebindDelegate();

        /// <summary>
        /// Returns current binding as the dictionary
        /// (it can be used as the parameter for <see cref="PerformOverride"/>.)
        /// Use this for saving the custom key binds.
        /// </summary>
        /// <returns></returns>
        Dictionary<T, string> GetCurrentBindings();

        /// <summary>
        /// "Intended" frame rate for this input manager.
        /// Used to calculate the frame count for <see cref="OnKeyHoldDelegate{T}"/>.
        /// </summary>
        int TargetFrameRate { get; }

        /// <summary>
        /// Start a rebind attempt. This is async. Calls <see cref="OnRebindDelegate{T}"/> on any state of completion.
        /// </summary>
        /// <param name="target">Rebind target</param>
        /// <param name="targetBindingGroup">The group in which the target key bind is associated with</param>
        /// <param name="operationConfigCallback">
        /// Callback to set up the rebinding operation options
        /// </param>
        /// <param name="onComplete">Callback on complete rebind</param>
        /// <param name="onCancel">Callback on cancelled rebind attempt</param>
        void RequestRebind(
            T target,
            string targetBindingGroup,
            Func<InputActionRebindingExtensions.RebindingOperation, InputActionRebindingExtensions.RebindingOperation>
                operationConfigCallback,
            Action onComplete = null,
            Action onCancel = null
        );
    }
}