using UnityEngine;

public class PlayerMovement
{
    private  float _moveSpeed;
    private  float _sprintSpeed;
    private float _gravity;
    private float _jumpHeight;
    
    private Transform _groundCheck;
    private float _groundDistance;
    private LayerMask _groundMask;

    private CharacterController _characterController;
    private Vector3 _velocity;
    private bool _isGrounded;

    private IInputHandler _inputHandler;

    public PlayerMovement(IInputHandler inputHandler, float moveSpeed, float sprintSpeed, float gravity, float jumpHeight, Transform groundCheck,float groundDistance, LayerMask groundMask, CharacterController characterController)
    {
        _inputHandler = inputHandler;
        _moveSpeed = moveSpeed;
        _sprintSpeed = sprintSpeed;
        _gravity = gravity;
        _jumpHeight = jumpHeight;
        _groundCheck = groundCheck;
        _groundDistance = groundDistance;
        _groundMask = groundMask;
        _characterController = characterController;
    }
    public void HandleMovement()
    {
        _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundDistance, _groundMask);
        
        Vector2 movementInput = _inputHandler.GetMovementInput().normalized;
        Vector3 move = _characterController.transform.right * movementInput.x + _characterController.transform.forward * movementInput.y;
        float currentSpeed = _inputHandler.IsSprintPressed() ? _sprintSpeed : _moveSpeed;

        _characterController.Move(move * currentSpeed * Time.deltaTime);
    }

    public void Jump()
    {
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }
        if (_inputHandler.IsJumpPressed() && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }
        
        _velocity.y += _gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }
}
