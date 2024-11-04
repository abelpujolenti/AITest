using UnityEngine;

namespace Managers
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance => _instance;

        private int ALLY_LAYER = 7;
        private int ENEMY_LAYER = 8;
        private int TERRAIN_LAYER = 9;
        private int ALLY_TRIGGER_DETECTION = 10;
        private int ENEMY_TRIGGER_DETECTION = 11;
        private int RIVAL_TRIGGER_DETECTION = 12;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                
                DontDestroyOnLoad(gameObject);
                
                return;
            }
            
            Destroy(gameObject);
        }

        public int GetAllyLayer()
        {
            return ALLY_LAYER;
        }

        public int GetEnemyLayer()
        {
            return ENEMY_LAYER;
        }

        public int GetTerrainLayer()
        {
            return TERRAIN_LAYER;
        }

        public int GetAllyTriggerDetectionLayer()
        {
            return ALLY_TRIGGER_DETECTION;
        }

        public int GetEnemyTriggerDetectionLayer()
        {
            return ENEMY_TRIGGER_DETECTION;
        }

        public int GetRivalTriggerDetectionLayer()
        {
            return RIVAL_TRIGGER_DETECTION;
        }
    }
}