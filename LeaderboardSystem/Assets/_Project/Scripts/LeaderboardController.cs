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

    private void BindWindow(bool animated, bool skipMeDuringAnimation = false)
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
            bool isMe = data.id == "me";

            // Eðer Me tween sýrasýnda hareket ediyorsa, bind iþlemini atla
            if (skipMeDuringAnimation && isMe) continue;

            activeRows[i].gameObject.SetActive(true);
            activeRows[i].Bind(data, isMe);

            float targetY = -(i * rowHeight);
            if (animated)
                activeRows[i].SnapTo(targetY); // þimdilik anýnda hizalama
            else
                activeRows[i].SnapTo(targetY);
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
        // Me’yi pencerenin ortasýna yakýn tutmak için üst baþlangýcý
        int half = (maxVisible - 1) / 2;            // tam ortalama (tek sayýda kusursuz)
        int idealTop = meIndex - half;
        return Mathf.Clamp(idealTop, 0, Mathf.Max(0, total - maxVisible));
    }

    public void SimulateRandomUpdateAnimated()
    {
        if (model.MeIndex < 0 || activeRows.Count == 0) return;

        // 0) Eski pencerenin sözlüðü: PlayerID -> RowItemView
        //    (Sadece ekranda olanlar)
        var idToView = new Dictionary<string, RowItemView>();
        int oldTop = topIndex;
        for (int i = 0; i < activeRows.Count; i++)
        {
            int dataIndex = oldTop + i;
            if (dataIndex < 0 || dataIndex >= model.Players.Count) continue;
            string id = model.Players[dataIndex].id;
            idToView[id] = activeRows[i];
        }

        // 1) MODELÝ GÜNCELLE — Artýk büyük sýçramalar da mümkün
        System.Random rng = new System.Random();
        model.RandomBumpScoresWithJumps(rng);
        // veya: model.RandomizeAllScores(rng);

        Debug.Log($"[Leaderboard] After update: Me new rank = {model.Me?.rank}");

        // 2) Yeni pencere baþlangýcýný hesapla (Me merkeze yakýn dursun)
        topIndex = ComputeTopIndex(model.MeIndex, model.Players.Count, config.maxVisibleRows);

        // 3) Her yeni görünür slot için hedef Y'yi hesapla ve varsa eski view'u oraya tween'le
        //    (Yoksa þimdilik finalize'a býrakýyoruz; finalize'da zaten doðru bind olacak)
        int newVisible = Mathf.Min(config.maxVisibleRows, model.Players.Count);
        float duration = config.rowMoveDuration;

        // Container'ý DA hemen tween'lemeye baþla ki arka plan da akarken Me ile birlikte kayýyor görünsün
        CenterOnMe(true);

        // Ayný anda çalýþacak tweenleri bir Sequence ile toplayalým (tamamý bitince finalize)
        var seq = DOTween.Sequence();

        for (int i = 0; i < newVisible; i++)
        {
            int newDataIndex = topIndex + i;
            if (newDataIndex < 0 || newDataIndex >= model.Players.Count) continue;

            var pdata = model.Players[newDataIndex];
            string id = pdata.id;
            float targetY = -(i * rowHeight);

            // Eski pencerede görünüyorsa: o satýrý hedef Y'ye taþý
            if (idToView.TryGetValue(id, out RowItemView view))
            {
                // Bind'ý finalize'da yapacaðýz; þimdilik sadece hareket ettiriyoruz
                seq.Join(view.transform.DOLocalMoveY(targetY, duration).SetEase(config.rowEase));
            }
            // Eski pencerede görünmüyorsa: (yeni giren oyuncu)
            // Bu durumda tween için elde view yok; finalize'da doðru yerde spawn/bind olacak.
        }

        // 4) Me özel bir þey yapmaya gerek yok; yukarýdaki döngü zaten Me'yi de yeni yerine taþýdý.
        //    (Me eskiden görünürde deðilse, finalize'da pencerede yerini alacak.)

        // 5) Tüm tweenler bitince finalize: herkesi yeni sýraya bind et (metin/highlight güncel)
        seq.OnComplete(() =>
        {
            BindWindow(false);   // Her oyuncu doðru view'a baðlansýn
            CenterOnMe(true);    // Bir kez daha merkezle (ince düzeltme; istersen kaldýrabilirsin)
        });
    }



}
