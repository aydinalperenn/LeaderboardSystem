using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardController : MonoBehaviour
{
    [Header("Config & Data")]
    [SerializeField] private LeaderboardConfig config;     // ScriptableObject

    [TextArea(5, 10)]
    [SerializeField] private string jsonText;              // Dev/test için
    [SerializeField] private TextAsset jsonFile;           // Dosyadan okumak için

    [Header("View (Canvas'sýz)")]
    [SerializeField] private Transform rowsContainer;
    [SerializeField] private RowItemView rowPrefab;

    private LeaderboardModel model = new LeaderboardModel();
    private ILeaderboardDataSource dataSource;

    private RowItemPool pool;
    private List<RowItemView> activeRows = new List<RowItemView>();
    private int topIndex = 0;
    private int visibleCount { get { return Mathf.Min(config.maxVisibleRows, model.Players.Count); } }
    private float rowHeight = 1.0f;


    void Awake()
    {
        // DataSource seçimi
        if (jsonFile != null) dataSource = new TextAssetDataSource(jsonFile);
        else if (!string.IsNullOrEmpty(jsonText)) dataSource = new JsonTextDataSource(jsonText);
        else if (config != null && config.defaultJson != null) dataSource = new TextAssetDataSource(config.defaultJson);
        else dataSource = new JsonTextDataSource("{}");

        // Model yükle
        var list = dataSource.Load();
        model.SetData(list);

        Debug.Log($"[Leaderboard] Loaded {model.Players.Count} players. Me={model.Me?.nickname} Rank={model.Me?.rank}");
        foreach (var p in model.Players)
            Debug.Log($"Player ID={p.id}, Nickname={p.nickname}, Score={p.score}, Rank={p.rank}");

        // View init
        if (rowPrefab == null && config != null && config.rowItemPrefab != null)
            rowPrefab = config.rowItemPrefab.GetComponent<RowItemView>();

        if (rowPrefab == null)
        {
            Debug.LogError("[Leaderboard] Row prefab atanmamýþ!");
            return;
        }

        rowHeight = rowPrefab.RowHeight;
        
        pool = new RowItemPool(rowPrefab, rowsContainer, visibleCount);
        SpawnInitialRows();

        topIndex = ComputeTopIndex(model.MeIndex, model.Players.Count, config.maxVisibleRows);
        BindWindow(false);
        CenterOnMe(false);

    }

    public void SimulateRandomUpdate()
    {

        System.Random rng = new System.Random();
        model.RandomBumpScores(rng, 5, 50, 0.4f);

        Debug.Log($"[Leaderboard] After update: Me new rank = {model.Me?.rank}");

        topIndex = ComputeTopIndex(model.MeIndex, model.Players.Count, config.maxVisibleRows);
        BindWindow(false);
        CenterOnMe(true);
    }


    // ---------- View yardýmcýlarý ----------

    private void SpawnInitialRows()
    {
        for (int i = 0; i < visibleCount; i++)
        {
            var item = pool.Get();
            activeRows.Add(item);

            float y = -(i * rowHeight);
            item.SnapTo(y);
        }
    }

    private void BindWindow(bool animated)
    {
        for (int i = 0; i < activeRows.Count; i++)
        {
            int dataIndex = topIndex + i;
            if (dataIndex < 0 || dataIndex >= model.Players.Count)
            {
                activeRows[i].gameObject.SetActive(false);
                continue;
            }

            var data = model.Players[dataIndex];
            activeRows[i].gameObject.SetActive(true);
            activeRows[i].Bind(data, data.id == "me");

            float targetY = -(i * rowHeight);
            if (animated)
                activeRows[i].SnapTo(targetY); // þimdilik anýnda hizala
            else
                activeRows[i].SnapTo(targetY);
        }
    }

    private void CenterOnMe(bool animated)
    {
        if (rowsContainer == null || model.MeIndex < 0 || config == null) return;

        float y = ComputeContainerY(topIndex, rowHeight, config.meCenterOffset);

        if (animated)
            rowsContainer.DOLocalMoveY(y, config.containerMoveDuration).SetEase(config.containerEase);
        else
        {
            var p = rowsContainer.localPosition;
            rowsContainer.localPosition = new Vector3(p.x, y, p.z);
        }
    }

    private int ComputeTopIndex(int meIndex, int total, int maxVisible)
    {
        float half = (maxVisible - 1) * 0.5f;
        int idealTop = Mathf.RoundToInt(meIndex - half);
        return Mathf.Clamp(idealTop, 0, Mathf.Max(0, total - maxVisible));
    }

    private float ComputeContainerY(int top, float rowH, float offset)
    {
        return -(top * rowH) + offset;
    }
}
