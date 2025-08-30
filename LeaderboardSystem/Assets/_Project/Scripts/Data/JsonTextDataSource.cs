using UnityEngine;

public class JsonTextDataSource : ILeaderboardDataSource
{
    private readonly string json;
    public JsonTextDataSource(string json) { this.json = json; }

    public PlayerList Load()
    {
        if (string.IsNullOrEmpty(json))
            return new PlayerList { players = new System.Collections.Generic.List<PlayerData>() };

        return JsonUtility.FromJson<PlayerList>(json);
    }
}
