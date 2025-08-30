using UnityEngine;
using DG.Tweening;

public class LeaderboardController : MonoBehaviour
{
    [Header("Config & Data")]
    [SerializeField] private LeaderboardConfig config;     // ScriptableObject
    [TextArea(5, 10)]
    [SerializeField] private string jsonText;              // Dev/test için
    [SerializeField] private TextAsset jsonFile;           // Dosyadan okumak için

    [Header("View (hazýrlýk)")]
    [SerializeField] private Transform rowsContainer;      // Satýrlarýn parent’ý
    [SerializeField] private float rowHeight = 1.0f;       // Satýr yüksekliði

    private LeaderboardModel model = new LeaderboardModel();
    private ILeaderboardDataSource dataSource;

    void Awake()
    {
        if (jsonFile != null)
        {
            dataSource = new TextAssetDataSource(jsonFile);
        }
        else if (!string.IsNullOrEmpty(jsonText))
        {
            dataSource = new JsonTextDataSource(jsonText);
        }
        else if (config != null && config.defaultJson != null)
        {
            dataSource = new TextAssetDataSource(config.defaultJson);
        }
        else
        {
            dataSource = new JsonTextDataSource("{}");
        }


        var list = dataSource.Load();
        model.SetData(list);

        // 3) Debug
        Debug.Log($"[Leaderboard] Loaded {model.Players.Count} players. Me={model.Me?.nickname} Rank={model.Me?.rank}");
        foreach (var player in model.Players)
        {
            Debug.Log($"Player ID={player.id}, Nickname={player.nickname}, Score={player.score}, Rank={player.rank}");
        }

        // 4) (Hazýrlýk) Ýlk açýlýþta container’ý “me” ortasýna taþýmak istersen:
        CenterOnMe(); // istersen þimdilik yorumlayabilirsin

    }

    public void SimulateRandomUpdate()
    {

        System.Random rng = new System.Random();
        model.RandomBumpScores(rng, minDelta: 5, maxDelta: 50, bumpChance: 0.4f);

        Debug.Log($"[Leaderboard] After update: Me new rank = {model.Me?.rank}");

        // Görsel katman geldiðinde:
        // - “me” item’ýný eski konumdan yeni konuma DOLocalMoveY ile taþýyacaðýz
        // - diðerlerini yeni sýraya konumlayacaðýz (genelde anýnda)
        // - container’ý tekrar me’ye göre merkezleyeceðiz
        CenterOnMe(true);
    }


    private void CenterOnMe(bool animated = false)
    {
        if (rowsContainer == null || model.MeIndex < 0 || config == null) return;

        float y = ComputeContainerY(
            model.MeIndex,
            model.Players.Count,
            rowHeight,
            config.maxVisibleRows,
            config.meCenterOffset
        );

        if (animated)
        {
            rowsContainer.DOLocalMoveY(y, config.containerMoveDuration)
                         .SetEase(config.containerEase);
        }
        else
        {
            var p = rowsContainer.localPosition;
            rowsContainer.localPosition = new Vector3(p.x, y, p.z);
        }
    }



    // “me”’yi görünür alanda ortalamak için hedef Y’yi hesapla
    private float ComputeContainerY(int meIndex, int total, float rowH, int maxVisible, float offset)
    {
        // Basit clamp’li merkezleme: meIndex’i ortalamaya çalýþ, listenin sýnýrlarýný aþma
        float half = (maxVisible - 1) * 0.5f;
        float idealTopIndex = Mathf.Clamp(meIndex - half, 0, Mathf.Max(0, total - maxVisible));
        return -(idealTopIndex * rowH) + offset;
    }
}
