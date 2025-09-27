using Photon.Pun;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public static IInputHandler inputHandler;
    public int ActorNumber;
    [Header("Movement")]
    [SerializeField]private  float moveSpeed = 5f;
    [SerializeField]private  float sprintSpeed = 8f;
    [SerializeField]private float gravity = -9.81f;
    [SerializeField]private float jumpHeight = 2f;
    
    [SerializeField]private Transform groundCheck;
    [SerializeField]private float groundDistance = 0.4f;
    [SerializeField]private LayerMask groundMask;

    [SerializeField]private CharacterController characterController;
    [Header("Camera")]
    [SerializeField]private float mouseSensitivity = 100f;
    
    [SerializeField]private float recoilSpeed = 10f; 
    [SerializeField]private float recoilReturnSpeed = 5f;  

    [SerializeField]private Transform cameraTransform;
    
    private PlayerMovement _playerMovement;
    private PlayerCamera _playerCamera;
    private PhotonView _photonView;
    public PlayerCamera PlayerCamera => _playerCamera;

    public bool CanMove { get; set; } = true;



    void Awake()
    {
        inputHandler = new DesktopInput();
        _playerMovement = new PlayerMovement(inputHandler,moveSpeed,sprintSpeed,gravity,jumpHeight,groundCheck,groundDistance,groundMask,characterController);
        _playerCamera = new PlayerCamera(inputHandler,mouseSensitivity,recoilSpeed,recoilReturnSpeed, cameraTransform, transform);
        _photonView = GetComponent<PhotonView>();
        if (!_photonView.IsMine)
        {
            Camera[] camers = GetComponentsInChildren<Camera>();
            foreach (Camera cam in camers)
            {
                Destroy(cam);
            }
        }
        
    }

    void Update()
    {
        if(!_photonView.IsMine) return;
        if (CanMove)
        {
            _playerMovement.HandleMovement();
            _playerMovement.Jump();
        }
        _playerCamera.HandleMouseLook();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position,groundDistance);
    }

    public void SetMovementLock(bool locked)
    {
        throw new System.NotImplementedException();
    }
}
