
using UnityEngine;
using TMPro;

public class HistoryEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtEntry;

    public void SetEntryText(string text)
    {
        if (txtEntry != null)
        {
            txtEntry.text = text;
        }
    }
}
