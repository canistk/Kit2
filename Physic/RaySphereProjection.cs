using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2.Physic
{
    /// <summary>Recursive raycast function, attempt to project on collider surface.
    /// and find the possible end point based on refection times & buffer size.
    /// </summary>
    public class RaySphereProjection
    {
        private int depth;
        private Vector3[] waypoint;
        private Vector3[] directions;
        private RaycastHit[] hits;
        private float[] sessionDis;

        private static RaycastHit[] _sphereCastHits = new RaycastHit[20];

        private float radius;
        private float skinWidth;

        public RaySphereProjection(int bufferSize)
        {
            this.waypoint = new Vector3[bufferSize];
            this.directions = new Vector3[bufferSize];
            this.sessionDis = new float[bufferSize];
            this.hits = new RaycastHit[bufferSize];
            this.depth = 0;
        }


        public struct CollisionInfo 
        {
            public Vector3 origin;
            public float radius;
            public Vector3 direction;
            public RaycastHit hit;
        }

        public delegate bool CollisionIgnoreCondition(CollisionInfo collisionInfo);
        
        private CollisionIgnoreCondition _mCollisionIgnoreFilter = null;
        public void SetIgnoreFilter(CollisionIgnoreCondition filter)
        {
            _mCollisionIgnoreFilter = filter;
        }

        public bool Execute(
            Vector3 origin, Vector3 direction, float maxDistance,
            float radius, float skinWidth,
            LayerMask layerMask, QueryTriggerInteraction qti)
        {

            waypoint[0] = origin;
            directions[0] = direction;
            sessionDis[0] = maxDistance;
            hits[0] = default;

            this.radius = radius;
            this.skinWidth = skinWidth;
            depth = 1;
            return InternalRaycast(ref depth, origin, direction, maxDistance, layerMask, qti);
        }

        private float s_DebugOffset => Application.isPlaying ? 0.2f : 0f;
        private float s_DebugDur => Application.isPlaying ? 3f : 0f;
        private bool InternalRaycast(ref int depth, in Vector3 from, Vector3 direction, in float maxDistance, in LayerMask layerMask, in QueryTriggerInteraction queryTriggerInteraction)
        {
            if (depth + 1 > waypoint.Length)
                return false;
            if (direction == Vector3.zero)
                return false; // unable to calculate

            direction.Normalize();
            int idx = depth++;
            
            var size = Physics.SphereCastNonAlloc( from, radius, direction, _sphereCastHits, maxDistance + skinWidth, layerMask, queryTriggerInteraction);
            RaycastHit closestHit = default;
            bool isHit = false;
            
            for (int i = 0; i < size; i++)
            {
                var h = _sphereCastHits[i];
                if (h.collider == null) continue;
                if (h.distance <= 0) continue; // self or inside
                
                if(_mCollisionIgnoreFilter != null && _mCollisionIgnoreFilter.Invoke(
                       new CollisionInfo() {
                           origin = from,
                           radius = radius,
                           direction = direction,
                           hit = h
                       }))
                    continue;

                if (isHit && !(h.distance < closestHit.distance)) continue;
                
                closestHit = h;
                isHit = true;

            }
            hits[idx]       = closestHit;
            
            if (isHit)
            {
                float realDis       = Mathf.Max(0f, hits[idx].distance - skinWidth); // keep skin width distance from hit point, when obstacle need to avoid

                var p0              = from;
                var p1              = p0 + direction * realDis; 

                float moveDistance  = sessionDis[idx]   = Mathf.Min(maxDistance, realDis);
                Vector3 to          = waypoint[idx]     = p1;
                Vector3 nextDir     = directions[idx]   = Vector3.ProjectOnPlane(direction, hits[idx].normal).normalized;
                float restDis       = maxDistance - moveDistance;
                //if (ignore)
                //{
                //    // hit but ignore
                //    DebugExtend.DrawLine(p0.AppendY(s_DebugOffset), p1.AppendY(s_DebugOffset), s_Yellow30, s_DebugDur, false);
                //    DebugExtend.DrawLine(p1.AppendY(s_DebugOffset), p2.AppendY(s_DebugOffset), s_Red30, s_DebugDur, false);
                //}
                //else
                //{
                //    // hit
                //    DebugExtend.DrawLine(from.AppendY(s_DebugOffset), to.AppendY(s_DebugOffset), s_Cyan30, s_DebugDur, false);
                //}
                //DebugExtend.DrawCircle(hits[idx].point, hits[idx].normal, Color.cyan, 0.3f, 3f, false);
                //DebugExtend.DrawWireSphere(to, radius, Color.cyan, 3f, false);
                InternalRaycast(ref depth, to, nextDir, restDis, layerMask, queryTriggerInteraction);
                return true;
            }
            else
            {
                var p0 = from;
                var p1 = from + direction * maxDistance;
                DebugExtend.DrawLine(p0.AppendY(s_DebugOffset), p1.AppendY(s_DebugOffset), s_Gray30, s_DebugDur, false);
            }
            // last way point without hit!
            sessionDis[idx] = maxDistance;
            directions[idx] = direction;
            waypoint[idx]   = from + direction * maxDistance;
            // DebugExtend.DrawLine(from.AppendY(s_DebugOffset), waypoint[idx].AppendY(s_DebugOffset), s_Green30, 0f, false);
            // DebugExtend.DrawLabel(from.AppendY((float)depth * 0.1f), $"{depth}");
            return false;
        }

        public static readonly Color s_Cyan30 = Color.cyan.CloneAlpha(0.3f);
        public static readonly Color s_Red30 = Color.red.CloneAlpha(0.3f);
        public static readonly Color s_Green30 = Color.green.CloneAlpha(0.3f);
        public static readonly Color s_Yellow30 = Color.yellow.CloneAlpha(0.3f);
        public static readonly Color s_Gray30 = Color.gray.CloneAlpha(0.3f);

        public void DrawGizmosPath()
        {
            if (depth < 1)
                return;

            using (new ColorScope(Color.gray.CloneAlpha(0.3f)))
            {
                for (int i = 1; i < depth && i < waypoint.Length; ++i)
                {
                    var f = waypoint[i - 1];
                    var t = waypoint[i];
                    Gizmos.DrawLine(f, t);
                    GizmosExtend.DrawSphere(f, radius);
                    GizmosExtend.DrawSphere(t, radius);
                    var hit = hits[i];
                    if (hit.collider != null)
                        GizmosExtend.DrawCircle(hit.point, hit.normal, Color.cyan, radius);
                }
            }
        }

        public bool TryGetStartEnd(out Vector3 from, out Vector3 to)
        {
            from = to = Vector3.zero;
            if (depth == 1)
            {
                from = to = waypoint[0];
            }
            else if (depth > 2)
            {
                from    = waypoint[0];
                to      = waypoint[depth - 1];
            }
            return false;
        }

        public Vector3 GetFinalPosition()
        {
            if (depth == 0)
                return Vector3.zero;
            return waypoint[depth - 1];
        }
        
        public IEnumerable<(RaycastHit,Vector3)> GetHits()
        {
            if (depth < 0 || depth > hits.Length)
                throw new System.ArgumentOutOfRangeException();

            // i = 1, skip the first hit, which is the origin
            for (int i = 1; i < hits.Length && i < depth; ++i)
                yield return (hits[i], waypoint[i]);
        }
    }
}