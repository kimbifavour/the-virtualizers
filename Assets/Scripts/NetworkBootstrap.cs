using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class NetworkBootstrap : MonoBehaviour {
    public enum Mode {
        RelaySession, Lan }

    public Mode mode = Mode.RelaySession;
    public string sessionName = "cmps297ag-team1"; // CHANGE per group, same on all headsets
    public bool autoConnect = true;

    [Header("LAN fallback")]
    public string hostIp = "192.168.1.10";
    public ushort port = 7777;

    public string Status { get; private set; } = "Starting...";

    ISession session;
    bool busy;

    async void Start() {
        NetworkManager.Singleton.OnClientStopped += wasHost => {
            session = null;
            Status = "Disconnected. Press Connect."; };
        if (autoConnect && mode == Mode.RelaySession) await ConnectRelay();
    }

    public async void ConnectButton() {
        if (mode == Mode.RelaySession) await ConnectRelay(); }

    async Task ConnectRelay() {
        if (busy || NetworkManager.Singleton.IsListening) return;
        busy = true;
        try {
            Status = "Signing in...";
            if (UnityServices.State != ServicesInitializationState.Initialized){
                var options = new InitializationOptions();
#if UNITY_EDITOR
                options.SetProfile("editor" + UnityEngine.Random.Range(0, 99999));
#endif
                await UnityServices.InitializeAsync(options); }

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Status = $"Joining '{sessionName}'...";
            var sessionOptions = new SessionOptions { MaxPlayers = 5 }.WithRelayNetwork();
            session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, sessionOptions);

            Status = session.IsHost ? $"HOST of '{sessionName}'" : $"Joined '{sessionName}'";
        }
        catch (Exception e) {
            Status = "Connect failed: " + e.Message;
            Debug.LogException(e); }
        finally {
            busy = false; }
    }

    public void HostLan() {
        if (NetworkManager.Singleton.IsListening) return;
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData("127.0.0.1", port, "0.0.0.0"); // listen on all network interfaces
        NetworkManager.Singleton.StartHost();
        Status = $"LAN host on port {port}";
    }

    public void JoinLan() {
        if (NetworkManager.Singleton.IsListening) return;
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData(hostIp, port);
        NetworkManager.Singleton.StartClient();
        Status = $"Connecting to {hostIp}...";
    }

    async void OnApplicationQuit() {
        if (session != null) await session.LeaveAsync(); }
}
