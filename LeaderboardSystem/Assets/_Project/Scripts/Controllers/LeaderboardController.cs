using DG.Tweening;
using UnityEngine;

public class LeaderboardController : MonoBehaviour
{
    [Header("Config & Data")]
    [SerializeField] private LeaderboardConfig config;

    [TextArea(5, 10)]
    [SerializeField] private string jsonText;
    [SerializeField] private TextAsset jsonFile;

    [Header("View (Canvas'sýz)")]
    [SerializeField] private Transform rowsContainer;
    [SerializeField] private RowItemView rowPrefab;

    private LeaderboardModel model = new LeaderboardModel();
    private ILeaderboardDataSource dataSource;

    private Sequence _animSeq;
    private const string REBIND_DELAY_ID = "LB_REBIND_DELAY";

    private RowItemPool pool;
    private int topIndex = 0;
    private int visibleCount { get { return Mathf.Min(config.maxVisibleRows, model.Players.Count); } }

    // Spacing'i de içeren satýr adýmý (rowHeight + rowSpacing)
    private float rowStep = 1.0f;

    // Me'yi Z ekseninde öne/arkaya itmek için
    private bool meZPushed = false;
    private float meBaseZ = 0f;
    private bool meBaseZSet = false;

    void Awake()
    {
        // Veri kaynaðýný sýrayla seç
        if (jsonFile != null) dataSource = new TextAssetDataSource(jsonFile);
        else if (!string.IsNullOrEmpty(jsonText)) dataSource = new JsonTextDataSource(jsonText);
        else if (config != null && config.defaultJson != null) dataSource = new TextAssetDataSource(config.defaultJson);
        else dataSource = new JsonTextDataSource("{}");

        // Modele aktar
        var list = dataSource.Load();
        model.SetData(list);

        // Prefab referansýný config'ten doldur
        if (rowPrefab == null && config != null && config.rowItemPrefab != null)
            rowPrefab = config.rowItemPrefab.GetComponent<RowItemView>();

        if (rowPrefab == null)
        {
            Debug.LogError("[Leaderboard] Row prefab atanmamýþ!");
            return;
        }

        // Satýr adýmý: rowHeight + rowSpacing
        rowStep = rowPrefab.Step;

        // Pool'u hazýrla ve görünür slotlarý oluþtur
        pool = new RowItemPool(rowPrefab, rowsContainer, prewarm: visibleCount);
        SpawnInitialRows();

        // Me ortalanacak þekilde pencere baþlangýcý
        topIndex = ComputeTopIndex(model.MeIndex, model.Players.Count, config.maxVisibleRows);
        BindWindow(false);
        CenterOnMe(false);
    }

    // Baþlangýçta görünür slot kadar child oluþtur ve konumlarý sabitle
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
            float y = -(i * rowStep);
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

    // Data penceresini mevcut topIndex ile görünür slotlara baðlar
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

            float targetY = -(i * rowStep);
            view.SnapTo(targetY);
        }
    }

    // Container'ý Me hedef noktasýna taþýr (Me görselde sabit/ortada kalýr)
    private void CenterOnMe(bool animated)
    {
        if (rowsContainer == null || model.MeIndex < 0 || config == null) return;

        float centerY = config.meCenterOffset;
        float y = centerY + (model.MeIndex - topIndex) * rowStep;

        if (animated)
            rowsContainer.DOLocalMoveY(y, config.containerMoveDuration).SetEase(config.containerEase);
        else
        {
            var p = rowsContainer.localPosition;
            rowsContainer.localPosition = new Vector3(p.x, y, p.z);
        }
    }

    // Me'yi ortalayacak üst indeks hesabý
    private int ComputeTopIndex(int meIndex, int total, int maxVisible)
    {
        int half = (maxVisible - 1) / 2;
        int idealTop = meIndex - half;
        return Mathf.Clamp(idealTop, 0, Mathf.Max(0, total - maxVisible));
    }

    // Görünür çocuklarda Me view'ýný bulur
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

    // Skor/rank rastgele güncelle, non-Me satýrlarý kaydýrarak Me'yi sabit tut
    public void SimulateRandomUpdateAnimated()
    {
        if (rowsContainer == null || config == null) return;

        if (_animSeq != null && _animSeq.IsActive()) _animSeq.Kill(false);
        KillAllRowTweens(false);

        int meOldScore = model.Me != null ? model.Me.score : 0;
        int meOldRank = model.Me != null ? model.Me.rank : 0;
        int oldMeIndex = model.MeIndex;

        System.Random rng = new System.Random();
        model.RandomBumpIncludingMe(rng, meMin: 5, meMax: 30, otherMin: 5, otherMax: 30, changeChance: 5f);

        int meNewScore = model.Me != null ? model.Me.score : meOldScore;
        int meNewRank = model.Me != null ? model.Me.rank : meOldRank;
        int newMeIndex = model.MeIndex;

        int newTop = ComputeTopIndex(newMeIndex, model.Players.Count, config.maxVisibleRows);
        int deltaRows = oldMeIndex - newMeIndex;

        // Yeni pencere için bind + baþlangýç konumlarý
        PrebindWindowForAnimation(newTop, deltaRows);

        // Me etiket tweenleri
        var meView = FindMeViewInChildren();
        if (meView != null)
        {
            meView.KillLabelTweens(false);
            float dur = Mathf.Max(0.01f, config.containerMoveDuration);
            var ease = config.containerEase;
            meView.AnimateRankInt(meOldRank, meNewRank, dur, ease);
            meView.AnimateScoreInt(meOldScore, meNewScore, dur, ease);
        }

        // Me'yi Z ekseninde hafif öne it
        const float meZOffset = 1.25f;
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

        // Me sýrasý deðiþmediyse sadece hafif efekt + rebind gecikmesi
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

                meView.transform.DOPunchScale(Vector3.one * 0.06f, 0.25f, 6, 0.5f);
            }

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

        // Non-Me satýrlarý yeni pozisyonlarýna kaydýr
        for (int j = 0; j < vis; j++)
        {
            var view = GetViewAt(j);
            if (view == null) continue;
            if (view.isMe) continue;

            float endY = -(j * rowStep);
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

    // Tüm görünür satýrlarýn tweenlerini sonlandýr
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

    // Animasyon öncesi yeni pencereye göre bind et ve baþlangýç Y konumlarýný ayarla
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
                ? -(j * rowStep)
                : -((j - deltaRows) * rowStep);

            view.SnapTo(startY);

            // Üst üste binecek satýr için kýsa bir fade-in (non-Me)
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
