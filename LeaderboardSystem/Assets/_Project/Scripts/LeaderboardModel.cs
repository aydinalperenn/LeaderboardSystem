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
