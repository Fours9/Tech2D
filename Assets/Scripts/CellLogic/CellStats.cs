using System.Collections.Generic;

namespace CellNameSpace
{
    /// <summary>
    /// Runtime-кеш рассчитанных статов клетки.
    /// Результат агрегации CellTypeStats + FeatureStats + BuildingStats через StatsCalculator.
    /// Имя CachedCellStats — чтобы не путать с устаревшим классом CellStats в CellTypeStats.cs.
    /// </summary>
    public class CachedCellStats
    {
        public int movementCost;
        public bool isWalkable;
        public Dictionary<string, float> resources = new Dictionary<string, float>();
        public List<ResourceBonus> bonuses = new List<ResourceBonus>();

        public CachedCellStats()
        {
        }

        public CachedCellStats(int movementCost, bool isWalkable, Dictionary<string, float> resources = null, List<ResourceBonus> bonuses = null)
        {
            this.movementCost = movementCost;
            this.isWalkable = isWalkable;
            this.resources = resources ?? new Dictionary<string, float>();
            this.bonuses = bonuses ?? new List<ResourceBonus>();
        }
    }
}
