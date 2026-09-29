using System;
using System.Collections.Generic;

namespace CatHotel.Services
{
    /// <summary>
    /// Serializable data models for cloud save.
    /// Each model maps to a separate cloud save key.
    /// </summary>

    [Serializable]
    public class SettingsSaveData
    {
        public string language;          // "fr" or "en"
        public float musicVolume;        // 0-1
        public float effectsVolume;      // 0-1
        public bool soundEnabled = true;
        public bool pushNotifications;
        public bool batterySaving = false;
    }

    [Serializable]
    public class ProgressionSaveData
    {
        public int reputationLevel;
        public int reputationXp;
        public int coins;
        public int gems;
        public int floorTileIndex = -1;  // which floor tile visual to use (-1 = not set)
        public List<PlacedObjectSaveData> placedObjects = new();
        public List<CatCloudSaveData> cats = new();
        public int tutorialStepIndex;    // current tutorial step (0 = start, N = last reached)
        public bool tutorialComplete;    // true once the tutorial is fully done OR skipped — never wipe save afterwards
        public int highestUnlockedFloor; // 0 = only RDC, 1 = floor 1 unlocked, ...
        public string lastSaveTime;      // ISO 8601
        public int playTimeSeconds;      // active seconds played in the hotel (analytics minutes_played). 0 for saves <= 0.35
    }

    [Serializable]
    public class PlacedObjectSaveData
    {
        public string objectAssetName;   // ScriptableObject.name (e.g. "Obj_Bed")
        public int gridX;
        public int gridY;
        public int floorIndex;           // 0 = RDC, 1 = étage 1, ...
    }

    [Serializable]
    public class CatCloudSaveData
    {
        public string breedName;
        public string catName;
        public string mode;              // "Pension" or "Refuge"
        public bool isSpecial;

        // Visual identity (must persist across saves so cats look identical on reload)
        public float scale = 1f;         // exact transform scale set on spawn

        // Position
        public int gridX;
        public int gridY;
        public int floorIndex;           // 0 = RDC, 1 = étage 1, ...
        public int[] visitedFloors;      // floors the cat has already been to

        // Needs (5 floats: Hunger, Thirst, Sleep, Play, Clean)
        public float[] needs;
        public float happiness;

        // Pension tracking
        public float pensionDuration;
        public float pensionTimeRemaining;

        // Happiness tracking
        public float happinessSum;
        public int happinessSamples;
        public float happyDuration;
    }
}
