using System.Text;
using TMPro;
using UnityEngine;

// Builds the final ranked list when the match ends.
// Assign a TMP_Text to "text" (e.g. a world-space panel). Until then it just logs the result.
public class GameOverBoard : MonoBehaviour {
    public TMP_Text text;
    public GameObject panel;

    GameManager subscribedTo;
    MatchState lastState = MatchState.Lobby;

    void Update() {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsSpawned) { Show(false); return; }

        MatchState s = gm.State.Value;
        if (s == lastState) return;
        lastState = s;

        if (s == MatchState.Ended) {
            string board = Build(gm);
            if (text) text.text = board;
            Debug.Log(board);
            Show(true);
        } else Show(false);
    }

    void Show(bool on) { if (panel && panel.activeSelf != on) panel.SetActive(on); }

    string Build(GameManager gm) {
        var sb = new StringBuilder();
        sb.AppendLine(gm.WinnerId.Value == GameManager.NoWinner
            ? "<b>MATCH OVER</b>"
            : $"<b>{gm.NameOf(gm.WinnerId.Value)} WINS!</b>");
        sb.AppendLine();
        foreach (PlayerState p in gm.Ranked()) {
            bool me = p == PlayerState.Local;
            string hex = ColorUtility.ToHtmlStringRGB(p.PlayerColor);
            string row = $"{gm.RankOf(p)}. <color=#{hex}>{p.DisplayName}</color>   K {p.Kills.Value}   D {p.Deaths.Value}";
            sb.AppendLine(me ? $"<b>{row}   (YOU)</b>" : row);
        }
        sb.AppendLine();
        sb.AppendLine("Host: press A + X to play again");
        return sb.ToString();
    }
}