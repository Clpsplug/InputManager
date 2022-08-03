using System;

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
    /// If this is not null, this action now has the binding indicated by swappedBinding.
    /// </param>
    /// <param name="swappedBinding">
    /// See swappedActionName.
    /// </param>
    /// <typeparam name="T">Action type expressed within the code</typeparam>
    public delegate void OnRebindDelegate<in T>(T target, bool isCancelled, string readableKey, bool isDuplicate,
        string swappedActionName, string swappedBinding) where T : Enum;
}