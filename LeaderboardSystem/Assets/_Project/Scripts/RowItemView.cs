using UnityEngine;
using TMPro;

public class RowItemView : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private TMP_Text scoreText;

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
        {
            scoreText.text = data.score.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);  // binlik ay�r�c� g�sterimi
        }

        if (highlightRenderer != null)
        {
            highlightRenderer.enabled = isMe;
        }
    }

    /// Sat�r� an�nda yeni pozisyona ta��r (DOTween kullanmadan).
    /// DOTween ile ta��yacaksan Controller�dan DOLocalMoveY �a��r.
    public void SnapTo(float targetY)
    {
        SetLocalY(targetY);
    }
}

