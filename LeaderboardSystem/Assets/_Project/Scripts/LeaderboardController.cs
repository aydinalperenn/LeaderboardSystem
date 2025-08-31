using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardController : MonoBehaviour
{
    [Header("Config & Data")]
    [SerializeField] private LeaderboardConfig config;     // ScriptableObject

    [TextArea(5, 10)]
    [SerializeField] private string jsonText;              // Dev/test i�in
    [SerializeField] private TextAsset jsonFile;           // Dosyadan okumak i�in

    [Header("View (Canvas's�z)")]
    [SerializeField] private Transform rowsContainer;
    [SerializeField] private RowItemView rowPrefab;

    private LeaderboardModel model = new LeaderboardModel();
    private ILeaderboardDataSource dataSource;

    private Sequence _animSeq;             // aktif sequence referans�
    private const string REBIND_DELAY_ID = "LB_REBIND_DELAY";

    private RowItemPool pool;
    private int topIndex = 0;
    private int visibleCount { get { return Mathf.Min(config.maxVisibleRows, model.Players.Count); } }
    private float rowHeight = 1.0f;

    void Awake()
    {
        // DataSource se�imi
        if (jsonFile != null) dataSource = new TextAssetDataSource(jsonFile);
        else if (!string.IsNullOrEmpty(jsonText)) dataSource = new JsonTextDataSource(jsonText);
        else if (config != null && config.defaultJson != null) dataSource = new TextAssetDataSource(config.defaultJson);
        else dataSource = new JsonTextDataSource("{}");

        // Model y�kle
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
            Debug.LogError("[Leaderboard] Row prefab atanmam��!");
            return;
        }

        rowHeight = rowPrefab.RowHeight;

        // SADECE POOL KULLAN
        pool = new RowItemPool(rowPrefab, rowsContainer, prewarm: visibleCount);
        SpawnInitialRows(); // pool.Get ile �ocuklar� olu�tur + hizala

        topIndex = ComputeTopIndex(model.MeIndex, model.Players.Count, config.maxVisibleRows);
        BindWindow(false);
        CenterOnMe(false);
    }

    

    // ---------- View yard�mc�lar� (activeRows YOK) ----------

    private void SpawnInitialRows()
    {
        // Gerekli say�da child yoksa pool�dan al ve rowsContainer alt�na koy
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
            view.SnapTo(targetY); // �imdilik an�nda hizalama
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
        // Me her zaman merkezde dursun istiyoruz: container Y�sini
        // "meCenterOffset + (MeIndex - topIdx) * rowHeight" form�l�yle hesapl�yoruz.
        float centerY = config.meCenterOffset;
        return centerY + (model.MeIndex - topIdx) * rowHeight;
    }

    public void SimulateRandomUpdateAnimated()
    {
        if (rowsContainer == null || config == null) return;

        if (_animSeq != null && _animSeq.IsActive()) _animSeq.Kill(false);
        KillAllRowTweens(false);

        // (A) Eski de�erler (Me score/rank tween'i i�in)
        int meOldScore = model.Me != null ? model.Me.score : 0;
        int meOldRank = model.Me != null ? model.Me.rank : 0;
        int oldMeIndex = model.MeIndex;

        // (B) Modeli g�ncelle (Me dahil)
        System.Random rng = new System.Random();
        model.RandomBumpIncludingMe(rng, meMin: 5, meMax: 50, otherMin: 5, otherMax: 50, changeChance: 2f);

        // (C) Yeni de�erler
        int meNewScore = model.Me != null ? model.Me.score : meOldScore;
        int meNewRank = model.Me != null ? model.Me.rank : meOldRank;
        int newMeIndex = model.MeIndex;

        int newTop = ComputeTopIndex(newMeIndex, model.Players.Count, config.maxVisibleRows);
        int deltaRows = oldMeIndex - newMeIndex;                 // + ise Me yukar� ��kt�
        float shift = -deltaRows * rowHeight;                  // di�erleri bu kadar kayacak

        //  �NCE ��ER��� G�NCELLE + BA�LANGI� POZ�SYONLARINI AYARLA
        PrebindWindowForAnimation(newTop, deltaRows);

        // (D) Me view��n� bul ve label tween (score/rank) ba�lat
        var meView = FindMeViewInChildren();
        if (meView != null)
        {
            meView.KillLabelTweens(false);
            float dur = Mathf.Max(0.01f, config.containerMoveDuration);
            var ease = config.containerEase;
            meView.AnimateRankInt(meOldRank, meNewRank, dur, ease);
            meView.AnimateScoreInt(meOldScore, meNewScore, dur, ease);
        }

        // (E) Di�er sat�rlar� shift kadar tween�le (Me sabit)
        int vis = VisibleNow();
        bool anyTween = false;
        _animSeq = DOTween.Sequence();

        if (deltaRows == 0)
        {
            if (meView != null)
                meView.transform.DOPunchScale(Vector3.one * 0.06f, 0.25f, 6, 0.5f);

            DOTween.Kill(REBIND_DELAY_ID, false);
            DOTween.Sequence()
                   .SetId(REBIND_DELAY_ID)
                   .AppendInterval(Mathf.Max(0.18f, config.containerMoveDuration * 0.5f))
                   .OnComplete(() =>
                   {
                       topIndex = newTop;
                       BindWindow(false); // i�erik zaten do�ru, bu sadece garanti snap
                   });
            return;
        }

        for (int j = 0; j < vis; j++)
        {
            var view = GetViewAt(j);
            if (view == null) continue;
            if (view.isMe) continue; // Me tweenlenmiyor

            float endY = -(j * rowHeight); // hedef slotun tam yeri
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
                BindWindow(false); // tek seferde snap/garanti
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

            v.transform.DOKill(complete);
            if (v.isMe)
                v.KillLabelTweens(complete);
        }

        DOTween.Kill(REBIND_DELAY_ID, complete);
    }

    /// Animasyon ba�lamadan, hedef pencereye g�re i�erikleri bind et ve
    /// her sat�r�n ba�lang�� Y'sini ayarla.
    /// j: 0..vis-1 hedef slot indeksi
    /// dataIndex: newTop + j
    /// Non-Me sat�rlar startY = -(j - deltaRows)*rowHeight  (shift ile -j*rowHeight'e oturacak)
    /// Me sat�r� startY = -j*rowHeight  (sabit kal�yor, tween'lenmiyor)
    private void PrebindWindowForAnimation(int newTop, int deltaRows)
    {
        int vis = VisibleNow();
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
            bool isMe = data.id == "me";

            view.gameObject.SetActive(true);
            view.Bind(data, isMe);

            // Ba�lang�� pozisyonu:
            float startY;
            if (isMe)
            {
                // Me ekranda sabit: hedef slotunun tam yerine koy
                startY = -(j * rowHeight);
            }
            else
            {
                // Di�erleri: animasyonla -j*rowHeight'e kayacak �ekilde offsetli ba�lat
                startY = -((j - deltaRows) * rowHeight);
            }
            view.SnapTo(startY);
        }
    }


}
