using DG.Tweening;
using UnityEngine;

public class LeaderboardController : MonoBehaviour
{
    [Header("Config & Data")]
    [SerializeField] private LeaderboardConfig config;

    [TextArea(5, 10)]
    [SerializeField] private string jsonText;
    [SerializeField] private TextAsset jsonFile;

    [Header("View (Canvas's�z)")]
    [SerializeField] private Transform rowsContainer;
    [SerializeField] private RowItemView rowPrefab;

    private LeaderboardModel model = new LeaderboardModel();
    private ILeaderboardDataSource dataSource;

    private Sequence _animSeq;
    private const string REBIND_DELAY_ID = "LB_REBIND_DELAY";

    private RowItemPool pool;
    private int topIndex = 0;
    private int visibleCount { get { return Mathf.Min(config.maxVisibleRows, model.Players.Count); } }
    private float rowHeight = 1.0f;

    private bool meZPushed = false;
    private float meBaseZ = 0f;
    private bool meBaseZSet = false;

    void Awake()
    {
        // - veri kayna��n� s�rayla se� ve modeli doldur
        if (jsonFile != null) dataSource = new TextAssetDataSource(jsonFile);
        else if (!string.IsNullOrEmpty(jsonText)) dataSource = new JsonTextDataSource(jsonText);
        else if (config != null && config.defaultJson != null) dataSource = new TextAssetDataSource(config.defaultJson);
        else dataSource = new JsonTextDataSource("{}");

        var list = dataSource.Load();
        model.SetData(list);

        Debug.Log($"[Leaderboard] Loaded {model.Players.Count} players. Me={model.Me?.nickname} Rank={model.Me?.rank}");
        foreach (var p in model.Players)
            Debug.Log($"Player ID={p.id}, Nickname={p.nickname}, Score={p.score}, Rank={p.rank}");

        // - prefab ve �l��leri haz�rla
        if (rowPrefab == null && config != null && config.rowItemPrefab != null)
            rowPrefab = config.rowItemPrefab.GetComponent<RowItemView>();

        if (rowPrefab == null)
        {
            Debug.LogError("[Leaderboard] Row prefab atanmam��!");
            return;
        }

        rowHeight = rowPrefab.RowHeight;

        // - sadece pool kullanarak g�r�n�r sat�rlar� �ret ve hizala
        pool = new RowItemPool(rowPrefab, rowsContainer, prewarm: visibleCount);
        SpawnInitialRows();

        topIndex = ComputeTopIndex(model.MeIndex, model.Players.Count, config.maxVisibleRows);
        BindWindow(false);
        CenterOnMe(false);
    }

    // - g�r�n�r slot kadar child olu�tur ve konumlar�n� ba�lang��ta sabitle
    private void SpawnInitialRows()
    {
        int need = visibleCount - rowsContainer.childCount;
        for (int i = 0; i < need; i++)
        {
            var item = pool.Get();
            if (item.transform.parent != rowsContainer)
                item.transform.SetParent(rowsContainer, worldPositionStays: false);
            item.gameObject.SetActive(true);
        }

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

    // - data penceresini mevcut topIndex'le g�r�n�r slotlara ba�lar
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
            view.SnapTo(targetY);
        }
    }

    // - container'� Me hedef noktas�na ta��r (Me g�rselde ortada kal�r)
    private void CenterOnMe(bool animated)
    {
        if (rowsContainer == null || model.MeIndex < 0 || config == null) return;

        float centerY = config.meCenterOffset;
        float y = centerY + (model.MeIndex - topIndex) * rowHeight;

        if (animated)
            rowsContainer.DOLocalMoveY(y, config.containerMoveDuration).SetEase(config.containerEase);
        else
        {
            var p = rowsContainer.localPosition;
            rowsContainer.localPosition = new Vector3(p.x, y, p.z);
        }
    }

    // - Me'yi ortalayacak �st indeks hesab�
    private int ComputeTopIndex(int meIndex, int total, int maxVisible)
    {
        int half = (maxVisible - 1) / 2;
        int idealTop = meIndex - half;
        return Mathf.Clamp(idealTop, 0, Mathf.Max(0, total - maxVisible));
    }

    // - g�r�n�r �ocuklarda Me view'�n� bulur
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

    // - skor/rank rastgele g�ncelle, non-Me sat�rlar� kayd�rarak Me'yi sabit tut
    public void SimulateRandomUpdateAnimated()
    {
        if (rowsContainer == null || config == null) return;

        if (_animSeq != null && _animSeq.IsActive()) _animSeq.Kill(false);
        KillAllRowTweens(false);

        int meOldScore = model.Me != null ? model.Me.score : 0;
        int meOldRank = model.Me != null ? model.Me.rank : 0;
        int oldMeIndex = model.MeIndex;

        System.Random rng = new System.Random();
        model.RandomBumpIncludingMe(rng, meMin: 5, meMax: 50, otherMin: 5, otherMax: 50, changeChance: 1f);

        int meNewScore = model.Me != null ? model.Me.score : meOldScore;
        int meNewRank = model.Me != null ? model.Me.rank : meOldRank;
        int newMeIndex = model.MeIndex;

        int newTop = ComputeTopIndex(newMeIndex, model.Players.Count, config.maxVisibleRows);
        int deltaRows = oldMeIndex - newMeIndex;
        float shift = -deltaRows * rowHeight; // - bilerek tutuyorum (davran�� de�i�mesin)

        PrebindWindowForAnimation(newTop, deltaRows);

        var meView = FindMeViewInChildren();
        if (meView != null)
        {
            meView.KillLabelTweens(false);
            float dur = Mathf.Max(0.01f, config.containerMoveDuration);
            var ease = config.containerEase;
            meView.AnimateRankInt(meOldRank, meNewRank, dur, ease);
            meView.AnimateScoreInt(meOldScore, meNewScore, dur, ease);
        }

        const float meZOffset = 1.5f;
        const float meZDuration = 0.25f;

        if (meView != null)
        {
            if (!meBaseZSet)
            {
                meBaseZ = meView.transform.localPosition.z;
                meBaseZSet = true;
            }

            if (!meZPushed)
            {
                meZPushed = true;
                meView.transform
                      .DOLocalMoveZ(meBaseZ - meZOffset, meZDuration)
                      .SetEase(Ease.OutQuad)
                      .SetTarget(meView);
            }
        }

        int vis = VisibleNow();
        bool anyTween = false;
        _animSeq = DOTween.Sequence();

        if (deltaRows == 0)
        {
            if (meView != null)
            {
                float wait = Mathf.Max(0.01f, config.containerMoveDuration);
                DOVirtual.DelayedCall(wait, () =>
                {
                    meView.transform.DOLocalMoveZ(meBaseZ, meZDuration)
                                    .SetEase(Ease.InQuad)
                                    .SetTarget(meView);
                    meZPushed = false;
                }).SetTarget(meView);
            }

            if (meView != null)
                meView.transform.DOPunchScale(Vector3.one * 0.06f, 0.25f, 6, 0.5f);

            DOTween.Kill(REBIND_DELAY_ID, false);
            DOTween.Sequence()
                   .SetId(REBIND_DELAY_ID)
                   .AppendInterval(Mathf.Max(0.18f, config.containerMoveDuration * 0.5f))
                   .OnComplete(() =>
                   {
                       topIndex = newTop;
                       BindWindow(false);
                   });
            return;
        }

        for (int j = 0; j < vis; j++)
        {
            var view = GetViewAt(j);
            if (view == null) continue;
            if (view.isMe) continue;

            float endY = -(j * rowHeight);
            _animSeq.Join(
                view.transform
                    .DOLocalMoveY(endY, config.containerMoveDuration)
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

                if (meView != null)
                {
                    meView.transform.DOLocalMoveZ(meBaseZ, meZDuration)
                                    .SetEase(Ease.InQuad)
                                    .SetTarget(meView);
                }

                meZPushed = false;
                _animSeq = null;
            });
        }
        else
        {
            topIndex = newTop;
            BindWindow(false);

            if (meView != null)
            {
                meView.transform.DOLocalMoveZ(meBaseZ, meZDuration)
                                .SetEase(Ease.InQuad)
                                .SetTarget(meView);
            }
            meZPushed = false;
            _animSeq = null;
        }
    }

    // - t�m g�r�n�r sat�rlar�n tweenlerini sonland�r
    private void KillAllRowTweens(bool complete = false)
    {
        int vis = VisibleNow();
        for (int i = 0; i < vis; i++)
        {
            var v = GetViewAt(i);
            if (v == null) continue;

            v.transform.DOKill(complete);
            if (v.isMe)
                v.KillLabelTweens(complete);

            DOTween.Kill(v, complete);
        }

        DOTween.Kill(REBIND_DELAY_ID, complete);
    }

    // - animasyon �ncesi yeni pencereye g�re bind et ve ba�lang�� Y konumlar�n� ayarla
    private void PrebindWindowForAnimation(int newTop, int deltaRows)
    {
        int vis = VisibleNow();

        int jMe = model.MeIndex - newTop;
        int jOverlap = jMe + deltaRows;

        for (int j = 0; j < vis; j++)
        {
            int dataIndex = newTop + j;
            var view = GetViewAt(j);

            if (dataIndex < 0 || dataIndex >= model.Players.Count)
            {
                view.gameObject.SetActive(false);
                continue;
            }

            var data = model.Players[dataIndex];
            bool isMe = (data.id == "me");

            view.gameObject.SetActive(true);
            view.Bind(data, isMe);

            float startY = isMe
                ? -(j * rowHeight)
                : -((j - deltaRows) * rowHeight);

            view.SnapTo(startY);

            if (!isMe && j == jOverlap && jOverlap >= 0 && jOverlap < vis)
            {
                view.SetAlpha(0f);
                DOVirtual.DelayedCall(0.50f, () =>
                {
                    if (view != null) view.SetAlpha(1f);
                }).SetTarget(view);
            }
            else
            {
                view.SetAlpha(1f);
            }
        }
    }
}
