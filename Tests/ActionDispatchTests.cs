using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ActionDispatchTests : InputTestFixture
{
    public class Receiver : MonoBehaviour
    {
        public string label;
        public List<string> log;
        void ButtonDownCancel() { log.Add(label + ":DownCancel"); }
        void ButtonUpCancel() { log.Add(label + ":UpCancel"); }
        void ButtonDownSelect() { log.Add(label + ":DownSelect"); }
        void ButtonUpSelect() { log.Add(label + ":UpSelect"); }
    }

    private Keyboard keyboard;
    private Mouse mouse;
    private InputActionAsset asset;
    private GameObject hub;
    private ActionDispatch actionDispatch;
    private List<GameObject> spawned = new List<GameObject>();
    private List<string> log = new List<string>();

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        log.Clear();
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();

        asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("Gameplay");
        map.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
        map.AddAction("Select", InputActionType.Button, "<Mouse>/leftButton");
        map.AddAction("Point", InputActionType.Value, "<Mouse>/position");
        map.AddAction("Pan", InputActionType.Value).AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");

        hub = new GameObject("Hub");
        // The components are inactive until configured, so OnEnable sees the asset.
        hub.SetActive(false);
        hub.AddComponent<Dispatcher>();
        actionDispatch = hub.AddComponent<ActionDispatch>();
        actionDispatch.actions = asset;
        actionDispatch.mapNames = new[] { "Gameplay" };
        hub.SetActive(true);
    }

    [TearDown]
    public override void TearDown()
    {
        foreach (var go in spawned) { Object.DestroyImmediate(go); }
        Object.DestroyImmediate(hub);
        Object.DestroyImmediate(asset);
        base.TearDown();
    }

    private Receiver Spawn(string label, bool parallel)
    {
        var go = new GameObject(label);
        spawned.Add(go);
        var receiver = go.AddComponent<Receiver>();
        receiver.label = label;
        receiver.log = log;
        Dispatcher.instance.Register("ButtonDownCancel", go, parallel);
        Dispatcher.instance.Register("ButtonUpCancel", go, parallel);
        Dispatcher.instance.Register("ButtonDownSelect", go, parallel);
        Dispatcher.instance.Register("ButtonUpSelect", go, parallel);
        return receiver;
    }

    [UnityTest]
    public IEnumerator ButtonActionReachesReceiverAsDownAndUp()
    {
        Spawn("a", true);
        yield return null;

        Press(keyboard.escapeKey);
        yield return null;
        Release(keyboard.escapeKey);
        yield return null;

        CollectionAssert.AreEqual(new[] { "a:DownCancel", "a:UpCancel" }, log);
    }

    [UnityTest]
    public IEnumerator SerialStackDeliversToTopReceiverOnly()
    {
        Spawn("bottom", false);
        Spawn("top", false);
        yield return null;

        Press(keyboard.escapeKey);
        yield return null;

        CollectionAssert.AreEqual(new[] { "top:DownCancel" }, log);
    }

    [UnityTest]
    public IEnumerator PointerPressOverUIRaisesNothing()
    {
        BuildFullScreenPanel();

        Spawn("a", true);
        Set(mouse.position, new Vector2(10, 10));
        yield return null;
        yield return null;
        Assert.IsTrue(actionDispatch.PointerOverUI, "Setup: panel should cover the pointer");

        Press(mouse.leftButton);
        yield return null;
        Release(mouse.leftButton);
        yield return null;

        CollectionAssert.IsEmpty(log);
    }

    [UnityTest]
    public IEnumerator PointerPressAndReleaseInOneFrameOverUIRaisesNothing()
    {
        BuildFullScreenPanel();

        Spawn("a", true);
        Set(mouse.position, new Vector2(10, 10));
        yield return null;
        yield return null;
        Assert.IsTrue(actionDispatch.PointerOverUI, "Setup: panel should cover the pointer");

        Press(mouse.leftButton);
        Release(mouse.leftButton);
        yield return null;

        CollectionAssert.IsEmpty(log);
    }

    private void BuildFullScreenPanel()
    {
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        spawned.Add(eventSystem);
        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
        spawned.Add(canvasObject);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        var rect = (RectTransform) panel.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    [UnityTest]
    public IEnumerator PointerPressOffUIRaisesSelect()
    {
        Spawn("a", true);
        Set(mouse.position, new Vector2(10, 10));
        yield return null;

        Press(mouse.leftButton);
        yield return null;
        Release(mouse.leftButton);
        yield return null;

        CollectionAssert.AreEqual(new[] { "a:DownSelect", "a:UpSelect" }, log);
    }

    [UnityTest]
    public IEnumerator PointerPressAndReleaseInOneFrameRaisesBoth()
    {
        Spawn("a", true);
        Set(mouse.position, new Vector2(10, 10));
        yield return null;

        // A fast click in a slow frame: the action is back to Waiting when Update runs.
        Press(mouse.leftButton);
        Release(mouse.leftButton);
        yield return null;

        CollectionAssert.AreEqual(new[] { "a:DownSelect", "a:UpSelect" }, log);
    }

    [UnityTest]
    public IEnumerator KeyPressAndReleaseInOneFrameRaisesBoth()
    {
        Spawn("a", true);
        yield return null;

        Press(keyboard.escapeKey);
        Release(keyboard.escapeKey);
        yield return null;

        CollectionAssert.AreEqual(new[] { "a:DownCancel", "a:UpCancel" }, log);
    }

    [UnityTest]
    public IEnumerator PressProcessedInSeparateInputUpdateRaisesDownAndUpOnce()
    {
        Spawn("a", true);
        yield return null;

        // simulate_key and manual updates process events outside the frame's own update,
        // so WasPressedThisFrame is already false when ActionDispatch.Update runs.
        Press(keyboard.escapeKey);
        InputSystem.Update();
        yield return null;
        CollectionAssert.AreEqual(new[] { "a:DownCancel" }, log);

        yield return null;
        CollectionAssert.AreEqual(new[] { "a:DownCancel" }, log, "Held key must not repeat Down");

        Release(keyboard.escapeKey);
        InputSystem.Update();
        yield return null;
        CollectionAssert.AreEqual(new[] { "a:DownCancel", "a:UpCancel" }, log);
    }

    [UnityTest]
    public IEnumerator WasPressedAndWasReleasedFollowTheSameEdges()
    {
        yield return null;
        Assert.IsFalse(actionDispatch.WasPressed("Cancel"));

        Press(keyboard.escapeKey);
        InputSystem.Update();
        yield return null;
        Assert.IsTrue(actionDispatch.WasPressed("Cancel"));
        Assert.IsFalse(actionDispatch.WasReleased("Cancel"));

        yield return null;
        Assert.IsFalse(actionDispatch.WasPressed("Cancel"), "Edge lasts one frame");

        Release(keyboard.escapeKey);
        InputSystem.Update();
        yield return null;
        Assert.IsTrue(actionDispatch.WasReleased("Cancel"));
        Assert.IsFalse(actionDispatch.WasPressed("Cancel"));

        // Inside one frame both edges count.
        yield return null;
        Press(keyboard.escapeKey);
        Release(keyboard.escapeKey);
        yield return null;
        Assert.IsTrue(actionDispatch.WasPressed("Cancel"));
        Assert.IsTrue(actionDispatch.WasReleased("Cancel"));
    }

    [UnityTest]
    public IEnumerator PointerPressOverUIIsNotWasPressed()
    {
        BuildFullScreenPanel();

        Set(mouse.position, new Vector2(10, 10));
        yield return null;
        yield return null;
        Assert.IsTrue(actionDispatch.PointerOverUI, "Setup: panel should cover the pointer");

        Press(mouse.leftButton);
        InputSystem.Update();
        yield return null;
        Assert.IsFalse(actionDispatch.WasPressed("Select"));

        Release(mouse.leftButton);
        InputSystem.Update();
        yield return null;
        Assert.IsFalse(actionDispatch.WasReleased("Select"));
    }

    [UnityTest]
    public IEnumerator ReadVector2AndIsPressedFollowDeviceState()
    {
        yield return null;
        Assert.AreEqual(Vector2.zero, actionDispatch.Read<Vector2>("Pan"));
        Assert.IsFalse(actionDispatch.IsPressed("Cancel"));

        // Queued presses on one device overwrite each other, so process each.
        Press(keyboard.dKey);
        yield return null;
        Press(keyboard.wKey);
        yield return null;
        Press(keyboard.escapeKey);
        Set(mouse.position, new Vector2(30, 40));
        yield return null;

        var pan = actionDispatch.Read<Vector2>("Pan");
        Assert.Greater(pan.x, 0f);
        Assert.Greater(pan.y, 0f);
        Assert.IsTrue(actionDispatch.IsPressed("Cancel"));
        Assert.AreEqual(new Vector2(30, 40), actionDispatch.PointerPosition);

        Release(keyboard.escapeKey);
        yield return null;
        Assert.IsFalse(actionDispatch.IsPressed("Cancel"));
    }
}
