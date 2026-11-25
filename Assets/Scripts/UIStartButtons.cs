using TMPro;
using Unity.Netcode;
using UnityEngine;

public class UIStartButtons : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput; //Donde el cliente escribe el codigo
    [SerializeField] private TMP_Text joinCodeText;        //Donde el host ve su codigo
    [SerializeField] private TMP_Text statusText;          //Mensajes (opcional)
    [SerializeField] private GameObject menuPanel;         //Botones del menu, se ocultan al conectar (opcional)

    private bool _busy;
    private string _joinCode;

    //Host con Relay
    public async void OnStartHostClick()
    {
        if (_busy || !CheckRelay()) return;
        _busy = true;
        SetStatus("Creando partida...");

        string code = await RelayManager.Instance.CreateRelay();
        _busy = false;

        if (code == null)
        {
            SetStatus("No se pudo crear la partida");
            return;
        }

        _joinCode = code;
        if (joinCodeText != null) joinCodeText.text = "Codigo: " + code;
        SetStatus("");
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    //Cliente con Relay
    public async void OnStartClientClick()
    {
        if (_busy || !CheckRelay()) return;

        string code = joinCodeInput != null ? joinCodeInput.text : "";
        if (string.IsNullOrWhiteSpace(code))
        {
            SetStatus("Escribe el codigo de la partida");
            return;
        }

        _busy = true;
        SetStatus("Conectando...");

        bool ok = await RelayManager.Instance.JoinRelay(code);
        _busy = false;

        if (!ok)
        {
            SetStatus("No se pudo conectar. Revisa el codigo");
            return;
        }

        SetStatus("");
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    //Servidor dedicado (sin Relay, igual que antes)
    public void OnStartServerClick()
    {
        NetworkManager.Singleton.StartServer();
    }

    //Boton opcional para copiar el codigo
    public void OnCopyCodeClick()
    {
        if (!string.IsNullOrEmpty(_joinCode)) GUIUtility.systemCopyBuffer = _joinCode;
    }

    private bool CheckRelay()
    {
        if (RelayManager.Instance != null) return true;
        SetStatus("Falta el RelayManager en la escena");
        return false;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}