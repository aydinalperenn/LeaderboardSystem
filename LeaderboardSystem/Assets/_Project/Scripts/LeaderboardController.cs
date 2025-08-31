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

        // SADECE POOL KULLAN
        pool = new RowItemPool(rowPrefab, rowsContainer, prewarm: visibleCount);
        SpawnInitialRows(); // pool.Get ile çocuklarý oluþtur + hizala

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

    // ---------- View yardýmcýlarý (activeRows YOK) ----------

    private void SpawnInitialRows()
    {
        // Gerekli sayýda child yoksa pool’dan al ve rowsContainer altýna koy
        int need = visibleCount - rowsContainer.childCount;
        for (int i = 0; i < need; i++)
        {
            var item = pool.Get();
            if (item.transform.parent != rowsContainer)
                item.transform.SetParent(rowsContainer, worldPositionStays: false);
            item.gameObject.SetActive(true);
        }

        // Slotlara diz
        int childCount = VisibleNow();
        for (int i = 0; i < childCount; i++)
        {
            var view = GetViewAt(i);
            float y = -(i * rowHeight);
            view.SnapTo(y);
        }
    }

    private int VisibleNow()
    {
        return Mathf.Min(visibleCount, rowsContainer.childCount);
    }

    private RowItemView GetViewAt(int i)
    {
        return rowsContainer.GetChild(i).GetComponent<RowItemView>();
    }

    private void BindWindow(bool animated, bool skipMeDuringAnimation = false)
    {
        int vis = VisibleNow();
        for (int i = 0; i < vis; i++)
        {
            int dataIndex = topIndex + i;
            var view = GetViewAt(i);

            if (dataIndex < 0 || dataIndex >= model.Players.Count)
            {
                view.gameObject.SetActive(false);
                continue;
            }

            var data = model.Players[dataIndex];
            bool isMe = data.id == "me";
            if (skipMeDuringAnimation && isMe) continue;

            view.gameObject.SetActive(true);
            view.Bind(data, isMe);

            float targetY = -(i * rowHeight);
            view.SnapTo(targetY); // þimdilik anýnda hizalama
        }
    }

    private void CenterOnMe(bool animated)
    {
        if (rowsContainer == null || model.MeIndex < 0 || config == null) return;

        float centerY = config.meCenterOffset; // ekran merkezi
        float y = centerY + (model.MeIndex - topIndex) * rowHeight;

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
        int half = (maxVisible - 1) / 2;
        int idealTop = meIndex - half;
        return Mathf.Clamp(idealTop, 0, Mathf.Max(0, total - maxVisible));
    }

    // ---------- Animasyon (activeRows yok; pool + children) ----------
    public void SimulateRandomUpdateAnimated()
    {
        int vis = VisibleNow();
        if (model.MeIndex < 0 || vis == 0) return;

        // Eski görünürlerin sözlüðü: PlayerID -> RowItemView
        var idToView = new Dictionary<string, RowItemView>();
        int oldTop = topIndex;
        for (int i = 0; i < vis; i++)
        {
            int dataIndex = oldTop + i;
            if (dataIndex < 0 || dataIndex >= model.Players.Count) continue;
            string id = model.Players[dataIndex].id;
            idToView[id] = GetViewAt(i);
        }

        // Modeli güncelle (örnek: büyük sýçramalar)
        System.Random rng = new System.Random();
        model.RandomBumpScoresWithJumps(rng);

        Debug.Log($"[Leaderboard] After update: Me new rank = {model.Me?.rank}");

        // Yeni pencere baþlangýcý
        topIndex = ComputeTopIndex(model.MeIndex, model.Players.Count, config.maxVisibleRows);

        int newVisible = Mathf.Min(config.maxVisibleRows, model.Players.Count);
        float duration = config.rowMoveDuration;

        CenterOnMe(true);

        var seq = DOTween.Sequence();

        // Eski görünür id varsa, mevcut view’ý hedef slot Y’sine tween’le
        for (int i = 0; i < newVisible && i < vis; i++)
        {
            int newDataIndex = topIndex + i;
            if (newDataIndex < 0 || newDataIndex >= model.Players.Count) continue;

            var pdata = model.Players[newDataIndex];
            string id = pdata.id;
            float targetY = -(i * rowHeight);

            if (idToView.TryGetValue(id, out RowItemView view))
            {
                seq.Join(view.transform.DOLocalMoveY(targetY, duration).SetEase(config.rowEase));
            }
        }

        // Bittiðinde kesin bind + hizalama
        seq.OnComplete(() =>
        {
            BindWindow(false);
            CenterOnMe(true);
        });
    }
}
