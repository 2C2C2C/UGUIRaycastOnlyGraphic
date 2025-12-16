using System.Collections.Generic;

namespace UnityEngine.UI.RaycastOnlyGraphic
{
    [RequireComponent(typeof(CanvasRenderer))]
    public partial class RaycastOnlyPolygonGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        [SerializeField]
        private bool _generateMesh = false;

        /// <summary> Use normalized value, clockwise 
        /// The bottom-left of rect is (0,0), top-right is (1,1)
        /// </summary>
        [SerializeField]
        private List<Vector2> m_innerPoints;
        /// <summary> Index of points </summary>
        [SerializeField]
        private List<int> m_trianglePoints;

        public IReadOnlyList<Vector2> PointList => m_innerPoints;
        public int PointCount => null == m_innerPoints ? 0 : m_innerPoints.Count;

        public override void SetMaterialDirty()
        {
            if (_generateMesh)
            {
                base.SetMaterialDirty();
            }
        }

        public override void SetVerticesDirty()
        {
            if (_generateMesh)
            {
                base.SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (!IsActive())
            {
                return;
            }

            vh.Clear();
            if (_generateMesh)
            {
                Rect rect = rectTransform.rect;
                Vector2 bottomLeft = rect.position;
                float width = rect.width;
                float height = rect.height;

                // Dont know how to use VertexHelper to generate irregular mesh.
                for (int i = 0, length = m_innerPoints.Count; i < length; i++)
                {
                    Vector2 tempPoint = m_innerPoints[i];
                    tempPoint = bottomLeft + new Vector2(width * tempPoint.x, height * tempPoint.y);
                    vh.AddVert(tempPoint, color, Vector3.zero);
                }

                for (int i = 0, length = m_trianglePoints.Count / 3; i < length; i++)
                {
                    int index = i * 3;
                    vh.AddTriangle(m_trianglePoints[index], m_trianglePoints[index + 1], m_trianglePoints[index + 2]);
                }
            }
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // Point in rect
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 local))
            {
                Vector2 bottomLeft = GetLocalRectPosition(rectTransform, Vector2.zero);
                Vector2 localDelta = new(local.x - bottomLeft.x, local.y - bottomLeft.y);

#if UNITY_EDITOR
                Debug.DrawLine(rectTransform.TransformPoint(bottomLeft), rectTransform.TransformPoint(local), Color.red);
                Vector2 edge = GetLocalRectPosition(rectTransform, Vector2.right);
                edge.y = local.y;
                Debug.DrawLine(rectTransform.TransformPoint(local), rectTransform.TransformPoint(edge), Color.red);
#endif

                Rect rect = rectTransform.rect;
                Vector2 normalizedLocalPoint = new(localDelta.x / rect.width, localDelta.y / rect.height);
                bool result = RaycastCheckPointInPolygon(normalizedLocalPoint);
                return result;
            }
            return false;
        }

        [ContextMenu(nameof(CreateNewPoint))]
        public void CreateNewPoint()
        {
            m_innerPoints ??= new List<Vector2>();
            if (2 > m_innerPoints.Count)
            {
                m_innerPoints.Add(Vector2.zero);
                return;
            }

            int pointCount = m_innerPoints.Count;
            Vector2 newPoint = m_innerPoints[pointCount - 1];
            Vector2 tempDir = m_innerPoints[0] - m_innerPoints[pointCount - 1];
            newPoint += 0.5f * tempDir;
            m_innerPoints.Add(newPoint);
        }

        public Vector2 GetPoint(int index)
        {
            if (null == m_innerPoints)
            {
                return Vector2.zero;
            }
            return m_innerPoints[index];
        }

        public void SetPoint(int index, Vector2 point)
        {
            if (0 > index || m_innerPoints.Count <= index)
            {
                Debug.LogError($"Index {index} is out of range for points list.");
                return;
            }
            m_innerPoints[index] = point;
        }

        /// <summary>
        /// Bottom left as (0,0)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="normalizedRectPosition"></param>
        /// <returns></returns>
        private Vector2 GetLocalRectPosition(RectTransform target, Vector2 normalizedRectPosition)
        {
            Vector2 result = Vector2.zero;
            Vector2 targetSize = target.rect.size;
            Vector2 targetPivot = target.pivot;

            Vector2 pivotOffset = Vector2.one * 0.5f - targetPivot;
            pivotOffset.x *= targetSize.x;
            pivotOffset.y *= targetSize.y;

            result += pivotOffset;

            // start from bottom left
            result.x -= targetSize.x * 0.5f;
            result.y -= targetSize.y * 0.5f;

            result.x += normalizedRectPosition.x * targetSize.x;
            result.y += normalizedRectPosition.y * targetSize.y;

            return result;
        }

        private UIVertex GenerateVert(Vector2 normalized, Rect vertRect, Rect uvRect, Color color)
        {
            Vector2 position = normalized * vertRect.size + vertRect.position;
            Vector2 uv = new Vector2(
                Mathf.Lerp(uvRect.xMin, uvRect.xMax, normalized.x),
                Mathf.Lerp(uvRect.yMin, uvRect.yMax, normalized.y)
            );

            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(position.x, position.y, 0f);
            vertex.normal = Vector3.forward;
            vertex.tangent = new Vector4(1f, 0f, 0f, -1f);
            vertex.color = color;
            vertex.uv0 = uv;

            return vertex;
        }

        private bool RaycastCheckPointInPolygon(Vector2 normalizedPoint)
        {
            int pointCount = m_innerPoints.Count;
            if (3 > pointCount)
            {
                return false;
            }

            if (3 == pointCount)
            {
                return PointInTriangle
                (
                    normalizedPoint,
                    m_innerPoints[0],
                    m_innerPoints[1],
                    m_innerPoints[2]
                );
            }

            int intersectionCount = 0;
            for (int i = 0, length = pointCount; i < length; i++)
            {
                Vector2 upPoint = m_innerPoints[i];
                Vector2 downPoint = m_innerPoints[(i + 1) % length];
                if (upPoint.y < downPoint.y)
                {
                    (upPoint, downPoint) = (downPoint, upPoint);
                }

                // Right edge-point
                Vector2 edge = new(1f, normalizedPoint.y);

                // First check
                if (Mathf.Max(upPoint.x, downPoint.x) < normalizedPoint.x ||
                    upPoint.y < normalizedPoint.y ||
                    downPoint.y > normalizedPoint.y)
                {
                    continue; // This segment wont collider
                }

                Vector2 p2up = upPoint - normalizedPoint;
                Vector2 down2up = upPoint - downPoint;

                // Point is the same as upPoint or downPoint or the point is on the segment or point is
                if ((Mathf.Approximately(upPoint.x, normalizedPoint.x) && Mathf.Approximately(upPoint.y, normalizedPoint.y)) ||
                    (Mathf.Approximately(downPoint.x, normalizedPoint.x) && Mathf.Approximately(downPoint.y, normalizedPoint.y)) ||
                    0 == Vector3.Cross(p2up, down2up).z)
                {
                    return true;
                }

                Vector2 p2edge = edge - normalizedPoint;
                Vector2 p2down = downPoint - normalizedPoint;

                Vector3 cross1 = Vector3.Cross(p2up, p2edge);
                Vector3 cross2 = Vector3.Cross(p2down, p2edge);

                Vector2 edge2p = normalizedPoint - edge;
                Vector2 edge2up = upPoint - edge;
                Vector2 edge2down = downPoint - edge;

                Vector3 cross3 = Vector3.Cross(edge2up, edge2p);
                Vector3 cross4 = Vector3.Cross(edge2down, edge2p);

                // Lines are intersecting
                if (0 >= cross1.z * cross2.z &&
                    0 >= cross3.z * cross4.z &&
                    0 < Vector3.Cross(p2down, p2up).z) // Check if point is at the left side of current segment
                {
                    intersectionCount++;
                }
            }

            return intersectionCount % 2 == 1;
        }

    }
}