using System.Collections.Generic;
using CineShot.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Playables;

namespace CineShot.Editor
{
    public sealed class StoryboardWindow : EditorWindow
    {
        CineSequenceAsset m_Sequence;
        PlayableDirector m_Director;
        ReorderableList m_List;
        Vector2 m_Scroll;
        readonly List<ShotAsset> m_PendingShots = new List<ShotAsset>();

        [MenuItem("Tools/Cine/Storyboard %#&b")]
        public static void Open()
        {
            GetWindow<StoryboardWindow>("Cine 分镜表");
        }

        public static void OpenWithShot(ShotAsset shot)
        {
            var win = GetWindow<StoryboardWindow>("Cine 分镜表");
            win.m_PendingShots.Add(shot);
            win.EnsureSequence();
            win.FlushPending();
        }

        void OnEnable()
        {
            EnsureSequence();
            RebuildList();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                m_Director = (PlayableDirector)EditorGUILayout.ObjectField(m_Director, typeof(PlayableDirector), true, GUILayout.Width(220));
                m_Sequence = (CineSequenceAsset)EditorGUILayout.ObjectField(m_Sequence, typeof(CineSequenceAsset), false);
                if (GUILayout.Button("New Seq", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    CreateSequence();
                if (GUILayout.Button("Lay Out Tracks", EditorStyles.toolbarButton, GUILayout.Width(110)))
                    LayOut();
            }

            FlushPending();
            if (m_Sequence == null)
            {
                EditorGUILayout.HelpBox("创建或指定 CineSequenceAsset。", MessageType.Info);
                return;
            }

            if (m_List == null)
                RebuildList();

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            m_List.DoLayoutList();
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Batch Set Duration 2.5s"))
                    BatchSetDuration(2.5);
            }
        }

        void EnsureSequence()
        {
            if (m_Sequence != null)
                return;
            string[] guids = AssetDatabase.FindAssets("t:CineSequenceAsset");
            if (guids.Length > 0)
                m_Sequence = AssetDatabase.LoadAssetAtPath<CineSequenceAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        void CreateSequence()
        {
            if (!AssetDatabase.IsValidFolder("Assets/CineShot/Samples/Assets/Sequences"))
            {
                System.IO.Directory.CreateDirectory("Assets/CineShot/Samples/Assets/Sequences");
                AssetDatabase.Refresh();
            }
            m_Sequence = ScriptableObject.CreateInstance<CineSequenceAsset>();
            AssetDatabase.CreateAsset(m_Sequence, AssetDatabase.GenerateUniqueAssetPath("Assets/CineShot/Samples/Assets/Sequences/Seq_.asset"));
            AssetDatabase.SaveAssets();
            RebuildList();
        }

        void FlushPending()
        {
            if (m_Sequence == null || m_PendingShots.Count == 0)
                return;
            Undo.RecordObject(m_Sequence, "Add Storyboard Rows");
            for (int i = 0; i < m_PendingShots.Count; i++)
            {
                var s = m_PendingShots[i];
                if (s == null) continue;
                m_Sequence.rows.Add(new StoryboardRow
                {
                    shotGuid = s.Id,
                    rowIndex = m_Sequence.rows.Count,
                    duration = s.defaultDuration,
                    blendMode = ShotBlendMode.Cut
                });
            }
            m_PendingShots.Clear();
            EditorUtility.SetDirty(m_Sequence);
            RebuildList();
        }

        void RebuildList()
        {
            if (m_Sequence == null)
            {
                m_List = null;
                return;
            }
            m_List = new ReorderableList(m_Sequence.rows, typeof(StoryboardRow), true, true, true, true);
            m_List.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Storyboard Rows");
            m_List.elementHeight = 44f;
            m_List.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= m_Sequence.rows.Count)
                    return;
                var row = m_Sequence.rows[index];
                float y = rect.y + 2;
                float w = rect.width;
                EditorGUI.LabelField(new Rect(rect.x, y, 28, 18), (index + 1).ToString());
                row.shotGuid = EditorGUI.TextField(new Rect(rect.x + 30, y, w * 0.35f, 18), row.shotGuid);
                row.duration = EditorGUI.DoubleField(new Rect(rect.x + w * 0.38f, y, 60, 18), row.duration);
                row.blendMode = (ShotBlendMode)EditorGUI.EnumPopup(new Rect(rect.x + w * 0.38f + 66, y, 90, 18), row.blendMode);
                row.note = EditorGUI.TextField(new Rect(rect.x + w * 0.38f + 160, y, w - (w * 0.38f + 160), 18), row.note);
                row.rowIndex = index;
            };
            m_List.onReorderCallback = list =>
            {
                Undo.RecordObject(m_Sequence, "Reorder Storyboard");
                for (int i = 0; i < m_Sequence.rows.Count; i++)
                    m_Sequence.rows[i].rowIndex = i;
                EditorUtility.SetDirty(m_Sequence);
            };
            m_List.onAddCallback = list =>
            {
                Undo.RecordObject(m_Sequence, "Add Storyboard Row");
                m_Sequence.rows.Add(new StoryboardRow { rowIndex = m_Sequence.rows.Count, duration = 4.0 });
                EditorUtility.SetDirty(m_Sequence);
            };
        }

        void BatchSetDuration(double d)
        {
            if (m_Sequence == null) return;
            Undo.RecordObject(m_Sequence, "Batch Duration");
            for (int i = 0; i < m_Sequence.rows.Count; i++)
                m_Sequence.rows[i].duration = d;
            EditorUtility.SetDirty(m_Sequence);
        }

        void LayOut()
        {
            if (m_Director == null)
                m_Director = Selection.activeGameObject != null
                    ? Selection.activeGameObject.GetComponent<PlayableDirector>()
                    : null;
            if (m_Director == null)
            {
                EditorUtility.DisplayDialog("CineShot", "请先选中带 PlayableDirector 的对象，或在窗口顶部指定。", "OK");
                return;
            }
            ShotTrackLayer.Reconcile(m_Director, m_Sequence, ConflictPolicy.Ask);
        }
    }
}
