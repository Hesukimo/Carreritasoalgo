
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ConnectionApprovalHandler : MonoBehaviour
{
	private NetworkManager m_NetworkManager;

	[SerializeField]
	private int maxPlayers = 4;

	private HashSet<ulong> approvedPlayers = new HashSet<ulong>();

	private void Awake()
	{
		m_NetworkManager = GetComponent<NetworkManager>();

		if (m_NetworkManager == null)
		{
			Debug.LogError("No se encontró NetworkManager.");
			return;
		}

		m_NetworkManager.NetworkConfig.ConnectionApproval = true;
		m_NetworkManager.ConnectionApprovalCallback = ApprovalCheck;

		m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
	}

	private void ApprovalCheck(
		NetworkManager.ConnectionApprovalRequest request,
		NetworkManager.ConnectionApprovalResponse response)
	{
		ulong clientId = request.ClientNetworkId;

		// El Host ya ocupa una plaza
		int maxClients = maxPlayers - 1;

		if (approvedPlayers.Count < maxClients)
		{
			approvedPlayers.Add(clientId);
			response.Approved = true;

			Debug.Log("Cliente conectado: " + clientId);
		}
		else
		{
			response.Approved = false;
			response.Reason = "La sala está completa.";
		}

		response.CreatePlayerObject = response.Approved;
		response.PlayerPrefabHash = null;
		response.Position = Vector3.zero;
		response.Rotation = Quaternion.identity;
		response.Pending = false;
	}

	private void OnClientDisconnectCallback(ulong clientId)
	{
		if (m_NetworkManager != null && m_NetworkManager.IsServer)
		{
			approvedPlayers.Remove(clientId);
			Debug.Log("Cliente desconectado: " + clientId);
		}
	}

	private void OnDestroy()
	{
		if (m_NetworkManager != null)
		{
			m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
			m_NetworkManager.ConnectionApprovalCallback = null;
		}
	}
}
