using UnityEngine;
using UnityEngine.Playables;

public class Hitbox : MonoBehaviour {
    public float damageMultiplier = 1f;   // head = 2, body = 1
    public PlayerState Owner {
        get; private set; }

    void Awake() {
        Owner = GetComponentInParent<PlayerState>(); }// walk up to the player root
    
}
