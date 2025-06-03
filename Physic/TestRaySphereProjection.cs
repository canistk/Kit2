#define SPHERE_CAST
using UnityEngine;
using Kit2.Physic;

[ExecuteInEditMode]
public class TestRaySphereProjection : MonoBehaviour
{
    [SerializeField] bool m_Debug = true;

    [Header("Physics")]
    [SerializeField, Min(2), Range(2, 20)] private int m_MemoryBudget = 8;
#if SPHERE_CAST
        [SerializeField] private float m_RayRadius = 0.5f;
#endif
    [SerializeField, Min(float.Epsilon)] private float m_SkinWidth = 0.001f;
    [SerializeField] private LayerMask m_LayerMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction m_QueryTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Simulate Movement")]
    [SerializeField] private float m_ForwardDistance = 1f;

    private RaySphereProjection raySphere = null;

    private void Update()
    {
        Vector3 fromPos         = transform.position;
        Vector3 heading         = transform.forward;
        float maxDistance       = m_ForwardDistance;
        if (raySphere == null)
        {
            raySphere = new RaySphereProjection(m_MemoryBudget);
        }
        raySphere.Execute(fromPos, heading, maxDistance, m_RayRadius, m_SkinWidth, m_LayerMask, m_QueryTriggerInteraction);
    }


    private void OnDrawGizmos()
    {
        if (!m_Debug)
            return;

        raySphere.DrawGizmosPath();
    }
}
