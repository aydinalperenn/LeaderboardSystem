using System;
using System.Collections.Generic;

public class LeaderboardModel
{
    private List<PlayerData> players = new List<PlayerData>();
    private PlayerData me;
    private int meIndex = -1;

    // Dýþ eriþimler
    public List<PlayerData> Players => players;
    public PlayerData Me => me;
    public int MeIndex => meIndex;

    // ----------------- Kurulum -----------------
    public void SetData(PlayerList list)
    {
        players = (list != null && list.players != null) ? list.players : new List<PlayerData>();
        ResortAndRerank();
    }

    // ----------------- Sýralama / Rank -----------------
    /// Skora göre (DESC) sýralar; eþitlikte id (ASC). Rank atar, Me/MeIndex günceller.
    public void ResortAndRerank()
    {
        players.Sort((a, b) =>
        {
            int cmp = b.score.CompareTo(a.score);  // DESC
            if (cmp != 0) return cmp;
            return string.Compare(a.id, b.id, StringComparison.Ordinal); // eþitlik kýrýcý
        });

        for (int i = 0; i < players.Count; i++)
            players[i].rank = i + 1;

        meIndex = players.FindIndex(p => p.id == "me");
        me = (meIndex >= 0) ? players[meIndex] : null;
    }

    // ----------------- Skor Güncelleme -----------------
    /// Me’yi kesin deðiþtir + diðerlerini olasýlýkla ± deðiþtir; sonra yeniden sýrala.
    public void RandomBumpIncludingMe(
        Random rng,
        int meMin = 10, int meMax = 40,
        int otherMin = 5, int otherMax = 50,
        float changeChance = 0.4f)
    {
        // Diðerleri
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p.id == "me") continue;

            if (rng.NextDouble() <= changeChance)
            {
                int d = rng.Next(otherMin, otherMax + 1);
                if (rng.Next(0, 2) == 0) d = -d; // ± yön
                p.score = Math.Max(0, p.score + d);
            }
        }

        // Me – kesin deðiþsin
        if (me != null)
        {
            int d = rng.Next(meMin, meMax + 1);
            if (rng.Next(0, 2) == 0) d = -d;
            me.score = Math.Max(0, me.score + d);
        }

        ResortAndRerank();
    }
}
