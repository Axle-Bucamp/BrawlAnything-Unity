# Documentation Technique - Brawl Anything

## Introduction

Cette documentation technique présente l'application "Brawl Anything", un jeu de combat en réalité augmentée qui permet aux utilisateurs de faire combattre des personnages générés à partir d'images capturées. Le document couvre l'architecture du projet, les corrections apportées aux scripts, la structure des objets de jeu, et un guide d'installation et de configuration.

## Table des matières

1. [Architecture du projet](#architecture-du-projet)
2. [Corrections apportées aux scripts](#corrections-apportées-aux-scripts)
3. [Arborescence des objets de jeu](#arborescence-des-objets-de-jeu)
4. [Guide d'installation et de configuration](#guide-dinstallation-et-de-configuration)
5. [Références](#références)

## Architecture du projet

"Brawl Anything" est une application Unity qui utilise la réalité augmentée pour permettre aux utilisateurs de créer et de faire combattre des personnages générés à partir d'images. L'architecture est modulaire et suit les principes de conception orientée objet.

### Structure du projet Unity

```
BrawlAnything/
├── Assets/
│   ├── Animations/       # Animations génériques et systèmes d'animation
│   ├── Materials/        # Matériaux et shaders
│   ├── Models/           # Modèles 3D par défaut et placeholders
│   ├── Prefabs/          # Préfabriqués pour les personnages, UI, etc.
│   ├── Scenes/           # Scènes du jeu (menu, combat, personnalisation)
│   ├── Scripts/          # Scripts C# (voir détail ci-dessous)
│   ├── StreamingAssets/  # Assets chargés dynamiquement
│   ├── Textures/         # Textures et sprites
│   └── ThirdParty/       # Plugins et assets tiers
└── ProjectSettings/      # Configuration du projet Unity
```

### Architecture des scripts

```
Scripts/
├── Core/                 # Fonctionnalités fondamentales
│   ├── GameManager.cs    # Gestion globale du jeu
│   ├── SceneController.cs # Gestion des transitions entre scènes
│   ├── InputManager.cs   # Système d'entrée unifié
│   └── EventSystem.cs    # Système d'événements pour la communication
├── AR/                   # Fonctionnalités de réalité augmentée
│   ├── ARManager.cs      # Gestion de la session AR
│   ├── ARBattleEnvironment.cs # Environnement de combat AR
│   ├── SharedARExperience.cs # Expérience AR partagée
│   └── CameraCapture.cs  # Capture d'images pour la création de personnages
├── Networking/           # Communication avec le backend
│   ├── APIClient.cs      # Client REST pour les appels API
│   ├── BackendCommunicator.cs # Communication avec le backend
│   ├── MultiplayerClient.cs # Client multijoueur
│   ├── NetworkOptimizer.cs # Optimisation réseau
│   └── SecurityManager.cs # Gestion de la sécurité
├── Character/            # Système de personnages
│   ├── CharacterManager.cs # Gestion des personnages
│   ├── CharacterBehaviour.cs # Comportement des personnages
│   ├── CharacterCard.cs  # Carte de personnage pour l'UI
│   └── ModelViewer.cs    # Visualisation des modèles 3D
├── Combat/               # Système de combat
│   ├── GameplayManager.cs # Gestion du gameplay
│   └── LeaderboardManager.cs # Gestion des classements
├── UI/                   # Interface utilisateur
│   ├── UIManager.cs      # Gestion globale de l'UI
│   ├── FeedbackUI.cs     # Interface de feedback
│   └── FeedbackManager.cs # Gestion des feedbacks
└── Utils/                # Utilitaires
    ├── ImageProcessor.cs # Traitement d'images
    ├── AnalyticsManager.cs # Analyse des données
    └── PhaseRolloutManager.cs # Gestion du déploiement par phases
```

### Modules principaux

1. **Module Core** : Gère les fonctionnalités fondamentales du jeu
2. **Module AR** : Gère l'intégration de la réalité augmentée
3. **Module Networking** : Gère la communication avec le backend
4. **Module Character** : Gère la création et l'animation des personnages
5. **Module Combat** : Gère le système de combat
6. **Module UI** : Gère l'interface utilisateur
7. **Module Utils** : Fournit des utilitaires divers

## Corrections apportées aux scripts

Après analyse des scripts fournis, plusieurs problèmes ont été identifiés et corrigés pour assurer la cohérence et le bon fonctionnement de l'application.

### Problèmes de namespace

1. **Incohérence dans les namespaces** : Standardisation des namespaces selon la structure définie dans le document d'architecture.
2. **Référence à `BrawlAnything.Camera` dans ARManager.cs** : Remplacement par le namespace approprié.
3. **Utilisation de `static BrawlAnything.Models.CharacterManager` dans UIManager.cs** : Restructuration pour éviter la dépendance circulaire.

### Classes manquantes

1. **CharacterBehaviour** : Déplacement dans son propre fichier pour une meilleure organisation.
2. **CharacterCard** : Déplacement dans son propre fichier.
3. **SceneController**, **InputManager**, **EventSystem** : Création de ces classes selon les spécifications de l'architecture.

### Références incorrectes entre scripts

1. **FeedbackManagerUI vs FeedbackManager** : Harmonisation des noms et des références.
2. **Problème dans APIClient.cs** : Correction de la concaténation d'URL.
3. **Problème dans ModelViewer.cs** : Correction de la coroutine `LoadModelCoroutine`.
4. **Problème dans FeedbackManager.cs** : Correction de la désérialisation JSON.

### Incohérences avec l'architecture décrite

1. **Structure des dossiers** : Réorganisation des scripts selon la structure définie.
2. **Modules manquants** : Implémentation des modules manquants.
3. **Patterns de conception** : Révision de l'implémentation des patterns de conception.

Pour plus de détails sur les corrections, voir le fichier [corrections.md](corrections.md).

## Arborescence des objets de jeu

L'arborescence des objets de jeu définit la structure hiérarchique des GameObjects dans l'application "Brawl Anything". Cette structure est conçue pour faciliter l'organisation des composants, la communication entre les différents systèmes, et assurer une expérience AR fluide.

### Structure principale

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

Pour plus de détails sur l'arborescence des objets de jeu, voir le fichier [game_object_hierarchy.md](game_object_hierarchy.md).

## Guide d'installation et de configuration

Le guide d'installation et de configuration explique comment configurer et installer correctement le projet "Brawl Anything" dans Unity, en mettant en place tous les composants nécessaires pour le développement et les tests.

### Prérequis

- Unity 6000.045f
- Firebase SDK pour Unity
- AR Foundation package
- Newtonsoft.Json pour Unity
- TextMeshPro package
- Un appareil mobile compatible AR (Android ou iOS)

### Étapes principales

1. **Configuration initiale du projet Unity**
   - Création du projet
   - Installation des packages requis
   - Configuration de Firebase

2. **Structure des dossiers**
   - Création de la structure de dossiers selon l'architecture

3. **Configuration de la scène principale**
   - Configuration des Managers
   - Configuration de l'environnement AR
   - Configuration des Characters
   - Configuration de l'UI
   - Configuration des Systems

4. **Configuration des références entre scripts**
   - Configuration des références pour chaque script

5. **Configuration des préfabriqués**
   - Création des préfabriqués nécessaires

6. **Configuration pour les plateformes mobiles**
   - Configuration Android
   - Configuration iOS

7. **Configuration du backend**
   - Configuration de l'APIClient
   - Configuration de Firebase

8. **Tests et débogage**
   - Test en éditeur
   - Test sur appareil

9. **Résolution des problèmes courants**
   - Problèmes AR
   - Problèmes de scripts
   - Problèmes de backend

Pour plus de détails sur l'installation et la configuration, voir le fichier [setup_guide.md](setup_guide.md).

## Références

- Documentation Unity AR Foundation : https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.1/manual/index.html
- Documentation Firebase pour Unity : https://firebase.google.com/docs/unity/setup
- Forums Unity : https://forum.unity.com/
- Documentation API Brawl Anything : [URL de votre documentation API]
