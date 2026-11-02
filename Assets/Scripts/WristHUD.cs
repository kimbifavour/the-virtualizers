using TMPro;
using Unity.Netcode;
using UnityEngine;

public class WristHUD : MonoBehaviour {
    public TMP_Text text;
    public NetworkBootstrap bootstrap;
    public GameObject hostPanel;
    public GameObject connectButton;

    float nextRefresh;

    void Update() {
        var nm = NetworkManager.Singleton;
        bool connected = nm != null && nm.IsListening;
        if (hostPanel) hostPanel.SetActive(connected && nm.IsServer);
        if (connectButton) connectButton.SetActive(!connected);

        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + 0.1f;

        PlayerState me = PlayerState.Local;
        GameManager gm = GameManager.Instance;
        if (!connected || me == null) {
            text.text = bootstrap ? bootstrap.Status : "Not connected";
            return; }

        int bars = Mathf.RoundToInt(me.Charge.Value / PlayerState.MaxCharge * 10);
        text.text = $"<b>{me.DisplayName}</b>\n" + $"HP {me.Health.Value}/{PlayerState.MaxHealth}\n" + $"Charge [{new string('#', bars)}{new string('-', 10 - bars)}]\n" 
            + $"Kills {me.Kills.Value}   Deaths {me.Deaths.Value}\n" + (me.IsAlive.Value ? "" : "<color=red>ELIMINATED</color>\n") + (gm ? gm.StatusLine() : "");
    }
}
