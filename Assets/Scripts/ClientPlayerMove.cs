using Unity.Netcode;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
namespace NetcodeDemo
{
    public class ClientPlayerMove : NetworkBehaviour
    {
        [SerializeField]
        CharacterController m_CharacterController;
        [SerializeField]
        ThirdPersonController m_ThirdPersonController;
        [SerializeField]
        PlayerInput m_PlayerInput;
        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        private void Awake()
        {
            m_PlayerInput.enabled = false;
            m_ThirdPersonController.enabled = false;
            m_CharacterController.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            enabled = IsClient; // Enable if this is a client.
            if (!IsOwner)
            {
                // Disable if this is not the owner
                enabled = false;
                m_PlayerInput.enabled = false;
                m_CharacterController.enabled = false;
                m_ThirdPersonController.enabled = false;
                return;
            }

            GameObject virtualCameraObject = GameObject.Find("PlayerFollowCamera");
            if (virtualCameraObject != null)
            {
                var vCam3 = virtualCameraObject.GetComponent<Unity.Cinemachine.CinemachineCamera>();
                if (vCam3 != null) vCam3.Follow = CinemachineCameraTarget.transform;
            }

            // Enable if this is an owner
            m_PlayerInput.enabled = true;
            m_CharacterController.enabled = true;
            m_ThirdPersonController.enabled = true;

        }
    }
}
