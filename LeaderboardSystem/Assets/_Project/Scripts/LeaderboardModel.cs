using System;
using System.Collections.Generic;

public class LeaderboardModel
{
    private List<PlayerData> players = new List<PlayerData>();
    private PlayerData me;
    private int meIndex = -1;


    public void SetData(PlayerList list)
    {
        if (list != null && list.players != null)
        {
            players = list.players;
        }
        else
        {
            players = new List<PlayerData>();
        }

        RecalculateRanks();
    }

    public void RecalculateRanks()
    {
        // Score DESC, eger skor esitse id'ye gore siraliyor
        players.Sort((a, b) =>
        {
            int cmp = b.score.CompareTo(a.score);
            if (cmp != 0) return cmp;
            return string.Compare(a.id, b.id, StringComparison.Ordinal);
        });

        for (int i = 0; i < players.Count; i++)
        {
            players[i].rank = i + 1;
        }

        meIndex = players.FindIndex(p => p.id == "me");

        if (meIndex >= 0)
        {
            me = players[meIndex];
        }
        else
        {
            me = null;
        }

    }

    public void RandomBumpScoresSymmetric(System.Random rng, int minDelta = 5, int maxDelta = 50, float changeChance = 0.4f)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (rng.NextDouble() <= changeChance)
            {
                int delta = rng.Next(minDelta, maxDelta + 1);

                // Yönü rastgele seç (+/-)
                if (rng.Next(0, 2) == 0) delta = -delta;

                int newScore = players[i].score + delta;
                if (newScore < 0) newScore = 0; // negatif olmasýn
                players[i].score = newScore;
            }
        }
        RecalculateRanks();
    }
    public void RandomBumpScoresWithJumps(System.Random rng)
    {
        for (int i = 0; i < players.Count; i++)
        {
            // Deðiþecek mi? (hemen herkes deðiþsin istiyorsan bu oraný 0.8–1.0 yap)
            if (rng.NextDouble() <= 0.85f)
            {
                bool bigJump = rng.NextDouble() < 0.30f; // %30 büyük sýçrama
                int minDelta = bigJump ? 200 : 10;
                int maxDelta = bigJump ? 600 : 60;

                int delta = rng.Next(minDelta, maxDelta + 1);
                if (rng.Next(0, 2) == 0) delta = -delta; // yön (±)

                int newScore = players[i].score + delta;
                if (newScore < 0) newScore = 0;
                players[i].score = newScore;
            }
        }
        RecalculateRanks();
    }

    // Alternatif “tamamen karýþtýr” modu — herkesin skorunu baþtan daðýtýr.
    // Büyük sýçrama garantili görünür.
    public void RandomizeAllScores(System.Random rng, int minScore = 500, int maxScore = 5000)
    {
        for (int i = 0; i < players.Count; i++)
            players[i].score = rng.Next(minScore, maxScore + 1);

        RecalculateRanks();
    }


    /// Skorlarý rasgele artýrmak için (update butonu simülasyonu – görsel kýsým sonra)
    public void RandomBumpScores(System.Random rng, int minDelta = 5, int maxDelta = 50, float bumpChance = 0.4f)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].id == "me") continue; // “me”’nin görseli animasyonla taþýnacak; sayý sonra güncellenecek
            if (rng.NextDouble() <= bumpChance)
                players[i].score += rng.Next(minDelta, maxDelta + 1);
        }
        // Me’nin gerçek skoru da deðiþebilir ama ekranda anlýk göstermeyeceðiz; 
        // istersen burada da deðeri güncelleyip sadece view’da geç göstermeyi tercih edebilirsin.
        RecalculateRanks();
    }



    public List<PlayerData> Players {get { return players; }}

    public PlayerData Me{get { return me; } }

    public int MeIndex{get { return meIndex; }}
}
