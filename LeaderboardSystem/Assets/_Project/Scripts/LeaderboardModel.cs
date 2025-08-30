using System;
using System.Collections.Generic;

public class LeaderboardModel
{
    public List<PlayerData> Players { get; private set; } = new List<PlayerData>();
    public PlayerData Me { get; private set; }
    public int MeIndex { get; private set; } = -1;

    /// JSON’dan parse edilmiþ liste ver (PlayerList) ya da ham json string ile overload yazabilirsin.
    public void SetData(PlayerList list)
    {
        Players = list?.players ?? new List<PlayerData>();
        RecalculateRanks();
    }

    public void RecalculateRanks()
    {
        // Score DESC, tie-breaker id ASC (stabilite için)
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

    /// Skorlarý rasgele artýrmak için (update butonu simülasyonu – görsel kýsým sonra)
    public void RandomBumpScores(System.Random rng, int minDelta = 5, int maxDelta = 50, float bumpChance = 0.4f)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].id == "me") continue; // “me”’nin görseli animasyonla taþýnacak; sayý sonra güncellenecek
            if (rng.NextDouble() <= bumpChance)
                Players[i].score += rng.Next(minDelta, maxDelta + 1);
        }
        // Me’nin gerçek skoru da deðiþebilir ama ekranda anlýk göstermeyeceðiz; 
        // istersen burada da deðeri güncelleyip sadece view’da geç göstermeyi tercih edebilirsin.
        RecalculateRanks();
    }
}
