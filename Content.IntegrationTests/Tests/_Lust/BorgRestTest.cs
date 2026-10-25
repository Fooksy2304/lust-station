using Content.Client.Actions;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Lust.Borgs;
using Content.Shared._Lust.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Lust;

[TestFixture]
public sealed class BorgRestTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    public async Task ActionButtonTogglesRestWithoutBrain()
    {
        var borg = await SpawnControlledBorg(insertBrain: false);
        NetEntity actionNet = default;
        NetEntity borgNet = default;

        await Server.WaitPost(() =>
        {
            var action = SEntMan.GetComponent<BorgRestActionComponent>(borg).ToggleActionEntity;
            Assert.That(action, Is.Not.Null);
            actionNet = SEntMan.GetNetEntity(action.Value);
            borgNet = SEntMan.GetNetEntity(borg);
        });

        await Pair.RunTicksSync(5);
        await Client.WaitPost(() =>
        {
            var action = CEntMan.GetEntity(actionNet);
            Client.System<ActionsSystem>().TriggerAction((action, CEntMan.GetComponent<ActionComponent>(action)));
        });

        await Pair.RunTicksSync(5);
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.True));
        await Client.WaitAssertion(() =>
        {
            var clientBorg = CEntMan.GetEntity(borgNet);
            var sprite = Client.System<SpriteSystem>();
            Assert.That(sprite.TryGetLayer((clientBorg, null), BorgVisualLayers.Resting, out var resting, false), Is.True);
            Assert.That(resting!.Visible, Is.True);
        });

        await Pair.RunTicksSync(35);
        await Client.WaitPost(() =>
        {
            var action = CEntMan.GetEntity(actionNet);
            Client.System<ActionsSystem>().TriggerAction((action, CEntMan.GetComponent<ActionComponent>(action)));
        });

        await Pair.RunTicksSync(60);
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<BorgRestingComponent>(borg), Is.False));
    }

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

    private async Task<EntityUid> SpawnControlledBorg(bool insertBrain = true)
    {
        var map = await Pair.CreateTestMap();
        var containers = Server.System<SharedContainerSystem>();
        EntityUid borg = default;
        var brainInserted = false;

        await Server.WaitPost(() =>
        {
            borg = SEntMan.SpawnEntity("BorgChassisGeneric", map.GridCoords);
            if (insertBrain)
            {
                var brain = SEntMan.SpawnEntity("PositronicBrain", map.GridCoords);
                var chassis = SEntMan.GetComponent<BorgChassisComponent>(borg);
                brainInserted = containers.Insert(brain, chassis.BrainContainer);
            }
            Server.PlayerMan.SetAttachedEntity(ServerSession!, borg);
        });

        if (insertBrain)
            await Server.WaitAssertion(() => Assert.That(brainInserted, Is.True));

        return borg;
    }
}
