using System;
using JetBrains.Annotations;

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
    /// where Frame is an integer that <i>would</i> be the frame count if the game were to run constantly
    /// with <see cref="IInputManager{T}.TargetFrameRate"/> which is by default 60fps.
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
    /// If currentFrame - previousFrame &gt; 1, you may want to iterate over frames that went past between these.
    /// </remarks>
    public delegate void OnKeyHoldDelegate<in T>(T key, int currentFrame, int previousFrame) where T : Enum;

    /// <summary>
    /// Delegate to check key hold event, where Frame is an integer that always increments by 1
    /// every time this is called until a key up event occurs.
    /// </summary>
    /// <remarks>
    /// The frame count passed to this function has no regards for framerate fluctuation,
    /// so if your game slows down for any reason and/or gets quicker than you expect,
    /// the frame count will increase in an unsteady rate.
    /// If non-timing-aware context, this delegate may be simpler to use.
    /// </remarks>
    /// <typeparam name="T">Action type expressed within the code</typeparam>
    public delegate void OnKeyHoldFrameRateUnawareDelegate<in T>(T key, int currentFrame) where T : Enum;

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
    /// If this is not null, this action now has the binding indicated by <see cref="swappedBinding"/>.
    /// </param>
    /// <param name="swappedBinding">
    /// See <see cref="swappedActionName"/>.
    /// </param>
    /// <typeparam name="T">Action type expressed within the code</typeparam>
    public delegate void OnRebindDelegate<in T>(T target, bool isCancelled, string readableKey, bool isDuplicate,
        [CanBeNull] string swappedActionName, [CanBeNull] string swappedBinding) where T : Enum;

#if UNITASK
    /// <summary>
    /// "High-frequency" variant of <see cref="OnKeyDownDelegate{T}"/>.
    /// The main idea is that this delegate is called with time information.
    /// <b>DO</b> expect this delegate to be called multiple times within a single frame,
    /// each call indicating a change that occurred since the last frame update.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="actionTimestamp">
    /// Timestamp (<see cref="UnityEngine.Time.realtimeSinceStartupAsDouble"/>) of this action
    /// </param>
    /// <param name="currentTimestamp">
    /// Equivalent of <see cref="UnityEngine.Time.realtimeSinceStartupAsDouble"/>
    /// </param>
    /// <remarks>
    /// <see cref="currentTimestamp"/> - <see cref="actionTimestamp"/> may be of use for you;
    /// use this information to calculate the time by which the action occurred earlier than the current frame.
    /// </remarks>
    public delegate void OnKeyDownFrameUnlockedDelegate<in T>(T key, double actionTimestamp, double currentTimestamp)
        where T : Enum;

    /// <summary>
    /// "High-frequency" variant of <see cref="OnKeyHoldDelegate{T}"/>.
    /// Instead of hold frame count, it comes with key down timestamp and current timestamp.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public delegate void OnKeyHoldFrameUnlockedDelegate<in T>(T key, double keyDownTimestamp, double currentTimestamp)
        where T : Enum;

    /// <summary>
    /// "High-frequency" variant of <see cref="OnKeyUpDelegate{T}"/>.
    /// The main idea is that this delegate is called with time information.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="actionTimestamp">
    /// Timestamp (<see cref="UnityEngine.Time.realtimeSinceStartupAsDouble"/>) of this action (seconds)
    /// </param>
    /// <param name="currentTimestamp">
    /// Equivalent of <see cref="UnityEngine.Time.realtimeSinceStartupAsDouble"/> (seconds)
    /// </param>
    /// <remarks>
    /// <see cref="currentTimestamp"/> - <see cref="actionTimestamp"/> may be of use for you;
    /// use this information to calculate the time by which the action occurred earlier than the current frame.
    /// </remarks>
    public delegate void OnKeyUpFrameUnlockedDelegate<in T>(T key, double actionTimestamp, double currentTimestamp)
        where T : Enum;
#endif
}