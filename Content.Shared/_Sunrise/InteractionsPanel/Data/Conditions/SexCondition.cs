using Content.Shared._Lust.Borgs;
using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class SexCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; } = false;

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField]
    public List<Sex> RequiredSex { get; private set; } = new();

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (CheckInitiator)
        {
            // Lust edit - выбранный пол борга участвует в условиях взаимодействий.
            if (!BorgSexSystem.TryGetSex(initiator, entityManager, out var initiatorSex))
                return false;

            if (!RequiredSex.Contains(initiatorSex))
                return false;
        }

        if (CheckTarget)
        {
            if (!BorgSexSystem.TryGetSex(target, entityManager, out var targetSex))
                return false;

            if (!RequiredSex.Contains(targetSex))
                return false;
        }

        return true;
    }
}
