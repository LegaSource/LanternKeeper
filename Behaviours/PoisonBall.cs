using GameNetcodeStuff;
using LegaFusionCore.Behaviours;
using LegaFusionCore.Managers;
using LegaFusionCore.Managers.NetworkManagers;
using LegaFusionCore.Registries;
using UnityEngine;

namespace LanternKeeper.Behaviours;

public class PoisonBall : LFCBouncyAoEProjectile
{
    protected override void PlayExplosionFx(Vector3 position, Quaternion rotation)
        => LFCGlobalManager.PlayParticle($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.poisonExplosionParticle.name}", position, rotation, 0.75f);

    protected override void PlayExplosionSfx(Vector3 position)
        => LFCGlobalManager.PlayAudio($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.poisonExplosionAudio.name}", position);

    protected override void OnAffectPlayerServer(PlayerControllerB player)
        => LFCNetworkManager.Instance.ApplyStatusEveryoneRpc(throwingPlayer, (int)player.playerClientId, (int)LFCStatusEffectRegistry.StatusEffectType.POISON, 10, 10);

    protected override void OnAffectEnemyServer(EnemyAI enemy)
        => LFCNetworkManager.Instance.ApplyStatusEveryoneRpc(throwingPlayer, enemy.NetworkObject, (int)LFCStatusEffectRegistry.StatusEffectType.POISON, 10, 100);
}