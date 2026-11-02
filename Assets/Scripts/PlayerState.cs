using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;
[RequireComponent(typeof(PlayerAvatar))]
public class PlayerState : NetworkBehaviour {
    public const int MaxHealth = 100;
    public const float MaxCharge = 100f;

    public static readonly List<PlayerState> All = new List<PlayerState>();
    public static PlayerState Local {
        get; private set; }
    public float respawnDelay = 4f;
    public float spawnProtection = 1.5f;

    public NetworkVariable<int> Health = new NetworkVariable<int>(MaxHealth);
    public NetworkVariable<float> Charge = new NetworkVariable<float>(MaxCharge);
    public NetworkVariable<int> Kills = new NetworkVariable<int>(0);
    public NetworkVariable<int> Deaths = new NetworkVariable<int>(0);
    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(true);
    public NetworkVariable<int> SpawnIndex = new NetworkVariable<int>(-1);

    PlayerAvatar avatar;
    float protectedUntil;
    Coroutine respawnRoutine;

    public Vector3 HeadPosition => avatar.head.position;
    public Color PlayerColor => PlayerAvatar.PlayerColors[Mathf.Max(0, SpawnIndex.Value) % PlayerAvatar.PlayerColors.Length];
    public string DisplayName => SpawnIndex.Value >= 0
        ? $"{PlayerAvatar.PlayerNames[SpawnIndex.Value % PlayerAvatar.PlayerNames.Length]} (P{SpawnIndex.Value + 1})"
        : $"Player {OwnerClientId}";

    void Awake() { avatar = GetComponent<PlayerAvatar>(); }

    public override void OnNetworkSpawn() {
        All.Add(this);
        if (IsOwner) Local = this;

        // React whenever the server changes these values.
        SpawnIndex.OnValueChanged += OnSpawnIndexChanged;
        IsAlive.OnValueChanged += OnAliveChanged;

        OnSpawnIndexChanged(-1, SpawnIndex.Value);
        avatar.SetVisible(IsAlive.Value);

        if (IsServer)
            SpawnIndex.Value = SpawnManager.Instance.ClaimIndex(OwnerClientId);
        }

    public override void OnNetworkDespawn() {
        All.Remove(this);
        if (Local == this) Local = null;
        SpawnIndex.OnValueChanged -= OnSpawnIndexChanged;
        IsAlive.OnValueChanged -= OnAliveChanged;
        if (IsServer && SpawnManager.Instance)
            SpawnManager.Instance.Release(SpawnIndex.Value, OwnerClientId);
    }

    void OnSpawnIndexChanged(int oldIndex, int newIndex) {
        if (newIndex < 0) return;
        avatar.ApplyColor(newIndex);

        // first spawn: the owner moves their own rig onto their spawn point.
        if (IsOwner && LocalRig.Instance != null) {
            Transform p = SpawnManager.Instance.Get(newIndex);
            LocalRig.Instance.TeleportTo(p.position, SpawnManager.Instance.FacingCenter(p.position)); }
    }

    void OnAliveChanged(bool wasAlive, bool isAlive) {
        avatar.SetVisible(isAlive);
        if (IsOwner && LocalRig.Instance) LocalRig.Instance.SetAlive(isAlive);
        if (wasAlive && !isAlive) AudioSource.PlayClipAtPoint(Sfx.Death, HeadPosition, 1f);
    }

    public void ServerTakeDamage(int amount, ulong attackerClientId) {
        if (!IsServer || !IsAlive.Value) return;
        if (Time.time < protectedUntil) return;           // spawn protection

        Health.Value = Mathf.Max(0, Health.Value - amount);
        HitFeedbackRpc();

        if (Health.Value == 0) {
            IsAlive.Value = false;
            Deaths.Value++;
            GameManager.Instance.ServerRegisterKill(attackerClientId, OwnerClientId);
            respawnRoutine = StartCoroutine(RespawnLater());
        }
    }

    IEnumerator RespawnLater() {
        yield return new WaitForSeconds(respawnDelay);
        respawnRoutine = null;
        ServerRespawnAt(SpawnManager.Instance.GetSafest(this));
    }

    public void ServerRespawnAt(Transform point) {
        Health.Value = MaxHealth;
        Charge.Value = MaxCharge;
        IsAlive.Value = true;
        protectedUntil = Time.time + spawnProtection;
        MoveRigRpc(point.position, SpawnManager.Instance.FacingCenter(point.position));
    }

    public void ServerResetForNewMatch() {
        if (respawnRoutine != null) { StopCoroutine(respawnRoutine); respawnRoutine = null; }
        Kills.Value = 0;
        Deaths.Value = 0;
        ServerRespawnAt(SpawnManager.Instance.Get(SpawnIndex.Value));
    }

    [Rpc(SendTo.Owner)]
    void MoveRigRpc(Vector3 position, Vector3 facing) {
        if (LocalRig.Instance) LocalRig.Instance.TeleportTo(position, facing); }

    [Rpc(SendTo.Everyone)]
    void HitFeedbackRpc() {
        avatar.Flash();                                   // everyone sees a flash
        if (IsOwner) {                                  // victim feels it
            Haptics.Pulse(XRNode.LeftHand, 0.8f, 0.15f);
            Haptics.Pulse(XRNode.RightHand, 0.8f, 0.15f);
            AudioSource.PlayClipAtPoint(Sfx.Hurt, HeadPosition, 1f); }
    }
}
