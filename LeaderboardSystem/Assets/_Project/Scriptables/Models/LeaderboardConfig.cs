using UnityEngine;

[CreateAssetMenu(fileName = "LeaderboardConfig", menuName = "Scriptable Objects/LeaderboardConfig")]
public class LeaderboardConfig : ScriptableObject
{
    [Header("View Settings")]
    [Tooltip("Maksimum g�sterilecek oyuncu sat�r� say�s�")]
    public int maxVisibleRows = 25;

    [Tooltip("Me oyuncusunun ortalanma offset de�eri")]
    public float meCenterOffset = 0f;

    [Header("Prefabs & References")]
    [Tooltip("Sat�r prefab (Canvas kullan�lmadan TMP ile haz�rlanm��)")]
    public GameObject rowItemPrefab;

    [Header("Animation")]
    [Tooltip("Sat�r hareket s�resi (saniye)")]
    public float rowMoveDuration = 0.35f;

    [Tooltip("Container hareket s�resi (saniye)")]
    public float containerMoveDuration = 0.45f;

    //[Tooltip("Sat�rlar�n DOTween easing tipi")]
    //public DG.Tweening.Ease rowEase = DG.Tweening.Ease.OutCubic;

    //[Tooltip("Container DOTween easing tipi")]
    //public DG.Tweening.Ease containerEase = DG.Tweening.Ease.InOutCubic;

    [Header("Data (dev/test)")]
    [Tooltip("Varsay�lan JSON dosyas� (test i�in)")]
    public TextAsset defaultJson;
}
