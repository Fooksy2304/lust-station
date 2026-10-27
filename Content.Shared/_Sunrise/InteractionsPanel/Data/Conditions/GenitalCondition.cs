using Content.Shared._Lust.Borgs;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class GenitalCondition : IAppearCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField]
    public GenitalSlot RequiredGenital { get; private set; } = GenitalSlot.Penis;

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entityManager)
    {
        if (CheckInitiator)
        {
            if (!HasRequiredGenital(initiator, entityManager))
                return false;
        }

        if (CheckTarget)
        {
            if (!HasRequiredGenital(target, entityManager))
                return false;
        }

        return true;
    }

    private bool HasRequiredGenital(EntityUid entity, EntityManager entityManager)
    {
        // Lust edit - учитываем выбранный пол борга при определении гениталий.
        if (!BorgSexSystem.TryGetSex(entity, entityManager, out var sex))
            return false;

        var genitals = GenitalsHelper.GetGenitals(sex);
        return genitals.Contains(RequiredGenital);
    }
}
