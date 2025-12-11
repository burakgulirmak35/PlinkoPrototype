using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PlinkoPrototype
{
    [System.Serializable]
    public class BucketData
    {
        public int score;
        public string color; // Hex format "#RRGGBB"
    }

    [System.Serializable]
    public class LevelData
    {
        public int id;
        public int bucketCount;
        public int ballCount;
        public List<BucketData> buckets;
    }

    [System.Serializable]
    public class LevelDatabase
    {
        public List<LevelData> levels;
    }
}


