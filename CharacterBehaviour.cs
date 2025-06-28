using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrawlAnything.Core;
using BrawlAnything.Character;

namespace BrawlAnything.Character
{
    /// <summary>
    /// Classe qui définit le comportement d'un personnage dans le jeu
    /// </summary>
    public class CharacterBehaviour : MonoBehaviour
    {
        [Header("Character Properties")]
        [SerializeField] private int characterId;
        [SerializeField] private string characterName;
        [SerializeField] private float health = 100f;
        [SerializeField] private float attackPower = 10f;
        [SerializeField] private float defenseValue = 5f;
        [SerializeField] private float moveSpeed = 3f;
        
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string idleAnimationName = "Idle";
        [SerializeField] private string attackAnimationName = "Attack";
        [SerializeField] private string hitAnimationName = "Hit";
        [SerializeField] private string deathAnimationName = "Death";
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private ParticleSystem attackEffect;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip deathSound;
        
        // État du personnage
        private bool isAlive = true;
        private bool isAttacking = false;
        private float currentHealth;
        private Vector3 moveDirection;
        
        // Références
        private InputManager inputManager;
        private CharacterManager characterManager;
        
        // Événements
        public event Action<float> OnHealthChanged;
        public event Action OnCharacterDeath;
        public event Action<int> OnDamageDealt;
        
        private void Awake()
        {
            // Initialiser la santé
            currentHealth = health;
            
            // Obtenir les références
            inputManager = InputManager.Instance;
            characterManager = FindObjectOfType<CharacterManager>();
            
            // Vérifier l'animator
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }
        
        private void Start()
        {
            // S'abonner aux événements d'entrée si c'est le personnage du joueur
            if (IsPlayerCharacter())
            {
                if (inputManager != null)
                {
                    inputManager.OnAttack += HandleAttackInput;
                    inputManager.OnMove += HandleMoveInput;
                }
            }
            
            // S'abonner à l'événement système pour les événements de combat
            EventSystem.Instance.Subscribe("combat_event", OnCombatEvent);
        }
        
        private void OnDestroy()
        {
            // Se désabonner des événements d'entrée
            if (inputManager != null && IsPlayerCharacter())
            {
                inputManager.OnAttack -= HandleAttackInput;
                inputManager.OnMove -= HandleMoveInput;
            }
            
            // Se désabonner de l'événement système
            if (EventSystem.Instance != null)
            {
                EventSystem.Instance.Unsubscribe("combat_event", OnCombatEvent);
            }
        }
        
        private void Update()
        {
            if (!isAlive)
                return;
                
            // Déplacer le personnage
            if (moveDirection.magnitude > 0.1f)
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
                
                // Faire face à la direction du mouvement
                transform.forward = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
                
                // Jouer l'animation de marche
                if (animator != null)
                {
                    animator.SetFloat("Speed", moveDirection.magnitude);
                }
            }
            else
            {
                // Jouer l'animation d'idle
                if (animator != null)
                {
                    animator.SetFloat("Speed", 0);
                }
            }
        }
        
        private void HandleAttackInput()
        {
            if (!isAlive || isAttacking)
                return;
                
            StartCoroutine(PerformAttack());
        }
        
        private void HandleMoveInput(Vector2 direction)
        {
            if (!isAlive)
                return;
                
            moveDirection = new Vector3(direction.x, 0, direction.y);
        }
        
        private void OnCombatEvent(object data)
        {
            if (data is Dictionary<string, object> eventData)
            {
                if (eventData.TryGetValue("type", out object typeObj) && typeObj is string type)
                {
                    if (type == "damage" && eventData.TryGetValue("target_id", out object targetIdObj) && targetIdObj is int targetId)
                    {
                        if (targetId == characterId)
                        {
                            if (eventData.TryGetValue("amount", out object amountObj) && amountObj is float amount)
                            {
                                TakeDamage(amount);
                            }
                        }
                    }
                }
            }
        }
        
        private IEnumerator PerformAttack()
        {
            isAttacking = true;
            
            // Jouer l'animation d'attaque
            if (animator != null)
            {
                animator.SetTrigger(attackAnimationName);
            }
            
            // Jouer l'effet d'attaque
            if (attackEffect != null)
            {
                attackEffect.Play();
            }
            
            // Jouer le son d'attaque
            if (audioSource != null && attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            
            // Attendre que l'animation se termine
            yield return new WaitForSeconds(0.5f);
            
            // Détecter les cibles dans la portée
            Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward, 1.5f);
            foreach (var hitCollider in hitColliders)
            {
                CharacterBehaviour target = hitCollider.GetComponent<CharacterBehaviour>();
                if (target != null && target != this)
                {
                    // Calculer les dégâts
                    float damage = CalculateDamage();
                    
                    // Notifier la cible via le système d'événements
                    Dictionary<string, object> damageEvent = new Dictionary<string, object>
                    {
                        { "type", "damage" },
                        { "source_id", characterId },
                        { "target_id", target.characterId },
                        { "amount", damage }
                    };
                    
                    EventSystem.Instance.TriggerEvent("combat_event", damageEvent);
                    
                    // Notifier les écouteurs locaux
                    OnDamageDealt?.Invoke((int)damage);
                }
            }
            
            isAttacking = false;
        }
        
        public void TakeDamage(float amount)
        {
            if (!isAlive)
                return;
                
            // Calculer les dégâts réels en tenant compte de la défense
            float actualDamage = Mathf.Max(0, amount - defenseValue);
            
            // Appliquer les dégâts
            currentHealth -= actualDamage;
            
            // Notifier les écouteurs
            OnHealthChanged?.Invoke(currentHealth / health);
            
            // Jouer l'animation de dégâts
            if (animator != null)
            {
                animator.SetTrigger(hitAnimationName);
            }
            
            // Jouer l'effet de dégâts
            if (hitEffect != null)
            {
                hitEffect.Play();
            }
            
            // Jouer le son de dégâts
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
            
            // Vérifier si le personnage est mort
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        private void Die()
        {
            isAlive = false;
            currentHealth = 0;
            
            // Jouer l'animation de mort
            if (animator != null)
            {
                animator.SetTrigger(deathAnimationName);
            }
            
            // Jouer le son de mort
            if (audioSource != null && deathSound != null)
            {
                audioSource.PlayOneShot(deathSound);
            }
            
            // Notifier les écouteurs
            OnCharacterDeath?.Invoke();
            
            // Notifier le gestionnaire de personnages
            if (characterManager != null)
            {
                characterManager.OnCharacterDeath(characterId);
            }
        }
        
        private float CalculateDamage()
        {
            // Formule de base pour les dégâts
            float baseDamage = attackPower;
            
            // Ajouter une variation aléatoire
            float randomFactor = UnityEngine.Random.Range(0.8f, 1.2f);
            
            return baseDamage * randomFactor;
        }
        
        public bool IsPlayerCharacter()
        {
            // Logique pour déterminer si c'est le personnage du joueur
            // Cela pourrait être basé sur un tag, une couche, ou une propriété
            return CompareTag("Player");
        }
        
        public int GetCharacterId()
        {
            return characterId;
        }
        
        public string GetCharacterName()
        {
            return characterName;
        }
        
        public float GetHealthPercentage()
        {
            return currentHealth / health;
        }
    }
}
