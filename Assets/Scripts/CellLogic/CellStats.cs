using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Runtime-кеш рассчитанных статов клетки.
    /// Результат агрегации CellTypeStats + FeatureStats + BuildingStats через StatsCalculator.
    /// </summary>
    public class CellStats
    {
        public int movementCost;
        public bool isWalkable;
        public Dictionary<string, float> resources = new Dictionary<string, float>();

        public CellStats()
        {
        }

        public CellStats(int movementCost, bool isWalkable, Dictionary<string, float> resources = null)
        {
            this.movementCost = movementCost;
            this.isWalkable = isWalkable;
            this.resources = resources ?? new Dictionary<string, float>();
        }
    }
}
