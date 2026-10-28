using Content.Shared._Lust.Borgs.Components;

#pragma warning disable IDE0130
namespace Content.Server._Sunrise.InteractionsPanel;

public partial class InteractionsPanel
{
    private void OnBorgSexChanged(Entity<BorgSexComponent> ent, ref BorgSexChangedEvent args)
    {
        UpdateUIForClothingChange(ent.Owner);
    }
}
#pragma warning restore IDE0130
