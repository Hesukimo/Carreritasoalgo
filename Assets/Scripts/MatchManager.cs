
using UnityEngine;
using Unity.Netcode;

public class MatchManager : MonoBehaviour
{
	private const string GameSceneName = "Escena";

	private void OnEnable()
	{
		if (NetworkManager.Singleton != null)
		{
			NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvents;
		}
	}

	private void OnDisable()
	{
		if (NetworkManager.Singleton != null)
		{
			NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvents;
		}
	}

	private void HandleSceneEvents(SceneEvent sceneEvent)
	{
		if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
		{
			Debug.Log(
				"Jugador " + sceneEvent.ClientId +
				" ha cargado " + sceneEvent.SceneName
			);
		}

		if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted &&
			NetworkManager.Singleton.IsServer &&
			sceneEvent.SceneName == GameSceneName)
		{
			Debug.Log("Todos los jugadores han entrado en la partida.");
		}
	}
}

