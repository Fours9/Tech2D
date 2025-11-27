using UnityEngine;
using CellNameSpace;

/// <summary>
/// Данные типа клетки: статы движения, проходимость, визуал.
/// Хранится как ScriptableObject-ассет.
/// </summary>
[CreateAssetMenu(fileName = "CellStats", menuName = "Tech2D/Cell Stats", order = 1)]
public class CellStats : ScriptableObject
{
    [Header("Идентификатор")]
    public string id;              // Внутренний ID (уникальный ключ, латиницей)
    public string displayName;     // Имя для UI
    [TextArea]
    public string description;     // Описание

    [Header("Тип клетки")]
    public CellType cellType;      // Enum тип для обратной совместимости

    [Header("Движение")]
    public int movementCost = 1;   // Стоимость перемещения по клетке
    public bool isWalkable = true; // Проходима ли клетка для юнитов

    [Header("Визуал")]
    public Material material;      // Материал для этого типа клетки (опционально)
    public Color baseColor = Color.white; // Базовый цвет (используется, если material не задан)
    public Sprite resourceOverlay; // Спрайт оверлея ресурсов (опционально)
    public Sprite buildingOverlay; // Спрайт оверлея построек (опционально)
}



