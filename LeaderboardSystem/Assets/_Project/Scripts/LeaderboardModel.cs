using System;
using System.Collections.Generic;

public class LeaderboardModel
{
    public List<PlayerData> Players { get; private set; } = new List<PlayerData>();
    public PlayerData Me { get; private set; }
    public int MeIndex { get; private set; } = -1;

    /// JSON�dan parse edilmi� liste ver (PlayerList) ya da ham json string ile overload yazabilirsin.
    public void SetData(PlayerList list)
    {
        Players = list?.players ?? new List<PlayerData>();
        RecalculateRanks();
    }

    public void RecalculateRanks()
    {
        // Score DESC, tie-breaker id ASC (stabilite i�in)
        Players.Sort((a, b) =>
        {
            int cmp = b.score.CompareTo(a.score);
            if (cmp != 0) return cmp;
            return string.Compare(a.id, b.id, StringComparison.Ordinal);
        });

        for (int i = 0; i < Players.Count; i++)
            Players[i].rank = i + 1;

        MeIndex = Players.FindIndex(p => p.id == "me");
        Me = MeIndex >= 0 ? Players[MeIndex] : null;
    }

    /// Skorlar� rasgele art�rmak i�in (update butonu sim�lasyonu � g�rsel k�s�m sonra)
    public void RandomBumpScores(System.Random rng, int minDelta = 5, int maxDelta = 50, float bumpChance = 0.4f)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].id == "me") continue; // �me��nin g�rseli animasyonla ta��nacak; say� sonra g�ncellenecek
            if (rng.NextDouble() <= bumpChance)
                Players[i].score += rng.Next(minDelta, maxDelta + 1);
        }
        // Me�nin ger�ek skoru da de�i�ebilir ama ekranda anl�k g�stermeyece�iz; 
        // istersen burada da de�eri g�ncelleyip sadece view�da ge� g�stermeyi tercih edebilirsin.
        RecalculateRanks();
    }
}
