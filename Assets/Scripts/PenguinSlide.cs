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

    [Header("Inicio del deslizamiento")]
    [SerializeField] private float slideAngle = 25f;
    [SerializeField] private bool alwaysSlide = false;
    [SerializeField] private float rayLength = 1f;

    [Header("Velocidad")]
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float maxSpeed = 14f;
    [SerializeField] private float flatSpeed = 8f;
    [SerializeField] private float deceleration = 3f;

    [Header("Frenado")]
    [SerializeField] private float brakeDeceleration = 6f;
    [SerializeField, Range(0f, 1f)] private float minSpeedPercent = 0.75f; //Minimo al frenar (% de la velocidad maxima)

    [Header("Salto")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float jumpLockTime = 0.15f; //Tiempo en el que se ignora el suelo tras saltar

    [Header("Pegado al suelo")]
    [SerializeField] private float stickForce = 6f;
    [SerializeField] private float groundGrace = 0.15f;
    [SerializeField] private float airGravity = 20f;

    [Header("Fin del deslizamiento")]
    [SerializeField] private bool stopOnFlatGround = false;

    [Header("Control")]
    [SerializeField] private float steerSpeed = 3f;
    [SerializeField] private float turnSpeed = 10f;

    [SerializeField] private string slidingParam = "Sliding";

    private bool _sliding;
    private float _speed;
    private Vector3 _slideDir = Vector3.forward; //Ultima direccion de deslizamiento
    private Vector3 _lastVelocity;
    private Vector3 _airHorizontal;
    private float _verticalSpeed;
    private float _yaw; //Rotacion propia mientras desliza

    private Vector3 _groundNormal = Vector3.up;
    private float _groundTimer;
    private float _jumpLock;

    private int _animSliding;
    private bool _hasAnimParam;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (thirdPerson == null) thirdPerson = GetComponent<ThirdPersonController>();
        if (input == null) input = GetComponent<StarterAssetsInputs>();
        if (effects == null) effects = GetComponent<PlayerEffects>();
        if (animator == null) animator = GetComponent<Animator>();

        //Validar animator
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

        float dt = Time.deltaTime;

        //Detectar suelo (se ignora un momento tras saltar para no pegarse otra vez)
        if (_jumpLock > 0f)
        {
            _jumpLock -= dt;
            _groundTimer = 0f;
        }
        else if (Physics.Raycast(
            transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit,
            rayLength, thirdPerson.GroundLayers, QueryTriggerInteraction.Ignore))
        {
            _groundNormal = hit.normal;
            _groundTimer = groundGrace;
        }
        else
        {
            _groundTimer -= dt;
        }

        bool grounded = _groundTimer > 0f;
        Vector3 normal = grounded ? _groundNormal : Vector3.up;
        bool steep = grounded && Vector3.Angle(normal, Vector3.up) >= slideAngle;

        //Siempre deslizando
        if (alwaysSlide && !_sliding)
        {
            Vector3 fwd = new Vector3(transform.forward.x, 0f, transform.forward.z);
            _slideDir = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
            _speed = flatSpeed;
            SetSliding(true);
        }

        //Salto (solo mientras desliza; el input se consume para que no se acumule)
        if (_sliding && input != null && input.jump)
        {
            input.jump = false;

            if (grounded)
            {
                _verticalSpeed = jumpForce;
                _jumpLock = jumpLockTime;
                _groundTimer = 0f;
                grounded = false;
                normal = Vector3.up;
                steep = false;
            }
        }

        //Efectos
        float speedMult = effects != null ? effects.Multiplier(StatType.MoveSpeed) : 1f;

        //Frenado (S / stick hacia atras)
        bool braking = input != null && input.move.y < -0.1f;

        //Velocidad target
        float targetSpeed;
        float rate;

        if (steep)
        {
            //Rampa: acelera hasta la velocidad maxima
            _slideDir = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
            SetSliding(true);
            targetSpeed = maxSpeed * speedMult;
            rate = acceleration;

            //Frenar un poco: solo baja hasta un porcentaje de la velocidad maxima
            float brakeFloor = targetSpeed * minSpeedPercent;
            if (braking && _speed > brakeFloor)
            {
                targetSpeed = brakeFloor;
                rate = brakeDeceleration;
            }
        }
        else if (_sliding)
        {
            //Plano
            if (stopOnFlatGround && grounded && !alwaysSlide)
            {
                ResetSlide();
                return;
            }

            //Velocidad de crucero
            targetSpeed = flatSpeed * speedMult;
            rate = deceleration;
        }
        else
        {
            return; //No esta deslizando
        }

        //La velocidad solo cambia en el suelo (en el aire se conserva)
        if (grounded)
            _speed = Mathf.MoveTowards(_speed, targetSpeed, rate * dt);

        //Seguir la superficie
        Vector3 dir = Vector3.ProjectOnPlane(_slideDir, normal).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = _slideDir;

        //Movimiento lateral
        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        Vector3 right = flat.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, flat.normalized) : Vector3.zero;
        float steer = input != null ? input.move.x : 0f;
        Vector3 side = right * (steer * steerSpeed);

        Vector3 velocity;

        if (grounded)
        {
            //En el suelo: sigue la superficie y se pega a ella
            velocity = dir * _speed + side;
            _lastVelocity = velocity;
            _airHorizontal = new Vector3(velocity.x, 0f, velocity.z);
            _verticalSpeed = velocity.y;

            velocity -= normal * stickForce;
        }
        else
        {
            //En el aire: gravedad propia
            _verticalSpeed -= airGravity * dt;
            velocity = _airHorizontal + side + Vector3.up * _verticalSpeed;
            _lastVelocity = _airHorizontal + side;
        }

        controller.Move(velocity * dt);
    }

    //Orientar al deslizamiento (rotacion propia, ignora lo que haga el ThirdPersonController con W/A/S/D)
    private void LateUpdate()
    {
        if (!IsOwner || !_sliding) return;

        Vector3 flat = new Vector3(_lastVelocity.x, 0f, _lastVelocity.z);
        if (flat.sqrMagnitude >= 0.01f)
        {
            float targetYaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;
            _yaw = Mathf.LerpAngle(_yaw, targetYaw, turnSpeed * Time.deltaTime);
        }

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    private void SetSliding(bool value)
    {
        if (_sliding == value) return;
        _sliding = value;
        if (value) _yaw = transform.eulerAngles.y;

        //Bloquear controles
        if (effects != null) effects.SetLocalMoveLock(value);
        if (animator != null && _hasAnimParam) animator.SetBool(_animSliding, value);
    }

    //Validacion para el teletransportar
    public void ResetSlide()
    {
        _speed = 0f;
        _lastVelocity = Vector3.zero;
        _airHorizontal = Vector3.zero;
        _verticalSpeed = 0f;
        _groundTimer = 0f;
        _jumpLock = 0f;
        SetSliding(false);
    }
}