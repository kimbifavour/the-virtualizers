using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// Host-only: press A + X together (or Enter in the editor) to start or restart the match.
// It creates itself at runtime, so no scene changes are needed.
public class HostStartInput : MonoBehaviour {
    bool wasPressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create() {
        var go = new GameObject("HostStartInput");
        go.AddComponent<HostStartInput>();
        DontDestroyOnLoad(go);
    }

    static bool Button(XRNode node) {
        var device = InputDevices.GetDeviceAtXRNode(node);
        return device.isValid
            && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool pressed)
            && pressed;
    }

    void Update() {
        var nm = NetworkManager.Singleton;
        var gm = GameManager.Instance;
        if (nm == null || !nm.IsServer || gm == null || !gm.IsSpawned) return;

        bool combo = Button(XRNode.LeftHand) && Button(XRNode.RightHand);
        bool key = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;

        if ((combo && !wasPressed) || key) gm.StartOrRestartMatch();
        wasPressed = combo;
    }
}