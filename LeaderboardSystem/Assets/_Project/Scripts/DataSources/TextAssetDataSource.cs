using UnityEngine;

public class TextAssetDataSource : ILeaderboardDataSource
{
    private readonly TextAsset asset;
    public TextAssetDataSource(TextAsset asset) { this.asset = asset; }

    public PlayerList Load()
    {
        if (asset == null || string.IsNullOrEmpty(asset.text))
            return new PlayerList { players = new System.Collections.Generic.List<PlayerData>() };

        return JsonUtility.FromJson<PlayerList>(asset.text);
    }
}
