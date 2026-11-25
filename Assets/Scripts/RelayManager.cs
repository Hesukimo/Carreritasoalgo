using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

//Crea y une partidas usando Unity Relay.
//Poner en un objeto del menu principal (la escena donde esta el NetworkManager).
public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    [SerializeField] private int maxPlayers = 4;            //Incluye al host
    [SerializeField] private bool useRandomProfile = true;  //Necesario para probar con 2 instancias en el mismo PC

    private Task _initTask;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    //Preparar los servicios al abrir el menu
    private async void Start()
    {
        try { await InitServices(); }
        catch (System.Exception e)
        {
            Debug.LogError($"Error iniciando servicios: {e.Message}");
            _initTask = null;
        }
    }

    //Servicios

    private Task InitServices()
    {
        if (_initTask == null) _initTask = InitServicesAsync();
        return _initTask;
    }

    private async Task InitServicesAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            var options = new InitializationOptions();
            if (useRandomProfile) options.SetProfile("p" + Random.Range(0, 100000));
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    //Host

    //Crea la partida y devuelve el codigo para compartir (null si falla)
    public async Task<string> CreateRelay()
    {
        try
        {
            await InitServices();

            //maxConnections no cuenta al host
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData);

            return NetworkManager.Singleton.StartHost() ? joinCode : null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creando Relay: {e.Message}");
            _initTask = null;
            return null;
        }
    }

    //Cliente

    //Se une con el codigo del host (devuelve true si conecta)
    public async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            await InitServices();

            joinCode = joinCode.Trim().ToUpper();
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData);

            return NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error uniendose a Relay: {e.Message}");
            _initTask = null;
            return false;
        }
    }
}
