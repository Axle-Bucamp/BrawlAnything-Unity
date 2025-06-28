# Guide d'installation et de configuration - Brawl Anything

Ce guide vous explique comment configurer et installer correctement le projet "Brawl Anything" dans Unity, en mettant en place tous les composants nécessaires pour le développement et les tests.

## Prérequis

- Unity 6000.045f
- Firebase SDK pour Unity
- AR Foundation package
- Newtonsoft.Json pour Unity
- TextMeshPro package
- Un appareil mobile compatible AR (Android ou iOS)

## 1. Configuration initiale du projet Unity

### 1.1 Création du projet

1. Ouvrez Unity Hub et cliquez sur "Nouveau projet"
2. Sélectionnez le template "3D (URP)" pour bénéficier du Universal Render Pipeline
3. Nommez votre projet "BrawlAnything"
4. Définissez l'emplacement du projet
5. Cliquez sur "Créer"

### 1.2 Installation des packages requis

1. Ouvrez le gestionnaire de packages (Window > Package Manager)
2. Installez les packages suivants depuis le registre Unity :
   - AR Foundation
   - ARCore XR Plugin (pour Android)
   - ARKit XR Plugin (pour iOS)
   - TextMeshPro
   - Universal RP
   - Input System

3. Pour installer Newtonsoft.Json :
   - Dans le Package Manager, cliquez sur "+" en haut à gauche
   - Sélectionnez "Add package from git URL..."
   - Entrez : `com.unity.nuget.newtonsoft-json`
   - Cliquez sur "Ajouter"

### 1.3 Configuration de Firebase

1. Créez un projet Firebase sur la console Firebase (https://console.firebase.google.com/)
2. Ajoutez une application Android et/ou iOS à votre projet Firebase
3. Téléchargez le SDK Firebase pour Unity depuis https://firebase.google.com/docs/unity/setup
4. Importez le package Firebase dans votre projet Unity
5. Placez le fichier de configuration Firebase (google-services.json pour Android ou GoogleService-Info.plist pour iOS) dans le dossier Assets de votre projet

## 2. Structure des dossiers

Créez la structure de dossiers suivante dans votre projet :

```
Assets/
├── Animations/
├── Materials/
├── Models/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Core/
│   ├── AR/
│   ├── Networking/
│   ├── Character/
│   ├── Combat/
│   ├── UI/
│   └── Utils/
├── StreamingAssets/
├── Textures/
└── ThirdParty/
```

## 3. Configuration de la scène principale

1. Créez une nouvelle scène (File > New Scene)
2. Enregistrez-la sous "MainScene" dans le dossier Scenes
3. Configurez la hiérarchie des objets selon la structure décrite dans le document d'arborescence des objets de jeu

### 3.1 Configuration des Managers

1. Créez un objet vide nommé "Managers" à la racine de la scène
2. Ajoutez les objets enfants suivants, chacun avec son script correspondant :
   - GameManager (GameManager.cs)
   - ARManager (ARManager.cs)
   - UIManager (UIManager.cs)
   - CharacterManager (CharacterManager.cs)
   - AnalyticsManager (AnalyticsManager.cs)
   - FeedbackManager (FeedbackManager.cs)
   - PhaseRolloutManager (PhaseRolloutManager.cs)
   - APIClient (APIClient.cs)
   - NetworkOptimizer (NetworkOptimizer.cs)

### 3.2 Configuration de l'environnement AR

1. Créez un objet vide nommé "AR Environment" à la racine de la scène
2. Ajoutez un objet ARSession avec le composant ARSession
3. Ajoutez un objet ARSessionOrigin avec le composant ARSessionOrigin
4. Sous ARSessionOrigin, ajoutez une caméra AR avec les composants suivants :
   - Camera
   - AR Camera Manager
   - AR Camera Background
5. Ajoutez un objet ARBattleEnvironment avec le script ARBattleEnvironment.cs
6. Configurez les préfabriqués nécessaires pour ARBattleEnvironment :
   - ArenaIndicatorPrefab
   - ArenaPrefab

### 3.3 Configuration des Characters

1. Créez un objet vide nommé "Characters" à la racine de la scène
2. Ajoutez un objet CharacterContainer
3. Ajoutez un objet ModelViewer avec le script ModelViewer.cs

### 3.4 Configuration de l'UI

1. Créez un Canvas (UI > Canvas) nommé "UI Canvas" avec le mode "Screen Space - Overlay"
2. Ajoutez les panneaux suivants comme enfants du Canvas :
   - LoginPanel
   - CharacterSelectionPanel
   - CharacterCreationPanel
   - ModelSelectionPanel
   - BattlePanel
   - LoadingPanel
   - FeedbackPanel
   - ConfirmationPanel
3. Créez un second Canvas nommé "AR UI Canvas" avec le mode "World Space"
4. Ajoutez les éléments suivants comme enfants du AR UI Canvas :
   - PlacementIndicator
   - CharacterLabels
   - BattleUI

### 3.5 Configuration des Systems

1. Créez un objet vide nommé "Systems" à la racine de la scène
2. Ajoutez les objets suivants, chacun avec son script correspondant :
   - CameraCapture (CameraCapture.cs)
   - ImageProcessor (ImageProcessor.cs)
   - SecurityManager (SecurityManager.cs)
   - LeaderboardManager (LeaderboardManager.cs)
   - SpectatorManager (SpectatorManager.cs)
   - BackendCommunicator (BackendCommunicator.cs)

## 4. Configuration des références entre scripts

Pour chaque script, vous devez configurer les références aux autres composants. Voici les principales références à configurer :

### 4.1 GameManager

- ARManager : Glissez l'objet ARManager depuis la hiérarchie vers le champ correspondant dans l'inspecteur
- UIManager : Glissez l'objet UIManager depuis la hiérarchie vers le champ correspondant dans l'inspecteur

### 4.2 ARManager

- ARSession : Glissez l'objet ARSession depuis la hiérarchie
- ARSessionOrigin : Glissez l'objet ARSessionOrigin depuis la hiérarchie
- ARRaycastManager : Ce composant doit être ajouté à l'objet ARSessionOrigin
- ARPlaneManager : Ce composant doit être ajouté à l'objet ARSessionOrigin
- PlacementIndicator : Glissez le préfabriqué ou l'objet PlacementIndicator
- CharacterPrefab : Glissez le préfabriqué de personnage par défaut

### 4.3 UIManager

Configurez tous les panneaux et éléments d'interface utilisateur en glissant les objets correspondants depuis la hiérarchie vers les champs de l'inspecteur.

### 4.4 CharacterManager

- CharacterContainer : Glissez l'objet CharacterContainer depuis la hiérarchie
- DefaultCharacterPrefab : Glissez le préfabriqué de personnage par défaut

### 4.5 ARBattleEnvironment

- ARSession : Glissez l'objet ARSession depuis la hiérarchie
- ARPlaneManager : Glissez le composant ARPlaneManager depuis ARSessionOrigin
- ARRaycastManager : Glissez le composant ARRaycastManager depuis ARSessionOrigin
- ArenaIndicatorPrefab : Glissez le préfabriqué d'indicateur d'arène
- ArenaPrefab : Glissez le préfabriqué d'arène
- PlayerSpawnPoint : Créez et glissez un Transform pour le point d'apparition du joueur
- OpponentSpawnPoint : Créez et glissez un Transform pour le point d'apparition de l'adversaire

## 5. Configuration des préfabriqués

### 5.1 Création du préfabriqué CharacterItemPrefab

1. Créez un objet UI > Button
2. Ajoutez un composant Image pour l'aperçu du personnage
3. Ajoutez un composant TextMeshProUGUI pour le nom du personnage
4. Ajoutez le script CharacterCard.cs
5. Configurez les références dans l'inspecteur
6. Faites glisser l'objet dans le dossier Prefabs pour créer un préfabriqué

### 5.2 Création du préfabriqué ModelItemPrefab

Suivez une procédure similaire à celle du CharacterItemPrefab.

### 5.3 Création du préfabriqué ArenaIndicatorPrefab

1. Créez un objet vide
2. Ajoutez un Quad comme enfant et positionnez-le horizontalement
3. Appliquez un matériau semi-transparent avec une texture d'indicateur
4. Faites glisser l'objet dans le dossier Prefabs

### 5.4 Création du préfabriqué ArenaPrefab

1. Créez un objet vide
2. Ajoutez un Plane comme enfant pour le sol de l'arène
3. Ajoutez des objets pour les limites de l'arène
4. Ajoutez des points d'apparition pour le joueur et l'adversaire
5. Faites glisser l'objet dans le dossier Prefabs

### 5.5 Création du préfabriqué DefaultCharacterPrefab

1. Créez un objet vide
2. Ajoutez un modèle 3D simple comme enfant
3. Ajoutez un Animator pour les animations
4. Ajoutez des colliders pour la détection des coups
5. Faites glisser l'objet dans le dossier Prefabs

## 6. Configuration pour les plateformes mobiles

### 6.1 Configuration Android

1. Ouvrez File > Build Settings et sélectionnez Android
2. Cliquez sur "Switch Platform"
3. Ouvrez Player Settings et configurez :
   - Company Name et Product Name
   - Package Name (doit correspondre à celui de Firebase)
   - Minimum API Level (au moins Android 7.0 pour AR)
   - Target API Level (la plus récente recommandée)
4. Dans Other Settings, activez "Auto Graphics API" et "Multithreaded Rendering"
5. Dans XR Plug-in Management, activez ARCore

### 6.2 Configuration iOS

1. Ouvrez File > Build Settings et sélectionnez iOS
2. Cliquez sur "Switch Platform"
3. Ouvrez Player Settings et configurez :
   - Company Name et Product Name
   - Bundle Identifier (doit correspondre à celui de Firebase)
   - Target minimum iOS Version (au moins iOS 11.0 pour AR)
4. Dans Other Settings, configurez Camera Usage Description et Location Usage Description
5. Dans XR Plug-in Management, activez ARKit

## 7. Configuration du backend

### 7.1 Configuration de l'APIClient

1. Ouvrez le script APIClient.cs
2. Configurez les URLs des services backend :
   - backendBaseUrl : URL de votre API backend
   - assetServiceBaseUrl : URL de votre service de génération de modèles

### 7.2 Configuration de Firebase

1. Ouvrez le script AnalyticsManager.cs
2. Configurez l'API key et le server URL pour votre projet Firebase

## 8. Tests et débogage

### 8.1 Test en éditeur

1. Assurez-vous que tous les scripts sont correctement attachés aux objets
2. Appuyez sur Play dans l'éditeur Unity
3. Vérifiez que l'interface utilisateur s'affiche correctement
4. Vérifiez les logs pour détecter d'éventuelles erreurs

### 8.2 Test sur appareil

1. Connectez votre appareil mobile à votre ordinateur
2. Dans Build Settings, cliquez sur "Build and Run"
3. Testez les fonctionnalités AR sur l'appareil
4. Vérifiez que la détection des plans fonctionne correctement
5. Testez le placement des personnages et les combats

## 9. Résolution des problèmes courants

### 9.1 Problèmes AR

- **Les plans ne sont pas détectés** : Vérifiez que ARPlaneManager est activé et correctement configuré
- **Les objets AR ne s'affichent pas** : Vérifiez que la caméra AR est correctement configurée

### 9.2 Problèmes de scripts

- **Erreurs de référence null** : Vérifiez que toutes les références sont correctement assignées dans l'inspecteur
- **Erreurs de compilation** : Vérifiez les namespaces et les imports

### 9.3 Problèmes de backend

- **Échec de connexion au backend** : Vérifiez les URLs et les paramètres de connexion
- **Échec d'authentification** : Vérifiez les clés API et les tokens

## 10. Ressources supplémentaires

- Documentation Unity AR Foundation : https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.1/manual/index.html
- Documentation Firebase pour Unity : https://firebase.google.com/docs/unity/setup
- Forums Unity : https://forum.unity.com/
- Documentation API Brawl Anything : [URL de votre documentation API]
