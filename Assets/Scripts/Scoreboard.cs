using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class Scoreboard : MonoBehaviour {
    public TMP_Text text;
    public int feedLines = 4;

    readonly Queue<string> feed = new Queue<string>();
    float nextRefresh;
    GameManager subscribedTo;

    void Update() {
        GameManager gm = GameManager.Instance;
        if (gm != null && subscribedTo != gm) {
            gm.KillFeed += OnFeed; subscribedTo = gm; }

        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + 0.2f;

        var sb = new StringBuilder();
        sb.AppendLine(gm != null && gm.IsSpawned ? gm.StatusLine() : "Not connected");
        sb.AppendLine();
        foreach (PlayerState p in PlayerState.All.OrderByDescending(p => p.Kills.Value)) {
            string hex = ColorUtility.ToHtmlStringRGB(p.PlayerColor);
            sb.AppendLine($"<color=#{hex}>{p.DisplayName}</color>   K {p.Kills.Value}   D {p.Deaths.Value}" + (p.IsAlive.Value ? "" : "   (down)"));
        }
        sb.AppendLine();
        foreach (string line in feed) sb.AppendLine(line);
        text.text = sb.ToString();
    }

    void OnFeed(string message) {
        feed.Enqueue(message);
        while (feed.Count > feedLines) feed.Dequeue(); }

    void OnDestroy() {
        if (subscribedTo != null) subscribedTo.KillFeed -= OnFeed; }
}