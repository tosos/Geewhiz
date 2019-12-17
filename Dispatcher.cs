using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if CLICKS_USES_PUN
using Photon.Pun;
using Photon.Realtime;
#endif

public class Dispatcher : MonoBehaviour {

    private Dictionary< string, List<GameObject> > serialRecv;
    private Dictionary< string, List<GameObject> > parallelRecv;

    public string debugMessage = "";

    static private Dispatcher _instance = null;
    static public Dispatcher instance {
        get { return _instance; }
    }

	private const byte DispatchMessageId = 100;

    void Awake()
    {
        if (_instance != null)
        {
            Debug.LogError("Instance should be null");
        }
        _instance = this;

        serialRecv = new Dictionary<string, List<GameObject>>();
        parallelRecv = new Dictionary<string, List<GameObject>>();

#if CLICKS_USES_PUN
        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
#endif
    }

    void OnDestroy () {
        _instance = null;

#if CLICKS_USES_PUN
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
#endif
    }

    public void Register (string message, GameObject obj, bool isParallel = true) {
        if (isParallel) {
            if (!parallelRecv.ContainsKey (message)) {
                parallelRecv.Add (message, new List<GameObject> ());
            } 
            if (!parallelRecv[message].Contains (obj)) { 
                parallelRecv[message].Add(obj);
            }
        } else {
            if (!serialRecv.ContainsKey (message)) {
                serialRecv.Add (message, new List<GameObject> ());
            } 
            if (!serialRecv[message].Contains (obj)) {
                serialRecv[message].Insert(0, obj);
            }
            UpdateDebug ();
        }
    }

    public void Unregister (string message, GameObject obj) {
        if (parallelRecv.ContainsKey (message)) {
            parallelRecv[message].Remove (obj);
        }
        if (serialRecv.ContainsKey (message)) {
            serialRecv[message].Remove (obj);
            UpdateDebug ();
        }
    }

    public void Dispatch (string message, object parameter = null) {
        if (serialRecv.ContainsKey (message) && serialRecv[message].Count > 0) {
            if (serialRecv[message][0] != null && serialRecv[message][0].activeSelf) {
                serialRecv[message][0].SendMessage (message, parameter);
            } else {
                serialRecv[message].RemoveAt (0);
            }
        }

        if (parallelRecv.ContainsKey (message)) {
            foreach (GameObject go in parallelRecv[message]) {
                if (go != null) {
                    go.SendMessage (message, parameter);
                }
            }
        }
    }

    public void RemoteDispatch(string message, object parameter = null)
    {
#if CLICKS_USES_PUN
        object[] parameters;
        if (parameter == null) {
            parameters = new object[]{ message };
        }
        else
        {
            parameters = new object[]{ message, parameter };
        }
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        var sendOptions = new ExitGames.Client.Photon.SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(DispatchMessageId, parameters, raiseEventOptions, sendOptions);
#endif
        // Fallback to local send also
        Dispatch(message, parameter);
    }

#if CLICKS_USES_PUN
    public void OnPhotonEvent(ExitGames.Client.Photon.EventData photonEvent) { 
        if (photonEvent.Code != DispatchMessageId)
        {
            return;
        }
        object[] data = (object[])photonEvent.CustomData;
        if (data.Length > 1)
        {
            Dispatch((string)data[0], data[1]);
        }
        else
        {
            Dispatch((string)data[0]);
        }
    }
#endif
    
    void UpdateDebug () {
/* TODO Update
        if (debugMessage != "" && GetComponent<GUIText>() != null) {
            GetComponent<GUIText>().text = "";
            foreach (GameObject go in serialRecv[debugMessage]) {
                GetComponent<GUIText>().text += go.name + "\n";
            }
        }
*/
    }
}
