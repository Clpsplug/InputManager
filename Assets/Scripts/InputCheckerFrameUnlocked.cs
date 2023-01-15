#if UNITASK
using System.Threading;
#endif
using InputManager.Domain;
using InputManager.Infra;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

public class InputCheckerFrameUnlocked : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI actionKeyStatus;
    [SerializeField] private TextMeshProUGUI rebindInstruction;

    [SerializeField] private SampleKeySettings sampleKeySettings;

    private IFrameUnlockedInputManager<SampleKeys> _inputManager;
#if UNITASK
    private CancellationTokenSource _cancellationTokenSource;
#else
    private Coroutine _checkKeyCoroutine;
#endif

    private void Start()
    {
        _inputManager = new FrameUnlockedInputManager<SampleKeys>(sampleKeySettings);
        _inputManager.AddOnKeyDownDelegate(OnKeyDown);
        _inputManager.AddOnRebindDelegate(OnKeyRebound);
        _inputManager.SetPollingFrequency(1000);
#if UNITASK
        // NOTE: It is essential to create a cancellation token and explicitly cancel the CheckKey method. Otherwise, it may lead to memory leaks.
        _cancellationTokenSource = new CancellationTokenSource();
        _inputManager.CheckKey(() => true, _cancellationTokenSource.Token);
#else
        // If using the Coroutine variant, manual resource disposal is required. See OnDestroy().
        _checkKeyCoroutine = StartCoroutine(_inputManager.CheckKey(() => true));
#endif
    }

    private void OnKeyDown(SampleKeys k, double actionTimestamp, double currentTimestamp)
    {
        switch (k)
        {
            case SampleKeys.Action:
                actionKeyStatus.text =
                    $"Action key was pressed at\n{actionTimestamp:F5} since starting this session,\nwhich is {currentTimestamp - actionTimestamp:F5} seconds\nbefore this frame.";
                Debug.Log($"Action Timestamp: {actionTimestamp:F5} (s), Current Timestamp: {currentTimestamp:F5} (s)");
                break;
            case SampleKeys.Rebind:
                rebindInstruction.text =
                    "Please press another key to assign to the action.\nYou cannot assign the escape key and the enter key, though.\nPress the escape key to cancel.";
                _inputManager.RequestRebind(
                    SampleKeys.Action,
                    "Keyboard",
                    op =>
                    {
                        // Here, you will use those methods to do the following:
                        // * What binding group does the target key bind must be a part of
                        // * What key (or control path) must be ignored as the key bind
                        // * What key to press to cancel the operation.
                        op
                            .WithControlsHavingToMatchPath("<keyboard>")
                            .WithCancelingThrough("<keyboard>/escape")
                            .WithControlsExcluding("<keyboard>/enter")
                            // Known issue: mashing keys while rebinding may bind the action to this catch-all control path.
                            //              Thus it is recommended to filter out this path.
                            .WithControlsExcluding("<keyboard>/anyKey");
                        return op;
                    }
                );
                break;
        }
    }

    private void OnKeyRebound(SampleKeys target, bool isCancelled, string readableKey, bool isDuplicate,
        [CanBeNull] string swappedActionName, [CanBeNull] string swappedBinding)
    {
        if (isCancelled)
        {
            rebindInstruction.text = "Rebind operation has been cancelled.\nPress the enter key to start rebind again.";
            return;
        }

        rebindInstruction.text =
            $"Key bind for action {target} has been bound to {readableKey}.\nPress the enter key to start rebind again.";
    }

    private void OnDestroy()
    {
#if UNITASK
        // Cancel unless something crashed at Start()
        _cancellationTokenSource?.Cancel();
#else
        // Stop the CheckKey coroutine and dispose of the internal resources
        StopCoroutine(_checkKeyCoroutine);
        _inputManager.Dispose();
#endif
    }
}