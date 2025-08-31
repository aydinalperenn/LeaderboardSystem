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

        // 1) Eski durum
        int oldMeIndex = model.MeIndex;
        int oldTop = topIndex;

        // 2) Skorlarý güncelle (hemen rebind etme!)
        System.Random rng = new System.Random();
        model.RandomBumpScores(rng, 5, 50, 0.4f);

        // 3) Yeni durum
        int newMeIndex = model.MeIndex;
        int newTop = ComputeTopIndex(newMeIndex, model.Players.Count, config.maxVisibleRows);

        // Me'nin kaç sýra hareket ettiðini bul (pozitif => Me yukarý çýktý)
        int deltaRows = oldMeIndex - newMeIndex;

        // Container’ý oynatmýyoruz; Me’yi de oynatmayacaðýz.
        // Diðer tüm satýrlarý delta kadar KAYDIRACAÐIZ.
        // Eksen: row 0: y=0, row 1: y=-rowHeight ... Aþaðý = negatif Y.
        // Me yukarý çýktýysa (deltaRows>0) diðerleri aþaðý kaymalý => shift = -deltaRows * rowHeight
        float shift = -deltaRows * rowHeight;

        // 0 hareket varsa, büyük kaydýrma yok. Küçük bir "pulse" ve gecikmeli bind yapalým ki pat pat olmasýn.
        if (deltaRows == 0)
        {
            var meView = FindMeViewInChildren(); // önceki mesajda verdiðim yardýmcý
            if (meView != null)
                meView.transform.DOPunchScale(Vector3.one * 0.05f, 0.25f, 6, 0.5f);

            // Minik gecikmeden sonra rebind; istersen bu gecikmeyi 0 yapabilirsin.
            DOTween.Sequence()
                   .AppendInterval(0.20f)
                   .OnComplete(() => { topIndex = newTop; BindWindow(false); });
            return;
        }

        // 4) Görünen satýrlarý animasyonla kaydýr
        int vis = VisibleNow();
        var seq = DOTween.Sequence();
        bool anyTween = false;

        for (int i = 0; i < vis; i++)
        {
            var view = GetViewAt(i);
            if (view == null) continue;

            // Var olan tweenleri öldür ki üst üste binmesin
            view.transform.DOKill(false);

            // Me sabit kalsýn, KESÝNLÝKLE tween'leme
            if (view.isMe) continue;

            float y0 = view.transform.localPosition.y;
            seq.Join(
                view.transform
                    .DOLocalMoveY(y0 + shift, config.containerMoveDuration)
                    .SetEase(config.containerEase)
            );
            anyTween = true;
        }

        // 5) Animasyon bittikten sonra yeni pencereyi baðla ve slotlara oturt
        if (anyTween)
        {
            seq.OnComplete(() =>
            {
                topIndex = newTop;      // Yeni pencere aralýðýný uygula
                BindWindow(false);      // Her satýrý -i*rowHeight'e snap et + yeni veriyi bas
                                        // DÝKKAT: rowsContainer'ý HÝÇ oynatmadýk; Me zaten merkezde kalýyor.
            });
        }
        else
        {
            // Görünürde yalnýzca Me varsa vb. (edge-case)
            topIndex = newTop;
            BindWindow(false);
        }
    }

}
