using InputManager.Infra;
using UnityEngine;
using UnityEngine.UI;

public class InputChecker : MonoBehaviour
{
    [SerializeField] private Text spaceBarStatus;

    [SerializeField] private SampleKeySettings sampleKeySettings;
    private InputManager<SampleKeys> _inputManager;

    private void Awake()
    {
        _inputManager = new InputManager<SampleKeys>(sampleKeySettings);
    }

    private void Start()
    {
        _inputManager.AddOnKeyDownDelegate(OnKeyDown);
        _inputManager.AddOnKeyUpDelegate(OnKeyUp);
        _inputManager.AddOnKeyHoldDelegate(OnKeyHold);
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
            case SampleKeys.Space:
                spaceBarStatus.text = "ON";
                break;
        }
    }

    private void OnKeyUp(SampleKeys k)
    {
        switch (k)
        {
            case SampleKeys.Space:
                spaceBarStatus.text = "OFF";
                break;
        }
    }

    private void OnKeyHold(SampleKeys k, int currentFrame, int previousFrame)
    {
        switch (k)
        {
            case SampleKeys.Space:
                spaceBarStatus.text =
                    $"ON: Held for {currentFrame} frames\n(adjusted for input manager's target frame rate ({_inputManager.TargetFrameRate} fps.)";
                break;
        }
    }
}