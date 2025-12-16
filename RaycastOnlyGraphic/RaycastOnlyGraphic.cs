namespace UnityEngine.UI.RaycastOnlyGraphic
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class RaycastOnlyGraphic : MaskableGraphic
    {
        public override void SetMaterialDirty() { return; }
        public override void SetVerticesDirty() { return; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (IsActive()) // only update at this point
            {
                vh.Clear();
            }
        }

#if UNITY_EDITOR

    protected override void Reset()
    {
        base.Reset();
        raycastTarget = true;
    }

    public enum RectPositionType
    {
        Center = 0,

        Top = 1,
        Bottom = 2,
        Left = 3,
        Right = 4,

        TopLeft = 5,
        TopRight = 6,

        BottomLeft = 7,
        BottomRight = 8,
    }

    private void OnDrawGizmosSelected()
    {
        if (!this.gameObject.activeSelf || !this.enabled) return;
        RectTransform rectTransform = this.transform as RectTransform;
        Color wireColor = Color.yellow;
        if (!this.raycastTarget)
        {
            wireColor *= 0.45f;
            wireColor.a = 0.5f;
        }

        // Padding to be applied to the masking
        // X = Left, Y = Bottom, Z = Right, W = Top
        // if you wanna make it bigger, then the all value shouble be negative

        Vector4 padding = this.raycastPadding * -1.0f;
        Matrix4x4 localToWorld = rectTransform.localToWorldMatrix;
        Vector3 topLeft = GetLocalRectPosition(rectTransform, RectPositionType.TopLeft);
        Vector3 topRight = GetLocalRectPosition(rectTransform, RectPositionType.TopRight);
        Vector3 bottomLeft = GetLocalRectPosition(rectTransform, RectPositionType.BottomLeft);
        Vector3 bottomRight = GetLocalRectPosition(rectTransform, RectPositionType.BottomRight);
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
        Gizmos.color = tempColor;
    }

    public Vector2 GetLocalRectPosition(RectTransform target, RectPositionType offsetType)
    {
        Vector2 result = Vector2.zero;
        Vector2 targetSize = target.rect.size;
        Vector2 targetPivot = target.pivot;

        Vector2 pivotOffset = Vector2.one * 0.5f - targetPivot;
        pivotOffset.x *= targetSize.x;
        pivotOffset.y *= targetSize.y;

        result += pivotOffset;

        switch (offsetType)
        {
            case RectPositionType.Top:
                result.y += targetSize.y * 0.5f;
                break;
            case RectPositionType.Bottom:
                result.y -= targetSize.y * 0.5f;
                break;
            case RectPositionType.Left:
                result.x -= targetSize.x * 0.5f;
                break;
            case RectPositionType.Right:
                result.x += targetSize.x * 0.5f;
                break;

            case RectPositionType.TopLeft:
                result.y += targetSize.y * 0.5f;
                result.x -= targetSize.x * 0.5f;
                break;
            case RectPositionType.TopRight:
                result.y += targetSize.y * 0.5f;
                result.x += targetSize.x * 0.5f;
                break;

            case RectPositionType.BottomLeft:
                result.y -= targetSize.y * 0.5f;
                result.x -= targetSize.x * 0.5f;
                break;

            case RectPositionType.BottomRight:
                result.y -= targetSize.y * 0.5f;
                result.x += targetSize.x * 0.5f;
                break;

            default:
                break;
        }

        return result;
    }

#endif
    }
}
