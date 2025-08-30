using UnityEngine;
using DG.Tweening;

public class LeaderboardController : MonoBehaviour
{
    [Header("Config & Data")]
    [SerializeField] private LeaderboardConfig config;     // ScriptableObject
    [TextArea(5, 10)]
    [SerializeField] private string jsonText;              // Dev/test i�in
    [SerializeField] private TextAsset jsonFile;           // Dosyadan okumak i�in

    [Header("View (haz�rl�k)")]
    [SerializeField] private Transform rowsContainer;      // Sat�rlar�n parent��
    [SerializeField] private float rowHeight = 1.0f;       // Sat�r y�ksekli�i

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

        // 4) (Haz�rl�k) �lk a��l��ta container�� �me� ortas�na ta��mak istersen:
        CenterOnMe(); // istersen �imdilik yorumlayabilirsin

    }

    public void SimulateRandomUpdate()
    {

        System.Random rng = new System.Random();
        model.RandomBumpScores(rng, minDelta: 5, maxDelta: 50, bumpChance: 0.4f);

        Debug.Log($"[Leaderboard] After update: Me new rank = {model.Me?.rank}");

        // G�rsel katman geldi�inde:
        // - �me� item��n� eski konumdan yeni konuma DOLocalMoveY ile ta��yaca��z
        // - di�erlerini yeni s�raya konumlayaca��z (genelde an�nda)
        // - container�� tekrar me�ye g�re merkezleyece�iz
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



    // �me��yi g�r�n�r alanda ortalamak i�in hedef Y�yi hesapla
    private float ComputeContainerY(int meIndex, int total, float rowH, int maxVisible, float offset)
    {
        // Basit clamp�li merkezleme: meIndex�i ortalamaya �al��, listenin s�n�rlar�n� a�ma
        float half = (maxVisible - 1) * 0.5f;
        float idealTopIndex = Mathf.Clamp(meIndex - half, 0, Mathf.Max(0, total - maxVisible));
        return -(idealTopIndex * rowH) + offset;
    }
}
