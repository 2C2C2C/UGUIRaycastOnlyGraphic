#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.UI.RaycastOnlyGraphic
{
    public partial class RaycastOnlyPolygonGraphic
    {
        [SerializeField]
        private bool _onlyDrawGizmoSelected = false;

        [ContextMenu(nameof(CreateConvexHullFromPoints))]
        public void CreateConvexHullFromPoints()
        {
            if (null == m_trianglePoints)
            {
                m_trianglePoints = new();
            }
            else
            {
                m_trianglePoints.Clear();
            }

            if (m_innerPoints == null || m_innerPoints.Count < 3)
            {
                return;
            }

            List<Vector2> result = GenerateConvexHull(m_innerPoints);
            m_innerPoints.Clear();
            m_innerPoints.AddRange(result);
            CreateTrianglesFromPoints();
        }

        [ContextMenu(nameof(CreateTrianglesFromPoints))]
        public void CreateTrianglesFromPoints()
        {
            if (null == m_trianglePoints)
            {
                m_trianglePoints = new();
            }
            else
            {
                m_trianglePoints.Clear();
            }

            if (m_innerPoints == null || m_innerPoints.Count < 3)
            {
                return;
            }
            m_trianglePoints = GenerateTriangles(m_innerPoints);
        }

        protected override void Reset()
        {
            raycastTarget = true;
            m_innerPoints = new()
            {
                Vector2.zero,
                Vector2.up,
                Vector2.one,
                Vector2.right
            };

            CreateTrianglesFromPoints();
            base.Reset();
        }

        private void OnDrawGizmosSelected()
        {
            if (_onlyDrawGizmoSelected)
            {
                InternalDrawGizmos();
            }
        }

        private void OnDrawGizmos()
        {
            if (!_onlyDrawGizmoSelected)
            {
                InternalDrawGizmos();
            }
        }

        private void InternalDrawGizmos()
        {
            if (!this.gameObject.activeSelf || !this.enabled)
            {
                return;
            }
            RectTransform rectTransform = this.transform as RectTransform;
            Color wireColor = Color.gray;
            Color colliderColor = Color.yellow;
            if (!this.raycastTarget)
            {
                wireColor *= 0.45f;
                wireColor.a = 0.5f;

                colliderColor *= 0.45f;
                colliderColor.a *= 0.45f;
            }

            // Padding to be applied to the masking
            // X = Left, Y = Bottom, Z = Right, W = Top
            // if you wanna make it bigger, then the all value shouble be negative

            Vector4 padding = this.raycastPadding * -1.0f;
            Matrix4x4 localToWorld = rectTransform.localToWorldMatrix;
            Vector3 topLeft = GetLocalRectPosition(rectTransform, Vector2.up); // Top left
            Vector3 topRight = GetLocalRectPosition(rectTransform, Vector2.one); // Top right
            Vector3 bottomLeft = GetLocalRectPosition(rectTransform, Vector2.zero); // Bottom left
            Vector3 bottomRight = GetLocalRectPosition(rectTransform, Vector2.right); // Bottrom right
            topLeft = localToWorld.MultiplyPoint(topLeft + (Vector3.left * padding.x) + (Vector3.up * padding.w));
            topRight = localToWorld.MultiplyPoint(topRight + (Vector3.right * padding.z) + (Vector3.up * padding.w));
            bottomLeft = localToWorld.MultiplyPoint(bottomLeft + (Vector3.left * padding.x) + (Vector3.down * padding.y));
            bottomRight = localToWorld.MultiplyPoint(bottomRight + (Vector3.right * padding.z) + (Vector3.down * padding.y));
            Color tempColor = Gizmos.color;
            Gizmos.color = wireColor;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);

            Gizmos.color = colliderColor;
            // draw polygon
            for (int i = 0, length = m_innerPoints.Count; i < length; i++)
            {
                Vector2 noramlizedPosition = m_innerPoints[i];
                Vector3 point1 = GetLocalRectPosition(rectTransform, noramlizedPosition);
                point1 = localToWorld.MultiplyPoint(point1);

                int nextIndex = (i + 1) % length;
                noramlizedPosition = m_innerPoints[nextIndex];
                Vector3 point2 = GetLocalRectPosition(rectTransform, noramlizedPosition);
                point2 = localToWorld.MultiplyPoint(point2);

                Gizmos.DrawLine(point1, point2);
            }

            // // draw triangle
            // int triangleCount = null == m_trianglePoints ? 0 : m_trianglePoints.Count / 3;
            // int pointCount = m_innerPoints.Count;
            // bool meshValid = !(null == m_innerPoints || 3 > pointCount || (0 == triangleCount));
            // if (meshValid)
            // {
            //     for (int i = 0; i < triangleCount; i++)
            //     {
            //         int trianglePointIndex = i * 3;
            //         int pointIndex = m_trianglePoints[trianglePointIndex];
            //         Vector2 noramlizedPosition = m_innerPoints[pointIndex];
            //         Vector3 point1 = GetLocalRectPosition(rectTransform, noramlizedPosition);
            //         point1 = localToWorld.MultiplyPoint(point1);

            //         pointIndex = m_trianglePoints[++trianglePointIndex];
            //         noramlizedPosition = m_innerPoints[pointIndex];
            //         Vector3 point2 = GetLocalRectPosition(rectTransform, noramlizedPosition);
            //         point2 = localToWorld.MultiplyPoint(point2);

            //         pointIndex = m_trianglePoints[++trianglePointIndex];
            //         noramlizedPosition = m_innerPoints[pointIndex];
            //         Vector3 point3 = GetLocalRectPosition(rectTransform, noramlizedPosition);
            //         point3 = localToWorld.MultiplyPoint(point3);

            //         Gizmos.DrawLine(point1, point2);
            //         Gizmos.DrawLine(point2, point3);
            //         Gizmos.DrawLine(point3, point1);
            //     }
            // }
            Gizmos.color = tempColor;
        }

        protected override void OnValidate()
        {
            if (_generateMesh && (null == m_trianglePoints || 0 == m_trianglePoints.Count))
            {
                CreateTrianglesFromPoints();
            }
        }

    }
}
#endif