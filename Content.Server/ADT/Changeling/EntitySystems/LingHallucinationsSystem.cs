using Content.Server.Actions;
using Content.Server.Store.Systems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Actions;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Alert;
using Content.Shared.Tag;
using Content.Shared.StatusEffect;
using Robust.Shared.Timing;
using Content.Shared.Eye;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Changeling.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.Mind;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class LingHallucinationsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public static string HallucinatingKey = "Hallucinations";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LingHallucinationsComponent, MapInitEvent>(OnHallucinationsInit);
        SubscribeLocalEvent<LingHallucinationsComponent, ComponentShutdown>(OnHallucinationsShutdown);
    }

    private void OnHallucinationsInit(EntityUid uid, LingHallucinationsComponent component, MapInitEvent args)
    {
        component.Layer = _random.Next(50, 500);
        if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
            return;
        _eye.SetVisibilityMask(uid, eye.VisibilityMask | component.Layer, eye);
    }

    private void OnHallucinationsShutdown(EntityUid uid, LingHallucinationsComponent component, ComponentShutdown args)
    {
        if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
            return;
        _eye.SetVisibilityMask(uid, eye.VisibilityMask & ~component.Layer, eye);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LingHallucinationsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stat, out var xform))
        {
            if (_timing.CurTime < stat.NextSecond)
                continue;
            var rate = stat.SpawnRate;
            stat.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(stat.SpawnRate);

            if (!_random.Prob(stat.Chance))
                continue;

            var range = stat.Range * 4;

            foreach (var (ent, comp) in _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(xform.MapPosition, range))
            {
                var newCoords = Transform(ent).MapPosition.Offset(_random.NextVector2(stat.Range));

                var hallucination = Spawn(_random.Pick(stat.Spawns), newCoords);
                EnsureComp<LingHallucinationComponent>(hallucination, out var visibility);
                visibility.Layer = stat.Layer;
            }

            var uidnewCoords = Transform(uid).MapPosition.Offset(_random.NextVector2(stat.Range));

            var uidhallucination = Spawn(_random.Pick(stat.Spawns), uidnewCoords);
            EnsureComp<LingHallucinationComponent>(uidhallucination, out var uidvisibility);
            uidvisibility.Layer = stat.Layer;
        }
    }
}
