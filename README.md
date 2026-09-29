# Meowtel — Cat Hotel

Jeu mobile free-to-play de gestion d'un **hôtel pour chats**, entre pension et refuge : accueillir des
pensionnaires, veiller à leurs besoins et à leur bonheur, décorer, monter en réputation et débloquer
des étages.

- **Google Play** : [com.royalpourceaustudios.meowtel](https://play.google.com/store/apps/details?id=com.royalpourceaustudios.meowtel)
- **Statut** : publié en mai 2026, version **0.35** (`versionCode` 35)
- **Éditeur** : Poiesis Interactive. Le package garde l'ancien nom de studio (Royal Pourceau Studios)
  parce que Google Play ne permet pas de changer l'identifiant d'une application publiée (voir #1).

## Stack

| Élément | Version / détail |
|---|---|
| Moteur | Unity **6000.3.5f2**, 2D URP 17.3 |
| Entrées | Input System 1.17 |
| Contenu | Addressables 2.8.1 (races de chats) |
| Services | Unity Gaming Services : Authentication, Cloud Save (Analytics, Remote Config et Cloud Code installés) |
| Publicité | Unity LevelPlay 9.4.1 (rewarded) |
| Connexion | Google Play Games 2.1.0 |
| Android | IL2CPP, ARM64, minSdk 25, targetSdk 35, paysage |
| Langues | français (défaut), anglais, allemand, espagnol, portugais |

## Ouvrir le projet

1. Installer **Unity 6000.3.5f2** avec le module *Android Build Support* (OpenJDK, SDK et NDK).
2. Ouvrir le dossier du dépôt dans Unity Hub.
3. Lier le projet à son projet Unity Gaming Services (*Edit > Project Settings > Services*).
4. Scène de départ : `Assets/_Project/Scenes/_Test/Boot.unity`, qui charge ensuite `Proto.unity`
   (la scène de jeu).

## Arborescence

```text
Assets/
├── _Project/
│   ├── Scripts/        # code du jeu, namespaces CatHotel.*
│   │   ├── Boot/       # démarrage, menu principal
│   │   ├── Core/       # données et utilitaires : GameConfig, LocalizedStrings, SaveManager, BreedRegistry
│   │   ├── Grid/       # grille, pièces, rendu des sols et des murs (GridRenderer)
│   │   ├── Input/      # caméra, saisie de construction
│   │   ├── Audio/      # sons des chats et de l'UI, musique
│   │   ├── Shop/       # articles de boutique (ShopItemData)
│   │   ├── Hotel/      # simulation : HotelManager, réputation, étages, placement d'objets
│   │   ├── Cats/       # IA et état des chats : besoins, bonheur, affinités
│   │   ├── Economy/    # pièces, monnaie premium (Puurls), pièces flottantes
│   │   ├── Services/   # pubs (LevelPlay), auth, sauvegarde cloud, consentement
│   │   ├── UI/         # HUD et panneaux
│   │   ├── Tutorial/   # tutoriel et narration (Jasper, Winston)
│   │   └── Editor/     # outils du menu « Cat Hotel/… »
│   ├── Data/           # ScriptableObjects : GameConfig, races, objets, tutoriel
│   ├── Art/            # sprites intégrés (chats, objets, environnement, UI)
│   ├── Audio/          # musique et effets
│   └── Scenes/_Test/   # Boot.unity, Proto.unity (scènes du build)
├── GRAPH/              # livraisons brutes de l'art (toutes ne sont pas encore intégrées)
└── LevelPlay/, GooglePlayGames/, Plugins/   # SDK tiers
docs/                   # GDD, UI/UX, jalons, narration, art, animation, son, privacy policy
CloudCode/              # modules Cloud Code (vides pour l'instant)
```

## Outils éditeur

Menu **Cat Hotel** : configuration des Addressables, banques de sons, prefab de l'écran de chargement,
assets du tutoriel, configuration du déblocage des étages, zone de sécurité (*safe area*),
correction d'import des sprites de léchage, optimisation des textures, testeur de sons, débogage
(tentative de déblocage de l'étage suivant en Play Mode, soumise aux conditions de réputation et de pièces)
et fenêtre d'aide.

> ⚠️ **Ne pas lancer `Cat Hotel/Scene/Setup Proto Scene`.** Ce script reconstruit `Proto.unity` de zéro
> et écraserait toutes les retouches faites à la main depuis sa dernière exécution.

## Contenu et localisation

- **Races de chats** : `Assets/_Project/Data/Breeds/Breed_*.asset`, chargées par Addressables
  (l'adresse est le nom du fichier).
- **Objets** : `Assets/_Project/Data/Objects/Obj_*.asset` (`HotelObjectData`).
- **Réglages** : `Assets/_Project/Data/GameConfig.asset`, `Assets/_Project/AdConfig.asset`.
- **Textes** : tout texte visible passe par une clé de `Assets/_Project/Scripts/Core/LocalizedStrings.cs`,
  renseignée dans les **5 langues**.
- **Sauvegardes** : un nouveau champ de sauvegarde doit avoir une valeur par défaut ; ne jamais renommer
  ni supprimer un champ existant, pour que les parties des joueurs restent lisibles.

## Build Android et publication

1. Incrémenter `bundleVersion` et `versionCode` (*Player Settings > Other Settings*).
2. Vérifier le niveau d'API cible exigé par Google Play au moment de l'envoi.
3. Addressables : le projet est réglé pour **ne pas** les construire avec le build
   (*Build Addressables on Player Build* = *Do not Build Addressables content on Player build*) ;
   les construire avant chaque build (*Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script*).
4. *File > Build Profiles > Android*, format **App Bundle (AAB)**, signé avec la clé d'upload.
   Les fichiers de signature et les identifiants de service ne doivent pas être versionnés.
5. Envoyer l'AAB sur la piste de **test interne**, vérifier sur un appareil, puis promouvoir en production.
6. Taguer le commit publié (`vX.Y.0`).
7. Si un SDK a été ajouté ou retiré, mettre à jour `docs/privacy-policy.html` et le formulaire
   *Sécurité des données* de la Play Console (#4).

## Documentation

| Document | Contenu |
|---|---|
| [docs/gdd.md](docs/gdd.md) | document de conception (intention de design ; le jeu publié s'en écarte sur plusieurs points) |
| [docs/ui-ux.md](docs/ui-ux.md) | écrans, flux, économie F2P |
| [docs/jalons.md](docs/jalons.md) | jalons de production J1 à J3 |
| [docs/narration.md](docs/narration.md) | personnages et textes narratifs |
| [docs/2d-art.md](docs/2d-art.md), [docs/2d-animation.md](docs/2d-animation.md) | listes d'assets graphiques et d'animations |
| [docs/sound-design.md](docs/sound-design.md) | liste des assets audio |
| [docs/privacy-policy.html](docs/privacy-policy.html) | politique de confidentialité |

## Travailler avec Claude Code

Le skill **`meowtel`** du plugin interne `poiesis-skills` porte l'état réel du jeu (architecture,
économie, assets inexploités, bugs connus) et la roadmap en cours. Voir aussi [CLAUDE.md](CLAUDE.md).

## Licence

Aucune licence n'est accordée pour l'instant : code et assets sont sous tous droits réservés (#3).
