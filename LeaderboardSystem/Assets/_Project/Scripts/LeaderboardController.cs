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

    private Sequence _animSeq;             // aktif sequence referansý
    private const string REBIND_DELAY_ID = "LB_REBIND_DELAY";

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


    private RowItemView FindMeViewInChildren()
    {
        int vis = VisibleNow();
        for (int i = 0; i < vis; i++)
        {
            var view = GetViewAt(i);
            if (view != null && view.isMe)
                return view;
        }
        return null;
    }

    private float ComputeContainerYFor(int topIdx)
    {
        // Me her zaman merkezde dursun istiyoruz: container Y’sini
        // "meCenterOffset + (MeIndex - topIdx) * rowHeight" formülüyle hesaplýyoruz.
        float centerY = config.meCenterOffset;
        return centerY + (model.MeIndex - topIdx) * rowHeight;
    }

    public void SimulateRandomUpdateAnimated()
    {
        if (rowsContainer == null || config == null) return;

        // aktif animasyonlarý/iplikleri durdur
        if (_animSeq != null && _animSeq.IsActive()) _animSeq.Kill(false);
        KillAllRowTweens(false);

        // Eski durum
        int oldMeIndex = model.MeIndex;
        int oldTop = topIndex;

        // --- FÝX: RNG oluþtur ve modele ver ---
        System.Random rng = new System.Random();
        model.RandomBumpIncludingMe(rng, meMin: 10, meMax: 40, otherMin: 5, otherMax: 50, changeChance: 0.4f);

        // Yeni durum
        int newMeIndex = model.MeIndex;
        int newTop = ComputeTopIndex(newMeIndex, model.Players.Count, config.maxVisibleRows);

        int deltaRows = oldMeIndex - newMeIndex;
        float shift = -deltaRows * rowHeight;

        int vis = VisibleNow();
        bool anyTween = false;
        _animSeq = DOTween.Sequence();

        if (deltaRows == 0)
        {
            var meView = FindMeViewInChildren();
            if (meView != null)
                meView.transform.DOPunchScale(Vector3.one * 0.06f, 0.25f, 6, 0.5f);

            DOTween.Kill(REBIND_DELAY_ID, false);
            DOTween.Sequence()
                   .SetId(REBIND_DELAY_ID)
                   .AppendInterval(0.20f)
                   .OnComplete(() =>
                   {
                       topIndex = newTop;
                       BindWindow(false);
                   });
            return;
        }

        for (int i = 0; i < vis; i++)
        {
            var view = GetViewAt(i);
            if (view == null) continue;
            // --- FÝX: IsMe kullan ---
            if (view.isMe) continue;

            float y0 = view.transform.localPosition.y;
            _animSeq.Join(
                view.transform
                    .DOLocalMoveY(y0 + shift, config.containerMoveDuration)
                    .SetEase(config.containerEase)
            );
            anyTween = true;
        }

        if (anyTween)
        {
            _animSeq.OnComplete(() =>
            {
                topIndex = newTop;
                BindWindow(false);
                _animSeq = null;
            });
        }
        else
        {
            topIndex = newTop;
            BindWindow(false);
            _animSeq = null;
        }
    }



    private void KillAllRowTweens(bool complete = false)
    {
        int vis = VisibleNow();
        for (int i = 0; i < vis; i++)
        {
            var v = GetViewAt(i);
            if (v == null) continue;
            v.transform.DOKill(complete);  // complete=false: kaldýðý yerde dursun
        }

        // Eski rebind gecikmelerini de iptal et
        DOTween.Kill(REBIND_DELAY_ID, complete);
    }


}
