using System;
using System.Collections.Generic;
using System.Threading;
#if UNITASK
using Cysharp.Threading.Tasks;
#endif
using UnityEngine.InputSystem;

namespace InputManager.Domain
{
    /// <summary>
    /// The regular input manager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IInputManager<T> : ICommonInputManagerMethods<T> where T : Enum
    {
        /// <summary>
        /// You MUST call this every frame or the input will NOT work.
        /// You may probably want to call this in your Update() method.
        /// </summary>
        void CheckKey();

        /// <summary>
        /// Registers a delegate for key down event.
        /// </summary>
        /// <param name="d"></param>
        void AddOnKeyDownDelegate(OnKeyDownDelegate<T> d);


        /// <summary>
        /// Registers a delegate for key hold event.
        /// Since <see cref="OnKeyHoldDelegate{T}"/> is passed,
        /// the frame count passed to this delegate is adjusted to the implementation's <see cref="TargetFrameRate"/>.
        /// </summary>
        /// <param name="d"></param>
        void AddOnKeyHoldDelegate(OnKeyHoldDelegate<T> d);

        /// <summary>
        /// Registers a delegate for key hold event.
        /// Since <see cref="OnKeyHoldFrameRateUnawareDelegate{T}"/> is passed,
        /// the frame count passed to this delegate is a raw frame count.
        /// </summary>
        /// <param name="d"></param>
        void AddOnKeyHoldDelegate(OnKeyHoldFrameRateUnawareDelegate<T> d);

        /// <summary>
        /// Registers a delegate for key up event.
        /// </summary>
        /// <param name="d"></param>
        void AddOnKeyUpDelegate(OnKeyUpDelegate<T> d);

        /// <summary>
        /// "Intended" frame rate for this input manager.
        /// Used to calculate the frame count for <see cref="OnKeyHoldDelegate{T}"/>.
        /// </summary>
        int TargetFrameRate { get; }
    }

#if UNITASK
    /// <summary>
    /// High frequency variant of <see cref="IInputManager{T}"/>.
    /// Some methods and delegates return <see cref="UniTask"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFrameUnlockedInputManager<T> : ICommonInputManagerMethods<T> where T : Enum
    {
        /// <summary>
        /// Checks for key input.
        /// Simply put, unlike for <see cref="IInputManager{T}"/>, this method checks for user inputs
        /// in a different "thread." This method runs in sync with Unity's player loop,
        /// so you can do everything else without worrying about it.
        /// It is best called from your Start or Awake methods.
        /// You need to supply <see cref="CancellationToken"/> to ensure that you can stop it from outside
        /// and release relevant resources to prevent memory leaks.
        /// See <see cref="CancellationTokenSource"/> for generating and issuing cancellation.
        /// </summary>
        /// <param name="enabledCondition">
        /// Condition under which you want to check for the key input.
        /// You could use <see cref="ICommonInputManagerMethods{T}.Enable"/> or <see cref="ICommonInputManagerMethods{T}.Disable"/>, but
        /// this is provided here so that you don't have to manually call them and potentially fail to stop the input detection in time.
        /// </param>
        /// <param name="token">Required cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// You did either one of those:
        /// <list type="bullet">
        /// <item>You passed a default <see cref="CancellationToken"/> or <see cref="CancellationToken.None"/></item>
        /// <item>You called this method without stopping the previous one</item>
        /// </list>
        /// </exception>
        UniTask CheckKey(Func<bool> enabledCondition, CancellationToken token);

        void AddOnKeyDownDelegate(OnKeyDownFrameUnlockedDelegate<T> d);
        void AddOnKeyHoldDelegate(OnKeyHoldFrameUnlockedDelegate<T> d);
        void AddOnKeyUpDelegate(OnKeyUpFrameUnlockedDelegate<T> d);

        /// <summary>
        /// Change the polling frequency and the time until <see cref="CheckKey"/> returns.
        /// WARNING: This will change <see cref="InputSystem.pollingFrequency"/> as well which means it will be applied everywhere.
        /// WARNING: Be wary if you are using multiple <see cref="IFrameUnlockedInputManager{T}"/> at once.
        /// </summary>
        /// <param name="frequency"></param>
        void SetPollingFrequency(int frequency);
    }
#endif

    /// <summary>
    /// Common input manager methods
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICommonInputManagerMethods<T> where T : Enum
    {
        /// <summary>
        /// Enables all the key delegates.
        /// </summary>
        void Enable();

        /// <summary>
        /// Disables all the key delegates.
        /// Relevant input checking methods (e.g., <see cref="IInputManager{T}.CheckKey"/>) still pick up the keys,
        /// but it will not trigger key delegates.
        /// </summary>
        void Disable();

        /// <summary>
        /// Unregisters ALL key down delegates.
        /// </summary>
        void ResetOnKeyDownDelegate();

        /// <summary>
        /// Unregisters ALL key hold delegates.
        /// </summary>
        void ResetOnKeyHoldDelegate();

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
        /// Performs a batch override over a dictionary.
        /// Probably useful to restore the custom key bind from a save file.
        /// </summary>
        /// <param name="overridePaths">Pass the overrides with the enum member as their corresponding keys</param>
        /// <remarks>If the corresponding <see cref="KeySetting{T}"/>'s <see cref="KeySetting{T}.overrideable"/> is false, this will have no effect!</remarks>
        void PerformOverride(Dictionary<T, string> overridePaths);

        /// <summary>
        /// Start a rebind attempt. This is effectively an async operation.
        /// Calls <see cref="OnRebindDelegate{T}"/> on any state of completion.
        /// </summary>
        /// <param name="target">Rebind target</param>
        /// <param name="targetBindingGroup">The group in which the target key bind is associated with</param>
        /// <param name="operationConfigCallback">
        /// Callback to set up the rebinding operation options
        /// </param>
        /// <param name="onComplete">
        /// Callback on a successful rebind. This is where you attempt to save your new key configuration.
        /// For displaying the new key config on the UI, use <see cref="OnRebindDelegate{T}"/> where such information is posted.
        /// </param>
        /// <param name="onCancel">Callback on a cancelled rebind attempt</param>
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