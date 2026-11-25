using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

//Controla el cursor:
//- Antes de que aparezca tu jugador (menu de Host/Cliente): cursor libre para clicar la interfaz.
//- Con el jugador en la partida: cursor bloqueado.
//- Manteniendo Alt: cursor libre para clicar la interfaz (al soltar se vuelve a bloquear).
//Poner en un objeto vacio de la escena. Se mantiene entre escenas.
[DefaultExecutionOrder(1000)] //Se ejecuta el ultimo, despues de cualquier otro script que toque el cursor
public class CursorManager : MonoBehaviour
{
    private static CursorManager _instance;

    [Header("Tecla Alt")]
    [SerializeField] private bool useAltKey = true;
    [SerializeField] private bool toggleMode = false; //false = libre mientras la mantienes, true = alterna con cada pulsacion

    //Poner a true desde otros scripts para liberar el cursor (menu de pausa, etc)
    public static bool ForceFree;

    private StarterAssetsInputs _playerInput;
    private bool _wasLocked;
    private bool _toggledFree;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void LateUpdate()
    {
        FindLocalPlayerInput();

        //Solo hay partida cuando ya existe el jugador local
        bool inGame = _playerInput != null;

        bool altFree = inGame && useAltKey && IsAltFree();
        bool wantLocked = inGame && !ForceFree && !altFree;

        //Evita que StarterAssetsInputs vuelva a bloquear el cursor o mueva la camara con el raton
        if (_playerInput != null)
        {
            _playerInput.cursorLocked = wantLocked;
            _playerInput.cursorInputForLook = wantLocked;
        }

        if (!wantLocked)
        {
            //Cursor libre (se fuerza cada frame)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (!_wasLocked)
        {
            //Bloquear solo al cambiar de estado (asi Esc sigue funcionando en el Editor)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        _wasLocked = wantLocked;
    }

    //Alt pulsado (izquierdo o derecho)
    private bool IsAltFree()
    {
        var kb = Keyboard.current;
        if (kb == null) return false;

        if (toggleMode)
        {
            if (kb.leftAltKey.wasPressedThisFrame || kb.rightAltKey.wasPressedThisFrame)
                _toggledFree = !_toggledFree;
            return _toggledFree;
        }

        return kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;
    }

    //Buscar el StarterAssetsInputs del jugador local
    private void FindLocalPlayerInput()
    {
        if (_playerInput != null) return;

        _toggledFree = false;

        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || nm.LocalClient == null) return;

        var playerObject = nm.LocalClient.PlayerObject;
        if (playerObject != null) _playerInput = playerObject.GetComponent<StarterAssetsInputs>();
    }
}
