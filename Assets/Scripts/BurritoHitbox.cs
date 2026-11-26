using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BurritoHitbox : MonoBehaviour
{
	[Header("Duración")]
	[SerializeField] private float hitboxDuration = 0.25f;

	[Header("Impulso a los jugadores alcanzados")]
	[SerializeField] private float horizontalForce = 14f;
	[SerializeField] private float verticalForce = 2f;
	[SerializeField] private float controlLockDuration = 0.65f;

	private NetworkObject _ownerPlayer;
	private Coroutine _deactivationRoutine;

	private readonly HashSet<ulong> _hitPlayers = new();

	private bool _isActive;

	private void Awake()
	{
		_ownerPlayer = GetComponentInParent<NetworkObject>();
	}

	/// <summary>
	/// Activa la hitbox desde el servidor.
	/// </summary>
	public void ActivateServer()
	{
		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) { return; }

		if (_ownerPlayer == null)
		{
			_ownerPlayer = GetComponentInParent<NetworkObject>();
		}

		if (_ownerPlayer == null || !_ownerPlayer.IsSpawned)
		{
			Debug.LogWarning("[BurritoHitbox] No se encuentra el NetworkObject del jugador.");
			return;
		}

		// Evitar activaciones anteriores.
		if (_deactivationRoutine != null)
		{
			StopCoroutine(_deactivationRoutine);
		}

		_hitPlayers.Clear();
		_isActive = true;

		// Activar el objeto.
		gameObject.SetActive(true);

		_deactivationRoutine = StartCoroutine(DeactivateRoutine());
	}

	private void OnTriggerEnter(Collider other)
	{
		TryHitPlayer(other);
	}

	private void OnTriggerStay(Collider other)
	{
		TryHitPlayer(other);
	}

	// Detecta y golpea a cada jugador una sola vez.
	private void TryHitPlayer(Collider other)
	{
		if (!_isActive) { return; }

		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) { return; }

		NetworkObject target = other.GetComponentInParent<NetworkObject>();

		if (target == null || !target.IsPlayerObject) { return; }

		// No golpearse a uno mismo.
		if (target.NetworkObjectId == _ownerPlayer.NetworkObjectId) { return; }

		// Evitar golpear dos veces al mismo jugador.
		if (!_hitPlayers.Add(target.OwnerClientId)) { return; }

		// Dirección hacia detrás del jugador que usa el burrito.
		Vector3 direction = _ownerPlayer.transform.forward;
		direction.y = 0f;
		direction.Normalize();

		Debug.Log($"[BurritoHitbox] Jugador golpeado: {target.OwnerClientId}");

		// Solicitar impulso mediante Events.
		Events.RaisePlayerKnockback(target.OwnerClientId, direction, horizontalForce, verticalForce, controlLockDuration);
	}

	private IEnumerator DeactivateRoutine()
	{
		yield return new WaitForSeconds(hitboxDuration);

		_isActive = false;
		_deactivationRoutine = null;

		gameObject.SetActive(false);
	}

	private void OnDisable()
	{
		_isActive = false;
	}
}
