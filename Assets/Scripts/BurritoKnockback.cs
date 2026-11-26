using System.Collections;
using Unity.Netcode;
using UnityEngine;
using StarterAssets;

public class BurritoKnockback : NetworkBehaviour
{
	[Header("Referencias")]
	[SerializeField] private CharacterController controller;
	[SerializeField] private ThirdPersonController thirdPerson;
	[SerializeField] private PenguinSlide penguinSlide;

	[Header("Movimiento")]
	[SerializeField] private float deceleration = 18f;
	[SerializeField] private float gravity = 0f;

	private Coroutine _knockbackRoutine;
	private bool _isKnockedBack;

	private void Awake()
	{
		controller = GetComponent<CharacterController>();
		thirdPerson = GetComponent<ThirdPersonController>();
		penguinSlide = GetComponent<PenguinSlide>();
	}

	// Solicita al servidor aplicar un impulso a este jugador.
	public void ApplyKnockback(Vector3 direction, float horizontalForce, float verticalForce, float controlLockDuration)
	{
		if (!IsServer) { return; }

		if (!IsSpawned) { return; }

		// Solo el propietario mueve su CharacterController.
		ClientRpcParams rpcParams = new ClientRpcParams
		{
			Send = new ClientRpcSendParams
			{
				TargetClientIds = new[] { OwnerClientId }
			}
		};

		ApplyKnockbackClientRpc(direction, horizontalForce, verticalForce, controlLockDuration, rpcParams);
	}

	[ClientRpc]
	private void ApplyKnockbackClientRpc(Vector3 direction, float horizontalForce, float verticalForce, float controlLockDuration, ClientRpcParams rpcParams = default)
	{
		if (!IsOwner) { return; }

		if (controller == null || !controller.enabled) { return; }

		// Cancelar un impulso anterior si lo hubiera.
		if (_knockbackRoutine != null)
		{
			StopCoroutine(_knockbackRoutine);
		}

		_knockbackRoutine = StartCoroutine(KnockbackRoutine(direction, horizontalForce, verticalForce, controlLockDuration));
	}

	private IEnumerator KnockbackRoutine(Vector3 direction, float horizontalForce, float verticalForce, float controlLockDuration)
	{
		_isKnockedBack = true;

		// Cancelar deslizamiento.
		if (penguinSlide != null)
		{
			penguinSlide.ResetSlide();
		}

		// Bloquear movimiento normal.
		if (thirdPerson != null)
		{
			thirdPerson.enabled = false;
		}

		// Preparar dirección horizontal.
		direction.y = 0f;

		if (direction.sqrMagnitude < 0.001f)
		{
			direction = -transform.forward;
		}

		direction.Normalize();

		Vector3 horizontalVelocity = direction * horizontalForce;

		float verticalVelocity = verticalForce;
		float elapsed = 0f;

		while (elapsed < controlLockDuration)
		{
			float dt = Time.deltaTime;

			// Frenar progresivamente el impulso horizontal.
			horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, deceleration * dt);

			// Gravedad.
			verticalVelocity -= gravity * dt;

			Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;

			controller.Move(velocity * dt);

			elapsed += dt;

			yield return null;
		}

		// Devolver el control.
		if (thirdPerson != null)
		{
			thirdPerson.enabled = true;
		}

		_isKnockedBack = false;
		_knockbackRoutine = null;
	}

	public bool IsKnockedBack => _isKnockedBack;

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			Events.OnBurritoKnockback += HandleKnockback;
		}
	}

	private void HandleKnockback(ulong clientId, Vector3 direction, float horizontalForce, float verticalForce, float duration)
	{
		if (!IsServer || OwnerClientId != clientId)
		{
			return;
		}

		ApplyKnockback(direction, horizontalForce, verticalForce, duration);
	}

	public override void OnNetworkDespawn()
	{
		Events.OnBurritoKnockback -= HandleKnockback;

		if (_knockbackRoutine != null)
		{
			StopCoroutine(_knockbackRoutine);
			_knockbackRoutine = null;
		}

		_isKnockedBack = false;

		if (IsOwner && thirdPerson != null)
		{
			thirdPerson.enabled = true;
		}
	}
}