using Unity.Networking.Transport.Analytics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class LocalRig : MonoBehaviour {
    public static LocalRig Instance {
        get; private set; }
    public XROrigin origin;
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public TeleportationProvider teleportProvider;
    public GameObject deathOverlay;

    void Awake() {
        Instance = this;
        if (deathOverlay) deathOverlay.SetActive(false);
    }

    public void TeleportTo(Vector3 feetPos, Vector3 faceDir) {
        faceDir.y = 0;
        if (faceDir.sqrMagnitude < 0.001f) faceDir = Vector3.forward;

        var cc = origin.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        origin.MatchOriginUpCameraForward(Vector3.up, faceDir.normalized);
        Vector3 cam = origin.Camera.transform.position;
        origin.MoveCameraToWorldLocation(new Vector3(feetPos.x, cam.y, feetPos.z));

        if (cc) cc.enabled = true;
    }

    public void SetAlive(bool alive) {
        if (teleportProvider) teleportProvider.enabled = alive;
        if (deathOverlay) deathOverlay.SetActive(!alive); }

}
