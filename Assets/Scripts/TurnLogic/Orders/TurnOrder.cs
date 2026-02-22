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
    /// Владелец приказа (ownerId = playerId).
    /// </summary>
    public abstract int OwnerId { get; }

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

    /// <summary>
    /// Завершён ли приказ. По умолчанию все моментальные приказы считаются
    /// завершёнными сразу после вызова Execute.
    /// Для асинхронных приказов (например, движения юнита) это свойство
    /// переопределяется и проверяет фактическое окончание действия.
    /// </summary>
    public virtual bool IsComplete => true;
}


