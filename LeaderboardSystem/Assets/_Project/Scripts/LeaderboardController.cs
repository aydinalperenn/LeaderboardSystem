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

    private bool meZPushed = false;  // Me þu an öne itilmiþ mi?
    private float meBaseZ = 0f;      // Me'nin orijinal Z'si (ilk kez push'ta set edilir)
    private bool meBaseZSet = false; // _meBaseZ set edildi mi?



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

    public void SimulateRandomUpdateAnimated()
    {
        if (rowsContainer == null || config == null) return;

        // aktif sequence ve satýr tweenlerini kes
        if (_animSeq != null && _animSeq.IsActive()) _animSeq.Kill(false);
        KillAllRowTweens(false);

        // (A) Eski Me deðerleri
        int meOldScore = model.Me != null ? model.Me.score : 0;
        int meOldRank = model.Me != null ? model.Me.rank : 0;
        int oldMeIndex = model.MeIndex;

        // (B) Modeli güncelle (Me dahil)
        System.Random rng = new System.Random();
        model.RandomBumpIncludingMe(rng, meMin: 5, meMax: 50,
                                    otherMin: 5, otherMax: 50,
                                    changeChance: 1f);

        // (C) Yeni deðerler
        int meNewScore = model.Me != null ? model.Me.score : meOldScore;
        int meNewRank = model.Me != null ? model.Me.rank : meOldRank;
        int newMeIndex = model.MeIndex;

        int newTop = ComputeTopIndex(newMeIndex, model.Players.Count, config.maxVisibleRows);
        int deltaRows = oldMeIndex - newMeIndex;
        float shift = -deltaRows * rowHeight;

        // içerikleri baðla + baþlangýç pozisyonlarýný ayarla
        PrebindWindowForAnimation(newTop, deltaRows);

        // (D) Me view: label tween
        var meView = FindMeViewInChildren();
        if (meView != null)
        {
            meView.KillLabelTweens(false);
            float dur = Mathf.Max(0.01f, config.containerMoveDuration);
            var ease = config.containerEase;
            meView.AnimateRankInt(meOldRank, meNewRank, dur, ease);
            meView.AnimateScoreInt(meOldScore, meNewScore, dur, ease);
        }

        // ---- Z-POP: Me sadece ilk kez öne gitsin ----
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

        // (E) Diðer satýrlarý hareket ettir (Me sabit)
        int vis = VisibleNow();
        bool anyTween = false;
        _animSeq = DOTween.Sequence();

        if (deltaRows == 0)
        {
            // kayma yok - belirli süre sonra geri çek
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

                meZPushed = false; // tekrar kullanýlabilir
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

            //  Bu view'a baðlý gecikmeli görünürlük iþleri varsa iptal et
            DOTween.Kill(v, complete);
        }

        DOTween.Kill(REBIND_DELAY_ID, complete);
    }


    /// Animasyon baþlamadan, hedef pencereye göre içerikleri bind et ve
    /// her satýrýn baþlangýç Y'sini ayarla.
    /// j: 0..vis-1 hedef slot indeksi
    /// dataIndex: newTop + j
    /// Non-Me satýrlar startY = -(j - deltaRows)*rowHeight  (shift ile -j*rowHeight'e oturacak)
    /// Me satýrý startY = -j*rowHeight  (sabit kalýyor, tween'lenmiyor)
    private void PrebindWindowForAnimation(int newTop, int deltaRows)
    {
        int vis = VisibleNow();

        // Me'nin hedef slotu ve çakýþan slot indeksi
        int jMe = model.MeIndex - newTop;
        int jOverlap = jMe + deltaRows; // prebind anýnda Me'nin altýna "oturan" slot

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
                ? -(j * rowHeight)                 // Me sabit: hedef yerine koy
                : -((j - deltaRows) * rowHeight);  // Non-Me: offsetli baþla

            view.SnapTo(startY);

            //  ÇAKIÞMA: Bu slot, prebind anýnda Me'nin hedef Y'sinde baþlýyor mu?
            // Evet ise 1 sn görünmez kalsýn, sonra aç.
            if (!isMe && j == jOverlap && jOverlap >= 0 && jOverlap < vis)
            {
                view.SetAlpha(0f);
                // 1 sn sonra görünür yap (bu delayed iþleme view'ý target veriyoruz ki kill'lenebilsin)
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
