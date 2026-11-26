
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class LobbyManager : MonoBehaviour
{
	private const string GameSceneName = "Escena";

	private bool startingGame = false;

	void Update()
	{
		if (Keyboard.current != null &&
			Keyboard.current.gKey.wasPressedThisFrame)
		{
			StartGame();
		}
	}

	public void StartGame()
	{
		if (startingGame)
			return;

		if (NetworkManager.Singleton == null)
		{
			Debug.LogError("No existe NetworkManager.");
			return;
		}

		// Comprobar que la conexión está iniciada
		if (!NetworkManager.Singleton.IsListening)
		{
			Debug.LogWarning("Primero debes iniciar el Host.");
			return;
		}

		// Solo el Host puede comenzar
		if (!NetworkManager.Singleton.IsServer)
		{
			Debug.LogWarning("Solo el Host puede iniciar la partida.");
			return;
		}

		// Obtener jugadores conectados
		int jugadores = NetworkManager.Singleton.ConnectedClientsList.Count;

		// Necesitamos mínimo 2 jugadores
		if (jugadores < 2)
		{
			Debug.LogWarning(
				"Necesitas al menos 2 jugadores para comenzar. Actualmente: "
				+ jugadores
			);
			return;
		}

		startingGame = true;

		Debug.Log("Iniciando partida con " + jugadores + " jugadores.");

		// Cargar Escena para todos
		SceneEventProgressStatus status =
			NetworkManager.Singleton.SceneManager.LoadScene(
				GameSceneName,
				LoadSceneMode.Single
			);

		if (status != SceneEventProgressStatus.Started)
		{
			startingGame = false;
			Debug.LogError("Error al cargar Escena: " + status);
		}
	}
}
