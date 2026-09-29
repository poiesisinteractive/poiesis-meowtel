# CLAUDE.md — Meowtel

Jeu mobile Unity publié sur Google Play. Présentation, ouverture du projet et build : [README.md](README.md).

## Avant toute tâche

Charger le skill **`poiesis-skills:meowtel`** (plugin `poiesis-skills`). Il porte l'état réel du jeu
(qui diffère souvent de `docs/gdd.md`), les bugs connus et la roadmap en cours, dont les identifiants de
tâche (`S1.2`…) sont à reprendre dans les branches, PR et commits.

## Règles

- **Ne jamais lancer `Cat Hotel/Scene/Setup Proto Scene`** : il reconstruit `Proto.unity` et écrase
  les retouches manuelles.
- **Nouvel écran** : un outil éditeur additif qui génère un prefab (modèle :
  `Assets/_Project/Scripts/Editor/LoadingScreenPrefabBuilder.cs`). **Contenu en série** : un outil
  `Cat Hotel/Content/…` qui crée les ScriptableObjects, plutôt que d'écrire des `.meta` à la main.
- **Textes visibles** : une clé dans `Assets/_Project/Scripts/Core/LocalizedStrings.cs`, dans les
  5 langues (fr, en, de, es, pt).
- **Sauvegarde** : nouveaux champs avec une valeur par défaut ; ne jamais renommer ni supprimer un champ.
- **SDK de plateforme** sous `#if UNITY_ANDROID` / `UNITY_WEBGL` / `UNITY_IOS`.
- **Unity ne tourne pas dans les sessions cloud** : vérifier chaque modification de code avec
  `Tools/TypeCheck/typecheck.sh -c all` (compilation sans Unity, voir son README ; SDK .NET 8 requis), et
  lister dans chaque PR les fichiers modifiés, les menus éditeur à exécuter et les étapes de test (Play Mode,
  appareil Android). Une compilation OK n'est pas un test : ne jamais présenter comme testé du code qui n'a
  pas été exécuté.
- **Dépôt public** : aucune valeur de secret, aucun identifiant de service, aucun chiffre de revenus.
- **Package id immuable** : `com.royalpourceaustudios.meowtel`.
