using System.Collections.Generic;
using CatHotel.Services;

namespace CatHotel.Core
{
    /// <summary>
    /// Key-based localization system. All user-visible text is referenced by key.
    /// To add a new language: create a new dictionary and swap it via SetLanguage().
    /// Format strings use {0}, {1} etc. for parameters.
    /// </summary>
    public static class LocalizedStrings
    {
        private static Dictionary<string, string> _current;

        static LocalizedStrings()
        {
            _current = French;
        }

        /// <summary>Get a localized string by key.</summary>
        public static string Get(string key)
        {
            return _current.TryGetValue(key, out var value) ? value : $"[{key}]";
        }

        /// <summary>Get a localized string with format parameters.</summary>
        public static string Get(string key, params object[] args)
        {
            if (!_current.TryGetValue(key, out var value)) return $"[{key}]";
            return string.Format(value, args);
        }

        /// <summary>Fired when language changes. UI should subscribe to refresh labels.</summary>
        public static event System.Action OnLanguageChanged;

        /// <summary>Current language key ("fr", "en", "de", "es" or "pt").</summary>
        public static string CurrentLanguage { get; private set; } = "fr";

        private const string PrefKey = "Param_Language";

        private static Dictionary<string, string> ResolveDictionary(string langCode)
        {
            return langCode switch
            {
                "en" => English,
                "de" => German,
                "es" => Spanish,
                "pt" => Portuguese,
                _ => French,
            };
        }

        /// <summary>Switch language at runtime and notify all listeners.</summary>
        public static void SetLanguage(string langCode)
        {
            CurrentLanguage = langCode;
            _current = ResolveDictionary(langCode);

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.Settings.language = langCode;
                CloudSaveManager.Instance.SaveSettings();
            }

            OnLanguageChanged?.Invoke();
        }

        /// <summary>Initialize language from saved preference or system language.</summary>
        public static void InitFromSystem()
        {
            // Try cloud save first
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.IsLoaded
                && !string.IsNullOrEmpty(CloudSaveManager.Instance.Settings.language))
            {
                string saved = CloudSaveManager.Instance.Settings.language;
                CurrentLanguage = saved;
                _current = ResolveDictionary(saved);
                return;
            }

            // Fallback: check PlayerPrefs (migration from old system)
            if (UnityEngine.PlayerPrefs.HasKey(PrefKey))
            {
                string saved = UnityEngine.PlayerPrefs.GetString(PrefKey);
                CurrentLanguage = saved;
                _current = ResolveDictionary(saved);

                // Migrate to cloud save
                if (CloudSaveManager.Instance != null)
                {
                    CloudSaveManager.Instance.Settings.language = saved;
                    CloudSaveManager.Instance.SaveSettings();
                }
                return;
            }

            // Auto-detect from system
            var sysLang = UnityEngine.Application.systemLanguage;
            if (sysLang == UnityEngine.SystemLanguage.French)
            {
                CurrentLanguage = "fr";
                _current = French;
            }
            else if (sysLang == UnityEngine.SystemLanguage.German)
            {
                CurrentLanguage = "de";
                _current = German;
            }
            else if (sysLang == UnityEngine.SystemLanguage.Spanish)
            {
                CurrentLanguage = "es";
                _current = Spanish;
            }
            else if (sysLang == UnityEngine.SystemLanguage.Portuguese)
            {
                CurrentLanguage = "pt";
                _current = Portuguese;
            }
            else
            {
                CurrentLanguage = "en";
                _current = English;
            }

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.Settings.language = CurrentLanguage;
                CloudSaveManager.Instance.SaveSettings();
            }
        }

        // ==================== FRENCH ====================
        public static readonly Dictionary<string, string> French = new()
        {
            // --- Consent / RGPD ---
            { "consent.title", "Confidentialité & publicités" },
            { "consent.body", "Pour soutenir le développement du jeu, Meowtel propose des publicités optionnelles avec récompenses. Acceptez-vous l'utilisation de votre identifiant publicitaire afin d'afficher des publicités personnalisées ? Vous pourrez modifier ce choix à tout moment dans le menu pause, rubrique « Confidentialité »." },
            { "consent.accept", "Accepter" },
            { "consent.refuse", "Refuser" },
            { "consent.analytics.title", "Statistiques d'utilisation" },
            { "consent.analytics.body", "Pour améliorer le jeu, acceptez-vous que Meowtel envoie à Unity Analytics des statistiques d'utilisation (progression, publicités regardées, type d'appareil), liées à un identifiant technique de joueur ? Refuser ne change rien à votre partie. Vous pourrez modifier ce choix à tout moment dans le menu pause, rubrique « Confidentialité »." },
            { "consent.current", "Votre choix actuel : {0}." },
            { "consent.state.accepted", "accepté" },
            { "consent.state.refused", "refusé" },
            { "ui.privacy", "Confidentialité" },

            // --- Cat Info Panel ---
            { "cat.kitten", "Chaton" },
            { "cat.adult", "Chat adulte" },
            { "cat.stay.pension", "En pension" },
            { "cat.stay.refuge", "Arrivé au refuge" },
            { "cat.likes", "Aime les {0}" },
            { "cat.dislikes", "Déteste les {0}" },
            { "cat.time.none", "--:--:--" },

            // --- End Pension Panel ---
            { "pension.bye", "Au revoir {0} !" },
            { "adoption.title", "Adoption réussie !" },
            { "adoption.happiness", "Bonheur au moment de l'adoption :" },
            { "adoption.fee", "Frais d'adoption" },
            { "adoption.bye", "Bonne vie {0} !" },

            // --- Loading ---
            { "ui.loading", "Chargement" },
            { "loading.tip.0", "Caresse tes chats pour les rendre heureux." },
            { "loading.tip.1", "Un chat malheureux risque de partir sans te payer." },
            { "loading.tip.2", "Tape deux fois sur un objet pour le déplacer." },
            { "loading.tip.3", "Les tapis offrent un bonus de confort à la zone." },
            { "loading.tip.4", "Certaines races ne s'entendent pas — garde un œil dessus." },
            { "loading.tip.5", "Plus ton hôtel est réputé, plus les chats sont exigeants." },
            { "loading.tip.6", "Les escaliers mènent aux étages supérieurs." },
            { "loading.tip.7", "Collecte tes pièces rapidement avant qu'elles disparaissent." },

            // --- Tutorial (Narration) ---
            { "tuto.speaker.jasper", "Jasper" },
            { "tuto.speaker.winston", "Winston" },
            { "tuto.skip.link", "Passer le tutoriel" },
            { "tuto.skip.ask", "Passer le tutoriel ?" },
            { "tuto.skip.yes", "Oui" },
            { "tuto.skip.no", "Non" },
            { "tuto.welcome", "Bienvenue dans <b>Meowtel</b> !\nJe suis Jasper, le maître d'hôtel." },
            { "tuto.explain", "Ici, vous accueillez des chats en pension et au refuge. Prenez soin d'eux et ils vous récompenseront !" },
            { "tuto.floors", "L'hôtel compte <b>10 étages</b> à débloquer au-dessus du RDC. Plus l'hôtel grandit, plus vous accueillez de chats !" },
            { "tuto.levelup.intro", "Pour débloquer un nouvel étage, il faut <b>monter de niveau de réputation</b>." },
            { "tuto.levelup.xp", "Gagnez de l'<b>XP</b> en rendant les chats heureux. Chaque pension réussie et chaque adoption en rapportent." },
            { "tuto.levelup.cond", "Pour passer un palier, il faut : assez d'<b>XP</b>, assez de <b>chats à plus de 75% de bonheur</b>, et payer en <b>Cat Coins</b>." },
            { "tuto.levelup.tap", "Tapez sur la <b>barre de niveau</b> en haut pour voir vos conditions actuelles." },
            { "tuto.levelup.done", "Parfait ! Vous y reviendrez régulièrement pour faire grandir l'hôtel. Refermez le panneau pour continuer." },
            { "tuto.coins", "La <b>monnaie</b> ici est le Cat Coin. Achetez des objets pour vos chats... et payez les passages de niveau !" },
            // --- HUD elements walkthrough ---
            { "tuto.hud.coins", "Voici votre <b>réserve de Cat Coins</b>. Elle se remplit quand vous récupérez les pièces laissées par les chats heureux. Sert à acheter des objets et à monter de niveau." },
            { "tuto.hud.purrls", "Les <b>Puurls</b> sont la monnaie premium. Plus tard, ils permettront d'acheter des objets spéciaux à gros bonus, ou d'activer des boosts de revenus." },
            { "tuto.hud.capacity", "La <b>capacité</b> de l'hôtel : pourcentage des places occupées par rapport au maximum autorisé par votre niveau." },
            { "tuto.hud.comfort", "Le <b>confort moyen</b> de l'hôtel. Base 50%, +5% si l'hôtel est peu rempli (<20%), augmente avec les objets de décoration, et chute si l'hôtel est surchargé (>80%)." },
            { "tuto.hud.floors", "L'<b>étage</b> actuellement affiché. Utilisez les flèches pour monter et descendre quand vous en aurez débloqué." },
            { "tuto.hud.system", "Le <b>menu pause</b>. Réglages, langue, son, et options du jeu." },
            { "tuto.hud.collectall", "<b>Récupérer toutes les pièces</b> d'un coup. Coût : 5 Cat Coins. Pratique quand l'hôtel se remplit !" },
            { "tuto.hud.addboost", "Regarder une pub pour <b>doubler tous vos revenus pendant 5 minutes</b>." },
            { "tuto.hud.nextcat", "Le <b>compte à rebours</b> avant l'arrivée du prochain chat. Plus votre hôtel grandit, plus le rythme s'accélère." },
            { "tuto.hud.shop", "La <b>boutique d'objets</b>. Achetez ici tout ce qu'il faut pour satisfaire les besoins de vos chats." },
            { "tuto.pension", "Des chats arrivent en <b>pension</b> pour un temps déterminé. Leur bonheur doit être le plus élevé possible pour gagner un maximum de Cat Coins !" },
            { "tuto.refuge", "Des chats tristes arriveront par le <b>refuge</b>. Rendez-les heureux pour qu'ils puissent être adoptés !" },
            { "tuto.unhappy", "Attention : un chat dont le bonheur tombe sous <b>20%</b> voudra partir. Ne le laissez pas filer !" },
            { "tuto.firstcat", "Oh ! Votre premier client arrive ! Observez-le..." },
            { "tuto.selectcat", "Tapez sur le chat pour le sélectionner." },
            { "tuto.catinfo", "Voici ses informations : son nom, sa race, son humeur et ses besoins. Gardez un œil dessus !" },
            { "tuto.pensiontime", "Regardez le temps restant en pension. Quand il s'écoulera, le chat repartira. Plus il est heureux, plus vous gagnez !" },
            { "tuto.refugecat", "Un chat du refuge arrive ! Il a faim... Regardez ses jauges : il a besoin de croquettes." },
            { "tuto.buyfood", "Ouvrez la boutique et achetez une <b>gamelle</b>, puis placez-la dans la pièce." },
            { "tuto.waiteat", "Bien ! Attendez que le chat aille manger..." },
            { "tuto.collectcoin", "Une pièce est apparue ! Tapez dessus pour collecter vos <b>Cat Coins</b>." },
            { "tuto.thirsty", "Il a soif maintenant ! Achetez de l'<b>eau</b> et placez-la." },
            { "tuto.waitdrink", "Laissez-le boire tranquillement..." },
            { "tuto.sleepy", "Le chat est fatigué. Offrez-lui un <b>lit</b> ou un <b>coussin</b> !" },
            { "tuto.waitsleep", "Il se repose... chut !" },
            { "tuto.dirty", "Il a besoin d'une <b>litière</b> ! Achetez-en une." },
            { "tuto.waitclean", "Il fait sa toilette..." },
            { "tuto.bored", "Il veut jouer ! Achetez une <b>balle de laine</b>." },
            { "tuto.waitplay", "Regardez-le s'amuser !" },
            { "tuto.complete", "Bravo ! Vous connaissez les bases. L'hôtel est à vous, bonne chance !" },

            // --- Sound ---
            { "sound.on", "Son activé" },
            { "sound.off", "Son désactivé" },

            // --- Mood ---
            { "mood.happy", "Content" },
            { "mood.ecstatic", "Enthousiaste" },
            { "mood.depressed", "Déprimé" },
            { "mood.aggressive", "Bagarreur" },
            { "mood.angry", "En colère" },

            // --- Needs ---
            { "need.hungry", "A faim" },
            { "need.thirsty", "A soif" },
            { "need.tired", "Fatigué" },
            { "need.bored", "S'ennuie" },
            { "need.dirty", "Sale" },

            // --- HUD ---
            { "hud.floor.ground", "RDC" },
            { "hud.floor", "Étage {0}" },
            { "hud.level", "Niveau {0}" },
            { "levelup.title.current", "Niveau {0}" },
            { "levelup.title.next", "Niveau {0}" },
            { "levelup.newstuff", "Nouveautés" },
            { "levelup.condition.xp", "{0} / {1} XP" },
            { "levelup.condition.happycats", "{0} chats > 75% de bonheur sur {1}" },
            { "levelup.newfloor", "Accès niveau {0}" },
            { "levelup.capacity.title", "Capacité de l'hôtel" },
            { "levelup.go.next", "Passer au niveau suivant" },
            { "hud.level.max", "MAX" },
            { "hud.level.max.reached", "Niveau maximum atteint !" },
            { "hud.level.objective", "{0}/{1} chats à +{2}% de bonheur" },
            { "hud.boost.active", "Boost x2 actif ! {0}s" },
            { "hud.boost.button", "Gains doublés" },
            { "hud.collect.label", "Tout collecter" },

            // --- Reputation Level Names ---
            { "rep.0", "Débutant" },
            { "rep.1", "Amateur" },
            { "rep.2", "Compétent" },
            { "rep.3", "Professionnel" },
            { "rep.4", "Expert" },
            { "rep.5", "Renommé" },
            { "rep.6", "Célèbre" },
            { "rep.7", "Prestigieux" },
            { "rep.8", "Élite" },
            { "rep.9", "Légendaire" },
            { "rep.10", "Maître des Chats" },

            // --- Shop Categories ---
            { "shop.beds", "Lits" },
            { "shop.pillows", "Coussins" },
            { "shop.croquettes", "Croquettes" },
            { "shop.water", "Eau" },
            { "shop.balls", "Balles" },
            { "shop.scratchers", "Griffoirs" },
            { "shop.litters", "Litières" },
            { "shop.frames", "Cadres" },
            { "shop.lamps", "Lampes" },
            { "shop.tables", "Tables" },
            { "shop.plants", "Plantes" },
            { "shop.shelves", "Étagères" },
            { "shop.aquariums", "Aquariums" },
            { "shop.carpets", "Tapis" },
            { "shop.trees", "Arbres à chat" },

            // --- Breed Names ---
            { "breed.europeen", "Européen" },
            { "breed.siamois", "Siamois" },
            { "breed.ragdoll", "Ragdoll" },
            { "breed.siberien", "Sibérien" },
            { "breed.chartreux", "Chartreux" },

            // --- Personality: Race Traits ---
            { "trait.hunger", "Gourmand" },
            { "trait.thirst", "Assoiffé" },
            { "trait.sleep", "Dormeur" },
            { "trait.play", "Joueur" },
            { "trait.clean", "Maniaque" },
            { "trait.aggressive", "Bagarreur" },
            { "trait.big", "Imposant" },
            { "trait.small", "Petit gabarit" },
            { "trait.fast", "Rapide" },
            { "trait.slow", "Nonchalant" },

            // --- Personality: Personality Pool ---
            { "personality.cuddly", "Câlin" },
            { "personality.independent", "Indépendant" },
            { "personality.curious", "Curieux" },
            { "personality.fearful", "Craintif" },
            { "personality.affectionate", "Affectueux" },
            { "personality.observer", "Observateur" },
            { "personality.discreet", "Discret" },
            { "personality.clingy", "Pot de colle" },
            { "personality.adventurer", "Aventurier" },
            { "personality.territorial", "Territorial" },
            { "personality.sociable", "Sociable" },
            { "personality.solitary", "Solitaire" },
            { "personality.clever", "Malin" },
            { "personality.lazy", "Paresseux" },
            { "personality.loyal", "Fidèle" },
            { "personality.capricious", "Capricieux" },

            // --- Personality: Quirky Pool ---
            { "quirk.grudge", "Rancunier" },
            { "quirk.touchy", "Susceptible" },
            { "quirk.dumb", "Idiot" },
            { "quirk.big_eater", "Gros mangeur" },
            { "quirk.drinker", "Soiffard" },
            { "quirk.food_thief", "Voleur de croquettes" },
            { "quirk.snorer", "Ronfleur" },
            { "quirk.litter_fear", "Peureux des litières" },
            { "quirk.jealous", "Jaloux" },
            { "quirk.show_off", "Frimeur" },
            { "quirk.drama_queen", "Drama queen" },
            { "quirk.hair_collector", "Collectionneur de poils" },
            { "quirk.grumpy", "Grognon" },
            { "quirk.clumsy", "Maladroit" },
            { "quirk.hypochondriac", "Hypocondriaque" },
            { "quirk.couch_king", "Roi du canapé" },
            { "quirk.snob", "Snob" },
            { "quirk.pilferer", "Chapardeur" },

            // --- Scene Labels (static text in scene hierarchy) ---
            // Boot scene
            { "ui.play", "Jouer" },
            { "ui.continue", "Continuer partie" },
            { "ui.new_game", "Nouvelle partie" },

            // Credits panel
            { "credits.title", "Crédits" },
            { "credits.gameby", "un jeu de" },
            { "credits.direction", "Direction / développement" },
            { "credits.disclaimer", "Ce jeu a été créé à l'aide du moteur Unity, propriété de Unity Technologies. Unity et le logo Unity sont des marques déposées de Unity Technologies aux États-Unis d'Amérique et dans d'autres pays" },
            { "ui.credits", "Crédits" },
            { "ui.parameters", "Paramètres" },

            // Options panel
            { "ui.resume", "Reprendre" },
            { "ui.main_menu", "Menu principal" },
            { "ui.meowdex", "Meowdex" },

            // Parameters panel
            { "param.languages", "Langues" },
            { "param.notifications", "Notifications push" },
            { "param.battery", "Économie de batterie" },
            { "param.music_volume", "Volume musique" },
            { "param.effects_volume", "Volume des effets" },

            // Shop
            { "ui.shop", "Boutique" },
            { "shop.category.food", "Nourriture" },
            { "shop.category.drink", "Boisson" },
            { "shop.category.sleep", "Sommeil" },
            { "shop.category.play", "Amusement" },
            { "shop.category.clean", "Propreté" },
            { "shop.category.comfort", "Confort" },
            { "shop.category.deco", "Décoration" },
            { "shop.double_gains", "Doubler les gains" },

            // Cat Info Panel
            { "cat.name_label", "Nom du chat" },
            { "cat.race_label", "Race" },
            { "cat.age_label", "Âge" },
            { "cat.character_label", "Caractère" },
            { "cat.affinity_label", "Affinités" },
            { "cat.needs_label", "Besoins" },
            { "cat.need.hunger", "Faim" },
            { "cat.need.thirst", "Soif" },
            { "cat.need.sleep", "Sommeil" },
            { "cat.need.play", "Jeu" },
            { "cat.need.clean", "Propreté" },
            { "cat.need.happiness", "Bonheur" },

            // End Pension Panel
            { "pension.title", "Fin de séjour en pension !" },
            { "pension.happiness", "Bonheur moyen lors du séjour :" },
            { "pension.details", "Détails du paiement" },
            { "pension.base", "Base" },
            { "pension.tip", "Pourboire" },
            { "pension.total", "Total" },
            { "pension.time_remaining", "Temps restant en pension" },

            // HUD
            { "hud.boost.label", "Boost x2 collecte de cat coins actif !" },

            // Object display names (shop items)
            { "obj.bed", "Lit" },
            { "obj.luxury_bed", "Lit de luxe" },
            { "obj.pillow", "Coussin" },
            { "obj.food_bowl", "Gamelle" },
            { "obj.food_bowl_v2", "Gamelle var. 2" },
            { "obj.food_bowl_v3", "Gamelle var. 3" },
            { "obj.food_bowl_v4", "Gamelle var. 4" },
            { "obj.water_bowl", "Bol d'eau" },
            { "obj.water_bowl_04", "Bol d'eau moderne" },
            { "obj.water_bowl_v2", "Bol d'eau var. 2" },
            { "obj.water_bowl_v3", "Bol d'eau var. 3" },
            { "obj.wool_ball", "Balle de laine" },
            { "obj.cat_tree", "Arbre à chat" },
            { "obj.scratcher", "Griffoir" },
            { "obj.litter", "Litière" },
            { "obj.litter_v1", "Litière var." },
            { "obj.frame_1", "Cadre 1" },
            { "obj.frame_2", "Cadre 2" },
            { "obj.frame_3", "Cadre 3" },
            { "obj.painting", "Peinture" },
            { "obj.lamp", "Grande lampe" },
            { "obj.coffee_table", "Table basse" },
            { "obj.drawer", "Commode" },
            { "obj.plant_big", "Grande plante" },
            { "obj.plant_small", "Petite plante" },
            { "obj.shelf", "Étagère" },
            { "obj.shelf_v1", "Étagère var." },
            { "obj.aquarium", "Aquarium" },
            { "obj.carpet_confort", "Tapis Confort" },
            { "obj.carpet_play", "Tapis Jeu" },
            { "obj.carpet_cosmic", "Tapis Cosmique" },

            // --- Cat Names ---
            { "names.pool", "Minou,Felix,Caramel,Luna,Tigrou,Noisette,Moustache,Pacha,Cannelle,Gribouille,Minette,Câlin,Filou,Chipie,Réglisse,Perle,Simba,Plume,Biscuit,Cookie,Praline,Nougat,Macaron,Brioche,Muffin,Crumble,Tiramisu,Meringue,Brownie,Fudge,Cosmos,Lune,Étoile,Comète,Nova,Nebula,Soleil,Aurore,Eclipse,Galaxie,Astro,Pluton,Rubis,Saphir,Émeraude,Jade,Opale,Ambre,Topaze,Diamant,Cristal,Onyx,Ivoire,Corail,Ninja,Pixel,Wifi,Sushi,Tofu,Wasabi,Mozart,Picasso,Darwin,Merlin,Zorro,Gatsby,Chouette,Papillon,Colibri,Hibou,Renard,Loutre,Belle,Roméo,Vanille,Sucre,Cacao,Latte,Madeleine,Vega,Sirius,Andromède,Apollo,Atlas,Mars,Indigo,Améthyste,Turquoise,Quartz,Voltaire,Hugo,Tesla,Curie,Athéna,Hermès,Iris,Pandora,Chouquette,Lapin,Belette,Castor,Vortex,Matrix" },
            // Per-breed extra names: appended to the common pool when a cat of this breed spawns.
            { "names.pool.Breed_Europeen2", "Odyssée" },
        };

        // ==================== ENGLISH ====================
        public static readonly Dictionary<string, string> English = new()
        {
            // --- Consent / GDPR ---
            { "consent.title", "Privacy & Ads" },
            { "consent.body", "To support the game's development, Meowtel offers optional rewarded ads. Do you agree to the use of your advertising ID to display personalized ads? You can change this choice at any time from the pause menu, under “Privacy”." },
            { "consent.accept", "Accept" },
            { "consent.refuse", "Decline" },
            { "consent.analytics.title", "Usage statistics" },
            { "consent.analytics.body", "To improve the game, do you agree to Meowtel sending usage statistics (progress, ads watched, device type) to Unity Analytics, linked to a technical player identifier? Declining changes nothing in your game. You can change this choice at any time from the pause menu, under “Privacy”." },
            { "consent.current", "Your current choice: {0}." },
            { "consent.state.accepted", "accepted" },
            { "consent.state.refused", "declined" },
            { "ui.privacy", "Privacy" },

            // --- Cat Info Panel ---
            { "cat.kitten", "Kitten" },
            { "cat.adult", "Adult cat" },
            { "cat.stay.pension", "Boarding" },
            { "cat.stay.refuge", "Shelter arrival" },
            { "cat.likes", "Likes {0}" },
            { "cat.dislikes", "Dislikes {0}" },
            { "cat.time.none", "--:--:--" },

            // --- End Pension Panel ---
            { "pension.bye", "Goodbye {0}!" },
            { "adoption.title", "Adoption complete!" },
            { "adoption.happiness", "Happiness at adoption:" },
            { "adoption.fee", "Adoption fee" },
            { "adoption.bye", "Have a happy life, {0}!" },

            // --- Loading ---
            { "ui.loading", "Loading" },
            { "loading.tip.0", "Pet your cats to make them happy." },
            { "loading.tip.1", "An unhappy cat may leave without paying." },
            { "loading.tip.2", "Long-press an object to move it." },
            { "loading.tip.3", "Carpets boost comfort in their area." },
            { "loading.tip.4", "Some breeds don't get along — keep an eye on them." },
            { "loading.tip.5", "The better your rep, the pickier the cats." },
            { "loading.tip.6", "Stairs lead to the upper floors." },
            { "loading.tip.7", "Grab your coins quickly before they fade away." },

            // --- Tutorial (Narration) ---
            { "tuto.speaker.jasper", "Jasper" },
            { "tuto.speaker.winston", "Winston" },
            { "tuto.skip.link", "Skip tutorial" },
            { "tuto.skip.ask", "Skip tutorial?" },
            { "tuto.skip.yes", "Yes" },
            { "tuto.skip.no", "No" },
            { "tuto.welcome", "Welcome to <b>Meowtel</b>!\nI'm Jasper, the concierge." },
            { "tuto.explain", "Here, you welcome cats for boarding and shelter. Take good care of them and they'll reward you!" },
            { "tuto.floors", "The hotel has <b>10 floors</b> to unlock above the ground floor. The bigger your hotel, the more cats you can host!" },
            { "tuto.levelup.intro", "To unlock a new floor, you need to <b>level up your reputation</b>." },
            { "tuto.levelup.xp", "Earn <b>XP</b> by keeping cats happy. Every successful boarding and adoption rewards XP." },
            { "tuto.levelup.cond", "To level up, you need: enough <b>XP</b>, enough <b>cats above 75% happiness</b>, and to pay in <b>Cat Coins</b>." },
            { "tuto.levelup.tap", "Tap the <b>level bar</b> at the top to see your current conditions." },
            { "tuto.levelup.done", "Great! Come back here regularly to grow the hotel. Close the panel to continue." },
            { "tuto.coins", "The <b>currency</b> here is the Cat Coin. Buy items for your cats... and pay for level-ups!" },
            // --- HUD elements walkthrough ---
            { "tuto.hud.coins", "This is your <b>Cat Coin reserve</b>. It fills up when you collect coins left by happy cats. Used to buy items and level up." },
            { "tuto.hud.purrls", "<b>Puurls</b> are the premium currency. Later, they'll let you buy special items with big bonuses, or activate revenue boosts." },
            { "tuto.hud.capacity", "The hotel's <b>capacity</b>: percentage of slots occupied compared to the max allowed by your level." },
            { "tuto.hud.comfort", "The hotel's <b>average comfort</b>. Base 50%, +5% if the hotel is sparsely populated (<20%), rises with decoration items, and drops when overcrowded (>80%)." },
            { "tuto.hud.floors", "The currently displayed <b>floor</b>. Use the arrows to go up and down once you've unlocked some." },
            { "tuto.hud.system", "The <b>pause menu</b>. Settings, language, sound, and game options." },
            { "tuto.hud.collectall", "<b>Collect all coins</b> at once. Cost: 5 Cat Coins. Handy when the hotel fills up!" },
            { "tuto.hud.addboost", "Watch an ad to <b>double all your revenue for 5 minutes</b>." },
            { "tuto.hud.nextcat", "The <b>countdown</b> before the next cat arrives. The more your hotel grows, the faster the rhythm." },
            { "tuto.hud.shop", "The <b>item shop</b>. Buy everything you need here to fulfill your cats' needs." },
            { "tuto.pension", "Cats arrive for <b>boarding</b> for a set time. Keep their happiness as high as possible to earn the most Cat Coins!" },
            { "tuto.refuge", "Sad cats will arrive through the <b>shelter</b>. Make them happy so they can be adopted!" },
            { "tuto.unhappy", "Warning: a cat whose happiness drops below <b>20%</b> will try to leave. Don't let that happen!" },
            { "tuto.firstcat", "Oh! Your first guest is arriving! Watch..." },
            { "tuto.selectcat", "Tap on the cat to select it." },
            { "tuto.catinfo", "Here's their info: name, breed, mood and needs. Keep an eye on it!" },
            { "tuto.pensiontime", "Check the remaining boarding time. When it runs out, the cat leaves. The happier they are, the more you earn!" },
            { "tuto.refugecat", "A shelter cat has arrived! It's hungry... Look at its gauges: it needs food." },
            { "tuto.buyfood", "Open the shop and buy a <b>food bowl</b>, then place it in the room." },
            { "tuto.waiteat", "Great! Wait for the cat to eat..." },
            { "tuto.collectcoin", "A coin appeared! Tap on it to collect your <b>Cat Coins</b>." },
            { "tuto.thirsty", "It's thirsty now! Buy some <b>water</b> and place it." },
            { "tuto.waitdrink", "Let it drink in peace..." },
            { "tuto.sleepy", "The cat is tired. Get it a <b>bed</b> or a <b>pillow</b>!" },
            { "tuto.waitsleep", "It's resting... shh!" },
            { "tuto.dirty", "It needs a <b>litter box</b>! Buy one." },
            { "tuto.waitclean", "Grooming time..." },
            { "tuto.bored", "It wants to play! Buy a <b>wool ball</b>." },
            { "tuto.waitplay", "Watch it have fun!" },
            { "tuto.complete", "Well done! You know the basics. The hotel is yours, good luck!" },

            // --- Sound ---
            { "sound.on", "Sound on" },
            { "sound.off", "Sound off" },

            // --- Mood ---
            { "mood.happy", "Happy" },
            { "mood.ecstatic", "Ecstatic" },
            { "mood.depressed", "Depressed" },
            { "mood.aggressive", "Aggressive" },
            { "mood.angry", "Angry" },

            // --- Needs ---
            { "need.hungry", "Hungry" },
            { "need.thirsty", "Thirsty" },
            { "need.tired", "Tired" },
            { "need.bored", "Bored" },
            { "need.dirty", "Dirty" },

            // --- HUD ---
            { "hud.floor.ground", "Ground floor" },
            { "hud.floor", "Floor {0}" },
            { "hud.level", "Level {0}" },
            { "levelup.title.current", "Level {0}" },
            { "levelup.title.next", "Level {0}" },
            { "levelup.newstuff", "What's new" },
            { "levelup.condition.xp", "{0} / {1} XP" },
            { "levelup.condition.happycats", "{0} cats > 75% happy out of {1}" },
            { "levelup.newfloor", "Floor {0} unlocked" },
            { "levelup.capacity.title", "Hotel capacity" },
            { "levelup.go.next", "Go to next level" },
            { "hud.level.max", "MAX" },
            { "hud.level.max.reached", "Maximum level reached!" },
            { "hud.level.objective", "{0}/{1} cats at +{2}% happiness" },
            { "hud.boost.active", "x2 Boost active! {0}s" },
            { "hud.boost.button", "Double earnings" },
            { "hud.collect.label", "Collect all" },

            // --- Reputation Level Names ---
            { "rep.0", "Beginner" },
            { "rep.1", "Novice" },
            { "rep.2", "Skilled" },
            { "rep.3", "Professional" },
            { "rep.4", "Expert" },
            { "rep.5", "Renowned" },
            { "rep.6", "Famous" },
            { "rep.7", "Prestigious" },
            { "rep.8", "Elite" },
            { "rep.9", "Legendary" },
            { "rep.10", "Cat Master" },

            // --- Shop Categories ---
            { "shop.beds", "Beds" },
            { "shop.pillows", "Pillows" },
            { "shop.croquettes", "Food bowls" },
            { "shop.water", "Water" },
            { "shop.balls", "Balls" },
            { "shop.scratchers", "Scratchers" },
            { "shop.litters", "Litter boxes" },
            { "shop.frames", "Frames" },
            { "shop.lamps", "Lamps" },
            { "shop.tables", "Tables" },
            { "shop.plants", "Plants" },
            { "shop.shelves", "Shelves" },
            { "shop.aquariums", "Aquariums" },
            { "shop.carpets", "Carpets" },
            { "shop.trees", "Cat trees" },

            // --- Breed Names ---
            { "breed.europeen", "European" },
            { "breed.siamois", "Siamese" },
            { "breed.ragdoll", "Ragdoll" },
            { "breed.siberien", "Siberian" },
            { "breed.chartreux", "Chartreux" },

            // --- Personality: Race Traits ---
            { "trait.hunger", "Glutton" },
            { "trait.thirst", "Thirsty" },
            { "trait.sleep", "Sleepyhead" },
            { "trait.play", "Playful" },
            { "trait.clean", "Neat freak" },
            { "trait.aggressive", "Fighter" },
            { "trait.big", "Large" },
            { "trait.small", "Small" },
            { "trait.fast", "Quick" },
            { "trait.slow", "Laid-back" },

            // --- Personality: Personality Pool ---
            { "personality.cuddly", "Cuddly" },
            { "personality.independent", "Independent" },
            { "personality.curious", "Curious" },
            { "personality.fearful", "Fearful" },
            { "personality.affectionate", "Affectionate" },
            { "personality.observer", "Observer" },
            { "personality.discreet", "Discreet" },
            { "personality.clingy", "Clingy" },
            { "personality.adventurer", "Adventurous" },
            { "personality.territorial", "Territorial" },
            { "personality.sociable", "Sociable" },
            { "personality.solitary", "Loner" },
            { "personality.clever", "Clever" },
            { "personality.lazy", "Lazy" },
            { "personality.loyal", "Loyal" },
            { "personality.capricious", "Fickle" },

            // --- Personality: Quirky Pool ---
            { "quirk.grudge", "Holds grudges" },
            { "quirk.touchy", "Touchy" },
            { "quirk.dumb", "Clueless" },
            { "quirk.big_eater", "Big eater" },
            { "quirk.drinker", "Water hog" },
            { "quirk.food_thief", "Food thief" },
            { "quirk.snorer", "Snorer" },
            { "quirk.litter_fear", "Litter-shy" },
            { "quirk.jealous", "Jealous" },
            { "quirk.show_off", "Show-off" },
            { "quirk.drama_queen", "Drama queen" },
            { "quirk.hair_collector", "Fur collector" },
            { "quirk.grumpy", "Grumpy" },
            { "quirk.clumsy", "Clumsy" },
            { "quirk.hypochondriac", "Hypochondriac" },
            { "quirk.couch_king", "Couch potato" },
            { "quirk.snob", "Snob" },
            { "quirk.pilferer", "Pilferer" },

            // --- Scene Labels ---
            // Boot scene
            { "ui.play", "Play" },
            { "ui.continue", "Continue" },
            { "ui.new_game", "New game" },

            // Credits panel
            { "credits.title", "Credits" },
            { "credits.gameby", "a game by" },
            { "credits.direction", "Direction / development" },
            { "credits.disclaimer", "This game was created using the Unity engine, owned by Unity Technologies. Unity and the Unity logo are trademarks of Unity Technologies in the United States of America and in other countries" },
            { "ui.credits", "Credits" },
            { "ui.parameters", "Settings" },

            // Options panel
            { "ui.resume", "Resume" },
            { "ui.main_menu", "Main menu" },
            { "ui.meowdex", "Meowdex" },

            // Parameters panel
            { "param.languages", "Languages" },
            { "param.notifications", "Push notifications" },
            { "param.battery", "Battery saver" },
            { "param.music_volume", "Music volume" },
            { "param.effects_volume", "Effects volume" },

            // Shop
            { "ui.shop", "Shop" },
            { "shop.category.food", "Food" },
            { "shop.category.drink", "Drink" },
            { "shop.category.sleep", "Sleep" },
            { "shop.category.play", "Play" },
            { "shop.category.clean", "Hygiene" },
            { "shop.category.comfort", "Comfort" },
            { "shop.category.deco", "Decoration" },
            { "shop.double_gains", "Double earnings" },

            // Cat Info Panel
            { "cat.name_label", "Cat name" },
            { "cat.race_label", "Breed" },
            { "cat.age_label", "Age" },
            { "cat.character_label", "Personality" },
            { "cat.affinity_label", "Affinities" },
            { "cat.needs_label", "Needs" },
            { "cat.need.hunger", "Hunger" },
            { "cat.need.thirst", "Thirst" },
            { "cat.need.sleep", "Sleep" },
            { "cat.need.play", "Play" },
            { "cat.need.clean", "Hygiene" },
            { "cat.need.happiness", "Happiness" },

            // End Pension Panel
            { "pension.title", "Boarding stay complete!" },
            { "pension.happiness", "Average happiness during stay:" },
            { "pension.details", "Payment details" },
            { "pension.base", "Base" },
            { "pension.tip", "Tip" },
            { "pension.total", "Total" },
            { "pension.time_remaining", "Time remaining" },

            // HUD
            { "hud.boost.label", "x2 coin collection boost active!" },

            // Object display names
            { "obj.bed", "Bed" },
            { "obj.luxury_bed", "Luxury bed" },
            { "obj.pillow", "Pillow" },
            { "obj.food_bowl", "Food bowl" },
            { "obj.food_bowl_v2", "Food bowl v.2" },
            { "obj.food_bowl_v3", "Food bowl v.3" },
            { "obj.food_bowl_v4", "Food bowl v.4" },
            { "obj.water_bowl", "Water bowl" },
            { "obj.water_bowl_04", "Modern water bowl" },
            { "obj.water_bowl_v2", "Water bowl v.2" },
            { "obj.water_bowl_v3", "Water bowl v.3" },
            { "obj.wool_ball", "Wool ball" },
            { "obj.cat_tree", "Cat tree" },
            { "obj.scratcher", "Scratcher" },
            { "obj.litter", "Litter box" },
            { "obj.litter_v1", "Litter box v." },
            { "obj.frame_1", "Frame 1" },
            { "obj.frame_2", "Frame 2" },
            { "obj.frame_3", "Frame 3" },
            { "obj.painting", "Painting" },
            { "obj.lamp", "Floor lamp" },
            { "obj.coffee_table", "Coffee table" },
            { "obj.drawer", "Dresser" },
            { "obj.plant_big", "Large plant" },
            { "obj.plant_small", "Small plant" },
            { "obj.shelf", "Shelf" },
            { "obj.shelf_v1", "Shelf v." },
            { "obj.aquarium", "Aquarium" },
            { "obj.carpet_confort", "Comfort carpet" },
            { "obj.carpet_play", "Play carpet" },
            { "obj.carpet_cosmic", "Cosmic carpet" },

            // --- Cat Names ---
            { "names.pool", "Whiskers,Felix,Caramel,Luna,Tigger,Hazel,Mittens,Paws,Cinnamon,Scribbles,Kitty,Cuddles,Rascal,Sassy,Licorice,Pearl,Simba,Feather,Biscuit,Cookie,Praline,Nougat,Macaron,Muffin,Crumble,Tiramisu,Brownie,Fudge,Marshmallow,Toffee,Cosmos,Moon,Star,Comet,Nova,Nebula,Sunny,Aurora,Eclipse,Galaxy,Astro,Pluto,Ruby,Sapphire,Emerald,Jade,Opal,Amber,Topaz,Diamond,Crystal,Onyx,Ivory,Coral,Ninja,Pixel,Wifi,Sushi,Tofu,Wasabi,Mozart,Picasso,Darwin,Merlin,Zorro,Gatsby,Owl,Butterfly,Hummingbird,Shadow,Fox,Otter,Belle,Romeo,Vanilla,Sugar,Cocoa,Latte,Madeleine,Vega,Sirius,Andromeda,Apollo,Atlas,Mars,Indigo,Amethyst,Turquoise,Quartz,Voltaire,Hugo,Tesla,Curie,Athena,Hermes,Iris,Pandora,Cookieface,Bunny,Weasel,Beaver,Vortex,Matrix" },
            // Per-breed extra names: appended to the common pool when a cat of this breed spawns.
            { "names.pool.Breed_Europeen2", "Odyssey" },
        };

        // ==================== GERMAN ====================
        public static readonly Dictionary<string, string> German = new()
        {
            // --- Consent / DSGVO ---
            { "consent.title", "Datenschutz & Werbung" },
            { "consent.body", "Zur Unterstützung der Spielentwicklung bietet Meowtel optionale Belohnungswerbung an. Stimmen Sie der Nutzung Ihrer Werbe-ID zur Anzeige personalisierter Werbung zu? Sie können diese Wahl jederzeit im Pause-Menü unter „Datenschutz“ ändern." },
            { "consent.accept", "Akzeptieren" },
            { "consent.refuse", "Ablehnen" },
            { "consent.analytics.title", "Nutzungsstatistiken" },
            { "consent.analytics.body", "Stimmen Sie zu, dass Meowtel zur Verbesserung des Spiels Nutzungsstatistiken (Fortschritt, angesehene Werbung, Gerätetyp) an Unity Analytics sendet, verknüpft mit einer technischen Spielerkennung? Eine Ablehnung ändert nichts an Ihrem Spiel. Sie können diese Wahl jederzeit im Pause-Menü unter „Datenschutz“ ändern." },
            { "consent.current", "Ihre aktuelle Wahl: {0}." },
            { "consent.state.accepted", "akzeptiert" },
            { "consent.state.refused", "abgelehnt" },
            { "ui.privacy", "Datenschutz" },

            // --- Cat Info Panel ---
            { "cat.kitten", "Kätzchen" },
            { "cat.adult", "Erwachsene Katze" },
            { "cat.stay.pension", "In Pension" },
            { "cat.stay.refuge", "Im Tierheim" },
            { "cat.likes", "Mag {0}" },
            { "cat.dislikes", "Mag keine {0}" },
            { "cat.time.none", "--:--:--" },

            // --- End Pension Panel ---
            { "pension.bye", "Auf Wiedersehen {0}!" },
            { "adoption.title", "Adoption erfolgreich!" },
            { "adoption.happiness", "Glück bei der Adoption:" },
            { "adoption.fee", "Adoptionsgebühr" },
            { "adoption.bye", "Alles Gute, {0}!" },

            // --- Loading ---
            { "ui.loading", "Lädt" },
            { "loading.tip.0", "Streichle deine Katzen, um sie glücklich zu machen." },
            { "loading.tip.1", "Eine unzufriedene Katze geht ohne zu zahlen." },
            { "loading.tip.2", "Tippe doppelt auf ein Objekt, um es zu verschieben." },
            { "loading.tip.3", "Teppiche bieten einen Komfortbonus für die Zone." },
            { "loading.tip.4", "Manche Rassen vertragen sich nicht — pass auf sie auf." },
            { "loading.tip.5", "Je angesehener dein Hotel, desto anspruchsvoller die Katzen." },
            { "loading.tip.6", "Treppen führen zu den oberen Stockwerken." },
            { "loading.tip.7", "Sammle deine Münzen schnell, bevor sie verschwinden." },

            // --- Tutorial (Narration) ---
            { "tuto.speaker.jasper", "Jasper" },
            { "tuto.speaker.winston", "Winston" },
            { "tuto.skip.link", "Tutorial überspringen" },
            { "tuto.skip.ask", "Tutorial überspringen?" },
            { "tuto.skip.yes", "Ja" },
            { "tuto.skip.no", "Nein" },
            { "tuto.welcome", "Willkommen bei <b>Meowtel</b>!\nIch bin Jasper, der Concierge." },
            { "tuto.explain", "Hier nimmst du Katzen in Pension und ins Tierheim auf. Kümmere dich um sie und sie werden dich belohnen!" },
            { "tuto.floors", "Das Hotel hat <b>10 Etagen</b> über dem Erdgeschoss freizuschalten. Je größer dein Hotel, desto mehr Katzen kannst du beherbergen!" },
            { "tuto.levelup.intro", "Um eine neue Etage freizuschalten, musst du <b>im Ruf aufsteigen</b>." },
            { "tuto.levelup.xp", "Verdiene <b>XP</b>, indem du Katzen glücklich machst. Jede erfolgreiche Pension und jede Adoption bringen XP." },
            { "tuto.levelup.cond", "Zum Aufsteigen brauchst du: genug <b>XP</b>, genug <b>Katzen über 75% Glück</b>, und du musst in <b>Cat Coins</b> bezahlen." },
            { "tuto.levelup.tap", "Tippe oben auf die <b>Levelleiste</b>, um deine aktuellen Bedingungen zu sehen." },
            { "tuto.levelup.done", "Perfekt! Komm regelmäßig hierher zurück, um das Hotel wachsen zu lassen. Schließe das Panel zum Fortfahren." },
            { "tuto.coins", "Die <b>Währung</b> hier ist der Cat Coin. Kaufe Objekte für deine Katzen... und bezahle Levelaufstiege!" },
            // --- HUD elements walkthrough ---
            { "tuto.hud.coins", "Das ist deine <b>Cat-Coin-Reserve</b>. Sie füllt sich, wenn du die Münzen glücklicher Katzen einsammelst. Wird zum Kaufen und Aufsteigen verwendet." },
            { "tuto.hud.purrls", "<b>Puurls</b> sind die Premium-Währung. Später kannst du damit besondere Objekte mit großen Boni kaufen oder Einnahmen-Boosts aktivieren." },
            { "tuto.hud.capacity", "Die <b>Auslastung</b> des Hotels: Prozent der belegten Plätze im Verhältnis zum Maximum deiner Stufe." },
            { "tuto.hud.comfort", "Der <b>durchschnittliche Komfort</b> des Hotels. Basis 50%, +5% bei wenig Belegung (<20%), steigt mit Dekoration und sinkt bei Überfüllung (>80%)." },
            { "tuto.hud.floors", "Die aktuell angezeigte <b>Etage</b>. Mit den Pfeilen wechselst du zwischen freigeschalteten Etagen." },
            { "tuto.hud.system", "Das <b>Pause-Menü</b>. Einstellungen, Sprache, Ton und Spieloptionen." },
            { "tuto.hud.collectall", "<b>Sammle alle Münzen</b> auf einmal. Kosten: 5 Cat Coins. Praktisch wenn das Hotel voll ist!" },
            { "tuto.hud.addboost", "Sieh dir eine Werbung an, um <b>alle deine Einnahmen 5 Minuten lang zu verdoppeln</b>." },
            { "tuto.hud.nextcat", "Der <b>Countdown</b> bis zur nächsten Katze. Je größer dein Hotel, desto schneller der Rhythmus." },
            { "tuto.hud.shop", "Der <b>Objekt-Shop</b>. Hier kaufst du alles, was deine Katzen brauchen." },
            { "tuto.pension", "Katzen kommen für eine festgelegte Zeit in <b>Pension</b>. Halte ihr Glück möglichst hoch, um maximal Cat Coins zu verdienen!" },
            { "tuto.refuge", "Traurige Katzen kommen über das <b>Tierheim</b>. Mach sie glücklich, damit sie adoptiert werden können!" },
            { "tuto.unhappy", "Achtung: Eine Katze, deren Glück unter <b>20%</b> fällt, will gehen. Lass das nicht zu!" },
            { "tuto.firstcat", "Oh! Dein erster Gast kommt! Beobachte ihn..." },
            { "tuto.selectcat", "Tippe auf die Katze, um sie auszuwählen." },
            { "tuto.catinfo", "Hier sind ihre Infos: Name, Rasse, Stimmung und Bedürfnisse. Behalte sie im Auge!" },
            { "tuto.pensiontime", "Schau auf die verbleibende Pensionszeit. Wenn sie abläuft, geht die Katze. Je glücklicher sie ist, desto mehr verdienst du!" },
            { "tuto.refugecat", "Eine Tierheim-Katze ist da! Sie hat Hunger... Schau dir ihre Anzeigen an: Sie braucht Futter." },
            { "tuto.buyfood", "Öffne den Shop und kaufe einen <b>Futternapf</b>, dann stell ihn ins Zimmer." },
            { "tuto.waiteat", "Gut! Warte, bis die Katze frisst..." },
            { "tuto.collectcoin", "Eine Münze ist erschienen! Tippe darauf, um deine <b>Cat Coins</b> zu sammeln." },
            { "tuto.thirsty", "Sie hat jetzt Durst! Kauf <b>Wasser</b> und stell es hin." },
            { "tuto.waitdrink", "Lass sie in Ruhe trinken..." },
            { "tuto.sleepy", "Die Katze ist müde. Bring ihr ein <b>Bett</b> oder ein <b>Kissen</b>!" },
            { "tuto.waitsleep", "Sie ruht sich aus... pssst!" },
            { "tuto.dirty", "Sie braucht eine <b>Katzentoilette</b>! Kauf eine." },
            { "tuto.waitclean", "Sie putzt sich..." },
            { "tuto.bored", "Sie will spielen! Kauf einen <b>Wollknäuel</b>." },
            { "tuto.waitplay", "Sieh ihr beim Spielen zu!" },
            { "tuto.complete", "Bravo! Du kennst die Grundlagen. Das Hotel gehört dir, viel Glück!" },

            // --- Sound ---
            { "sound.on", "Ton an" },
            { "sound.off", "Ton aus" },

            // --- Mood ---
            { "mood.happy", "Zufrieden" },
            { "mood.ecstatic", "Begeistert" },
            { "mood.depressed", "Niedergeschlagen" },
            { "mood.aggressive", "Aggressiv" },
            { "mood.angry", "Wütend" },

            // --- Needs ---
            { "need.hungry", "Hungrig" },
            { "need.thirsty", "Durstig" },
            { "need.tired", "Müde" },
            { "need.bored", "Gelangweilt" },
            { "need.dirty", "Schmutzig" },

            // --- HUD ---
            { "hud.floor.ground", "EG" },
            { "hud.floor", "Etage {0}" },
            { "hud.level", "Stufe {0}" },
            { "levelup.title.current", "Stufe {0}" },
            { "levelup.title.next", "Stufe {0}" },
            { "levelup.newstuff", "Neuheiten" },
            { "levelup.condition.xp", "{0} / {1} XP" },
            { "levelup.condition.happycats", "{0} von {1} Katzen > 75% Glück" },
            { "levelup.newfloor", "Etage {0} freigeschaltet" },
            { "levelup.capacity.title", "Hotelkapazität" },
            { "levelup.go.next", "Zur nächsten Stufe" },
            { "hud.level.max", "MAX" },
            { "hud.level.max.reached", "Maximalstufe erreicht!" },
            { "hud.level.objective", "{0}/{1} Katzen mit +{2}% Glück" },
            { "hud.boost.active", "x2 Boost aktiv! {0}s" },
            { "hud.boost.button", "Doppelte Einnahmen" },
            { "hud.collect.label", "Alles einsammeln" },

            // --- Reputation Level Names ---
            { "rep.0", "Anfänger" },
            { "rep.1", "Amateur" },
            { "rep.2", "Geübt" },
            { "rep.3", "Profi" },
            { "rep.4", "Experte" },
            { "rep.5", "Bekannt" },
            { "rep.6", "Berühmt" },
            { "rep.7", "Renommiert" },
            { "rep.8", "Elite" },
            { "rep.9", "Legendär" },
            { "rep.10", "Katzenmeister" },

            // --- Shop Categories ---
            { "shop.beds", "Betten" },
            { "shop.pillows", "Kissen" },
            { "shop.croquettes", "Futternäpfe" },
            { "shop.water", "Wasser" },
            { "shop.balls", "Bälle" },
            { "shop.scratchers", "Kratzpfosten" },
            { "shop.litters", "Katzentoiletten" },
            { "shop.frames", "Bilderrahmen" },
            { "shop.lamps", "Lampen" },
            { "shop.tables", "Tische" },
            { "shop.plants", "Pflanzen" },
            { "shop.shelves", "Regale" },
            { "shop.aquariums", "Aquarien" },
            { "shop.carpets", "Teppiche" },
            { "shop.trees", "Kratzbäume" },

            // --- Breed Names ---
            { "breed.europeen", "Europäer" },
            { "breed.siamois", "Siamkatze" },
            { "breed.ragdoll", "Ragdoll" },
            { "breed.siberien", "Sibirische Katze" },
            { "breed.chartreux", "Kartäuser" },

            // --- Personality: Race Traits ---
            { "trait.hunger", "Vielfraß" },
            { "trait.thirst", "Durstig" },
            { "trait.sleep", "Schlafmütze" },
            { "trait.play", "Verspielt" },
            { "trait.clean", "Reinlich" },
            { "trait.aggressive", "Kämpfer" },
            { "trait.big", "Groß" },
            { "trait.small", "Klein" },
            { "trait.fast", "Flink" },
            { "trait.slow", "Gemütlich" },

            // --- Personality: Personality Pool ---
            { "personality.cuddly", "Verschmust" },
            { "personality.independent", "Unabhängig" },
            { "personality.curious", "Neugierig" },
            { "personality.fearful", "Ängstlich" },
            { "personality.affectionate", "Anhänglich" },
            { "personality.observer", "Beobachter" },
            { "personality.discreet", "Diskret" },
            { "personality.clingy", "Klette" },
            { "personality.adventurer", "Abenteurer" },
            { "personality.territorial", "Territorial" },
            { "personality.sociable", "Gesellig" },
            { "personality.solitary", "Einzelgänger" },
            { "personality.clever", "Schlau" },
            { "personality.lazy", "Faul" },
            { "personality.loyal", "Treu" },
            { "personality.capricious", "Launisch" },

            // --- Personality: Quirky Pool ---
            { "quirk.grudge", "Nachtragend" },
            { "quirk.touchy", "Empfindlich" },
            { "quirk.dumb", "Tollpatschig" },
            { "quirk.big_eater", "Vielfraß" },
            { "quirk.drinker", "Säufer" },
            { "quirk.food_thief", "Futterdieb" },
            { "quirk.snorer", "Schnarcher" },
            { "quirk.litter_fear", "Toilettenscheu" },
            { "quirk.jealous", "Eifersüchtig" },
            { "quirk.show_off", "Angeber" },
            { "quirk.drama_queen", "Drama-Queen" },
            { "quirk.hair_collector", "Haarsammler" },
            { "quirk.grumpy", "Mürrisch" },
            { "quirk.clumsy", "Ungeschickt" },
            { "quirk.hypochondriac", "Hypochonder" },
            { "quirk.couch_king", "Couch-König" },
            { "quirk.snob", "Snob" },
            { "quirk.pilferer", "Langfinger" },

            // --- Scene Labels ---
            // Boot scene
            { "ui.play", "Spielen" },
            { "ui.continue", "Spiel fortsetzen" },
            { "ui.new_game", "Neues Spiel" },

            // Credits panel
            { "credits.title", "Mitwirkende" },
            { "credits.gameby", "ein Spiel von" },
            { "credits.direction", "Leitung / Entwicklung" },
            { "credits.disclaimer", "Dieses Spiel wurde mit der Unity-Engine erstellt, die Eigentum von Unity Technologies ist. Unity und das Unity-Logo sind eingetragene Marken von Unity Technologies in den Vereinigten Staaten von Amerika und in anderen Ländern" },
            { "ui.credits", "Mitwirkende" },
            { "ui.parameters", "Einstellungen" },

            // Options panel
            { "ui.resume", "Weiter" },
            { "ui.main_menu", "Hauptmenü" },
            { "ui.meowdex", "Meowdex" },

            // Parameters panel
            { "param.languages", "Sprachen" },
            { "param.notifications", "Push-Benachrichtigungen" },
            { "param.battery", "Energiesparmodus" },
            { "param.music_volume", "Musiklautstärke" },
            { "param.effects_volume", "Effektlautstärke" },

            // Shop
            { "ui.shop", "Shop" },
            { "shop.category.food", "Futter" },
            { "shop.category.drink", "Getränk" },
            { "shop.category.sleep", "Schlaf" },
            { "shop.category.play", "Spiel" },
            { "shop.category.clean", "Hygiene" },
            { "shop.category.comfort", "Komfort" },
            { "shop.category.deco", "Dekoration" },
            { "shop.double_gains", "Einnahmen verdoppeln" },

            // Cat Info Panel
            { "cat.name_label", "Katzenname" },
            { "cat.race_label", "Rasse" },
            { "cat.age_label", "Alter" },
            { "cat.character_label", "Charakter" },
            { "cat.affinity_label", "Vorlieben" },
            { "cat.needs_label", "Bedürfnisse" },
            { "cat.need.hunger", "Hunger" },
            { "cat.need.thirst", "Durst" },
            { "cat.need.sleep", "Schlaf" },
            { "cat.need.play", "Spiel" },
            { "cat.need.clean", "Hygiene" },
            { "cat.need.happiness", "Glück" },

            // End Pension Panel
            { "pension.title", "Pensionsaufenthalt beendet!" },
            { "pension.happiness", "Durchschnittliches Glück während des Aufenthalts:" },
            { "pension.details", "Zahlungsdetails" },
            { "pension.base", "Basis" },
            { "pension.tip", "Trinkgeld" },
            { "pension.total", "Gesamt" },
            { "pension.time_remaining", "Verbleibende Zeit" },

            // HUD
            { "hud.boost.label", "x2 Münzensammel-Boost aktiv!" },

            // Object display names (shop items)
            { "obj.bed", "Bett" },
            { "obj.luxury_bed", "Luxusbett" },
            { "obj.pillow", "Kissen" },
            { "obj.food_bowl", "Futternapf" },
            { "obj.food_bowl_v2", "Futternapf V.2" },
            { "obj.food_bowl_v3", "Futternapf V.3" },
            { "obj.food_bowl_v4", "Futternapf V.4" },
            { "obj.water_bowl", "Wassernapf" },
            { "obj.water_bowl_04", "Moderner Wassernapf" },
            { "obj.water_bowl_v2", "Wassernapf V.2" },
            { "obj.water_bowl_v3", "Wassernapf V.3" },
            { "obj.wool_ball", "Wollknäuel" },
            { "obj.cat_tree", "Kratzbaum" },
            { "obj.scratcher", "Kratzpfosten" },
            { "obj.litter", "Katzentoilette" },
            { "obj.litter_v1", "Katzentoilette V." },
            { "obj.frame_1", "Bilderrahmen 1" },
            { "obj.frame_2", "Bilderrahmen 2" },
            { "obj.frame_3", "Bilderrahmen 3" },
            { "obj.painting", "Gemälde" },
            { "obj.lamp", "Stehlampe" },
            { "obj.coffee_table", "Couchtisch" },
            { "obj.drawer", "Kommode" },
            { "obj.plant_big", "Große Pflanze" },
            { "obj.plant_small", "Kleine Pflanze" },
            { "obj.shelf", "Regal" },
            { "obj.shelf_v1", "Regal V." },
            { "obj.aquarium", "Aquarium" },
            { "obj.carpet_confort", "Komfort-Teppich" },
            { "obj.carpet_play", "Spiel-Teppich" },
            { "obj.carpet_cosmic", "Kosmischer Teppich" },

            // --- Cat Names ---
            { "names.pool", "Mieze,Felix,Karamell,Luna,Tiger,Haselnuss,Schnurri,Pascha,Zimt,Krickel,Kätzchen,Kuschel,Schlingel,Frechdachs,Lakritze,Perle,Simba,Feder,Keks,Cookie,Praline,Nougat,Macaron,Brioche,Muffin,Crumble,Tiramisu,Baiser,Brownie,Fudge,Kosmos,Mond,Stern,Komet,Nova,Nebula,Sonne,Aurora,Eclipse,Galaxie,Astro,Pluto,Rubin,Saphir,Smaragd,Jade,Opal,Bernstein,Topas,Diamant,Kristall,Onyx,Elfenbein,Koralle,Ninja,Pixel,Wifi,Sushi,Tofu,Wasabi,Mozart,Picasso,Darwin,Merlin,Zorro,Gatsby,Eule,Schmetterling,Kolibri,Schatten,Fuchs,Otter,Odyssee,Belle,Romeo,Vanille,Zucker,Kakao,Latte,Madeleine,Vega,Sirius,Andromeda,Apollo,Atlas,Mars,Indigo,Amethyst,Türkis,Quarz,Voltaire,Hugo,Tesla,Curie,Athena,Hermes,Iris,Pandora,Schmunzel,Hase,Wiesel,Biber,Vortex,Matrix" },
        };

        // ==================== SPANISH ====================
        public static readonly Dictionary<string, string> Spanish = new()
        {
            // --- Consent / RGPD ---
            { "consent.title", "Privacidad y anuncios" },
            { "consent.body", "Para apoyar el desarrollo del juego, Meowtel ofrece anuncios opcionales con recompensa. ¿Aceptas el uso de tu identificador publicitario para mostrar anuncios personalizados? Puedes cambiar esta elección en cualquier momento en el menú de pausa, en «Privacidad»." },
            { "consent.accept", "Aceptar" },
            { "consent.refuse", "Rechazar" },
            { "consent.analytics.title", "Estadísticas de uso" },
            { "consent.analytics.body", "Para mejorar el juego, ¿aceptas que Meowtel envíe a Unity Analytics estadísticas de uso (progreso, anuncios vistos, tipo de dispositivo), vinculadas a un identificador técnico de jugador? Rechazar no cambia nada en tu partida. Puedes cambiar esta elección en cualquier momento en el menú de pausa, en «Privacidad»." },
            { "consent.current", "Tu elección actual: {0}." },
            { "consent.state.accepted", "aceptada" },
            { "consent.state.refused", "rechazada" },
            { "ui.privacy", "Privacidad" },

            // --- Cat Info Panel ---
            { "cat.kitten", "Gatito" },
            { "cat.adult", "Gato adulto" },
            { "cat.stay.pension", "En pensión" },
            { "cat.stay.refuge", "En el refugio" },
            { "cat.likes", "Le gustan {0}" },
            { "cat.dislikes", "No le gustan {0}" },
            { "cat.time.none", "--:--:--" },

            // --- End Pension Panel ---
            { "pension.bye", "¡Adiós {0}!" },
            { "adoption.title", "¡Adopción completada!" },
            { "adoption.happiness", "Felicidad al ser adoptado:" },
            { "adoption.fee", "Tasa de adopción" },
            { "adoption.bye", "¡Que seas muy feliz, {0}!" },

            // --- Loading ---
            { "ui.loading", "Cargando" },
            { "loading.tip.0", "Acaricia a tus gatos para hacerlos felices." },
            { "loading.tip.1", "Un gato infeliz puede irse sin pagar." },
            { "loading.tip.2", "Toca dos veces un objeto para moverlo." },
            { "loading.tip.3", "Las alfombras dan un bonus de confort a la zona." },
            { "loading.tip.4", "Algunas razas no se llevan bien — vigílalas." },
            { "loading.tip.5", "Cuanta más reputación, más exigentes los gatos." },
            { "loading.tip.6", "Las escaleras llevan a los pisos superiores." },
            { "loading.tip.7", "Recoge tus monedas rápido antes de que desaparezcan." },

            // --- Tutorial (Narration) ---
            { "tuto.speaker.jasper", "Jasper" },
            { "tuto.speaker.winston", "Winston" },
            { "tuto.skip.link", "Saltar tutorial" },
            { "tuto.skip.ask", "¿Saltar tutorial?" },
            { "tuto.skip.yes", "Sí" },
            { "tuto.skip.no", "No" },
            { "tuto.welcome", "¡Bienvenido a <b>Meowtel</b>!\nSoy Jasper, el conserje." },
            { "tuto.explain", "Aquí recibes gatos en pensión y refugio. ¡Cuídalos y te recompensarán!" },
            { "tuto.floors", "El hotel tiene <b>10 pisos</b> que desbloquear sobre la planta baja. ¡Cuanto más crezca, más gatos podrás acoger!" },
            { "tuto.levelup.intro", "Para desbloquear un piso nuevo, debes <b>subir tu nivel de reputación</b>." },
            { "tuto.levelup.xp", "Gana <b>XP</b> haciendo felices a los gatos. Cada pensión exitosa y cada adopción otorgan XP." },
            { "tuto.levelup.cond", "Para subir de nivel necesitas: suficiente <b>XP</b>, suficientes <b>gatos con más del 75% de felicidad</b>, y pagar en <b>Cat Coins</b>." },
            { "tuto.levelup.tap", "Toca la <b>barra de nivel</b> de arriba para ver tus condiciones actuales." },
            { "tuto.levelup.done", "¡Perfecto! Vuelve aquí regularmente para hacer crecer el hotel. Cierra el panel para continuar." },
            { "tuto.coins", "La <b>moneda</b> aquí es el Cat Coin. ¡Compra objetos para tus gatos... y paga las subidas de nivel!" },
            // --- HUD elements walkthrough ---
            { "tuto.hud.coins", "Esta es tu <b>reserva de Cat Coins</b>. Se llena al recoger las monedas de los gatos felices. Sirve para comprar y subir de nivel." },
            { "tuto.hud.purrls", "Los <b>Puurls</b> son la moneda premium. Más adelante podrás comprar objetos especiales con grandes bonus o activar boosts de ingresos." },
            { "tuto.hud.capacity", "La <b>capacidad</b> del hotel: porcentaje de plazas ocupadas respecto al máximo de tu nivel." },
            { "tuto.hud.comfort", "El <b>confort medio</b> del hotel. Base 50%, +5% si está poco lleno (<20%), sube con la decoración y baja si está saturado (>80%)." },
            { "tuto.hud.floors", "El <b>piso</b> mostrado actualmente. Usa las flechas para subir y bajar cuando los desbloquees." },
            { "tuto.hud.system", "El <b>menú de pausa</b>. Ajustes, idioma, sonido y opciones." },
            { "tuto.hud.collectall", "<b>Recoger todas las monedas</b> de una vez. Coste: 5 Cat Coins. ¡Útil cuando el hotel se llena!" },
            { "tuto.hud.addboost", "Ve un anuncio para <b>duplicar todos tus ingresos durante 5 minutos</b>." },
            { "tuto.hud.nextcat", "La <b>cuenta atrás</b> hasta el próximo gato. Cuanto más crezca tu hotel, más rápido el ritmo." },
            { "tuto.hud.shop", "La <b>tienda de objetos</b>. Compra aquí todo lo que tus gatos necesitan." },
            { "tuto.pension", "Los gatos llegan en <b>pensión</b> por un tiempo determinado. ¡Mantén su felicidad alta para ganar el máximo de Cat Coins!" },
            { "tuto.refuge", "Gatos tristes llegan por el <b>refugio</b>. ¡Hazlos felices para que puedan ser adoptados!" },
            { "tuto.unhappy", "Atención: un gato cuya felicidad baje del <b>20%</b> querrá irse. ¡No lo dejes escapar!" },
            { "tuto.firstcat", "¡Oh! ¡Llega tu primer huésped! Obsérvalo..." },
            { "tuto.selectcat", "Toca al gato para seleccionarlo." },
            { "tuto.catinfo", "Aquí tienes su info: nombre, raza, humor y necesidades. ¡Vigílalo!" },
            { "tuto.pensiontime", "Mira el tiempo restante en pensión. Cuando termine, el gato se irá. ¡Cuanto más feliz, más ganas!" },
            { "tuto.refugecat", "¡Llega un gato del refugio! Tiene hambre... Mira sus indicadores: necesita comida." },
            { "tuto.buyfood", "Abre la tienda y compra un <b>comedero</b>, luego colócalo en la habitación." },
            { "tuto.waiteat", "¡Bien! Espera a que el gato coma..." },
            { "tuto.collectcoin", "¡Apareció una moneda! Tócala para recoger tus <b>Cat Coins</b>." },
            { "tuto.thirsty", "¡Ahora tiene sed! Compra <b>agua</b> y colócala." },
            { "tuto.waitdrink", "Déjalo beber tranquilo..." },
            { "tuto.sleepy", "El gato está cansado. ¡Dale una <b>cama</b> o un <b>cojín</b>!" },
            { "tuto.waitsleep", "Está descansando... ¡shh!" },
            { "tuto.dirty", "¡Necesita un <b>arenero</b>! Compra uno." },
            { "tuto.waitclean", "Se está aseando..." },
            { "tuto.bored", "¡Quiere jugar! Compra un <b>ovillo de lana</b>." },
            { "tuto.waitplay", "¡Míralo divertirse!" },
            { "tuto.complete", "¡Bravo! Ya conoces lo básico. ¡El hotel es tuyo, suerte!" },

            // --- Sound ---
            { "sound.on", "Sonido activado" },
            { "sound.off", "Sonido desactivado" },

            // --- Mood ---
            { "mood.happy", "Contento" },
            { "mood.ecstatic", "Entusiasta" },
            { "mood.depressed", "Deprimido" },
            { "mood.aggressive", "Agresivo" },
            { "mood.angry", "Enfadado" },

            // --- Needs ---
            { "need.hungry", "Hambriento" },
            { "need.thirsty", "Sediento" },
            { "need.tired", "Cansado" },
            { "need.bored", "Aburrido" },
            { "need.dirty", "Sucio" },

            // --- HUD ---
            { "hud.floor.ground", "PB" },
            { "hud.floor", "Piso {0}" },
            { "hud.level", "Nivel {0}" },
            { "levelup.title.current", "Nivel {0}" },
            { "levelup.title.next", "Nivel {0}" },
            { "levelup.newstuff", "Novedades" },
            { "levelup.condition.xp", "{0} / {1} XP" },
            { "levelup.condition.happycats", "{0} gatos > 75% de felicidad de {1}" },
            { "levelup.newfloor", "Piso {0} desbloqueado" },
            { "levelup.capacity.title", "Capacidad del hotel" },
            { "levelup.go.next", "Pasar al siguiente nivel" },
            { "hud.level.max", "MÁX" },
            { "hud.level.max.reached", "¡Nivel máximo alcanzado!" },
            { "hud.level.objective", "{0}/{1} gatos con +{2}% de felicidad" },
            { "hud.boost.active", "¡Boost x2 activo! {0}s" },
            { "hud.boost.button", "Ganancias dobladas" },
            { "hud.collect.label", "Recoger todo" },

            // --- Reputation Level Names ---
            { "rep.0", "Principiante" },
            { "rep.1", "Aficionado" },
            { "rep.2", "Competente" },
            { "rep.3", "Profesional" },
            { "rep.4", "Experto" },
            { "rep.5", "Renombrado" },
            { "rep.6", "Famoso" },
            { "rep.7", "Prestigioso" },
            { "rep.8", "Élite" },
            { "rep.9", "Legendario" },
            { "rep.10", "Maestro Felino" },

            // --- Shop Categories ---
            { "shop.beds", "Camas" },
            { "shop.pillows", "Cojines" },
            { "shop.croquettes", "Comederos" },
            { "shop.water", "Agua" },
            { "shop.balls", "Pelotas" },
            { "shop.scratchers", "Rascadores" },
            { "shop.litters", "Areneros" },
            { "shop.frames", "Cuadros" },
            { "shop.lamps", "Lámparas" },
            { "shop.tables", "Mesas" },
            { "shop.plants", "Plantas" },
            { "shop.shelves", "Estantes" },
            { "shop.aquariums", "Acuarios" },
            { "shop.carpets", "Alfombras" },
            { "shop.trees", "Árboles para gatos" },

            // --- Breed Names ---
            { "breed.europeen", "Europeo" },
            { "breed.siamois", "Siamés" },
            { "breed.ragdoll", "Ragdoll" },
            { "breed.siberien", "Siberiano" },
            { "breed.chartreux", "Cartujo" },

            // --- Personality: Race Traits ---
            { "trait.hunger", "Glotón" },
            { "trait.thirst", "Sediento" },
            { "trait.sleep", "Dormilón" },
            { "trait.play", "Juguetón" },
            { "trait.clean", "Maniático" },
            { "trait.aggressive", "Peleón" },
            { "trait.big", "Imponente" },
            { "trait.small", "Pequeño" },
            { "trait.fast", "Rápido" },
            { "trait.slow", "Tranquilo" },

            // --- Personality: Personality Pool ---
            { "personality.cuddly", "Mimoso" },
            { "personality.independent", "Independiente" },
            { "personality.curious", "Curioso" },
            { "personality.fearful", "Miedoso" },
            { "personality.affectionate", "Cariñoso" },
            { "personality.observer", "Observador" },
            { "personality.discreet", "Discreto" },
            { "personality.clingy", "Pegajoso" },
            { "personality.adventurer", "Aventurero" },
            { "personality.territorial", "Territorial" },
            { "personality.sociable", "Sociable" },
            { "personality.solitary", "Solitario" },
            { "personality.clever", "Astuto" },
            { "personality.lazy", "Perezoso" },
            { "personality.loyal", "Fiel" },
            { "personality.capricious", "Caprichoso" },

            // --- Personality: Quirky Pool ---
            { "quirk.grudge", "Rencoroso" },
            { "quirk.touchy", "Susceptible" },
            { "quirk.dumb", "Despistado" },
            { "quirk.big_eater", "Comilón" },
            { "quirk.drinker", "Bebedor" },
            { "quirk.food_thief", "Ladrón de comida" },
            { "quirk.snorer", "Roncador" },
            { "quirk.litter_fear", "Miedo al arenero" },
            { "quirk.jealous", "Celoso" },
            { "quirk.show_off", "Presumido" },
            { "quirk.drama_queen", "Drama queen" },
            { "quirk.hair_collector", "Coleccionista de pelos" },
            { "quirk.grumpy", "Gruñón" },
            { "quirk.clumsy", "Torpe" },
            { "quirk.hypochondriac", "Hipocondríaco" },
            { "quirk.couch_king", "Rey del sofá" },
            { "quirk.snob", "Esnob" },
            { "quirk.pilferer", "Hurtador" },

            // --- Scene Labels ---
            // Boot scene
            { "ui.play", "Jugar" },
            { "ui.continue", "Continuar partida" },
            { "ui.new_game", "Nueva partida" },

            // Credits panel
            { "credits.title", "Créditos" },
            { "credits.gameby", "un juego de" },
            { "credits.direction", "Dirección / desarrollo" },
            { "credits.disclaimer", "Este juego fue creado con el motor Unity, propiedad de Unity Technologies. Unity y el logo de Unity son marcas registradas de Unity Technologies en los Estados Unidos de América y en otros países" },
            { "ui.credits", "Créditos" },
            { "ui.parameters", "Ajustes" },

            // Options panel
            { "ui.resume", "Reanudar" },
            { "ui.main_menu", "Menú principal" },
            { "ui.meowdex", "Meowdex" },

            // Parameters panel
            { "param.languages", "Idiomas" },
            { "param.notifications", "Notificaciones push" },
            { "param.battery", "Ahorro de batería" },
            { "param.music_volume", "Volumen de música" },
            { "param.effects_volume", "Volumen de efectos" },

            // Shop
            { "ui.shop", "Tienda" },
            { "shop.category.food", "Comida" },
            { "shop.category.drink", "Bebida" },
            { "shop.category.sleep", "Sueño" },
            { "shop.category.play", "Juego" },
            { "shop.category.clean", "Higiene" },
            { "shop.category.comfort", "Confort" },
            { "shop.category.deco", "Decoración" },
            { "shop.double_gains", "Duplicar ganancias" },

            // Cat Info Panel
            { "cat.name_label", "Nombre del gato" },
            { "cat.race_label", "Raza" },
            { "cat.age_label", "Edad" },
            { "cat.character_label", "Carácter" },
            { "cat.affinity_label", "Afinidades" },
            { "cat.needs_label", "Necesidades" },
            { "cat.need.hunger", "Hambre" },
            { "cat.need.thirst", "Sed" },
            { "cat.need.sleep", "Sueño" },
            { "cat.need.play", "Juego" },
            { "cat.need.clean", "Higiene" },
            { "cat.need.happiness", "Felicidad" },

            // End Pension Panel
            { "pension.title", "¡Fin de la estancia en pensión!" },
            { "pension.happiness", "Felicidad media durante la estancia:" },
            { "pension.details", "Detalles del pago" },
            { "pension.base", "Base" },
            { "pension.tip", "Propina" },
            { "pension.total", "Total" },
            { "pension.time_remaining", "Tiempo restante en pensión" },

            // HUD
            { "hud.boost.label", "¡Boost x2 de recolección de cat coins activo!" },

            // Object display names (shop items)
            { "obj.bed", "Cama" },
            { "obj.luxury_bed", "Cama de lujo" },
            { "obj.pillow", "Cojín" },
            { "obj.food_bowl", "Comedero" },
            { "obj.food_bowl_v2", "Comedero var. 2" },
            { "obj.food_bowl_v3", "Comedero var. 3" },
            { "obj.food_bowl_v4", "Comedero var. 4" },
            { "obj.water_bowl", "Bebedero" },
            { "obj.water_bowl_04", "Bebedero moderno" },
            { "obj.water_bowl_v2", "Bebedero var. 2" },
            { "obj.water_bowl_v3", "Bebedero var. 3" },
            { "obj.wool_ball", "Ovillo de lana" },
            { "obj.cat_tree", "Árbol para gatos" },
            { "obj.scratcher", "Rascador" },
            { "obj.litter", "Arenero" },
            { "obj.litter_v1", "Arenero var." },
            { "obj.frame_1", "Cuadro 1" },
            { "obj.frame_2", "Cuadro 2" },
            { "obj.frame_3", "Cuadro 3" },
            { "obj.painting", "Pintura" },
            { "obj.lamp", "Lámpara de pie" },
            { "obj.coffee_table", "Mesa de centro" },
            { "obj.drawer", "Cómoda" },
            { "obj.plant_big", "Planta grande" },
            { "obj.plant_small", "Planta pequeña" },
            { "obj.shelf", "Estante" },
            { "obj.shelf_v1", "Estante var." },
            { "obj.aquarium", "Acuario" },
            { "obj.carpet_confort", "Alfombra Confort" },
            { "obj.carpet_play", "Alfombra Juego" },
            { "obj.carpet_cosmic", "Alfombra Cósmica" },

            // --- Cat Names ---
            { "names.pool", "Michi,Felix,Caramelo,Luna,Tigre,Avellana,Bigotes,Pacha,Canela,Garabato,Gatito,Mimoso,Pillo,Traviesa,Regaliz,Perla,Simba,Pluma,Galleta,Cookie,Praliné,Turrón,Macaron,Brioche,Muffin,Crumble,Tiramisú,Merengue,Brownie,Toffee,Cosmos,Lunita,Estrella,Cometa,Nova,Nebula,Sol,Aurora,Eclipse,Galaxia,Astro,Plutón,Rubí,Zafiro,Esmeralda,Jade,Ópalo,Ámbar,Topacio,Diamante,Cristal,Ónix,Marfil,Coral,Ninja,Pixel,Wifi,Sushi,Tofu,Wasabi,Mozart,Picasso,Darwin,Merlín,Zorro,Gatsby,Búho,Mariposa,Colibrí,Sombra,Zorrito,Nutria,Odisea,Belle,Romeo,Vainilla,Azúcar,Cacao,Latte,Madeleine,Vega,Sirio,Andrómeda,Apolo,Atlas,Marte,Índigo,Amatista,Turquesa,Cuarzo,Voltaire,Hugo,Tesla,Curie,Atenea,Hermes,Iris,Pandora,Bombón,Conejito,Comadreja,Castor,Vortex,Matrix" },
        };

        // ==================== PORTUGUESE (BR) ====================
        public static readonly Dictionary<string, string> Portuguese = new()
        {
            // --- Consent / LGPD ---
            { "consent.title", "Privacidade e anúncios" },
            { "consent.body", "Para apoiar o desenvolvimento do jogo, o Meowtel oferece anúncios opcionais com recompensa. Você aceita o uso do seu identificador de publicidade para exibir anúncios personalizados? Você pode alterar esta escolha a qualquer momento no menu de pausa, em “Privacidade”." },
            { "consent.accept", "Aceitar" },
            { "consent.refuse", "Recusar" },
            { "consent.analytics.title", "Estatísticas de uso" },
            { "consent.analytics.body", "Para melhorar o jogo, você aceita que o Meowtel envie ao Unity Analytics estatísticas de uso (progresso, anúncios assistidos, tipo de aparelho), vinculadas a um identificador técnico de jogador? Recusar não muda nada no seu jogo. Você pode alterar esta escolha a qualquer momento no menu de pausa, em “Privacidade”." },
            { "consent.current", "Sua escolha atual: {0}." },
            { "consent.state.accepted", "aceita" },
            { "consent.state.refused", "recusada" },
            { "ui.privacy", "Privacidade" },

            // --- Cat Info Panel ---
            { "cat.kitten", "Gatinho" },
            { "cat.adult", "Gato adulto" },
            { "cat.stay.pension", "Em hospedagem" },
            { "cat.stay.refuge", "No abrigo" },
            { "cat.likes", "Gosta de {0}" },
            { "cat.dislikes", "Não gosta de {0}" },
            { "cat.time.none", "--:--:--" },

            // --- End Pension Panel ---
            { "pension.bye", "Tchau {0}!" },
            { "adoption.title", "Adoção concluída!" },
            { "adoption.happiness", "Felicidade na adoção:" },
            { "adoption.fee", "Taxa de adoção" },
            { "adoption.bye", "Seja muito feliz, {0}!" },

            // --- Loading ---
            { "ui.loading", "Carregando" },
            { "loading.tip.0", "Faça carinho nos seus gatos para deixá-los felizes." },
            { "loading.tip.1", "Um gato infeliz pode ir embora sem pagar." },
            { "loading.tip.2", "Toque duas vezes em um objeto para movê-lo." },
            { "loading.tip.3", "Os tapetes dão um bônus de conforto à zona." },
            { "loading.tip.4", "Algumas raças não se dão bem — fique de olho." },
            { "loading.tip.5", "Quanto mais famoso seu hotel, mais exigentes os gatos." },
            { "loading.tip.6", "As escadas levam aos andares superiores." },
            { "loading.tip.7", "Pegue suas moedas rápido antes que elas sumam." },

            // --- Tutorial (Narration) ---
            { "tuto.speaker.jasper", "Jasper" },
            { "tuto.speaker.winston", "Winston" },
            { "tuto.skip.link", "Pular tutorial" },
            { "tuto.skip.ask", "Pular tutorial?" },
            { "tuto.skip.yes", "Sim" },
            { "tuto.skip.no", "Não" },
            { "tuto.welcome", "Bem-vindo ao <b>Meowtel</b>!\nEu sou Jasper, o concierge." },
            { "tuto.explain", "Aqui, você recebe gatos para hospedagem e abrigo. Cuide bem deles e eles vão te recompensar!" },
            { "tuto.floors", "O hotel tem <b>10 andares</b> para desbloquear acima do térreo. Quanto maior o hotel, mais gatos você pode hospedar!" },
            { "tuto.levelup.intro", "Para desbloquear um novo andar, você precisa <b>subir de nível de reputação</b>." },
            { "tuto.levelup.xp", "Ganhe <b>XP</b> deixando os gatos felizes. Cada hospedagem bem-sucedida e cada adoção rendem XP." },
            { "tuto.levelup.cond", "Para subir de nível você precisa: <b>XP</b> suficiente, <b>gatos com mais de 75% de felicidade</b> suficientes, e pagar em <b>Cat Coins</b>." },
            { "tuto.levelup.tap", "Toque na <b>barra de nível</b> no topo para ver suas condições atuais." },
            { "tuto.levelup.done", "Perfeito! Volte aqui regularmente para fazer o hotel crescer. Feche o painel para continuar." },
            { "tuto.coins", "A <b>moeda</b> aqui é o Cat Coin. Compre objetos para seus gatos... e pague as subidas de nível!" },
            // --- HUD elements walkthrough ---
            { "tuto.hud.coins", "Esta é sua <b>reserva de Cat Coins</b>. Ela enche quando você recolhe as moedas deixadas pelos gatos felizes. Serve para comprar e subir de nível." },
            { "tuto.hud.purrls", "<b>Puurls</b> são a moeda premium. Mais tarde, eles permitirão comprar objetos especiais com grandes bônus, ou ativar boosts de ganhos." },
            { "tuto.hud.capacity", "A <b>capacidade</b> do hotel: porcentagem das vagas ocupadas em relação ao máximo permitido pelo seu nível." },
            { "tuto.hud.comfort", "O <b>conforto médio</b> do hotel. Base 50%, +5% se o hotel está pouco cheio (<20%), sobe com a decoração e cai quando lotado (>80%)." },
            { "tuto.hud.floors", "O <b>andar</b> exibido atualmente. Use as setas para subir e descer quando tiver desbloqueado." },
            { "tuto.hud.system", "O <b>menu de pausa</b>. Configurações, idioma, som e opções." },
            { "tuto.hud.collectall", "<b>Recolher todas as moedas</b> de uma vez. Custo: 5 Cat Coins. Útil quando o hotel está cheio!" },
            { "tuto.hud.addboost", "Veja um anúncio para <b>dobrar todos os seus ganhos por 5 minutos</b>." },
            { "tuto.hud.nextcat", "A <b>contagem regressiva</b> até o próximo gato. Quanto maior seu hotel, mais rápido o ritmo." },
            { "tuto.hud.shop", "A <b>loja de objetos</b>. Compre aqui tudo que seus gatos precisam." },
            { "tuto.pension", "Gatos chegam em <b>hospedagem</b> por um tempo determinado. Mantenha a felicidade deles alta para ganhar o máximo de Cat Coins!" },
            { "tuto.refuge", "Gatos tristes chegam pelo <b>abrigo</b>. Deixe-os felizes para que possam ser adotados!" },
            { "tuto.unhappy", "Atenção: um gato cuja felicidade cair abaixo de <b>20%</b> vai querer ir embora. Não deixe escapar!" },
            { "tuto.firstcat", "Oh! Seu primeiro cliente está chegando! Observe-o..." },
            { "tuto.selectcat", "Toque no gato para selecioná-lo." },
            { "tuto.catinfo", "Aqui estão suas informações: nome, raça, humor e necessidades. Fique de olho!" },
            { "tuto.pensiontime", "Veja o tempo restante de hospedagem. Quando acabar, o gato vai embora. Quanto mais feliz, mais você ganha!" },
            { "tuto.refugecat", "Um gato do abrigo chegou! Ele está com fome... Olhe os indicadores: ele precisa de comida." },
            { "tuto.buyfood", "Abra a loja e compre um <b>comedouro</b>, depois coloque no quarto." },
            { "tuto.waiteat", "Ótimo! Espere o gato comer..." },
            { "tuto.collectcoin", "Uma moeda apareceu! Toque nela para recolher seus <b>Cat Coins</b>." },
            { "tuto.thirsty", "Agora ele está com sede! Compre <b>água</b> e coloque." },
            { "tuto.waitdrink", "Deixe ele beber em paz..." },
            { "tuto.sleepy", "O gato está cansado. Dê uma <b>cama</b> ou um <b>travesseiro</b>!" },
            { "tuto.waitsleep", "Ele está descansando... shh!" },
            { "tuto.dirty", "Ele precisa de uma <b>caixa de areia</b>! Compre uma." },
            { "tuto.waitclean", "Ele está se limpando..." },
            { "tuto.bored", "Ele quer brincar! Compre uma <b>bola de lã</b>." },
            { "tuto.waitplay", "Veja ele se divertir!" },
            { "tuto.complete", "Parabéns! Você conhece o básico. O hotel é seu, boa sorte!" },

            // --- Sound ---
            { "sound.on", "Som ligado" },
            { "sound.off", "Som desligado" },

            // --- Mood ---
            { "mood.happy", "Contente" },
            { "mood.ecstatic", "Entusiasmado" },
            { "mood.depressed", "Deprimido" },
            { "mood.aggressive", "Agressivo" },
            { "mood.angry", "Bravo" },

            // --- Needs ---
            { "need.hungry", "Com fome" },
            { "need.thirsty", "Com sede" },
            { "need.tired", "Cansado" },
            { "need.bored", "Entediado" },
            { "need.dirty", "Sujo" },

            // --- HUD ---
            { "hud.floor.ground", "Térreo" },
            { "hud.floor", "Andar {0}" },
            { "hud.level", "Nível {0}" },
            { "levelup.title.current", "Nível {0}" },
            { "levelup.title.next", "Nível {0}" },
            { "levelup.newstuff", "Novidades" },
            { "levelup.condition.xp", "{0} / {1} XP" },
            { "levelup.condition.happycats", "{0} gatos > 75% de felicidade de {1}" },
            { "levelup.newfloor", "Andar {0} desbloqueado" },
            { "levelup.capacity.title", "Capacidade do hotel" },
            { "levelup.go.next", "Ir para o próximo nível" },
            { "hud.level.max", "MAX" },
            { "hud.level.max.reached", "Nível máximo alcançado!" },
            { "hud.level.objective", "{0}/{1} gatos com +{2}% de felicidade" },
            { "hud.boost.active", "Boost x2 ativo! {0}s" },
            { "hud.boost.button", "Ganhos dobrados" },
            { "hud.collect.label", "Coletar tudo" },

            // --- Reputation Level Names ---
            { "rep.0", "Iniciante" },
            { "rep.1", "Amador" },
            { "rep.2", "Competente" },
            { "rep.3", "Profissional" },
            { "rep.4", "Especialista" },
            { "rep.5", "Renomado" },
            { "rep.6", "Famoso" },
            { "rep.7", "Prestigiado" },
            { "rep.8", "Elite" },
            { "rep.9", "Lendário" },
            { "rep.10", "Mestre dos Gatos" },

            // --- Shop Categories ---
            { "shop.beds", "Camas" },
            { "shop.pillows", "Travesseiros" },
            { "shop.croquettes", "Comedouros" },
            { "shop.water", "Água" },
            { "shop.balls", "Bolas" },
            { "shop.scratchers", "Arranhadores" },
            { "shop.litters", "Caixas de areia" },
            { "shop.frames", "Quadros" },
            { "shop.lamps", "Lâmpadas" },
            { "shop.tables", "Mesas" },
            { "shop.plants", "Plantas" },
            { "shop.shelves", "Prateleiras" },
            { "shop.aquariums", "Aquários" },
            { "shop.carpets", "Tapetes" },
            { "shop.trees", "Árvores para gatos" },

            // --- Breed Names ---
            { "breed.europeen", "Europeu" },
            { "breed.siamois", "Siamês" },
            { "breed.ragdoll", "Ragdoll" },
            { "breed.siberien", "Siberiano" },
            { "breed.chartreux", "Chartreux" },

            // --- Personality: Race Traits ---
            { "trait.hunger", "Guloso" },
            { "trait.thirst", "Sedento" },
            { "trait.sleep", "Dorminhoco" },
            { "trait.play", "Brincalhão" },
            { "trait.clean", "Higiênico" },
            { "trait.aggressive", "Brigão" },
            { "trait.big", "Imponente" },
            { "trait.small", "Pequeno" },
            { "trait.fast", "Rápido" },
            { "trait.slow", "Tranquilo" },

            // --- Personality: Personality Pool ---
            { "personality.cuddly", "Carinhoso" },
            { "personality.independent", "Independente" },
            { "personality.curious", "Curioso" },
            { "personality.fearful", "Medroso" },
            { "personality.affectionate", "Afetuoso" },
            { "personality.observer", "Observador" },
            { "personality.discreet", "Discreto" },
            { "personality.clingy", "Grudento" },
            { "personality.adventurer", "Aventureiro" },
            { "personality.territorial", "Territorial" },
            { "personality.sociable", "Sociável" },
            { "personality.solitary", "Solitário" },
            { "personality.clever", "Esperto" },
            { "personality.lazy", "Preguiçoso" },
            { "personality.loyal", "Fiel" },
            { "personality.capricious", "Caprichoso" },

            // --- Personality: Quirky Pool ---
            { "quirk.grudge", "Rancoroso" },
            { "quirk.touchy", "Sensível" },
            { "quirk.dumb", "Desligado" },
            { "quirk.big_eater", "Comilão" },
            { "quirk.drinker", "Bebedor" },
            { "quirk.food_thief", "Ladrão de ração" },
            { "quirk.snorer", "Roncador" },
            { "quirk.litter_fear", "Medo da caixa" },
            { "quirk.jealous", "Ciumento" },
            { "quirk.show_off", "Exibido" },
            { "quirk.drama_queen", "Drama queen" },
            { "quirk.hair_collector", "Colecionador de pelos" },
            { "quirk.grumpy", "Rabugento" },
            { "quirk.clumsy", "Desajeitado" },
            { "quirk.hypochondriac", "Hipocondríaco" },
            { "quirk.couch_king", "Rei do sofá" },
            { "quirk.snob", "Esnobe" },
            { "quirk.pilferer", "Furtador" },

            // --- Scene Labels ---
            // Boot scene
            { "ui.play", "Jogar" },
            { "ui.continue", "Continuar jogo" },
            { "ui.new_game", "Novo jogo" },

            // Credits panel
            { "credits.title", "Créditos" },
            { "credits.gameby", "um jogo de" },
            { "credits.direction", "Direção / desenvolvimento" },
            { "credits.disclaimer", "Este jogo foi criado usando o motor Unity, propriedade da Unity Technologies. Unity e o logotipo Unity são marcas registradas da Unity Technologies nos Estados Unidos da América e em outros países" },
            { "ui.credits", "Créditos" },
            { "ui.parameters", "Configurações" },

            // Options panel
            { "ui.resume", "Continuar" },
            { "ui.main_menu", "Menu principal" },
            { "ui.meowdex", "Meowdex" },

            // Parameters panel
            { "param.languages", "Idiomas" },
            { "param.notifications", "Notificações push" },
            { "param.battery", "Economia de bateria" },
            { "param.music_volume", "Volume da música" },
            { "param.effects_volume", "Volume dos efeitos" },

            // Shop
            { "ui.shop", "Loja" },
            { "shop.category.food", "Comida" },
            { "shop.category.drink", "Bebida" },
            { "shop.category.sleep", "Sono" },
            { "shop.category.play", "Brincadeira" },
            { "shop.category.clean", "Higiene" },
            { "shop.category.comfort", "Conforto" },
            { "shop.category.deco", "Decoração" },
            { "shop.double_gains", "Dobrar ganhos" },

            // Cat Info Panel
            { "cat.name_label", "Nome do gato" },
            { "cat.race_label", "Raça" },
            { "cat.age_label", "Idade" },
            { "cat.character_label", "Personalidade" },
            { "cat.affinity_label", "Afinidades" },
            { "cat.needs_label", "Necessidades" },
            { "cat.need.hunger", "Fome" },
            { "cat.need.thirst", "Sede" },
            { "cat.need.sleep", "Sono" },
            { "cat.need.play", "Brincadeira" },
            { "cat.need.clean", "Higiene" },
            { "cat.need.happiness", "Felicidade" },

            // End Pension Panel
            { "pension.title", "Fim da hospedagem!" },
            { "pension.happiness", "Felicidade média durante a estadia:" },
            { "pension.details", "Detalhes do pagamento" },
            { "pension.base", "Base" },
            { "pension.tip", "Gorjeta" },
            { "pension.total", "Total" },
            { "pension.time_remaining", "Tempo restante de hospedagem" },

            // HUD
            { "hud.boost.label", "Boost x2 de coleta de cat coins ativo!" },

            // Object display names (shop items)
            { "obj.bed", "Cama" },
            { "obj.luxury_bed", "Cama de luxo" },
            { "obj.pillow", "Travesseiro" },
            { "obj.food_bowl", "Comedouro" },
            { "obj.food_bowl_v2", "Comedouro var. 2" },
            { "obj.food_bowl_v3", "Comedouro var. 3" },
            { "obj.food_bowl_v4", "Comedouro var. 4" },
            { "obj.water_bowl", "Bebedouro" },
            { "obj.water_bowl_04", "Bebedouro moderno" },
            { "obj.water_bowl_v2", "Bebedouro var. 2" },
            { "obj.water_bowl_v3", "Bebedouro var. 3" },
            { "obj.wool_ball", "Bola de lã" },
            { "obj.cat_tree", "Árvore para gatos" },
            { "obj.scratcher", "Arranhador" },
            { "obj.litter", "Caixa de areia" },
            { "obj.litter_v1", "Caixa de areia var." },
            { "obj.frame_1", "Quadro 1" },
            { "obj.frame_2", "Quadro 2" },
            { "obj.frame_3", "Quadro 3" },
            { "obj.painting", "Pintura" },
            { "obj.lamp", "Luminária" },
            { "obj.coffee_table", "Mesa de centro" },
            { "obj.drawer", "Cômoda" },
            { "obj.plant_big", "Planta grande" },
            { "obj.plant_small", "Planta pequena" },
            { "obj.shelf", "Prateleira" },
            { "obj.shelf_v1", "Prateleira var." },
            { "obj.aquarium", "Aquário" },
            { "obj.carpet_confort", "Tapete Conforto" },
            { "obj.carpet_play", "Tapete Brincadeira" },
            { "obj.carpet_cosmic", "Tapete Cósmico" },

            // --- Cat Names ---
            { "names.pool", "Mingau,Felix,Caramelo,Luna,Tigrão,Avelã,Bigode,Pachá,Canela,Rabisco,Gatinho,Chamego,Pilantra,Travessa,Alcaçuz,Pérola,Simba,Pena,Biscoito,Cookie,Praliné,Nougat,Macaron,Brioche,Muffin,Crumble,Tiramisù,Suspiro,Brownie,Toffee,Cosmos,Lua,Estrela,Cometa,Nova,Nebula,Sol,Aurora,Eclipse,Galáxia,Astro,Plutão,Rubi,Safira,Esmeralda,Jade,Opala,Âmbar,Topázio,Diamante,Cristal,Ônix,Marfim,Coral,Ninja,Pixel,Wifi,Sushi,Tofu,Wasabi,Mozart,Picasso,Darwin,Merlin,Zorro,Gatsby,Coruja,Borboleta,Beija-flor,Sombra,Raposa,Lontra,Odisseia,Belle,Romeu,Baunilha,Açúcar,Cacau,Latte,Madeleine,Vega,Sirius,Andrômeda,Apolo,Atlas,Marte,Índigo,Ametista,Turquesa,Quartzo,Voltaire,Hugo,Tesla,Curie,Atena,Hermes,Íris,Pandora,Bombom,Coelho,Doninha,Castor,Vortex,Matrix" },
        };
    }
}
