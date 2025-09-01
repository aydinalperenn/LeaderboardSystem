using System;
using System.Collections.Generic;

public class LeaderboardModel
{
    private List<PlayerData> players = new List<PlayerData>();
    private PlayerData me;
    private int meIndex = -1;

    public List<PlayerData> Players => players;
    public PlayerData Me => me;
    public int MeIndex => meIndex;


    public void SetData(PlayerList list)
    {
        players = (list != null && list.players != null) ? list.players : new List<PlayerData>();
        ResortAndRerank();
    }

    // - skora göre (desc), eþitlikte id (asc) sýralama
    public void ResortAndRerank()
    {
        players.Sort((a, b) =>
        {
            int cmp = b.score.CompareTo(a.score);
            if (cmp != 0) return cmp;
            return string.Compare(a.id, b.id, StringComparison.Ordinal);
        });

        for (int i = 0; i < players.Count; i++)
            players[i].rank = i + 1;

        meIndex = players.FindIndex(p => p.id == "me");
        me = (meIndex >= 0) ? players[meIndex] : null;
    }

    // - Me dahil skorlarý rastgele deðiþtir ve yeniden sýrala
    public void RandomBumpIncludingMe(
        Random rng,
        int meMin = 5, int meMax = 50,
        int otherMin = 5, int otherMax = 50,
        float changeChance = 1f)
    {
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p.id == "me") continue;

            if (rng.NextDouble() <= changeChance)
            {
                int d = rng.Next(otherMin, otherMax + 1);
                if (rng.Next(0, 2) == 0) d = -d;
                p.score = Math.Max(0, p.score + d);
            }
        }

        if (me != null)
        {
            int d = rng.Next(meMin, meMax + 1);
            if (rng.Next(0, 2) == 0) d = -d;
            me.score = Math.Max(0, me.score + d);
        }

        ResortAndRerank();
    }
}
