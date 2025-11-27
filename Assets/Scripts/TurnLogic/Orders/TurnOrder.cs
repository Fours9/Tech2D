using System;
using UnityEngine;

/// <summary>
/// Базовый класс приказа хода (WEGO).
/// Конкретные приказы (перемещение юнита, строительство и т.п.) наследуются от него.
/// </summary>
public abstract class TurnOrder
{
    /// <summary>
    /// Уникальный идентификатор приказа (на будущее для сохранений/сети).
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Приоритет исполнения приказа.
    /// Меньшее значение = выполняется раньше.
    /// </summary>
    public int Priority { get; protected set; } = 0;

    /// <summary>
    /// Краткое описание приказа (для отладки/отображения в UI планирования).
    /// </summary>
    public virtual string GetDescription()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Исполнение приказа в фазе Resolving.
    /// </summary>
    public abstract void Execute(TurnManager turnManager);
}


