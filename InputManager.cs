using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrawlAnything.Core
{
    /// <summary>
    /// Système d'entrée unifié qui gère les entrées utilisateur sur différentes plateformes
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private bool enableTouchInput = true;
        [SerializeField] private bool enableKeyboardInput = true;
        [SerializeField] private bool enableGamepadInput = true;

        // Singleton instance
        private static InputManager _instance;
        public static InputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<InputManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("InputManager");
                        _instance = go.AddComponent<InputManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Input actions
        private InputAction touchPressAction;
        private InputAction touchPositionAction;
        private InputAction moveAction;
        private InputAction attackAction;
        private InputAction defendAction;
        private InputAction specialAction;
        private InputAction pauseAction;

        // Input state
        private Vector2 touchPosition;
        private bool isTouching;
        private Vector2 movementDirection;
        private bool isAttacking;
        private bool isDefending;
        private bool isSpecialAttacking;
        private bool isPausing;

        // Events
        public event Action<Vector2> OnTouchBegan;
        public event Action<Vector2> OnTouchMoved;
        public event Action<Vector2> OnTouchEnded;
        public event Action<Vector2> OnMove;
        public event Action OnAttack;
        public event Action OnDefend;
        public event Action OnSpecial;
        public event Action OnPause;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize input actions
            InitializeInputActions();
        }

        private void OnEnable()
        {
            // Enable input actions
            EnableInputActions();
        }

        private void OnDisable()
        {
            // Disable input actions
            DisableInputActions();
        }

        private void InitializeInputActions()
        {
            // Touch input
            touchPressAction = new InputAction("TouchPress", binding: "<Touchscreen>/primaryTouch/press");
            touchPositionAction = new InputAction("TouchPosition", binding: "<Touchscreen>/primaryTouch/position");

            // Movement input (keyboard, gamepad)
            moveAction = new InputAction("Move");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");

            // Action inputs
            attackAction = new InputAction("Attack");
            attackAction.AddBinding("<Keyboard>/space");
            attackAction.AddBinding("<Gamepad>/buttonSouth");

            defendAction = new InputAction("Defend");
            defendAction.AddBinding("<Keyboard>/leftShift");
            defendAction.AddBinding("<Gamepad>/buttonEast");

            specialAction = new InputAction("Special");
            specialAction.AddBinding("<Keyboard>/e");
            specialAction.AddBinding("<Gamepad>/buttonWest");

            pauseAction = new InputAction("Pause");
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.AddBinding("<Gamepad>/start");

            // Set up callbacks
            touchPressAction.performed += ctx => OnTouchPressPerformed(ctx);
            touchPressAction.canceled += ctx => OnTouchPressCanceled(ctx);
            touchPositionAction.performed += ctx => OnTouchPositionPerformed(ctx);
            moveAction.performed += ctx => OnMovePerformed(ctx);
            moveAction.canceled += ctx => OnMoveCanceled(ctx);
            attackAction.performed += ctx => OnAttackPerformed(ctx);
            defendAction.performed += ctx => OnDefendPerformed(ctx);
            specialAction.performed += ctx => OnSpecialPerformed(ctx);
            pauseAction.performed += ctx => OnPausePerformed(ctx);
        }

        private void EnableInputActions()
        {
            if (enableTouchInput)
            {
                touchPressAction.Enable();
                touchPositionAction.Enable();
            }

            if (enableKeyboardInput || enableGamepadInput)
            {
                moveAction.Enable();
                attackAction.Enable();
                defendAction.Enable();
                specialAction.Enable();
                pauseAction.Enable();
            }
        }

        private void DisableInputActions()
        {
            touchPressAction.Disable();
            touchPositionAction.Disable();
            moveAction.Disable();
            attackAction.Disable();
            defendAction.Disable();
            specialAction.Disable();
            pauseAction.Disable();
        }

        #region Input Callbacks

        private void OnTouchPressPerformed(InputAction.CallbackContext context)
        {
            isTouching = true;
            OnTouchBegan?.Invoke(touchPosition);
        }

        private void OnTouchPressCanceled(InputAction.CallbackContext context)
        {
            isTouching = false;
            OnTouchEnded?.Invoke(touchPosition);
        }

        private void OnTouchPositionPerformed(InputAction.CallbackContext context)
        {
            touchPosition = context.ReadValue<Vector2>();
            if (isTouching)
            {
                OnTouchMoved?.Invoke(touchPosition);
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            movementDirection = context.ReadValue<Vector2>();
            OnMove?.Invoke(movementDirection);
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            movementDirection = Vector2.zero;
            OnMove?.Invoke(movementDirection);
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            isAttacking = true;
            OnAttack?.Invoke();
        }

        private void OnDefendPerformed(InputAction.CallbackContext context)
        {
            isDefending = true;
            OnDefend?.Invoke();
        }

        private void OnSpecialPerformed(InputAction.CallbackContext context)
        {
            isSpecialAttacking = true;
            OnSpecial?.Invoke();
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            isPausing = true;
            OnPause?.Invoke();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Obtient la position actuelle du toucher
        /// </summary>
        public Vector2 GetTouchPosition()
        {
            return touchPosition;
        }

        /// <summary>
        /// Vérifie si l'écran est actuellement touché
        /// </summary>
        public bool IsTouching()
        {
            return isTouching;
        }

        /// <summary>
        /// Obtient la direction de mouvement actuelle
        /// </summary>
        public Vector2 GetMovementDirection()
        {
            return movementDirection;
        }

        /// <summary>
        /// Vérifie si le bouton d'attaque est actuellement pressé
        /// </summary>
        public bool IsAttacking()
        {
            bool result = isAttacking;
            isAttacking = false; // Reset after read
            return result;
        }

        /// <summary>
        /// Vérifie si le bouton de défense est actuellement pressé
        /// </summary>
        public bool IsDefending()
        {
            bool result = isDefending;
            isDefending = false; // Reset after read
            return result;
        }

        /// <summary>
        /// Vérifie si le bouton d'attaque spéciale est actuellement pressé
        /// </summary>
        public bool IsSpecialAttacking()
        {
            bool result = isSpecialAttacking;
            isSpecialAttacking = false; // Reset after read
            return result;
        }

        /// <summary>
        /// Vérifie si le bouton de pause est actuellement pressé
        /// </summary>
        public bool IsPausing()
        {
            bool result = isPausing;
            isPausing = false; // Reset after read
            return result;
        }

        #endregion
    }
}
