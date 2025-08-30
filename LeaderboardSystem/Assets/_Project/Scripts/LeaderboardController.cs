using UnityEngine;

public class LeaderboardController : MonoBehaviour
{
    [Header("Data Source")]
    [TextArea(5, 10)]
    [SerializeField] private string jsonText;            // Hýzlý test için Inspector’dan
    [SerializeField] private TextAsset jsonFile;         // Dosyadan

    private LeaderboardModel model = new LeaderboardModel();

    void Awake()
    {
        string source;
        if (jsonFile != null)
        {
            source = jsonFile.text;
        }
        else
        {
            source = jsonText;
        }

        var list = JsonUtility.FromJson<PlayerList>(source);   
        model.SetData(list);



        Debug.Log($"[Leaderboard] Loaded {model.Players.Count} players. Me={model.Me?.nickname} Rank={model.Me?.rank}");

        foreach (var player in model.Players)
        {
            Debug.Log($"Player ID={player.id}, Nickname={player.nickname}, Score={player.score}, Rank={player.rank}");
        }

    }

    public void SimulateRandomUpdate()
    {
        System.Random rng = new System.Random();
        model.RandomBumpScores(rng, minDelta: 5, maxDelta: 50, bumpChance: 0.4f);

        // Görsel katman eklendiðinde burada view’ý yenileyeceðiz/animasyon baþlatacaðýz
        Debug.Log($"[Leaderboard] After update: Me new rank = {model.Me?.rank}");
    }
}
