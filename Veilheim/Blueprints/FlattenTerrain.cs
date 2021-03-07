// Veilheim

using UnityEngine;

namespace Veilheim.Blueprints
{
    public class FlattenTerrain
    {
        public static void Flatten(Transform transform, Vector2 delta)
        {
            var groundPrefab = ZNetScene.instance.GetPrefab("raise");
            if (groundPrefab)
            {
                var startPosition = transform.position + transform.forward * 2.0f + Vector3.down * 0.5f;
                var rotation = transform.rotation;


                var forward = 0f;

                while (forward < delta.x)
                {
                    var right = 0f;
                    while (right < delta.y)
                    {
                        Object.Instantiate(groundPrefab, startPosition + transform.forward * forward + transform.right * right, rotation);
                        right++;
                    }

                    forward++;
                }
            }
        }
    }
}