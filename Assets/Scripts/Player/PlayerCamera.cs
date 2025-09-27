using UnityEngine;

public class PlayerCamera
{
    private float _mouseSensitivity;
    
    private float _recoilSpeed; 
    private float _recoilReturnSpeed;  

    private Transform _cameraTransform;

    private IInputHandler _inputHandler;
    private float _xRotation = 0f;

    private Transform _playerTransform;
    private Vector2 _recoilOffset;  
    private Vector2 _currentOffset; 

    public PlayerCamera(IInputHandler inputHandler, float mouseSensitivity, float recoilSpeed, float recoilReturnSpeed,Transform cameraTransform, Transform playerTransform)
    {
        
        _inputHandler = inputHandler;
        _mouseSensitivity = mouseSensitivity;
        _recoilSpeed = recoilSpeed;
        _recoilReturnSpeed = recoilReturnSpeed;
        _cameraTransform = cameraTransform;
        _playerTransform = playerTransform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void HandleMouseLook()
    {
        Vector2 mouseInput = _inputHandler.GetMouseInput();
        float mouseX = mouseInput.x * _mouseSensitivity * Time.deltaTime;
        float mouseY = mouseInput.y * _mouseSensitivity * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        
        _currentOffset = Vector2.Lerp(_currentOffset, _recoilOffset, Time.deltaTime * _recoilSpeed);
        _recoilOffset = Vector2.Lerp(_recoilOffset, Vector2.zero, Time.deltaTime * _recoilReturnSpeed);

        _cameraTransform.localRotation = Quaternion.Euler(_xRotation - _currentOffset.y, 0f, 0f);
        _playerTransform.Rotate(Vector3.up * (mouseX - _currentOffset.x));
    }

    public void AddRecoil(float verticalRecoil,float horizontalRecoil)
    {
        float vertical = Random.Range(verticalRecoil / 2f, verticalRecoil);
        float horizontal = Random.Range(-horizontalRecoil, horizontalRecoil);
        _recoilOffset += new Vector2(horizontal*Time.deltaTime, vertical);
    }
}
