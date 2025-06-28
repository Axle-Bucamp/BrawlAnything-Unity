# Liste des corrections nécessaires pour les scripts Unity

Après analyse approfondie des scripts fournis pour le jeu "Brawl Anything", voici les problèmes identifiés et les corrections à apporter :

## Problèmes de namespace

1. **Incohérence dans les namespaces** : Certains scripts utilisent des namespaces différents pour des fonctionnalités similaires.
   - Correction : Standardiser les namespaces selon la structure définie dans le document d'architecture.

2. **Référence à `BrawlAnything.Camera` dans ARManager.cs** : Ce namespace est utilisé mais n'est pas cohérent avec la structure définie.
   - Correction : Remplacer par le namespace approprié ou créer ce namespace s'il est nécessaire.

3. **Utilisation de `static BrawlAnything.Models.CharacterManager` dans UIManager.cs** : Cette référence statique crée une dépendance circulaire.
   - Correction : Restructurer pour éviter cette dépendance circulaire.

## Classes manquantes

1. **CharacterBehaviour** : Cette classe est référencée dans UIManager.cs mais est définie à l'intérieur de CharacterManager.cs.
   - Correction : Déplacer CharacterBehaviour dans son propre fichier pour une meilleure organisation.

2. **CharacterCard** : Similaire à CharacterBehaviour, cette classe est définie à l'intérieur de CharacterManager.cs.
   - Correction : Déplacer dans son propre fichier.

3. **SceneController** : Mentionné dans l'architecture mais non implémenté dans les scripts fournis.
   - Correction : Créer cette classe selon les spécifications de l'architecture.

4. **InputManager** : Mentionné dans l'architecture mais non implémenté.
   - Correction : Créer cette classe.

5. **EventSystem** : Mentionné dans l'architecture mais non implémenté.
   - Correction : Créer cette classe pour la communication entre modules.

## Références incorrectes entre scripts

1. **FeedbackManagerUI vs FeedbackManager** : Dans FeedbackUI.cs, la classe est nommée `FeedbackManagerUI` mais est référencée comme `FeedbackUI` dans FeedbackManager.cs.
   - Correction : Harmoniser les noms et les références.

2. **Problème dans APIClient.cs** : La méthode `PostJsonData` utilise une URL concaténée incorrectement.
   - Correction : Modifier la ligne `using (UnityWebRequest request = UnityWebRequest.Post($"{url}/feedback", form))` pour utiliser l'URL correctement.

3. **Problème dans ModelViewer.cs** : La coroutine `LoadModelCoroutine` retourne `request.result` à la fin, ce qui n'est pas correct pour un IEnumerator.
   - Correction : Remplacer par `yield break;` ou supprimer cette ligne.

4. **Problème dans FeedbackManager.cs** : Utilisation de `JsonUtility.FromJson<Dictionary<string, object>>` qui n'est pas supporté par JsonUtility.
   - Correction : Utiliser Newtonsoft.Json ou une autre méthode pour désérialiser le dictionnaire.

## Incohérences avec l'architecture décrite

1. **Structure des dossiers** : L'architecture décrit une structure de dossiers spécifique qui n'est pas reflétée dans l'organisation actuelle des scripts.
   - Correction : Réorganiser les scripts selon la structure définie.

2. **Modules manquants** : Certains modules décrits dans l'architecture ne sont pas implémentés.
   - Correction : Implémenter les modules manquants ou ajuster l'architecture.

3. **Patterns de conception** : Certains patterns mentionnés dans l'architecture ne sont pas correctement implémentés.
   - Correction : Revoir l'implémentation des patterns de conception.

## Problèmes spécifiques à corriger

1. **FeedbackUI.cs** : 
   - Renommer la classe en `FeedbackUI` au lieu de `FeedbackManagerUI` pour être cohérent avec les références.
   - Implémenter correctement l'événement `HandleFeedbackSubmission` pour qu'il puisse être utilisé par FeedbackManager.

2. **APIClient.cs** :
   - Corriger la méthode `PostJsonData` pour utiliser l'URL correctement.
   - Ajouter une méthode de désérialisation correcte pour les dictionnaires.

3. **ModelViewer.cs** :
   - Corriger la fin de la coroutine `LoadModelCoroutine`.

4. **CharacterManager.cs** :
   - Déplacer les classes `CharacterBehaviour` et `CharacterCard` dans leurs propres fichiers.
   - Corriger les références circulaires.

5. **Classes manquantes à créer** :
   - SceneController.cs
   - InputManager.cs
   - EventSystem.cs
   - Autres classes mentionnées dans l'architecture mais non implémentées.

Ces corrections permettront d'assurer la cohérence et le bon fonctionnement de l'application selon l'architecture définie.
