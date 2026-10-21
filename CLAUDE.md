# CLAUDE.md

Ce fichier guide Claude Code lorsqu'il travaille dans ce dépôt. Réponds et travaille **en français**.

## Le projet

Jeu d'horreur à la première personne réalisé en équipe pour une game jam (Horror Jam 2026).
Le joueur avance dans le noir avec un **zippo** comme seule source de lumière : quand la flamme
est éteinte trop longtemps, il perd. Des **crawlers** (nuée rendue par GPU instancing) fuient la
lumière, un **monstre** patrouille, entend les sons du joueur et le poursuit. Objectif : ramasser
des collectibles (lampes à huile, clés) et atteindre la zone de sortie.

- **Moteur** : Unity `6000.6.5f1`, URP 17.6, nouveau Input System (`InputSystem_Actions.inputactions`), AI Navigation.
- **Équipe** : Dorian (moi), Julien Rogel, Luckycio. Les commits et le code mélangent français et anglais.
- **Wwise** : des `.csproj` Wwise traînent à la racine mais **aucun fichier Wwise n'est versionné** ni utilisé. L'audio passe par `AudioSource` / `AudioClip`.
- Pas de ligne de commande de build ni de tests automatisés : tout se vérifie dans l'éditeur Unity (Play Mode).

## Structure des dossiers (`Assets/`)

| Dossier | Rôle |
|---|---|
| `CommonScripts/` | `GameManager` (boucle win/lose), `Common` (helpers `FindTypeInScene`, `FloatRange`), ambiance, pluie, flicker, `DebugDraw`, `NavMeshUtil` |
| `GameSettings/` | `GameSettings.cs` + un ScriptableObject de réglages par système dans `Scripts/SettingsScripts/`. Les assets sont dans `Resources/` et `Resources/Settings/` |
| `FirstPersonController/` | Joueur : `PlayerController` (physique, postures, checks sol/plafond), `MovementController`, `CameraController`, `ZippoController` + `ZippoFlameVisual`, `SoundController`, `CrawlerSoundController`, `ArmWallRetraction` |
| `Crawlers/` | `CrawlerManager` (nuée, KDTree, GPU instancing) et `CrawlerExit` |
| `Monster/` | `MonsterManager` (IA, blackboard, ouïe), `MonsterController`, `MonsterBehaviourState`, `EscapeZone` |
| `Interactibles/` | `InteractManager` + `InteractComponent` (objets ramassables, type `InteractType`) |
| `Porte/` | `Porte_Interactive` (porte à charnière, verrouillage) et `Key_Scripte` (clés) |
| `Rooms/` | `Room` + `RoomManager` statique (salles pour l'IA) |
| `Artillery/` | `ArtilleryShell` / `ArtillerySpawner` (bombardement d'ambiance) |
| `Menu_Scripts/` | `MenuPrincipal` (menu, écran de chargement) |
| `Renderring/` | `RenderTextureResolutionScaler` (rendu via RenderTexture) |
| `KDTree/` | Lib externe ViliWonka KDTree, ne pas modifier |
| `Scenes/` | `Scene_Menu`, `Scene_LVL_1`, `Scene_Interior`, `Scene_Demo` |
| `GamePrefabs/` | Prefabs des managers et du joueur (`PersonController`, `GameManager`, `MonsterManager`, `CrawlerManager`, etc.) |
| `InputManager.cs` | Accès statique aux `InputAction` (`InputManager.PlayerInteract`, `InputManager.ZippoLightAttempt`, etc.) |

## Conventions d'architecture

- **Réglages = ScriptableObjects.** Aucune valeur de tuning en dur dans un MonoBehaviour. Chaque système a son `XxxSettings` (modèle : `_Template_Settings.cs`), exposé via `GameSettings.Xxx` (propriété statique) et lu localement par `private XxxSettings Settings => GameSettings.Xxx;`. Pour un nouveau système : créer la classe, ajouter le champ + la propriété statique dans `GameSettings.cs`, créer l'asset et le référencer dans `Resources/GameSettings.asset`.
- **Managers = singletons implicites** (`private static Xxx INSTANCE;` dans `Awake`) avec des méthodes statiques publiques qui tolèrent l'absence du manager dans la scène (`if (INSTANCE != null)`), ex. `MonsterManager.SendSoundToMonsterHearing`, `GameManager.PlaySound`.
- **Références** résolues dans `Start()` via `Common.FindTypeInScene<T>()` plutôt que des champs sérialisés, sauf pour les assets (mesh, material, clips).
- **Feedback découplé par `UnityEvent`** câblés dans l'Inspector (`ZippoController.OnFlameLit`, `GameManager.OnLose`, etc.). Un nouveau son ou VFX s'accroche à un event existant plutôt que d'être codé en dur.
- **Inputs** : toujours via `InputManager`, s'abonner dans `OnEnable` et se désabonner dans `OnDisable`.
- **Sons du joueur** : `GameManager.PlaySound(clip)` (le monstre n'entend pas) ou `PlaySound(clip, volume, monsterLoudness)` (le monstre entend).
- Les sections d'un fichier sont séparées par des bannières `/// ---- PUBLIC FUNCTIONS / UNITY FUNCTIONS ----`. Respecter ce style dans les fichiers qui l'utilisent.

## Règles de collaboration

- **Ne jamais modifier `Assets/Crawlers/CrawlerManager.cs`**, même pour un ajout mineur. C'est le fichier d'un coéquipier qui le change souvent. Construire autour (ex. piloter un effet depuis `ZippoController` ou l'état du joueur dans un composant séparé). Si une donnée de CrawlerManager semble indispensable : demander d'abord.
- Les fichiers `Porte/`, `Menu_Scripts/`, les scènes et le level design sont surtout l'oeuvre de Luckycio ; `Monster/`, `Crawlers/`, `KDTree/` surtout de Julien. Éviter les refactors larges dans ces zones et signaler toute modification qui les touche.
- **Ne pas modifier** à la main : fichiers `.meta`, `Library/`, `Temp/`, `Logs/`, `UserSettings/`, les `.csproj` et `.slnx` (générés, ignorés par git).
- Les scènes (`.unity`) et prefabs se modifient dans l'éditeur Unity. Si une modification de scène ou de prefab est nécessaire, décrire précisément les étapes à faire dans l'éditeur plutôt que d'éditer le YAML.
- La plupart des scripts C# portent un BOM UTF-8 ; conserver l'encodage et les fins de ligne du fichier édité.
- Ne pas committer ni pousser sans demande explicite.

## Process de travail (obligatoire pour toute demande de modification)

Pour chaque demande qui implique de changer du code, des assets ou de la configuration, suivre ces étapes dans l'ordre. Ne pas sauter d'étape, ne pas fusionner deux validations en une.

1. **Analyse de l'état du projet.** Relire les fichiers concernés et détecter les modifications récentes : `git status`, `git log`, `git diff` et les fichiers touchés par les coéquipiers depuis la dernière session. Signaler tout changement inattendu avant de continuer.
2. **Compréhension de la demande.** Reformuler ce qui est demandé. Si un point est ambigu ou si plusieurs lectures mènent à un travail différent, poser des questions avant d'aller plus loin.
3. **Process technique.** Avant toute modification, rédiger un process détaillé : fichiers impactés, classes et méthodes touchées, Settings à créer ou modifier, events à câbler, impact sur les scènes et prefabs, interactions avec le code des coéquipiers, risques (merge, performance, physique, ordre d'exécution). Lister explicitement **tout ce qui me dérange ou me semble risqué**.
4. **Résumé pour un enfant de 12 ans.** Donner une version du process en quelques phrases simples, sans jargon, qui explique ce qui va changer dans le jeu et pourquoi.
5. **Attendre ma confirmation.** Une fois confirmé, présenter le **plan de modifications** : liste ordonnée de chaque changement, fichier par fichier, avec ce qui est ajouté, modifié ou supprimé, et les étapes à faire dans l'éditeur Unity.
6. **Attendre ma validation du plan.** Si je valide, appliquer le plan tel quel. Toute déviation en cours de route est signalée et justifiée.
7. **Vérification et compte rendu.** Relire le diff, vérifier les usings, namespaces (`Assets.CommonScripts`) et références, lister ce qui reste à tester dans l'éditeur (scène, prefab, Settings à renseigner) et récapituler ce qui a été fait, ce qui n'a pas été fait et pourquoi.

Les questions simples (explication de code, recherche, avis) ne déclenchent pas ce process : y répondre directement.
