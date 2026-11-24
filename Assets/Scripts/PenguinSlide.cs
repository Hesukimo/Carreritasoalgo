using StarterAssets;
using Unity.Netcode;
using UnityEngine;

public class PenguinSlide : NetworkBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private ThirdPersonController thirdPerson;
    [SerializeField] private StarterAssetsInputs input;
    [SerializeField] private PlayerEffects effects;
    [SerializeField] private Animator animator;

    [Header("Deteccion de rampa")]
    [Tooltip("Inclinacion minima para empezar a deslizar")]
    [SerializeField] private float slideAngle = 25f;
    [Tooltip("Raycast a la rampa")]
    [SerializeField] private float rayLength = 1f;

    [Header("Deslizamiento")]
    [SerializeField] private float acceleration = 12f; 
    [SerializeField] private float maxSpeed = 14f;    
    [SerializeField] private float friction = 8f;      
    [SerializeField] private float stopSpeed = 1f;    

    [Header("Control")]
    [SerializeField] private float steerSpeed = 3f;    
    [SerializeField] private float turnSpeed = 10f;    

    [SerializeField] private string slidingParam = "Sliding";

    private bool _sliding;
    private float _speed;
    private Vector3 _slideDir = Vector3.forward; 
    private Vector3 _lastVelocity;

    private int _animSliding;
    private bool _hasAnimParam;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (thirdPerson == null) thirdPerson = GetComponent<ThirdPersonController>();
        if (input == null) input = GetComponent<StarterAssetsInputs>();
        if (effects == null) effects = GetComponent<PlayerEffects>();
        if (animator == null) animator = GetComponent<Animator>();

        if (animator != null)
        {
            _animSliding = Animator.StringToHash(slidingParam);
            foreach (var p in animator.parameters)
                if (p.nameHash == _animSliding) _hasAnimParam = true;
        }
    }

    public override void OnNetworkDespawn()
    {
        ResetSlide();
    }

    private void Update()
    {
        if (!IsOwner || !controller.enabled) return;

        Vector3 normal = Vector3.up;
        bool grounded = Physics.Raycast(
            transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit,
            rayLength, thirdPerson.GroundLayers, QueryTriggerInteraction.Ignore);
        if (grounded) normal = hit.normal;

        bool steep = grounded && Vector3.Angle(normal, Vector3.up) >= slideAngle;

        //EFECTOS
        float speedMult = effects != null ? effects.Multiplier(StatType.MoveSpeed) : 1f;

        //ESTADO
        if (steep)
        {
            _slideDir = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
            SetSliding(true);
            _speed = Mathf.MoveTowards(_speed, maxSpeed * speedMult, acceleration * Time.deltaTime);
        }
        else if (_sliding)
        {
            //Suelo y deslizamiento
            if (grounded) _speed -= friction * Time.deltaTime;
            if (_speed <= stopSpeed)
            {
                ResetSlide();
                return;
            }
        }

        if (!_sliding) return;

        //Moverse con la rampa
        Vector3 dir = Vector3.ProjectOnPlane(_slideDir, normal).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = _slideDir;

        // Giro lateral con A/D
        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        Vector3 right = flat.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, flat.normalized) : Vector3.zero;
        float steer = input != null ? input.move.x : 0f;

        _lastVelocity = dir * _speed + right * (steer * steerSpeed);
        controller.Move(_lastVelocity * Time.deltaTime);
    }

    // Orientacion
    private void LateUpdate()
    {
        if (!IsOwner || !_sliding) return;

        Vector3 flat = new Vector3(_lastVelocity.x, 0f, _lastVelocity.z);
        if (flat.sqrMagnitude < 0.01f) return;

        Quaternion target = Quaternion.LookRotation(flat.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    private void SetSliding(bool value)
    {
        if (_sliding == value) return;
        _sliding = value;

        //TEST
        if (effects != null) effects.SetLocalMoveLock(value);
        if (animator != null && _hasAnimParam) animator.SetBool(_animSliding, value);
    }

    // TELEPORT TEST VALIDACION
    public void ResetSlide()
    {
        _speed = 0f;
        _lastVelocity = Vector3.zero;
        SetSliding(false);
    }
}

