using UnityEngine;
using CatHotel.Core;

namespace CatHotel.Cats
{
    /// <summary>
    /// Calculates and tracks happiness for a single cat.
    /// GDD formula:
    ///   happiness = avg(needs) × (1 - 0.2 × unsatisfied_caprices) - fight_penalty + (comfort - 50) × 0.2 + pet_bonus
    /// </summary>
    public class CatHappiness : MonoBehaviour
    {
        private CatNeeds _needs;
        private CatEntity _entity; // resolved lazily: HotelManager adds CatEntity AFTER CatHappiness.Init()
        private GameConfig _config;
        private CatTraitModifiers _traitMods = CatTraitModifiers.Default;

        private float _happiness = 100f;
        private float _fightPenalty;
        private float _petBonus;
        private float _petBonusTimer;
        private float _comfort = 50f;
        private int _unsatisfiedCaprices;
        private float _affinityOffset; // accumulated from proximity to liked/disliked breeds

        // Tracks time spent below leave threshold
        private float _unhappyTimer;

        // Tick-based update: happiness formula doesn't need per-frame precision
        private const float TickInterval = 0.25f;
        private float _tickAccumulator;

        public float Value => _happiness;
        public bool IsHappy => _happiness >= _config.happyThreshold;
        public bool IsNeutral => _happiness >= _config.neutralThreshold && _happiness < _config.happyThreshold;
        public bool IsUnhappy => _happiness < _config.neutralThreshold;
        public bool ShouldLeave => _unhappyTimer >= _config.leaveDelay;

        public void Init(CatNeeds needs, GameConfig config)
        {
            _needs = needs;
            _config = config;
            _happiness = 100f;
            _unhappyTimer = 0f;
        }

        public void SetComfort(float comfort) => _comfort = comfort;
        public void SetUnsatisfiedCaprices(int count) => _unsatisfiedCaprices = count;
        public void SetTraitModifiers(CatTraitModifiers mods) => _traitMods = mods;

        public void ApplyFightPenalty()
        {
            _fightPenalty += _config.fightPenalty * _traitMods.FightPenaltyMult;
        }

        public void ApplyPetBonus()
        {
            _petBonus = _config.petHappinessBoost * _traitMods.PetBonusMult;
            _petBonusTimer = _config.petCooldown;
        }

        /// <summary>Passive happiness bonus from being near a liked breed.</summary>
        public void ApplyAffinityBonus(float amount)
        {
            _affinityOffset += amount;
        }

        /// <summary>Passive happiness penalty from being near a disliked breed.</summary>
        public void ApplyAffinityPenalty(float amount)
        {
            _affinityOffset -= amount;
        }

        private void Update()
        {
            if (_needs == null || _config == null) return;

            _tickAccumulator += Time.deltaTime;
            if (_tickAccumulator < TickInterval) return;

            float dt = _tickAccumulator;
            _tickAccumulator = 0f;

            // Decay pet bonus over cooldown period
            if (_petBonusTimer > 0f)
            {
                _petBonusTimer -= dt;
                if (_petBonusTimer <= 0f)
                    _petBonus = 0f;
            }

            // Decay fight penalty over time (recover 5/s × trait recovery mult)
            if (_fightPenalty > 0f)
                _fightPenalty = Mathf.Max(0f, _fightPenalty - 5f * _traitMods.FightRecoveryMult * dt);

            // Decay affinity offset toward 0 (fades when no longer near liked/disliked)
            if (_affinityOffset > 0f)
                _affinityOffset = Mathf.Max(0f, _affinityOffset - 1f * dt);
            else if (_affinityOffset < 0f)
                _affinityOffset = Mathf.Min(0f, _affinityOffset + 1f * dt);

            // GDD formula + personality + affinity
            float needsAvg = _needs.Average;
            float capriceMultiplier = 1f - 0.2f * _unsatisfiedCaprices;
            float comfortBonus = (_comfort - _config.comfortNeutral) * _config.comfortHappinessScale;

            _happiness = Mathf.Clamp(
                needsAvg * capriceMultiplier - _fightPenalty + comfortBonus + _petBonus
                + _traitMods.HappinessOffset + _affinityOffset,
                0f, 100f);

            // Track time below leave threshold (but not during arrival — grace period)
            if (_entity == null) TryGetComponent(out _entity);
            bool isArriving = _entity != null && _entity.IsArriving;

            if (_happiness < _config.unhappyLeaveThreshold && !isArriving)
                _unhappyTimer += dt;
            else
                _unhappyTimer = 0f;
        }
    }
}
