using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TextoFinal : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;

    private void Start()
    {
        resultText.text = "";
    }

    private void Update()
    {
        if (RaceManager.Instance == null || NetworkManager.Singleton == null) { return; }

        var manager = RaceManager.Instance;

        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        int totalPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;

        int arrivedCount = manager.ArrivedPenguins.Count;

        // Si hemos llegado a la meta hemos perdido!
        if (manager.HasArrived(localClientId))
        {
            if (RaceManager.GameOver && manager.WinnerID.Value == localClientId)
            {
                resultText.text = "¡VICTORIA!";
            }
            else
            {
                resultText.text = "DERROTA";
            }
            return;
        }

        // Ganamos si no hemos llegado a la meta y los demás sí
        if (totalPlayers > 1 && arrivedCount == totalPlayers - 1)
        {
            resultText.text = "¡VICTORIA!";
            return;
        }

        resultText.text = "";
    }
}