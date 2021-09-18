using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if GEEWHIZ_USES_OCULUS
public class OVRInputDispatch : MonoBehaviour
{
    [System.Serializable]
    public class ButtonDefn
    {
        public OVRInput.RawButton button;
        [Tooltip("Set to none, if no touch message should be sent")]
        public OVRInput.RawTouch touch;
    }
    public ButtonDefn[] buttons;

    void Start()
    {
        if (buttons.Length == 0) { enabled = false; }
    }

    // Update is called once per frame
    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) {
            return;
        }

        foreach(var button in buttons)
        {
            if (OVRInput.GetDown(button.touch)) {
                Dispatcher.instance.Dispatch("ButtonTouched" + button.touch.ToString(), "");
            }
            if (OVRInput.GetUp(button.touch)) {
                Dispatcher.instance.Dispatch("ButtonUntouched" + button.touch.ToString(), "");
            }
            if (OVRInput.GetDown(button.button)) {
                
                Dispatcher.instance.Dispatch("ButtonDown" + button.button.ToString(), "");
            }
            if (OVRInput.GetUp(button.button)) {
                Dispatcher.instance.Dispatch("ButtonUp" + button.button.ToString(), "");
            }
        }
    }
}
#else
public class OVRInputDispatch : MonoBehaviour
{
    [Tooltip("Set GEEWHIZ_USES_OCULUS in player settings")]
    public bool noInputAvailable;
}
#endif
