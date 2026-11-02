using System;
using System.Collections;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
public enum MatchState : byte {Lobby, Countdown, Playing, Ended}

public class GameManager : NetworkBehaviour {
    public static GameManager Instance {
        get; private set; }
    public const ulong NoWinner = ulong.MaxValue;

    [Header("Rules")]
    public int killsToWin = 10;
    public float matchSeconds = 180f;
    public float countdownSeconds = 3f;

    public NetworkVariable<MatchState> State = new NetworkVariable<MatchState>(MatchState.Lobby);
    public NetworkVariable<float> TimeLeft = new NetworkVariable<float>(0f);
    public NetworkVariable<ulong> WinnerId = new NetworkVariable<ulong>(NoWinner);

    public event Action<string> KillFeed;
    public bool DamageEnabled => State.Value == MatchState.Playing;

    void Awake() { Instance = this; }

    public override void OnNetworkSpawn() {
        if (IsServer) NetworkManager.OnClientDisconnectCallback += OnClientLeft; }

    public override void OnNetworkDespawn() {
        if (IsServer && NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnClientLeft; }

    public void StartOrRestartMatch() {
        if (!IsServer) { Debug.LogWarning("Only the host can start the match."); return; }
        if (State.Value == MatchState.Countdown || State.Value == MatchState.Playing) return;
        StartCoroutine(MatchRoutine());
    }

    public void ForceEndMatch() {
        if (IsServer && State.Value == MatchState.Playing) ServerEndMatch(Leader()); }

    IEnumerator MatchRoutine() {
        foreach (var p in PlayerState.All.ToList()) p.ServerResetForNewMatch(); // scores 0, back to own spawn
        WinnerId.Value = NoWinner;

        State.Value = MatchState.Countdown;
        TimeLeft.Value = countdownSeconds;
        while (TimeLeft.Value > 0) { yield return null; TimeLeft.Value -= Time.deltaTime; }

        State.Value = MatchState.Playing;
        TimeLeft.Value = matchSeconds;
        AnnounceRpc("FIGHT!");
        while (State.Value == MatchState.Playing && TimeLeft.Value > 0) {
            yield return null;
            TimeLeft.Value -= Time.deltaTime; }
        if (State.Value == MatchState.Playing) ServerEndMatch(Leader()); // time ran out
    }

    public void ServerRegisterKill(ulong killerId, ulong victimId) {
        if (!IsServer) return;
        PlayerState killer = Find(killerId);
        if (killer != null && killerId != victimId && State.Value == MatchState.Playing) {
            killer.Kills.Value++;
            AnnounceRpc($"{NameOf(killerId)} tagged {NameOf(victimId)}");
            if (killer.Kills.Value >= killsToWin) ServerEndMatch(killerId);
        }
    }

    void ServerEndMatch(ulong winner) {
        WinnerId.Value = winner;
        State.Value = MatchState.Ended;
        AnnounceRpc(winner == NoWinner ? "Match over" : $"{NameOf(winner)} WINS!");
    }

    void OnClientLeft(ulong clientId) {
        AnnounceRpc($"{NameOf(clientId)} disconnected");
        StartCoroutine(CheckPlayersNextFrame());
    }

    IEnumerator CheckPlayersNextFrame() {
        yield return null;
        if (State.Value == MatchState.Playing && PlayerState.All.Count < 2)
            ServerEndMatch(Leader());
    }

    ulong Leader() {
        var best = PlayerState.All
            .OrderByDescending(p => p.Kills.Value)
            .ThenBy(p => p.Deaths.Value)
            .FirstOrDefault();
        return best != null ? best.OwnerClientId : NoWinner;
    }

    PlayerState Find(ulong clientId) => PlayerState.All.FirstOrDefault(p => p.OwnerClientId == clientId);
    public string NameOf(ulong clientId) => Find(clientId)?.DisplayName ?? $"Player {clientId}";

    [Rpc(SendTo.Everyone)]
    void AnnounceRpc(FixedString128Bytes message) {
        KillFeed?.Invoke(message.ToString()); }

    public string StatusLine() {
        switch (State.Value) {
            case MatchState.Lobby: return "Lobby - waiting for host to start";
            case MatchState.Countdown: return $"Starting in {Mathf.CeilToInt(TimeLeft.Value)}...";
            case MatchState.Playing:
                int t = Mathf.Max(0, Mathf.CeilToInt(TimeLeft.Value));
                return $"Time {t / 60}:{t % 60:00}   First to {killsToWin}";
            case MatchState.Ended:
                return WinnerId.Value == NoWinner ? "Match over" : $"WINNER: {NameOf(WinnerId.Value)}";
        }
        return "";
    }
}
