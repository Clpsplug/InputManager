using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.InputSystem;

namespace InputManager.Domain
{
    /// <summary>
    /// Delegate to check key down event
    /// </summary>
    /// <param name="key">Key which was pressed down</param>
    /// <typeparam name="T">Action type expressed within the code</typeparam>
    public delegate void OnKeyDownDelegate<in T>(T key) where T : Enum;

    /// <summary>
    /// Delegate to check key hold event. Takes current Frame and the previous Frame,
    /// where Frame is an integer that _would_ be the frame count if the game were to run
    /// on <see cref="IInputManager{T}.TargetFrameRate"/> which is by default 60fps.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="currentFrame">
    /// Current frame count since key down in <see cref="IInputManager{T}.TargetFrameRate"/>
    /// </param>
    /// <param name="previousFrame">
    /// Previous frame count passed by this function
    /// </param>
    /// <typeparam name="T">Action type expressed within the code</typeparam>
    /// <remarks>
    /// The frame count passed to this function hopefully minimizes the effect by the frame rate fluctuation
    /// (or vSync being off) and make sure that the frame count is consistent anywhere.
    /// The frame count passed to these functions
    /// may be the same for multiple frames (if actual fps &gt; <see cref="IInputManager{T}.TargetFrameRate"/>)
    /// OR may skip a large number of frame counts (if actual fps &lt; <see cref="IInputManager{T}.TargetFrameRate"/>.)
    /// That's why there are frame counts for previous actual game frame and the current actual game frame.
    /// On timing-aware context, if currentFrame == previousFrame, drop that frame.
    /// If currentFrame - previousFrame &gt; 1, you may want to enumerate over each frame that went past between these.
    /// </remarks>
    public delegate void OnKeyHoldDelegate<in T>(T key, int currentFrame, int previousFrame) where T : Enum;

    /// <summary>
    /// Delegate to check key up event
    /// </summary>
    /// <param name="key">Key which was released</param>
    /// <typeparam name="T">Action type expressed within the code</typeparam>
    public delegate void OnKeyUpDelegate<in T>(T key) where T : Enum;

    /// <summary>
    /// Rebind information
    /// </summary>
    /// <param name="target">
    /// Target of rebind
    /// </param>
    /// <param name="isCancelled">
    /// true if the rebind attempt was cancelled
    /// </param>
    /// <param name="readableKey">
    /// Key name, in human readable form. This should be used for UI.
    /// </param>
    /// <param name="isDuplicate">
    /// true if the rebind attempt ended up in duplicate key bind and a "swap" was performed instead.
    /// When this is true, check swappedActionName and swappedBinding.
    /// </param>
    /// <param name="swappedActionName">
    /// Action name of the swapped key bind on this rebind attempt.
    /// If this is not null, this action now has the binding indicated by swappedBinding.
    /// </param>
    /// <param name="swappedBinding">
    /// See swappedActionName.
    /// </param>
    /// <typeparam name="T">Action type expressed within the code</typeparam>
    public delegate void OnRebindDelegate<in T>(T target, bool isCancelled, string readableKey, bool isDuplicate,
        [CanBeNull] string swappedActionName, [CanBeNull] string swappedBinding) where T : Enum;

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