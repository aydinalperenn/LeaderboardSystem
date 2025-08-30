using UnityEngine;

[CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "Scriptable Objects/LeaderboardConfig")]
public class LeaderboardConfig : ScriptableObject
{
    [Header("View Settings")]
    [Tooltip("Maksimum gösterilecek oyuncu satýrý sayýsý")]
    public int maxVisibleRows = 25;

    [Tooltip("Me oyuncusunun ortalanma offset deðeri")]
    public float meCenterOffset = 0f;

    [Header("Prefabs & References")]
    [Tooltip("Satýr prefab (Canvas kullanýlmadan TMP ile hazýrlanmýþ)")]
    public GameObject rowItemPrefab;

    [Header("Animation")]
    [Tooltip("Satýr hareket süresi (saniye)")]
    public float rowMoveDuration = 0.35f;

    [Tooltip("Container hareket süresi (saniye)")]
    public float containerMoveDuration = 0.45f;

    //[Tooltip("Satýrlarýn DOTween easing tipi")]
    //public DG.Tweening.Ease rowEase = DG.Tweening.Ease.OutCubic;

    //[Tooltip("Container DOTween easing tipi")]
    //public DG.Tweening.Ease containerEase = DG.Tweening.Ease.InOutCubic;

    [Header("Data (dev/test)")]
    [Tooltip("Varsayýlan JSON dosyasý (test için)")]
    public TextAsset defaultJson;
}
