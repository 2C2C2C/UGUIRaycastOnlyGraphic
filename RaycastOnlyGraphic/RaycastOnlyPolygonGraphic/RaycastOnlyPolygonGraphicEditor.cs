#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;

namespace UnityEngine.UI.RaycastOnlyGraphic
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RaycastOnlyPolygonGraphic), true)]
    public class RaycastOnlyIrregularGraphicEditor : Editor
    {
        private RaycastOnlyPolygonGraphic m_target;
        private SerializedProperty m_pointsProperty;
        private ReorderableList m_pointList;

        private bool m_normalized;

        private void OnEnable()
        {
            m_target = target as RaycastOnlyPolygonGraphic;
            m_pointsProperty = serializedObject.FindProperty("m_innerPoints");

            m_normalized = true;
            m_pointList = CreatePointList();
        }

        private void OnDisable()
        {
            m_target = null;
        }

        public override void OnInspectorGUI()
        {
            // TODO draw only raycast target field and readonly points, triangles
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space();
            m_pointList.DoLayoutList();

            if (GUILayout.Button("Create New Point"))
            {
                m_target.CreateNewPoint();
            }
            if (GUILayout.Button("Create Convex Hull"))
            {
                m_target.CreateConvexHullFromPoints();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private ReorderableList CreatePointList()
        {
            ReorderableList reorderableList = new(serializedObject, m_pointsProperty, true, true, true, true);
            reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                const float spacing = 2, fieldWidth = 10;
                rect.y += spacing; rect.height = EditorGUIUtility.singleLineHeight;
                SerializedProperty pointProp = m_pointsProperty.GetArrayElementAtIndex(index);
                Vector2 pointValue = pointProp.vector2Value;
                rect.width = (rect.width - spacing) / 2;
                EditorGUIUtility.labelWidth = fieldWidth;
                OnNormalizeFloatField(rect, "X", ref pointValue.x, 0);
                rect.x += rect.width + spacing;
                OnNormalizeFloatField(rect, "Y", ref pointValue.y, 1);
                EditorGUIUtility.labelWidth = 0;
                pointProp.vector2Value = pointValue;
            };

            reorderableList.onAddCallback = list =>
            {
                ReorderableList.defaultBehaviours.DoAddButton(list);
                if (m_pointsProperty.arraySize <= 1)
                {
                    return;
                }
                SerializedProperty startPointProp = m_pointsProperty.GetArrayElementAtIndex(0);
                SerializedProperty newPointProp = m_pointsProperty.GetArrayElementAtIndex(m_pointsProperty.arraySize - 1);
                newPointProp.vector2Value = (newPointProp.vector2Value + startPointProp.vector2Value) / 2;
            };

            reorderableList.onRemoveCallback = list =>
            {
                if (m_pointsProperty.arraySize <= 3)
                {
                    return;
                }
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            };

            reorderableList.drawHeaderCallback = rect =>
            {
                const int buttonWidth = 75;
                rect.width -= buttonWidth;
                EditorGUI.LabelField(rect, "Points");
                rect.x += rect.width;
                rect.width = buttonWidth;
                m_normalized = GUI.Toggle(rect, m_normalized, "Normalize", EditorStyles.miniButton);
            };

            return reorderableList;
        }

        private void OnNormalizeFloatField(Rect rect, string label, ref float value, int axis)
        {
            if (m_normalized)
            {
                value = Mathf.Clamp01(EditorGUI.FloatField(rect, label, value));
            }
            else
            {
                Vector2 size = ((target as RaycastOnlyPolygonGraphic).transform as RectTransform).rect.size;
                value = Mathf.Clamp01(EditorGUI.FloatField(rect, label, value * size[axis]) / size[axis]);
            }
        }

        private void OnSceneGUI()
        {
            if (m_target == null || !m_target.enabled)
            {
                return;
            }
            DrawPointHandles();
        }

        private void DrawPointHandles()
        {
            Color beforeColor = Handles.color;
            Matrix4x4 beforeMatrix = Handles.matrix;
            RectTransform rectTransform = m_target.transform as RectTransform;

            Handles.color = Color.blue;
            Handles.matrix = m_target.transform.localToWorldMatrix;

            Vector3 moveHandle = Vector3.zero;
            Vector3 moveSnap = Vector3.one * 0.5f;
            float moveSize = HandleUtility.GetHandleSize(m_target.transform.position) * 0.1f;

            EditorGUI.BeginChangeCheck();
            int pointCount = m_target.PointCount;
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                normal = { textColor = Color.white }
            };
            for (int i = 0; i < pointCount; i++)
            {
                moveHandle = (m_target.GetPoint(i) - rectTransform.pivot) * rectTransform.rect.size;
                moveHandle = Handles.FreeMoveHandle(moveHandle, moveSize, moveSnap, Handles.CircleHandleCap);
                m_target.SetPoint(i, Clamp01(moveHandle / rectTransform.rect.size + rectTransform.pivot));
                Handles.Label(moveHandle + Vector3.right * 8f, $"{i}", labelStyle);
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(m_target);
            }

            Handles.color = beforeColor;
            Handles.matrix = beforeMatrix;
        }

        private Vector2 Clamp01(Vector2 vector)
        {
            vector.x = Mathf.Clamp01(vector.x);
            vector.y = Mathf.Clamp01(vector.y);
            return vector;
        }
    }
}
#endif