using System.Collections.Generic;
using CineShot.Core;
using UnityEditor;
using UnityEngine;

namespace CineShot.Editor
{
    public sealed class ShotLibraryWindow : EditorWindow
    {
        enum ViewMode { List, Grid }

        ShotQuery m_Query;
        List<ShotAsset> m_Hits = new List<ShotAsset>();
        Vector2 m_Scroll;
        ViewMode m_View = ViewMode.List;
        string m_SelectedId;
        double m_LastQueryTime;

        [MenuItem("Tools/Cine/Shot Library %#&l")]
        public static void Open()
        {
            GetWindow<ShotLibraryWindow>("Cine Shot 库");
        }

        void OnEnable()
        {
            Refresh();
        }

        void OnGUI()
        {
            DrawToolbar();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList();
                DrawInspector();
            }
            DrawStatus();
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                m_Query.text = GUILayout.TextField(m_Query.text ?? string.Empty, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
                if (EditorGUI.EndChangeCheck())
                    m_LastQueryTime = EditorApplication.timeSinceStartup;

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ShotLibrary.Invalidate();
                    Refresh();
                }

                m_View = (ViewMode)GUILayout.Toolbar((int)m_View, new[] { "List", "Grid" }, EditorStyles.toolbarButton, GUILayout.Width(100));

                if (GUILayout.Button("Create ▾", EditorStyles.toolbarDropDown, GUILayout.Width(80)))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("From Scene View"), false, CreateFromSceneView);
                    menu.AddItem(new GUIContent("From Selected Camera"), false, CreateFromSelectedCamera);
                    menu.AddItem(new GUIContent("Static Default"), false, () => CreateDefault(RigKind.Static));
                    menu.AddItem(new GUIContent("Orbit Default"), false, () => CreateDefault(RigKind.Orbit));
                    menu.ShowAsContext();
                }
            }

            if (EditorApplication.timeSinceStartup - m_LastQueryTime > 0.2 && m_LastQueryTime > 0)
            {
                Refresh();
                m_LastQueryTime = 0;
            }
        }

        void DrawList()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, GUILayout.ExpandWidth(true));
            for (int i = 0; i < m_Hits.Count; i++)
            {
                var s = m_Hits[i];
                string label = (string.IsNullOrEmpty(s.displayName) ? s.name : s.displayName) + "  [" + s.rigKind + "]  " + s.defaultDuration.ToString("0.0") + "s";
                bool selected = s.Id == m_SelectedId;
                if (GUILayout.Toggle(selected, label, "Button"))
                    m_SelectedId = s.Id;
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawInspector()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
            {
                var shot = FindSelected();
                if (shot == null)
                {
                    EditorGUILayout.HelpBox("选中一个 Shot 查看详情。", MessageType.Info);
                    return;
                }
                EditorGUILayout.LabelField(shot.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Id", shot.Id);
                EditorGUILayout.LabelField("Rig", shot.rigKind.ToString());
                EditorGUILayout.LabelField("Duration", shot.defaultDuration.ToString("0.00") + "s");
                EditorGUILayout.LabelField("FOV", shot.fov.ToString("0.0"));
                EditorGUILayout.LabelField("Refs", ShotLibrary.CountReferences(shot).ToString());
                if (GUILayout.Button("Ping"))
                    EditorGUIUtility.PingObject(shot);
                if (GUILayout.Button("Duplicate As Variant"))
                {
                    var v = ShotLibrary.DuplicateAsVariant(shot);
                    if (v != null)
                    {
                        m_SelectedId = v.Id;
                        Refresh();
                    }
                }
                if (GUILayout.Button("Send To Storyboard"))
                    StoryboardWindow.OpenWithShot(shot);
            }
        }

        void DrawStatus()
        {
            EditorGUILayout.LabelField("Hits: " + m_Hits.Count, EditorStyles.miniLabel);
        }

        void Refresh()
        {
            m_Hits = ShotLibrary.Query(m_Query);
            Repaint();
        }

        ShotAsset FindSelected()
        {
            for (int i = 0; i < m_Hits.Count; i++)
                if (m_Hits[i].Id == m_SelectedId)
                    return m_Hits[i];
            return null;
        }

        void CreateFromSceneView()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null || sv.camera == null)
            {
                EditorUtility.DisplayDialog("CineShot", "没有活动的 SceneView 相机。", "OK");
                return;
            }
            var cam = sv.camera;
            var shot = ShotLibrary.Create("Assets/CineShot/Samples/Assets/Shots", "Shot_SceneCapture", RigKind.Static);
            shot.fov = cam.fieldOfView;
            shot.nearClip = cam.nearClipPlane;
            shot.farClip = cam.farClipPlane;
            shot.rigParams.A = cam.transform.position;
            shot.rigParams.P0 = cam.transform.eulerAngles.y;
            shot.rigParams.P1 = cam.transform.eulerAngles.x;
            EditorUtility.SetDirty(shot);
            AssetDatabase.SaveAssets();
            m_SelectedId = shot.Id;
            Refresh();
        }

        void CreateFromSelectedCamera()
        {
            var cam = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Camera>() : null;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("CineShot", "请先选中带 Camera 的对象。", "OK");
                return;
            }
            var shot = ShotLibrary.Create("Assets/CineShot/Samples/Assets/Shots", "Shot_FromCamera", RigKind.Static);
            shot.fov = cam.fieldOfView;
            shot.nearClip = cam.nearClipPlane;
            shot.farClip = cam.farClipPlane;
            EditorUtility.SetDirty(shot);
            AssetDatabase.SaveAssets();
            m_SelectedId = shot.Id;
            Refresh();
        }

        void CreateDefault(RigKind kind)
        {
            var shot = ShotLibrary.Create("Assets/CineShot/Samples/Assets/Shots", "Shot_" + kind, kind);
            m_SelectedId = shot.Id;
            Refresh();
        }
    }
}
