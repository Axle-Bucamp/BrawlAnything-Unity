# Arborescence des objets de jeu - Brawl Anything

Ce document décrit la structure hiérarchique des objets de jeu (GameObjects) pour l'application "Brawl Anything". Cette structure est conçue pour faciliter l'organisation des composants, la communication entre les différents systèmes, et assurer une expérience AR fluide.

## Structure principale

```
BrawlAnything (Scene Root)
├── --- Managers ---
│   ├── GameManager
│   ├── ARManager
│   ├── UIManager
│   ├── CharacterManager
│   ├── AnalyticsManager
│   ├── FeedbackManager
│   ├── PhaseRolloutManager
│   ├── APIClient
│   └── NetworkOptimizer
│
├── --- AR Environment ---
│   ├── ARSession
│   ├── ARSessionOrigin
│   │   ├── AR Camera
│   │   │   ├── Directional Light
│   │   │   └── Camera Effects
│   │   └── Trackables
│   │       ├── AR Planes
│   │       └── AR Points
│   ├── ARBattleEnvironment
│   │   ├── ArenaIndicator
│   │   ├── BattleArena
│   │   │   ├── PlayerSpawnPoint
│   │   │   ├── OpponentSpawnPoint
│   │   │   └── BoundaryVisualizer
│   │   └── BattleEffects
│   └── SharedARExperience
│
├── --- Characters ---
│   ├── CharacterContainer
│   │   ├── Player_Character
│   │   │   ├── Model
│   │   │   ├── Animations
│   │   │   ├── Effects
│   │   │   └── Colliders
│   │   └── Opponent_Character
│   │       ├── Model
│   │       ├── Animations
│   │       ├── Effects
│   │       └── Colliders
│   └── ModelViewer
│
├── --- UI ---
│   ├── Canvas (Screen Space - Overlay)
│   │   ├── LoginPanel
│   │   ├── CharacterSelectionPanel
│   │   │   └── CharacterListContainer
│   │   ├── CharacterCreationPanel
│   │   ├── ModelSelectionPanel
│   │   │   └── ModelListContainer
│   │   ├── BattlePanel
│   │   ├── LoadingPanel
│   │   ├── FeedbackPanel
│   │   └── ConfirmationPanel
│   └── AR UI Canvas (World Space)
│       ├── PlacementIndicator
│       ├── CharacterLabels
│       └── BattleUI
│
└── --- Systems ---
    ├── CameraCapture
    ├── ImageProcessor
    ├── SecurityManager
    ├── LeaderboardManager
    ├── SpectatorManager
    └── BackendCommunicator
```

## Description des composants principaux

### Managers

#### GameManager
- **Rôle** : Coordonne tous les aspects du jeu et gère l'état global
- **Composants** :
  - GameManager.cs
- **Dépendances** :
  - ARManager
  - UIManager

#### ARManager
- **Rôle** : Gère la session AR et le placement des objets dans l'espace AR
- **Composants** :
  - ARManager.cs
  - ARSession
  - ARSessionOrigin
  - ARRaycastManager
  - ARPlaneManager
- **Dépendances** :
  - ARSession
  - ARSessionOrigin

#### UIManager
- **Rôle** : Gère l'interface utilisateur et les transitions entre les différents écrans
- **Composants** :
  - UIManager.cs
  - Canvas (Screen Space - Overlay)
- **Dépendances** :
  - Canvas
  - Panels

#### CharacterManager
- **Rôle** : Gère les personnages, leurs modèles et animations
- **Composants** :
  - CharacterManager.cs
  - CharacterContainer
- **Dépendances** :
  - APIClient
  - ModelViewer

### AR Environment

#### ARSession
- **Rôle** : Gère la session AR globale
- **Composants** :
  - ARSession component

#### ARSessionOrigin
- **Rôle** : Définit l'origine du système de coordonnées AR
- **Composants** :
  - ARSessionOrigin component
  - AR Camera
  - Trackables container

#### ARBattleEnvironment
- **Rôle** : Gère l'environnement de combat AR, y compris la détection des plans et le placement de l'arène
- **Composants** :
  - ARBattleEnvironment.cs
  - ArenaIndicator
  - BattleArena
- **Dépendances** :
  - ARPlaneManager
  - ARRaycastManager

### Characters

#### CharacterContainer
- **Rôle** : Contient tous les personnages instanciés
- **Enfants** :
  - Player_Character
  - Opponent_Character

#### ModelViewer
- **Rôle** : Permet de visualiser et manipuler les modèles 3D
- **Composants** :
  - ModelViewer.cs
  - ModelContainer

### UI

#### Canvas (Screen Space - Overlay)
- **Rôle** : Contient tous les éléments d'interface utilisateur en mode overlay
- **Enfants** :
  - LoginPanel
  - CharacterSelectionPanel
  - CharacterCreationPanel
  - ModelSelectionPanel
  - BattlePanel
  - LoadingPanel
  - FeedbackPanel
  - ConfirmationPanel

#### AR UI Canvas (World Space)
- **Rôle** : Contient les éléments d'interface utilisateur dans l'espace AR
- **Enfants** :
  - PlacementIndicator
  - CharacterLabels
  - BattleUI

### Systems

#### CameraCapture
- **Rôle** : Gère la capture d'images pour la création de personnages
- **Composants** :
  - CameraCapture.cs

#### ImageProcessor
- **Rôle** : Traite les images capturées avant de les envoyer au backend
- **Composants** :
  - ImageProcessor.cs

## Relations entre les objets

### Hiérarchie de contrôle
1. **GameManager** est au sommet de la hiérarchie et coordonne tous les autres managers
2. **ARManager** gère l'environnement AR et communique avec GameManager
3. **UIManager** gère l'interface utilisateur et reçoit des instructions de GameManager
4. **CharacterManager** gère les personnages et communique avec GameManager et ARManager

### Flux de données
1. **CameraCapture** capture des images qui sont traitées par **ImageProcessor**
2. **APIClient** envoie les images au backend et reçoit les modèles 3D
3. **CharacterManager** instancie les modèles dans **CharacterContainer**
4. **ARBattleEnvironment** place les personnages dans l'arène de combat
5. **UIManager** affiche les informations de combat et les contrôles

## Configuration des préfabriqués

### Préfabriqués principaux
- **CharacterItemPrefab** : Utilisé dans le panneau de sélection de personnage
- **ModelItemPrefab** : Utilisé dans le panneau de sélection de modèle
- **ArenaIndicatorPrefab** : Indique où l'arène sera placée
- **ArenaPrefab** : L'arène de combat elle-même
- **DefaultCharacterPrefab** : Modèle de personnage par défaut

## Considérations importantes

1. **Ordre de chargement** : Les managers doivent être initialisés dans un ordre spécifique :
   - GameManager
   - ARManager
   - UIManager
   - CharacterManager
   - Autres managers

2. **Persistance** : Les objets suivants utilisent DontDestroyOnLoad pour persister entre les scènes :
   - GameManager
   - CharacterManager
   - APIClient
   - AnalyticsManager
   - PhaseRolloutManager

3. **Références entre scripts** : Utilisez SerializeField pour les références directes ou FindObjectOfType pour les références dynamiques, mais préférez les références directes pour de meilleures performances.

4. **Événements** : Utilisez le système d'événements pour la communication entre les composants plutôt que des références directes lorsque c'est possible.
