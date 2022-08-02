using InputManager.Domain;
using InputManager.Infra;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class InputChecker : MonoBehaviour
{
    [SerializeField] private Text spaceBarStatus;
    [SerializeField] private Text rebindInstruction;

    [SerializeField] private SampleKeySettings sampleKeySettings;
    private IInputManager<SampleKeys> _inputManager;

    private void Awake()
    {
        _inputManager = new InputManager<SampleKeys>(sampleKeySettings);
    }

    private void Start()
    {
        _inputManager.AddOnKeyDownDelegate(OnKeyDown);
        _inputManager.AddOnKeyUpDelegate(OnKeyUp);
        _inputManager.AddOnKeyHoldDelegate(OnKeyHold);
        _inputManager.AddOnRebindDelegate(OnKeyRebound);
    }

    private void Update()
    {
        // This is mandatory for our input manager to realize something is going on.
        _inputManager.CheckKey();
    }

    private void OnKeyDown(SampleKeys k)
    {
        switch (k)
        {
            case SampleKeys.Action:
                spaceBarStatus.text = "ON";
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

    private void OnKeyUp(SampleKeys k)
    {
        switch (k)
        {
            case SampleKeys.Action:
                spaceBarStatus.text = "OFF";
                break;
        }
    }

    private void OnKeyHold(SampleKeys k, int currentFrame, int previousFrame)
    {
        switch (k)
        {
            case SampleKeys.Action:
                spaceBarStatus.text =
                    $"ON: Held for {currentFrame} frames\n(adjusted for input manager's target frame rate ({_inputManager.TargetFrameRate} fps.)";
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
}