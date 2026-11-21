using UnityEngine;

namespace CatHotel.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Cat Hotel/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Needs - Base Decay Per Second")]
        [Tooltip("GDD: Hunger 0.7/s, Sleep 0.5/s, Play 0.6/s, Clean 0.45/s")]
        public float hungerDecay = 0.7f;
        public float thirstDecay = 0.8f;
        public float sleepDecay = 0.5f;
        public float playDecay = 0.6f;
        public float cleanDecay = 0.45f;

        [Header("Needs - Thresholds")]
        public float seekThreshold = 60f;
        public float criticalThreshold = 20f;

        [Header("Happiness")]
        public float happyThreshold = 70f;
        public float neutralThreshold = 40f;
        public float unhappyLeaveThreshold = 20f;

        [Header("Mood Bubbles")]
        [Tooltip("Happiness above this → content (Joyous bubble)")]
        public float moodHappyThreshold = 60f;
        [Tooltip("Happiness above this → enthousiaste (Very Happy bubble)")]
        public float moodEcstaticThreshold = 90f;
        [Tooltip("Happiness below this → déprimé (Upset bubble)")]
        public float moodDepressedThreshold = 25f;
        [Tooltip("Height offset for mood bubble above cat (between cat and coin)")]
        public float moodBubbleHeight = 1f;
        [Tooltip("Seconds below unhappy threshold before cat leaves")]
        public float leaveDelay = 30f;

        [Header("Revenue - Service Use")]
        [Tooltip("Base coins earned when a cat finishes using a service object")]
        public int coinsPerServiceUse = 5;

        [Header("Pension")]
        [Tooltip("Base payment per second of stay")]
        public float pensionBaseRate = 0.5f;
        [Tooltip("Tip percentage if happiness > 80%")]
        public float tipPercent = 0.2f;

        [Header("Refuge")]
        [Tooltip("Seconds of continuous happiness >= adoptionHappyThreshold before an adopter shows up")]
        public float adoptionHappyDuration = 30f;
        [Tooltip("Happiness (0-100) a refuge cat must keep to attract an adopter")]
        public float adoptionHappyThreshold = 70f;
        [Tooltip("Base adoption fee multiplier")]
        public float adoptionFeeMultiplier = 2f;

        [Header("Hotel")]
        public int maxCats = 20;
        [Tooltip("Seconds between cat arrival attempts")]
        public float arrivalInterval = 30f;
        [Tooltip("Probability of pension vs refuge (0-1, pension)")]
        public float pensionProbability = 0.7f;

        [Header("Petting")]
        public float petHappinessBoost = 5f;
        public float petCooldown = 30f;

        [Header("Combat")]
        public float fightHappinessThreshold = 50f;
        public float fightRange = 3f;
        public float fightDuration = 3f;
        public float fightPenalty = 25f;
        public float fightCooldown = 15f;

        [Header("Caprices")]
        [Tooltip("Check interval in seconds")]
        public float capriceCheckInterval = 30f;
        [Tooltip("Chance for normal cat")]
        public float capriceChanceNormal = 0.08f;
        [Tooltip("Happiness penalty per unsatisfied caprice")]
        public float capricePenalty = 20f;

        [Header("Economy")]
        public int startingCoins = 500;
        public int startingGems;
        public int roomCostPerCell = 10;

        [Header("Breed Affinities")]
        [Tooltip("Chance (0-1) of liking a breed")]
        [Range(0f, 1f)] public float likedBreedChance = 0.6f;
        [Tooltip("Chance (0-1) of disliking a breed")]
        [Range(0f, 1f)] public float dislikedBreedChance = 0.4f;
        [Tooltip("Proximity radius in grid cells to detect liked/disliked breeds")]
        public float affinityCheckRadius = 3f;
        [Tooltip("Seconds between affinity proximity checks")]
        public float affinityCheckInterval = 1f;
        [Tooltip("Happiness bonus per second when near a liked breed")]
        public float likedHappinessBonus = 2f;
        [Tooltip("Happiness penalty per second when near a disliked breed")]
        public float dislikedHappinessPenalty = 3f;
        [Tooltip("Chance (0-1) per check of following a liked cat when both idle")]
        [Range(0f, 1f)] public float followLikedChance = 0.3f;
        [Tooltip("Chance (0-1) per check of becoming angry near disliked breed")]
        [Range(0f, 1f)] public float angerOnDislikeChance = 0.15f;

        [Header("Comfort")]
        public float comfortNeutral = 50f;
        public float comfortHappinessScale = 0.2f;
        public float overcrowdedThreshold = 0.8f;
        public float overcrowdedPenalty = 20f;

        /// <summary>Get the base decay rate for a specific need.</summary>
        public float GetBaseDecay(NeedType need)
        {
            return need switch
            {
                NeedType.Hunger => hungerDecay,
                NeedType.Thirst => thirstDecay,
                NeedType.Sleep => sleepDecay,
                NeedType.Play => playDecay,
                NeedType.Clean => cleanDecay,
                _ => 0.5f
            };
        }
    }
}
