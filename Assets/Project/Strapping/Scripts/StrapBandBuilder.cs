using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using NaughtyAttributes;

namespace Project.Strapping
{
    /// <summary>
    /// 轧带网格生成（与送带运动解耦）：跟踪 FeedTip 沿矩形回路位移，增量写带。
    /// 静止或关闭时不追加；一条带完成后可 SaveBandToCargo。
    /// </summary>
    [DisallowMultipleComponent]
    public class StrapBandBuilder : MonoBehaviour
    {
        [Header("目标")]
        [Label("货物根节点")]
        [SerializeField] private Transform m_cargo;

        [Label("带体节点")]
        [SerializeField] private Transform m_bandModel;

        [Header("带体")]
        [Label("带宽(米)")]
        [SerializeField] private float m_strapWidth = 0.025f;

        [Label("外扩间隙")]
        [SerializeField] private float m_clearance = 0.03f;

        [Header("送带跟踪")]
        [Label("送带跟踪点")]
        [SerializeField] private Transform m_feedTip;

        [Label("启用写带")]
        [SerializeField] private bool m_buildEnabled = true;

        [Label("最小段距")]
        [SerializeField] [Min(0.01f)] private float m_minSegmentDistance = 0.06f;

        [Label("静止阈值(m/s)")]
        [SerializeField] [Min(0f)] private float m_stopSpeedThreshold = 0.05f;

        [Label("瞬移距离")]
        [SerializeField] private float m_teleportDistance = 0.75f;

        private readonly List<Vector3> _vertices = new List<Vector3>(512);
        private readonly List<Vector3> _normals = new List<Vector3>(512);
        private readonly List<Vector2> _uvs = new List<Vector2>(512);
        private readonly List<int> _triangles = new List<int>(1024);

        private Mesh _mesh;
        private MeshFilter _filter;
        private Vector3 _lastTipPos;
        private Vector3 _lastSamplePos;
        private bool _hasLastTip;
        private bool _hasLastSample;
        private bool _meshDirty;
        private float _pathLength;
        private float _minSegSqr;
        private float _teleportSqr;
        private float _stopSpeed;
        private bool _isFeedMoving;

        public Transform Cargo => m_cargo;
        public Transform FeedTip => m_feedTip;
        public bool BuildEnabled
        {
            get => m_buildEnabled;
            set => m_buildEnabled = value;
        }

        public bool IsFeedMoving => _isFeedMoving;
        public float PathLength => _pathLength;
        public int SegmentCount => _hasLastSample ? _vertices.Count / 2 : 0;

        private void Awake()
        {
            CacheThresholds();
            EnsureBandModel();
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        private void LateUpdate()
        {
            TrackFeedAndBuild();
            if (_meshDirty)
                ApplyMesh();
        }

        public void ResetBand()
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _triangles.Clear();
            _pathLength = 0f;
            _hasLastSample = false;
            _hasLastTip = false;
            _meshDirty = true;

            if (m_feedTip != null)
            {
                _lastTipPos = m_feedTip.position;
                _hasLastTip = true;
            }

            ApplyMesh();
        }

        [Button("创建新带")]
        public void CreateNewBand()
        {
            if (m_bandModel == null)
            {
                var go = new GameObject("StrapBand", typeof(MeshFilter), typeof(MeshRenderer));
                m_bandModel = go.transform;
                m_bandModel.SetParent(transform, false);
            }

            _filter = m_bandModel.GetComponent<MeshFilter>();
            _mesh = new Mesh { name = "StrapBand" };
            _mesh.MarkDynamic();
            _filter.sharedMesh = _mesh;
            ResetBand();
        }

        /// <summary>复制当前带网格到 Cargo 下（本地空间）。</summary>
        [Button("保存带到货物")]
        public void SaveBandToCargo()
        {
            if (m_cargo == null || m_bandModel == null || _mesh == null || _mesh.vertexCount < 2)
                return;

            if (_meshDirty)
                ApplyMesh();

            Mesh copy = Instantiate(_mesh);
            copy.name = "StrapBandSaved";

            Vector3[] verts = copy.vertices;
            Vector3[] norms = copy.normals;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 world = m_bandModel.TransformPoint(verts[i]);
                verts[i] = m_cargo.InverseTransformPoint(world);
            }

            if (norms != null && norms.Length == verts.Length)
            {
                for (int i = 0; i < norms.Length; i++)
                    norms[i] = m_cargo.InverseTransformDirection(m_bandModel.TransformDirection(norms[i]));
                copy.normals = norms;
            }

            copy.vertices = verts;
            copy.RecalculateBounds();

            var saved = new GameObject("StrapBand");
            saved.transform.SetParent(m_cargo, false);
            saved.transform.localPosition = Vector3.zero;
            saved.transform.localRotation = Quaternion.identity;
            saved.transform.localScale = Vector3.one;

            saved.AddComponent<MeshFilter>().sharedMesh = copy;
            var srcMr = m_bandModel.GetComponent<MeshRenderer>();
            var mr = saved.AddComponent<MeshRenderer>();
            if (srcMr != null)
            {
                mr.sharedMaterials = srcMr.sharedMaterials;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private void EnsureBandModel()
        {
            if (m_bandModel == null)
                CreateNewBand();
            else if (_filter == null || _mesh == null)
            {
                _filter = m_bandModel.GetComponent<MeshFilter>();
                if (_filter == null)
                    _filter = m_bandModel.gameObject.AddComponent<MeshFilter>();
                _mesh = _filter.sharedMesh;
                if (_mesh == null)
                {
                    _mesh = new Mesh { name = "StrapBand" };
                    _mesh.MarkDynamic();
                    _filter.sharedMesh = _mesh;
                }
            }
        }

        private void CacheThresholds()
        {
            _minSegSqr = m_minSegmentDistance * m_minSegmentDistance;
            _teleportSqr = m_teleportDistance * m_teleportDistance;
            _stopSpeed = m_stopSpeedThreshold;
        }

        private void TrackFeedAndBuild()
        {
            if (m_feedTip == null)
            {
                _isFeedMoving = false;
                return;
            }

            Vector3 tip = m_feedTip.position;
            float dt = Time.deltaTime > 1e-6f ? Time.deltaTime : 1e-6f;

            if (!_hasLastTip)
            {
                _lastTipPos = tip;
                _hasLastTip = true;
                _isFeedMoving = false;
                return;
            }

            Vector3 delta = tip - _lastTipPos;
            float moveSqr = delta.sqrMagnitude;

            if (moveSqr >= _teleportSqr)
            {
                _lastTipPos = tip;
                _lastSamplePos = tip;
                _hasLastSample = false;
                _isFeedMoving = false;
                return;
            }

            _isFeedMoving = moveSqr >= _stopSpeed * _stopSpeed * dt * dt;

            if (m_buildEnabled && _isFeedMoving)
            {
                if (!_hasLastSample)
                {
                    AppendAt(tip, delta.normalized);
                    _lastSamplePos = tip;
                    _hasLastSample = true;
                }
                else if ((tip - _lastSamplePos).sqrMagnitude >= _minSegSqr)
                {
                    _pathLength += Vector3.Distance(tip, _lastSamplePos);
                    AppendAt(tip, delta.sqrMagnitude > 1e-8f ? delta.normalized : (tip - _lastSamplePos).normalized);
                    _lastSamplePos = tip;
                }
            }

            _lastTipPos = tip;
        }

        private void AppendAt(Vector3 worldPos, Vector3 tangent)
        {
            if (tangent.sqrMagnitude < 1e-8f)
                tangent = Vector3.forward;
            tangent.Normalize();

            Vector3 up = Vector3.up;
            Vector3 side = Vector3.Cross(up, tangent);
            if (side.sqrMagnitude < 1e-8f)
                side = Vector3.right;
            side.Normalize();

            float half = m_strapWidth * 0.5f;
            Vector3 n = Vector3.Cross(side, tangent).normalized;

            Vector3 v0 = worldPos + side * half;
            Vector3 v1 = worldPos - side * half;

            // 略外扩，避免与货箱 Z-fighting
            if (m_cargo != null)
            {
                Vector3 center = m_cargo.position;
                Vector3 outward = worldPos - center;
                outward.y = 0f;
                if (outward.sqrMagnitude > 1e-8f)
                {
                    outward.Normalize();
                    v0 += outward * m_clearance;
                    v1 += outward * m_clearance;
                }
            }

            int baseIndex = _vertices.Count;
            _vertices.Add(m_bandModel.InverseTransformPoint(v0));
            _vertices.Add(m_bandModel.InverseTransformPoint(v1));
            Vector3 localN = m_bandModel.InverseTransformDirection(n);
            _normals.Add(localN);
            _normals.Add(localN);
            _uvs.Add(new Vector2(_pathLength, 1f));
            _uvs.Add(new Vector2(_pathLength, 0f));

            if (baseIndex >= 2)
            {
                int i0 = baseIndex - 2;
                int i1 = baseIndex - 1;
                int i2 = baseIndex;
                int i3 = baseIndex + 1;
                _triangles.Add(i0);
                _triangles.Add(i2);
                _triangles.Add(i1);
                _triangles.Add(i1);
                _triangles.Add(i2);
                _triangles.Add(i3);
            }

            _meshDirty = true;
        }

        private void ApplyMesh()
        {
            _meshDirty = false;
            if (_mesh == null)
                return;

            if (_vertices.Count == 0)
            {
                _mesh.Clear();
                return;
            }

            _mesh.Clear(false);
            _mesh.SetVertices(_vertices);
            _mesh.SetNormals(_normals);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetTriangles(_triangles, 0, false);
            _mesh.RecalculateBounds();
        }

#if UNITY_EDITOR
        private void OnValidate() => CacheThresholds();
#endif
    }
}
