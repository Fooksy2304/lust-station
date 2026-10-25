using Content.IntegrationTests.Fixtures;
using Content.Shared._Lust.Borgs;
using Content.Shared._Lust.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Lust;

[TestFixture]
public sealed class BorgRestTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    public async Task StandingFromRestDoesNotClearExternalKnockdown()
    {
        var borg = await SpawnControlledBorg();
        var rest = Server.System<SharedBorgRestActionSystem>();
        var stun = Server.System<SharedStunSystem>();
        var rested = false;
        var stood = false;

        await Server.WaitPost(() =>
        {
            rested = rest.TryToggleRest((borg, null));
            stun.UpdateKnockdownTime(borg, TimeSpan.FromSeconds(30));
            stood = rest.TryToggleRest((borg, null));
        });

        await Server.WaitAssertion(() =>
        {
            Assert.That(rested, Is.True);
            Assert.That(stood, Is.False);
            Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.True);
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(borg), Is.True);
        });
    }

    [Test]
    public async Task StandingFromRestCompletesRecovery()
    {
        var borg = await SpawnControlledBorg();
        var rest = Server.System<SharedBorgRestActionSystem>();
        var actions = Server.System<SharedActionsSystem>();
        var rested = false;
        var startedStanding = false;
        EntityUid? actionId = null;

        await Server.WaitPost(() =>
            actionId = SEntMan.GetComponent<BorgRestActionComponent>(borg).ToggleActionEntity);
        await Server.WaitAssertion(() => Assert.That(actionId, Is.Not.Null));

        await Server.WaitPost(() =>
        {
            var action = SEntMan.GetComponent<ActionComponent>(actionId!.Value);
            actions.PerformAction((borg, null), (actionId.Value, action));
            rested = SEntMan.HasComponent<BorgRestingComponent>(borg);
            startedStanding = rest.TryToggleRest((borg, null));
        });

        await Server.WaitAssertion(() =>
        {
            Assert.That(rested, Is.True);
            Assert.That(startedStanding, Is.True);
        });

        await Pair.RunTicksSync(45);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.False);
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(borg), Is.False);
        });
    }

    private async Task<EntityUid> SpawnControlledBorg()
    {
        var map = await Pair.CreateTestMap();
        var containers = Server.System<SharedContainerSystem>();
        EntityUid borg = default;
        var brainInserted = false;

        await Server.WaitPost(() =>
        {
            borg = SEntMan.SpawnEntity("BorgChassisGeneric", map.GridCoords);
            var brain = SEntMan.SpawnEntity("PositronicBrain", map.GridCoords);
            var chassis = SEntMan.GetComponent<BorgChassisComponent>(borg);
            brainInserted = containers.Insert(brain, chassis.BrainContainer);
            Server.PlayerMan.SetAttachedEntity(ServerSession!, borg);
        });

        await Server.WaitAssertion(() => Assert.That(brainInserted, Is.True));

        return borg;
    }
}
