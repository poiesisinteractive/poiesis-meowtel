using System;
using UnityEngine;
using CatHotel.Core;

namespace CatHotel.Tutorial
{
    public enum TutorialTrigger
    {
        WaitForTap,
        WaitForCatSelected,
        WaitForObjectSelected,
        WaitForObjectPlaced,
        WaitForObjectMoved,
        WaitForCatPetted,
        WaitForShopOpened,
        WaitForFloorChanged,
        WaitForCoinCollected,
        WaitForCatServiceUsed,
        WaitForLevelPanelOpened,
        WaitForLevelPanelClosed,
        WaitForDelay,
        Action
    }

    public enum TutorialAction
    {
        None,
        SpawnFirstCat,
        SpawnRefugeCatHungry,
        FocusCameraOnLastSpawnedCat,
        FocusCameraOnPensionEntrance,
        FocusCameraOnRefugeEntrance,
        FocusCameraOnUnhappyExit,
        ReleaseCameraFocus,
        SetRefugeCatThirsty,
        SetRefugeCatSleepy,
        SetRefugeCatDirty,
        SetRefugeCatBored,
        EnableShopFood,
        EnableShopWater,
        EnableShopSleep,
        EnableShopClean,
        EnableShopBalls,
        EnableFullShop,
        DespawnFirstCat,
        HighlightGlobalPex, // legacy alias for HighlightUI(GlobalPex)
        HighlightUI,        // dim everything + pulse the GameObject named in step.highlightTarget
        RestoreHighlight,
    }

    [Serializable]
    public class TutorialStep
    {
        [Header("Speaker")]
        public string speakerLabelKey;
        public Sprite speakerPortrait;

        [Header("Dialogue")]
        public string textKey;

        [Header("Progression")]
        public TutorialTrigger trigger;
        [Tooltip("For WaitForObjectPlaced: which category is required.")]
        public ObjectCategory requiredCategory;
        [Tooltip("For WaitForDelay: seconds to wait.")]
        public float delaySeconds = 3f;
        [Tooltip("For HighlightUI action: GameObject name to dim-around + pulse.")]
        public string highlightTarget;
        [Tooltip("Show the dialogue with the bubble + portrait on the RIGHT side of the screen instead of the left.")]
        public bool bubbleOnRight;

        [Header("Actions")]
        public TutorialAction actionOnStart;
        public TutorialAction actionOnComplete;

        public bool HasDialogue => !string.IsNullOrEmpty(textKey);

        /// <summary>Stable id for analytics (survives reordering): the text key, or trigger/actions for silent steps.</summary>
        public string AnalyticsId => HasDialogue ? textKey : $"{trigger}:{actionOnStart}:{actionOnComplete}";
    }
}
