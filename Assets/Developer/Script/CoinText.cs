
using UnityEngine;
using TMPro;

public class CoinText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtCoin;

    public void SetEntryText(string text)
    {
        txtCoin.text = text;
    }
}
