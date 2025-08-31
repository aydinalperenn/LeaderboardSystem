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

    // Sat�r y�ksekli�ini Controller�a bildirmek istersen:
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



    /// Sat�r�n Y konumunu h�zl�ca ayarla (pool + layout i�in).
    public void SetLocalY(float y)
    {
        Vector3 pos = tr.localPosition;
        tr.localPosition = new Vector3(pos.x, y, pos.z);
    }



    /// Veriyi ba�la ve g�r�n�m� g�ncelle.
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


    // --- YARDIMCI: Tween ile skor/Rank ak�c� g�ncelle ---
    public void AnimateScoreInt(int from, int to, float duration, Ease ease)
    {
        if (scoreText == null) return;
        DOTween.Kill(scoreText); // eski skor tween'ini �ld�r
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
        DOTween.Kill(rankText); // eski rank tween'ini �ld�r
        int v = from;
        DOTween.To(() => v, x =>
        {
            v = x;
            if (rankText != null)
                rankText.text = v.ToString();
        }, to, duration).SetEase(ease).SetTarget(rankText);
    }


    // --- YARDIMCI: label tweenlerini �ld�r (spam t�klama i�in) ---
    public void KillLabelTweens(bool complete = false)
    {
        DOTween.Kill(scoreText, complete);
        DOTween.Kill(rankText, complete);
    }


    /// Sat�r� an�nda yeni pozisyona ta��r (DOTween kullanmadan).
    /// DOTween ile ta��yacaksan Controller�dan DOLocalMoveY �a��r.
    public void SnapTo(float targetY)
    {
        SetLocalY(targetY);
    }
}

