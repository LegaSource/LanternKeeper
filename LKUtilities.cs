using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LanternKeeper;

public class LKUtilities
{
    public static void Shuffle<T>(IList<T> collection)
    {
        for (int i = collection.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (collection[randomIndex], collection[i]) = (collection[i], collection[randomIndex]);
        }
    }

    public static string GetLanternColor(int index) => index switch
    {
        (int)LanternKeeper.ControlTip.RED => "Red",
        (int)LanternKeeper.ControlTip.BLUE => "Blue",
        (int)LanternKeeper.ControlTip.GREEN => "Green",
        _ => null,
    };

    public static GrabbableObject SpawnItem(GameObject spawnPrefab, Vector3 position, bool isInFactory)
    {
        GameObject gameObject = Object.Instantiate(spawnPrefab, position, Quaternion.identity, StartOfRound.Instance.propsContainer);
        GrabbableObject grabbableObject = gameObject.GetComponent<GrabbableObject>();
        grabbableObject.fallTime = 0f;
        grabbableObject.isInFactory = isInFactory;
        gameObject.GetComponent<NetworkObject>().Spawn();
        return grabbableObject;
    }

    public static ParticleSystem SpawnPoisonParticle(Transform transform)
    {
        Vector3 position = transform.position;
        Vector3 scale = transform.localScale;

        BoxCollider collider = transform.GetComponentInChildren<EnemyAICollisionDetect>()?.GetComponent<BoxCollider>()
            ?? transform.GetComponent<BoxCollider>();
        if (collider != null)
        {
            position = collider.transform.TransformPoint(collider.center);
            scale = collider.size;
        }

        GameObject spawnObject = Object.Instantiate(LanternKeeper.poisonParticle, position, Quaternion.identity, transform);
        ParticleSystem poisonParticle = spawnObject.GetComponent<ParticleSystem>();

        ParticleSystem.ShapeModule shapeModule = poisonParticle.shape;
        shapeModule.scale = scale;

        return poisonParticle;
    }
}
