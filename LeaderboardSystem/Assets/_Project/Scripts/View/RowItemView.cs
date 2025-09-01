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
    [SerializeField] private Material meMaterial;
    [SerializeField] private Material frameMaterial;

    [Header("Layout")]
    [SerializeField] private float rowHeight = 1.0f;
    [SerializeField] private float rowSpacing = 0.10f; // satýrlar arasý boþluk (EKLENDÝ)
    public float RowHeight => rowHeight;
    public float Step => rowHeight + rowSpacing;

    private Transform tr;

    private void Awake()
    {
        tr = transform;
        if (rankText == null || nicknameText == null || scoreText == null)
            Debug.Log($"[RowItemView] Missing TMP refs on {name}");
    }

    // - satýrýn local Y pozisyonunu anýnda ayarlar
    public void SetLocalY(float y)
    {
        var pos = tr.localPosition;
        tr.localPosition = new Vector3(pos.x, y, pos.z);
    }

    // - data baðlar, görünümü günceller
    public void Bind(PlayerData data, bool isMe)
    {
        if (data == null) return;

        if (rankText != null) rankText.text = data.rank.ToString();
        if (nicknameText != null) nicknameText.text = data.nickname;
        if (scoreText != null) scoreText.text = data.score.ToString("N0", CultureInfo.InvariantCulture);

        // highlightRenderer artýk kapatýlmýyor, sadece material atanýyor
        if (highlightRenderer != null)
        {
            if (isMe && meMaterial != null)
                highlightRenderer.material = meMaterial;
            else if (!isMe && frameMaterial != null)
                highlightRenderer.material = frameMaterial;
        }

        this.isMe = isMe;
    }

    // - skor tween
    public void AnimateScoreInt(int from, int to, float duration, Ease ease)
    {
        if (scoreText == null) return;
        DOTween.Kill(scoreText);
        int v = from;
        DOTween.To(() => v, x =>
        {
            v = x;
            if (scoreText != null)
                scoreText.text = v.ToString("N0", CultureInfo.InvariantCulture);
        }, to, duration).SetEase(ease).SetTarget(scoreText);
    }

    // - rank tween
    public void AnimateRankInt(int from, int to, float duration, Ease ease)
    {
        if (rankText == null) return;
        DOTween.Kill(rankText);
        int v = from;
        DOTween.To(() => v, x =>
        {
            v = x;
            if (rankText != null)
                rankText.text = v.ToString();
        }, to, duration).SetEase(ease).SetTarget(rankText);
    }

    public void KillLabelTweens(bool complete = false)
    {
        DOTween.Kill(scoreText, complete);
        DOTween.Kill(rankText, complete);
    }

    public void SnapTo(float targetY)
    {
        SetLocalY(targetY);
    }

    public void SetAlpha(float a)
    {
        if (rankText) rankText.color = SetA(rankText.color, a);
        if (nicknameText) nicknameText.color = SetA(nicknameText.color, a);
        if (scoreText) scoreText.color = SetA(scoreText.color, a);
    }

    static Color SetA(Color c, float a) { c.a = a; return c; }
}
