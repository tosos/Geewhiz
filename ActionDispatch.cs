using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Raises "ButtonDown<Action>" and "ButtonUp<Action>" through the Dispatcher for every
// Button action in the enabled maps. Pointer presses over UI are dropped.
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
    }

    public T Read<T>(string name) where T : struct
    {
        return actions.FindAction(name, true).ReadValue<T>();
    }

    public bool IsPressed(string name)
    {
        return actions.FindAction(name, true).IsPressed();
    }

    public bool WasPressed(string name) { return false; }

    public bool WasReleased(string name) { return false; }

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
    void Update()
    {
        var overUI = PointerOverUI;
        foreach (var action in buttons) {
            if (action.WasPressedThisFrame()) {
                if (overUI && IsPointerAction(action)) { continue; }
                raisedDown.Add(action);
                dispatcher.Dispatch("ButtonDown" + action.name);
            }
            if (action.WasReleasedThisFrame() && raisedDown.Remove(action)) {
                dispatcher.Dispatch("ButtonUp" + action.name);
            }
        }
    }
}
