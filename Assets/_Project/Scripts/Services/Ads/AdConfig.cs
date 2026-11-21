using UnityEngine;

namespace CatHotel.Services
{
    [CreateAssetMenu(fileName = "AdConfig", menuName = "Meowtel/Ad Config")]
    public class AdConfig : ScriptableObject
    {
        [Header("LevelPlay")]
        [Tooltip("App Key LevelPlay (dashboard LevelPlay > Apps > ton app)")]
        public string appKey = "258d1cc05";

        [Tooltip("Ad Unit ID pour le boost x2 revenus")]
        public string rewardedAdUnitId = "1rett8r2d58j6ob4";

        [Tooltip("Ad Unit ID pour le x2 gains fin de pension")]
        public string rewardedPensionAdUnitId = "ziumr8ervpf15qi4";

        [Header("Boost Settings")]
        [Tooltip("Multiplicateur de revenus apres visionnage")]
        public float boostMultiplier = 2f;

        [Tooltip("Duree du boost en secondes")]
        public float boostDuration = 300f;

        [Header("Daily Cap")]
        [Tooltip("Nombre max de rewarded ads par jour")]
        public int dailyCap = 30;

        [Header("Debug")]
        [Tooltip("Active le mode test (pas de vraies pubs, pas de revenu)")]
        public bool testMode = true;

        [Tooltip("Ouvre la Test Suite LevelPlay sur device au demarrage (touche T pour la rouvrir)")]
        public bool launchTestSuiteOnStart = false;

        [Tooltip("STUB : bypass total de LevelPlay, simule un visionnage de pub (delay + reward auto). Pour beta avant approbation.")]
        public bool stubAdsForBeta = false;

        [Tooltip("Duree simulee du visionnage stub en secondes")]
        public float stubAdDuration = 2f;
    }
}
