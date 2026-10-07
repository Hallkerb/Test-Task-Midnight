using UnityEngine;
using TMPro;

public class CurrencyExchangePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text fromText;
    [SerializeField] private TMP_Text toText;

    private void Awake()
    {
        inputField.onValueChanged.AddListener(OnValueInput);
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnValueInput(string value)
    {
        if (long.TryParse(value, out long amount))
        {
            if (amount == 1)
                fromText.text = $"From: {EconomyManager.Format(amount)} Gem";
            else
                fromText.text = $"From: {EconomyManager.Format(amount)} Gems";
                
            toText.text = $"To: ${EconomyManager.Format(amount * EconomyManager.ExchangeRate)}";
        }
        else
        {
            inputField.placeholder.GetComponent<TMP_Text>().text = "Enter a valid number";
        }
    }

    public void TryExchange()
    {
        if (long.TryParse(inputField.text, out long amount))
        {
            EconomyManager.Instance.TryExchange(CurrencyType.Gems, amount, CurrencyType.Cash, EconomyManager.ExchangeRate);
            GameSettings.Save();
        }
        else
        {
            inputField.placeholder.GetComponent<TMP_Text>().text = "Enter a valid number";
        }

        inputField.text = string.Empty;
    }
}
