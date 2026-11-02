using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class TeleporterOccupancyArea : TeleportationArea {
    public LayerMask playerMask;
    public float clearRadius = 0.5f;

    static readonly Collider[] buffer = new Collider[16];
    protected override bool GenerateTeleportRequest(IXRInteractor interactor,
        RaycastHit raycastHit, ref TeleportRequest teleportRequest) {
        if (!base.GenerateTeleportRequest(interactor, raycastHit, ref teleportRequest))
            return false;

        Vector3 p = teleportRequest.destinationPosition;
        int n = Physics.OverlapCapsuleNonAlloc(
            p + Vector3.up * clearRadius, p + Vector3.up * 1.8f, clearRadius,
            buffer, playerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < n; i++) {
            Hitbox hb = buffer[i].GetComponentInParent<Hitbox>();
            if (hb != null && hb.Owner != null && hb.Owner.IsOwner) continue;
            return false;
        }
        return true;
    }
}
