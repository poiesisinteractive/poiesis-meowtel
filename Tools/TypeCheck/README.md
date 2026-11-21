# TypeCheck — vérification de types du code du jeu sans Unity

Compile les scripts du jeu (`Assets/_Project/Scripts`, sur place) avec le SDK .NET, contre les
assemblies de référence UnityEngine et les **vraies sources** des packages Unity utilisés par le
projet. Sert aux sessions où Unity n'est pas disponible (Claude Code dans le cloud) : une erreur de
compilation est détectée avant d'arriver dans l'éditeur.

Ce n'est **pas** un remplacement de Unity : rien n'est exécuté, et une compilation OK ici ne prouve
pas que le jeu fonctionne. Tester dans l'éditeur et sur appareil reste obligatoire.

## Prérequis

- SDK .NET 8 (`dotnet --version`). Sur Ubuntu : `apt-get install -y dotnet-sdk-8.0`.
- Accès réseau à `api.nuget.org` et `github.com` au premier lancement (téléchargement dans `lib/`,
  ignoré par git).

## Utilisation

```bash
Tools/TypeCheck/typecheck.sh                 # Android + Editor (par défaut)
Tools/TypeCheck/typecheck.sh -c all          # Android, Editor, Dev, WebGL
Tools/TypeCheck/typecheck.sh --warnings      # affiche aussi les avertissements (API obsolètes)
HARNESS_EXTRA_DEFINES=ENABLE_UNITY_CONSENT Tools/TypeCheck/typecheck.sh   # branche Unity 6.2+ du consentement
HARNESS_EXTRA_SOURCES=Assets/_Project/Scripts/Editor/MonOutil.cs Tools/TypeCheck/typecheck.sh -c Editor
Tools/TypeCheck/selftest.sh                  # vérifie que le banc détecte bien les erreurs attendues
```

Sortie : les erreurs au format `fichier:ligne: error CSxxxx: message`, puis `TYPECHECK OK` ou
`TYPECHECK FAILED (N errors)`. Code de sortie : 0 OK, 1 erreurs de compilation, 2 problème du banc.

## Ce qui est compilé

| Élément | Source |
|---|---|
| Scripts du jeu (hors dossiers `Editor/`), Google Play Games, firstpass | le dépôt, sur place |
| UnityEngine | `UnityEngine.Modules` 2021.3.33 (NuGet), complété des membres Unity 6 par `tools/UnityRefPatcher` |
| uGUI, TextMesh Pro, Input System, Addressables, UGS (Core, Authentication, Cloud Save, Analytics), LevelPlay | sources réelles, téléchargées par `tools/fetch_packages.sh` (miroirs GitHub du registre Unity, SDK LevelPlay officiel) |
| URP, types Unity 6 absents de 2021.3, sous-ensemble d'UnityEditor, `UnityEngine.UnityConsent` | stubs écrits à la main (`stubs/`) |

Si les sources des packages ne peuvent pas être téléchargées, le banc bascule sur `stubs-fallback/`
(stubs écrits à la main, moins fidèles).

## Angles morts

- Les **stubs encodent des hypothèses** : les membres marqués `// GUESS` ou `// API-BELIEF` ne sont
  pas vérifiés (notamment l'écriture de `EndUserConsent`, Unity 6.2+).
- Les scripts des dossiers `Editor/` ne sont pas compilés (seulement à la demande via
  `HARNESS_EXTRA_SOURCES`, avec un sous-ensemble d'UnityEditor).
- LevelPlay est compilé en 9.4.0 (le projet utilise 9.4.1, non publié sur les miroirs).
- Versions à tenir alignées avec `Packages/packages-lock.json` en cas de mise à jour de package
  (`tools/fetch_packages.sh`).
