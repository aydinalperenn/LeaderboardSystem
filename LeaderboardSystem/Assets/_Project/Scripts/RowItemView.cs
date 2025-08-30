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
        {
            scoreText.text = data.score.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);  // binlik ayýrýcý gösterimi
        }

        if (highlightRenderer != null)
        {
            highlightRenderer.enabled = isMe;
        }
    }

    /// Satýrý anýnda yeni pozisyona taþýr (DOTween kullanmadan).
    /// DOTween ile taþýyacaksan Controller’dan DOLocalMoveY çaðýr.
    public void SnapTo(float targetY)
    {
        SetLocalY(targetY);
    }
}

