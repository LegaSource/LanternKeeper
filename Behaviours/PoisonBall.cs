using GameNetcodeStuff;
using LegaFusionCore.Behaviours;
using LegaFusionCore.Managers;
using LethalStatus.Managers;
using LethalStatus.StatusEffects;
using UnityEngine;

namespace LanternKeeper.Behaviours;

public class PoisonBall : LFCBouncyAoEProjectile
{
    protected override void PlayExplosionFx(Vector3 position, Quaternion rotation)
        => LFCGlobalManager.PlayParticle($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.poisonExplosionParticle.name}", position, rotation, scaleFactor: 0.75f);

    protected override void PlayExplosionSfx(Vector3 position)
        => LFCGlobalManager.PlayAudio($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.poisonExplosionAudio.name}", position);

    protected override void OnAffectPlayerServer(PlayerControllerB player)
        => LSNetworkManager.Instance.ApplyStatusEveryoneRpc(throwingPlayer, (int)player.playerClientId, (int)LSStatusEffectRegistry.StatusEffectType.POISON, 10, 10);

    protected override void OnAffectEnemyServer(EnemyAI enemy)
        => LSNetworkManager.Instance.ApplyStatusEveryoneRpc(throwingPlayer, enemy.NetworkObject, (int)LSStatusEffectRegistry.StatusEffectType.POISON, 10, 100);
}