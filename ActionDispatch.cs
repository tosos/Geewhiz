using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Raises "ButtonDown<Action>" and "ButtonUp<Action>" through the Dispatcher for every
// Button action in the enabled maps. Pointer presses over UI are dropped.
// Runs early so polls of WasPressed and WasReleased in the same frame see the edges.
[DefaultExecutionOrder(-100)]
public class ActionDispatch : MonoBehaviour
{
    public InputActionAsset actions;
    public string[] mapNames;
    public string pointAction = "Point";

    static private ActionDispatch _instance = null;
    static public ActionDispatch instance
    {
        get
        {
            return _instance;
        }
    }

    private Dispatcher dispatcher;
    private InputAction[] buttons;
    // A release only goes out when its press did, for every action, so receivers never
    // see an orphan ButtonUp after a press that was dropped over UI.
    private HashSet<InputAction> raisedDown = new HashSet<InputAction>();
    // IsPressed as of the last Update, to see presses that landed in another input update.
    private HashSet<InputAction> isDown = new HashSet<InputAction>();
    private HashSet<InputAction> pressedThisFrame = new HashSet<InputAction>();
    private HashSet<InputAction> releasedThisFrame = new HashSet<InputAction>();

    public Vector2 PointerPosition => Read<Vector2>(pointAction);

    public bool PointerOverUI
    {
        get
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }

    void Awake()
    {
        if (_instance != null) { Debug.LogError("Instance should be null"); }
        _instance = this;
    }

    void OnDestroy()
    {
        _instance = null;
    }

    void Start()
    {
        dispatcher = Dispatcher.instance;
    }

    void OnEnable()
    {
        var list = new List<InputAction>();
        foreach (var mapName in mapNames) {
            var map = actions.FindActionMap(mapName, true);
            map.Enable();
            foreach (var action in map.actions) {
                if (action.type == InputActionType.Button) { list.Add(action); }
            }
        }
        buttons = list.ToArray();
    }

    void OnDisable()
    {
        foreach (var mapName in mapNames) { actions.FindActionMap(mapName, true).Disable(); }
        raisedDown.Clear();
        isDown.Clear();
        pressedThisFrame.Clear();
        releasedThisFrame.Clear();
    }

    public T Read<T>(string name) where T : struct
    {
        return actions.FindAction(name, true).ReadValue<T>();
    }

    public bool IsPressed(string name)
    {
        return actions.FindAction(name, true).IsPressed();
    }

    // True for the frame in which ButtonDown was raised for the action. Pointer presses
    // dropped over UI do not count.
    public bool WasPressed(string name)
    {
        return pressedThisFrame.Contains(actions.FindAction(name, true));
    }

    // True for the frame in which ButtonUp was raised for the action.
    public bool WasReleased(string name)
    {
        return releasedThisFrame.Contains(actions.FindAction(name, true));
    }

    // Uses the bound controls, not activeControl, which is null when a press and its
    // release land in one frame.
    private bool IsPointerAction(InputAction action)
    {
        foreach (var control in action.controls) {
            if (control.device is Pointer) { return true; }
        }
        return false;
    }

    // Presses are read here, not in action callbacks, because
    // IsPointerOverGameObject misreports inside a callback.
    // Edges are the change in IsPressed since the last Update, plus the
    // WasPressedThisFrame/WasReleasedThisFrame edges, which catch a press and release
    // inside one frame. The change catches events processed in another input update.
    void Update()
    {
        pressedThisFrame.Clear();
        releasedThisFrame.Clear();
        var overUI = PointerOverUI;
        foreach (var action in buttons) {
            var pressed = action.IsPressed();
            var wasDown = isDown.Contains(action);
            if (pressed) { isDown.Add(action); } else { isDown.Remove(action); }

            if ((pressed && !wasDown) || action.WasPressedThisFrame()) {
                if (!(overUI && IsPointerAction(action))) {
                    raisedDown.Add(action);
                    pressedThisFrame.Add(action);
                    dispatcher.Dispatch("ButtonDown" + action.name);
                }
            }
            if (((!pressed && wasDown) || action.WasReleasedThisFrame()) && raisedDown.Remove(action)) {
                releasedThisFrame.Add(action);
                dispatcher.Dispatch("ButtonUp" + action.name);
            }
        }
    }
}
