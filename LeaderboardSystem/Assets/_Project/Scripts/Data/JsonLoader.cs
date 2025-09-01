using UnityEngine;

public class JsonLoader : MonoBehaviour
{
    [TextArea(5, 10)]
    [SerializeField] private string jsonText;                 // Test amaçlý inspector’dan
    [SerializeField] private TextAsset jsonFile;              // dosyadan

    public LeaderboardModel Model { get; private set; } = new LeaderboardModel();

    void Awake()
    {
        string source = jsonFile != null ? jsonFile.text : jsonText;
        var list = JsonUtility.FromJson<PlayerList>(source);
        Model.SetData(list);

        //Debug.Log($"Loaded {Model.Players.Count} players. MeIndex={Model.MeIndex}, MeRank={Model.Me?.rank}");
    }
}
