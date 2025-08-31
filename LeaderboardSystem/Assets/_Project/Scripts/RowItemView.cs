using DG.Tweening;
using System.Globalization;
using TMPro;
using UnityEngine;

public class RowItemView : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text scoreText;
    public bool isMe { get; private set; }

    [Header("Highlight")]
    [SerializeField] private Renderer highlightRenderer;

    // Satýr yüksekliðini Controller’a bildirmek istersen:
    [Header("Layout")]
    [SerializeField] private float rowHeight = 1.0f;
    public float RowHeight => rowHeight;


    // Cache
    private Transform tr;

    private void Awake()
    {
        tr = transform;

        if (rankText == null || nicknameText == null || scoreText == null)
        {
            Debug.Log($"[RowItemView] Missing TMP refs on {name}");
        }
    }



    /// Satýrýn Y konumunu hýzlýca ayarla (pool + layout için).
    public void SetLocalY(float y)
    {
        Vector3 pos = tr.localPosition;
        tr.localPosition = new Vector3(pos.x, y, pos.z);
    }



    /// Veriyi baðla ve görünümü güncelle.
    public void Bind(PlayerData data, bool isMe)
    {
        if (data == null) return;

        // Rank / Nick / Score
        if (rankText != null) rankText.text = data.rank.ToString();
        if (nicknameText != null) nicknameText.text = data.nickname;
        if (scoreText != null)
            scoreText.text = data.score.ToString("N0", CultureInfo.InvariantCulture);

        if (highlightRenderer != null)
            highlightRenderer.enabled = isMe;

        this.isMe = isMe;
    }


    // --- YARDIMCI: Tween ile skor/Rank akýcý güncelle ---
    public void AnimateScoreInt(int from, int to, float duration, Ease ease)
    {
        if (scoreText == null) return;
        DOTween.Kill(scoreText); // eski skor tween'ini öldür
        int v = from;
        DOTween.To(() => v, x =>
        {
            v = x;
            if (scoreText != null)
                scoreText.text = v.ToString("N0", CultureInfo.InvariantCulture);
        }, to, duration).SetEase(ease).SetTarget(scoreText);
    }


    public void AnimateRankInt(int from, int to, float duration, Ease ease)
    {
        if (rankText == null) return;
        DOTween.Kill(rankText); // eski rank tween'ini öldür
        int v = from;
        DOTween.To(() => v, x =>
        {
            v = x;
            if (rankText != null)
                rankText.text = v.ToString();
        }, to, duration).SetEase(ease).SetTarget(rankText);
    }


    // --- YARDIMCI: label tweenlerini öldür (spam týklama için) ---
    public void KillLabelTweens(bool complete = false)
    {
        DOTween.Kill(scoreText, complete);
        DOTween.Kill(rankText, complete);
    }


    /// Satýrý anýnda yeni pozisyona taþýr (DOTween kullanmadan).
    /// DOTween ile taþýyacaksan Controller’dan DOLocalMoveY çaðýr.
    public void SnapTo(float targetY)
    {
        SetLocalY(targetY);
    }
}

