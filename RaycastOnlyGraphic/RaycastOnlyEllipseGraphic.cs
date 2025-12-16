namespace UnityEngine.UI.RaycastOnlyGraphic
{
    public class RaycastOnlyEllipseGraphic : MaskableGraphic
    {
        [SerializeField]
        private bool _generateMesh = false;
        [SerializeField]
        private int _segments = 36;

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
            if (IsActive())
            {
                vh.Clear();

                if (_generateMesh)
                {
                    Rect rect = rectTransform.rect;

                    // Calculate the center and radii
                    Vector2 center = rect.center;
                    float radiusX = rect.width / 2;
                    float radiusY = rect.height / 2;

                    // Create the ellipse vertices
                    int segments = _segments; // Number of segments for the ellipse
                    for (int i = 0; i < segments; i++)
                    {
                        float angle = i * Mathf.PI * 2 / segments;
                        Vector2 point = new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                        vh.AddVert(point + center, color, Vector2.zero);
                    }

                    // Create the triangles
                    for (int i = 1; i < segments - 1; i++)
                    {
                        vh.AddTriangle(0, i, i + 1);
                    }
                }
            }
        }

        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out Vector2 localPoint);
            Vector2 ellipseCenter = rectTransform.rect.center;
            Vector2 ellipseSize = rectTransform.rect.size / 2;

#if UNITY_EDITOR
            Debug.DrawLine(rectTransform.TransformPoint(localPoint), rectTransform.position, Color.red);
#endif

            // Check if the point is inside the ellipse
            if (Mathf.Pow((localPoint.x - ellipseCenter.x) / ellipseSize.x, 2) + Mathf.Pow((localPoint.y - ellipseCenter.y) / ellipseSize.y, 2) <= 1)
            {
                return true;
            }

            return false;
        }

#if UNITY_EDITOR

        [SerializeField]
        private bool _onlyDrawGizmoWhenSelected = true;

        protected override void Reset()
        {
            base.Reset();
            _generateMesh = false;
            raycastTarget = true;
            _segments = 36;
        }

        private void OnDrawGizmos()
        {
            if (!_onlyDrawGizmoWhenSelected)
            {
                DrawEllipseGizmo();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_onlyDrawGizmoWhenSelected)
            {
                DrawEllipseGizmo();
            }
        }

        private void DrawEllipseGizmo()
        {
            if (!this.gameObject.activeSelf || !this.enabled)
            {
                return;
            }

            Color wireColor = Color.yellow;
            if (!this.raycastTarget)
            {
                wireColor *= 0.45f;
                wireColor.a = 0.5f;
            }

            Matrix4x4 localToWorld = rectTransform.localToWorldMatrix;
            Vector2 center = rectTransform.rect.center;
            Vector2 size = rectTransform.rect.size / 2;

            Color prevColor = Gizmos.color;
            Gizmos.color = wireColor;
            int segments = _segments;
            Vector3 previousPoint = localToWorld.MultiplyPoint(new Vector3(center.x + size.x, center.y, 0));
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                Vector3 point = new Vector3(center.x + Mathf.Cos(angle) * size.x, center.y + Mathf.Sin(angle) * size.y, 0);
                Vector3 worldPoint = localToWorld.MultiplyPoint(point);

                Gizmos.DrawLine(previousPoint, worldPoint);
                previousPoint = worldPoint;
            }
            Gizmos.color = prevColor;
        }

#endif

    }
}