using Content.Shared._Lust.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rotation;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Player;

namespace Content.Shared._Lust.Borgs;

public sealed partial class SharedBorgRestActionSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgRestActionComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BorgRestActionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BorgRestActionComponent, BorgToggleRestActionEvent>(OnToggleRest);
        SubscribeLocalEvent<BorgRestingComponent, ComponentStartup>(OnRestingStartup);
        SubscribeLocalEvent<BorgRestingComponent, ComponentRemove>(OnRestingRemove);
        SubscribeLocalEvent<KnockedDownComponent, ComponentRemove>(OnKnockedDownRemove);
    }

    private void OnStartup(Entity<BorgRestActionComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
        UpdateActionState(ent);
    }

    private void OnShutdown(Entity<BorgRestActionComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ToggleActionEntity);
    }

    private void OnToggleRest(Entity<BorgRestActionComponent> ent, ref BorgToggleRestActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryToggleRest(ent.AsNullable()))
            args.Handled = true;
    }

    private void OnRestingStartup(Entity<BorgRestingComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<BorgRestActionComponent>(ent, out var borgRest))
            UpdateActionState((ent.Owner, borgRest));
    }

    private void OnRestingRemove(Entity<BorgRestingComponent> ent, ref ComponentRemove args)
    {
        ClearRestingVisual(ent.Owner);

        if (TryComp<BorgRestActionComponent>(ent, out var borgRest))
            UpdateActionState((ent.Owner, borgRest), resting: false);
    }

    private void OnKnockedDownRemove(Entity<KnockedDownComponent> ent, ref ComponentRemove args)
    {
        RemCompDeferred<BorgRestingComponent>(ent);
    }

    private void UpdateActionState(Entity<BorgRestActionComponent> ent)
    {
        UpdateActionState(ent, HasComp<BorgRestingComponent>(ent));
    }

    private void UpdateActionState(Entity<BorgRestActionComponent> ent, bool resting)
    {
        _actions.SetToggled(ent.Comp.ToggleActionEntity, resting);
    }

    /// <summary>
    /// Пытается переключить состояние отдыха борга.
    /// </summary>
    public bool TryToggleRest(Entity<BorgRestActionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!CanToggleRest(ent))
            return false;

        Entity<BorgRestActionComponent> borgRest = (ent.Owner, ent.Comp);

        if (HasComp<BorgRestingComponent>(ent))
            return DoStandUp(borgRest);

        return DoRest(borgRest);
    }

    /// <summary>
    /// Проверяет, может ли борг переключить состояние отдыха.
    /// </summary>
    public bool CanToggleRest(Entity<BorgRestActionComponent?> ent, bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (HasComp<BorgRestingComponent>(ent))
            return TryComp<KnockedDownComponent>(ent, out var knockedDown) &&
                   _stun.CanStand((ent.Owner, knockedDown));

        if (!TryComp<BorgChassisComponent>(ent, out var borg) || borg.BrainEntity == null)
            return false;

        if (!HasComp<ActorComponent>(ent))
            return false;

        return _mobState.IsAlive(ent) && !HasComp<KnockedDownComponent>(ent);
    }

    private bool DoRest(Entity<BorgRestActionComponent> ent)
    {
        if (!_stun.TryKnockdown(ent.Owner, null, refresh: true, autoStand: false, drop: false, force: true))
        {
            ClearRestingVisual(ent);
            return false;
        }

        EnsureComp<BorgRestingComponent>(ent);
        SetRestingVisual(ent, true);
        return true;
    }

    private bool DoStandUp(Entity<BorgRestActionComponent> ent)
    {
        return _stun.TryStanding((ent.Owner, null));
    }

    private void SetRestingVisualRotation(EntityUid uid)
    {
        _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Vertical);
    }

    private void SetRestingVisual(EntityUid uid, bool resting)
    {
        _appearance.SetData(uid, BorgRestVisuals.Resting, resting);
        SetRestingVisualRotation(uid);
    }

    private void ClearRestingVisual(EntityUid uid)
    {
        SetRestingVisual(uid, false);
    }
}
