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

                // Y�n� rastgele se� (+/-)
                if (rng.Next(0, 2) == 0) delta = -delta;

                int newScore = players[i].score + delta;
                if (newScore < 0) newScore = 0; // negatif olmas�n
                players[i].score = newScore;
            }
        }
        RecalculateRanks();
    }
    public void RandomBumpScoresWithJumps(System.Random rng)
    {
        for (int i = 0; i < players.Count; i++)
        {
            // De�i�ecek mi? (hemen herkes de�i�sin istiyorsan bu oran� 0.8�1.0 yap)
            if (rng.NextDouble() <= 0.85f)
            {
                bool bigJump = rng.NextDouble() < 0.30f; // %30 b�y�k s��rama
                int minDelta = bigJump ? 200 : 10;
                int maxDelta = bigJump ? 600 : 60;

                int delta = rng.Next(minDelta, maxDelta + 1);
                if (rng.Next(0, 2) == 0) delta = -delta; // y�n (�)

                int newScore = players[i].score + delta;
                if (newScore < 0) newScore = 0;
                players[i].score = newScore;
            }
        }
        RecalculateRanks();
    }

    // Alternatif �tamamen kar��t�r� modu � herkesin skorunu ba�tan da��t�r.
    // B�y�k s��rama garantili g�r�n�r.
    public void RandomizeAllScores(System.Random rng, int minScore = 500, int maxScore = 5000)
    {
        for (int i = 0; i < players.Count; i++)
            players[i].score = rng.Next(minScore, maxScore + 1);

        RecalculateRanks();
    }


    /// Skorlar� rasgele art�rmak i�in (update butonu sim�lasyonu � g�rsel k�s�m sonra)
    public void RandomBumpScores(System.Random rng, int minDelta = 5, int maxDelta = 50, float bumpChance = 0.4f)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].id == "me") continue; // �me��nin g�rseli animasyonla ta��nacak; say� sonra g�ncellenecek
            if (rng.NextDouble() <= bumpChance)
                players[i].score += rng.Next(minDelta, maxDelta + 1);
        }
        // Me�nin ger�ek skoru da de�i�ebilir ama ekranda anl�k g�stermeyece�iz; 
        // istersen burada da de�eri g�ncelleyip sadece view�da ge� g�stermeyi tercih edebilirsin.
        RecalculateRanks();
    }



    public List<PlayerData> Players {get { return players; }}

    public PlayerData Me{get { return me; } }

    public int MeIndex{get { return meIndex; }}
}
