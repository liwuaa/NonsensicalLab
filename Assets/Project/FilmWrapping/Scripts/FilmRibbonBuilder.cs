using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using NaughtyAttributes;

namespace Project.FilmWrapping
{
    /// <summary>
    /// 缠膜生成（与运动解耦）：跟踪喷嘴位移追加膜段；静止则停止生成。
    /// </summary>
    [DisallowMultipleComponent]
    public class FilmRibbonBuilder : MonoBehaviour
    {
        [Header("包裹目标")]
        [Label("货物根节点")]
        [SerializeField] private Transform m_cargo;

        [Label("射线起点距离")]
        [SerializeField] private float m_rayStartDistance = 2.5f;

        [SerializeField, Label("膜节点")] private Transform m_ribbonModel;

        [Header("贴合表面")]
        [Label("贴合表面")]
        [SerializeField] private bool m_fitToSurface = true;

        [Label("表面外扩")]
        [SerializeField] [Min(0.005f)] private float m_surfaceOffset = 0.04f;

        [Header("膜带参数")]
        [Label("膜带宽度")]
        [SerializeField] private float m_filmWidth = 0.28f;

        [Header("喷嘴跟踪")]
        [Label("喷嘴节点")]
        [SerializeField] private Transform m_nozzleTip;

        [Label("启用缠膜")]
        [SerializeField] private bool m_wrappingEnabled = true;

        [Label("最小追加距离")]
        [SerializeField] [Min(0.01f)] private float m_minSegmentDistance = 0.08f;

        [Label("静止速度阈值")]
        [SerializeField] [Min(0f)] private float m_stopSpeedThreshold = 0.05f;

        [Label("瞬移判定距离")]
        [SerializeField] private float m_teleportDistance = 0.75f;

        [Header("喷嘴出膜")]
        [Label("显示出膜带")]
        [SerializeField] private bool m_showFeedStrip = true;

        [Label("出膜起点偏移")]
        [SerializeField] private Vector3 m_feedStartLocalOffset = new Vector3(-0.12f, 0f, -0.05f);

        private readonly List<Vector3> _vertices = new List<Vector3>(1024);
        private readonly List<Vector3> _normals = new List<Vector3>(1024);
        private readonly List<Vector2> _uvs = new List<Vector2>(1024);
        private readonly List<int> _triangles = new List<int>(2048);

        private readonly List<Vector3> _feedVertices = new List<Vector3>(8);
        private readonly List<Vector3> _feedNormals = new List<Vector3>(8);
        private readonly List<Vector2> _feedUvs = new List<Vector2>(8);
        private readonly List<int> _feedTriangles = new List<int>(12);

        private Mesh _mesh;
        private MeshFilter _filter;
        private Mesh _feedMesh;
        private MeshRenderer _feedRenderer;
        private Collider[] _cargoColliders;

        private Vector3 _lastNozzlePos;
        private Vector3 _lastSamplePos;
        private Vector3 _lastContactMid;
        private Vector3 _lastContactNormal;
        private bool _hasLastNozzlePos;
        private bool _hasLastSamplePos;
        private bool _hasContact;
        private bool _meshDirty;
        private float _pathLength;
        private float _minSegSqr;
        private float _teleportSqr;
        private float _stopSpeed;
        private bool _isNozzleMoving;

        public Transform Cargo => m_cargo;
        public Transform NozzleTip => m_nozzleTip;

        public bool WrappingEnabled
        {
            get => m_wrappingEnabled;
            set => m_wrappingEnabled = value;
        }

        public bool IsNozzleMoving => _isNozzleMoving;
        public float PathLength => _pathLength;
        public int RingCount => _hasLastSamplePos ? _vertices.Count / 2 : 0;

        public void SetCargo(Transform cargo)
        {
            m_cargo = cargo;
            CacheCargoColliders();
        }

        public void SetNozzleTip(Transform tip) => m_nozzleTip = tip;

        private void Awake()
        {
            CacheThresholds();
            CreateNewRibbon();

            var hostRenderer = GetComponent<MeshRenderer>();
            var ribbonRenderer = m_ribbonModel.GetComponent<MeshRenderer>();
            if (ribbonRenderer != null)
            {
                if (hostRenderer != null && hostRenderer.sharedMaterial != null)
                    ribbonRenderer.sharedMaterial = hostRenderer.sharedMaterial;
                ribbonRenderer.shadowCastingMode = ShadowCastingMode.Off;
                ribbonRenderer.receiveShadows = false;
                if (hostRenderer != null)
                    hostRenderer.enabled = false;
            }

            EnsureFeedMesh(ribbonRenderer != null ? ribbonRenderer.sharedMaterial : null);
            CacheCargoColliders();
            AutoFitFromCargoBounds();
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
            if (_feedMesh != null)
                Destroy(_feedMesh);
        }

        private void LateUpdate()
        {
            TrackNozzleAndWrap();
            if (_meshDirty)
                ApplyMesh();
        }

        public void ResetWrap()
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _triangles.Clear();
            _pathLength = 0f;
            _hasLastSamplePos = false;
            _hasLastNozzlePos = false;
            _hasContact = false;
            _meshDirty = true;

            if (m_nozzleTip != null)
            {
                _lastNozzlePos = m_nozzleTip.position;
                _hasLastNozzlePos = true;
            }

            ApplyMesh();
            UpdateFeedStrip(force: true);
        }

        [Button("创建新缠膜")]
        public void CreateNewRibbon()
        {
            if (m_ribbonModel == null)
            {
                m_ribbonModel = new GameObject("Ribbon", typeof(MeshRenderer), typeof(MeshFilter)).transform;
                m_ribbonModel.SetParent(transform, false);
                m_ribbonModel.localPosition = Vector3.zero;
                m_ribbonModel.localRotation = Quaternion.identity;
                m_ribbonModel.localScale = Vector3.one;
            }

            _filter = m_ribbonModel.GetComponent<MeshFilter>();
            if (_filter == null)
                _filter = m_ribbonModel.gameObject.AddComponent<MeshFilter>();

            _mesh = new Mesh { name = "FilmRibbon" };
            _mesh.MarkDynamic();
            _filter.sharedMesh = _mesh;

            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _triangles.Clear();
            _pathLength = 0f;
            _hasLastSamplePos = false;
            _hasContact = false;
            _meshDirty = false;
        }

        /// <summary>将当前缠膜网格复制一份，挂到 Cargo 下（本地空间对齐货物）。</summary>
        [Button("保存缠膜到货物")]
        public void SaveRibbonToCargo()
        {
            if (m_cargo == null || m_ribbonModel == null)
                return;

            if (_meshDirty)
                ApplyMesh();

            if (_mesh == null || _mesh.vertexCount < 2)
                return;

            Mesh copy = Instantiate(_mesh);
            copy.name = "FilmRibbonSaved";

            // 顶点按 Builder.transform 本地写入，转到 Cargo 本地，使子节点可随货物移动
            Vector3[] verts = copy.vertices;
            Vector3[] norms = copy.normals;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 world = transform.TransformPoint(verts[i]);
                verts[i] = m_cargo.InverseTransformPoint(world);
            }

            if (norms != null && norms.Length == verts.Length)
            {
                for (int i = 0; i < norms.Length; i++)
                {
                    Vector3 worldN = transform.TransformDirection(norms[i]);
                    norms[i] = m_cargo.InverseTransformDirection(worldN);
                }

                copy.normals = norms;
            }

            copy.vertices = verts;
            copy.RecalculateBounds();

            var saved = new GameObject("Ribbon");
            saved.transform.SetParent(m_cargo, false);
            saved.transform.localPosition = Vector3.zero;
            saved.transform.localRotation = Quaternion.identity;
            saved.transform.localScale = Vector3.one;

            var mf = saved.AddComponent<MeshFilter>();
            mf.sharedMesh = copy;

            var srcMr = m_ribbonModel.GetComponent<MeshRenderer>();
            var mr = saved.AddComponent<MeshRenderer>();
            if (srcMr != null)
            {
                mr.sharedMaterials = srcMr.sharedMaterials;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        [Button("完成并开始新缠膜")]
        public void FillRibbonComplete()
        {
            SaveRibbonToCargo();
            CreateNewRibbon();
        }

        private void CacheThresholds()
        {
            _minSegSqr = m_minSegmentDistance * m_minSegmentDistance;
            _teleportSqr = m_teleportDistance * m_teleportDistance;
            _stopSpeed = m_stopSpeedThreshold;
        }

        private void EnsureFeedMesh(Material filmMaterial)
        {
            Transform feedTf = transform.Find("FilmFeed");
            GameObject feedGo;
            if (feedTf == null)
            {
                feedGo = new GameObject("FilmFeed", typeof(MeshFilter), typeof(MeshRenderer));
                feedGo.transform.SetParent(transform, false);
            }
            else
            {
                feedGo = feedTf.gameObject;
            }

            var filter = feedGo.GetComponent<MeshFilter>();
            if (filter == null)
                filter = feedGo.AddComponent<MeshFilter>();

            _feedRenderer = feedGo.GetComponent<MeshRenderer>();
            if (_feedRenderer == null)
                _feedRenderer = feedGo.AddComponent<MeshRenderer>();

            if (filmMaterial != null)
                _feedRenderer.sharedMaterial = filmMaterial;
            _feedRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _feedRenderer.receiveShadows = false;

            if (_feedMesh == null)
            {
                _feedMesh = new Mesh { name = "FilmFeed" };
                _feedMesh.MarkDynamic();
            }

            filter.sharedMesh = _feedMesh;
        }

        private void TrackNozzleAndWrap()
        {
            if (m_nozzleTip == null)
            {
                _isNozzleMoving = false;
                if (_feedRenderer != null)
                    _feedRenderer.enabled = false;
                return;
            }

            Vector3 nozzlePos = m_nozzleTip.position;
            float dt = Time.deltaTime;
            if (dt < 1e-6f)
                dt = 1e-6f;

            if (!_hasLastNozzlePos)
            {
                _lastNozzlePos = nozzlePos;
                _hasLastNozzlePos = true;
                _isNozzleMoving = false;
                UpdateFeedStrip(force: true);
                return;
            }

            Vector3 delta = nozzlePos - _lastNozzlePos;
            float moveSqr = delta.sqrMagnitude;

            if (moveSqr >= _teleportSqr)
            {
                _lastNozzlePos = nozzlePos;
                _lastSamplePos = nozzlePos;
                _hasLastSamplePos = false;
                _hasContact = false;
                _isNozzleMoving = false;
                UpdateFeedStrip(force: true);
                return;
            }

            float speed = Mathf.Sqrt(moveSqr) / dt;
            _isNozzleMoving = speed >= _stopSpeed;

            bool appended = false;
            if (m_wrappingEnabled && _isNozzleMoving && m_cargo != null)
            {
                if (!_hasLastSamplePos)
                {
                    AppendRing(nozzlePos);
                    _lastSamplePos = nozzlePos;
                    _hasLastSamplePos = true;
                    appended = true;
                }
                else if ((nozzlePos - _lastSamplePos).sqrMagnitude >= _minSegSqr)
                {
                    _pathLength += Vector3.Distance(nozzlePos, _lastSamplePos);
                    AppendRing(nozzlePos);
                    _lastSamplePos = nozzlePos;
                    appended = true;
                }
            }

            _lastNozzlePos = nozzlePos;
            // 出膜带：追加段时必刷；否则仅在移动时刷，静止跳过
            if (appended || _isNozzleMoving)
                UpdateFeedStrip(force: appended);
        }

        private void AppendRing(Vector3 nozzlePos)
        {
            Vector3 center = m_cargo.position;
            Vector3 radial = nozzlePos - center;
            radial.y = 0f;
            if (radial.sqrMagnitude < 1e-8f)
                radial = Vector3.right;
            else
                radial.Normalize();

            float halfW = m_filmWidth * 0.5f;
            float localY = nozzlePos.y - center.y;

            SampleSurface(radial, localY + halfW, out Vector3 v0, out Vector3 n0);
            SampleSurface(radial, localY - halfW, out Vector3 v1, out Vector3 n1);

            Vector3 n = n0 + n1;
            if (n.sqrMagnitude < 1e-8f)
                n = radial;
            n.Normalize();

            _lastContactMid = (v0 + v1) * 0.5f;
            _lastContactNormal = n;
            _hasContact = true;

            int baseIndex = _vertices.Count;
            _vertices.Add(transform.InverseTransformPoint(v0));
            _vertices.Add(transform.InverseTransformPoint(v1));
            Vector3 localN = transform.InverseTransformDirection(n);
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

        private void CacheCargoColliders()
        {
            _cargoColliders = m_cargo != null ? m_cargo.GetComponentsInChildren<Collider>() : null;
        }

        /// <summary>水平径向向内采样货物表面；命中后沿法线外扩，不再做多轮 ClosestPoint 推挤。</summary>
        private void SampleSurface(Vector3 radial, float localY, out Vector3 point, out Vector3 normal)
        {
            Vector3 center = m_cargo.position;
            float startDist = m_rayStartDistance > 1.5f ? m_rayStartDistance : 1.5f;
            Vector3 probe = new Vector3(center.x, center.y + localY, center.z) + radial * startDist;

            if (m_fitToSurface)
            {
                if (_cargoColliders == null || _cargoColliders.Length == 0)
                    CacheCargoColliders();

                if (_cargoColliders != null && _cargoColliders.Length > 0)
                {
                    Ray ray = new Ray(probe, -radial);
                    float bestDist = float.MaxValue;
                    RaycastHit bestHit = default;
                    bool hitAny = false;

                    for (int i = 0; i < _cargoColliders.Length; i++)
                    {
                        Collider col = _cargoColliders[i];
                        if (col == null || !col.enabled)
                            continue;
                        if (col.Raycast(ray, out RaycastHit hit, startDist + 2f) && hit.distance < bestDist)
                        {
                            bestDist = hit.distance;
                            bestHit = hit;
                            hitAny = true;
                        }
                    }

                    if (hitAny)
                    {
                        normal = bestHit.normal;
                        if (normal.sqrMagnitude < 1e-8f)
                            normal = radial;
                        else
                            normal.Normalize();
                        if (Vector3.Dot(normal, radial) < 0f)
                            normal = -normal;
                        point = bestHit.point + normal * m_surfaceOffset;
                        return;
                    }

                    // 射线未命中：ClosestPoint 一次即可
                    float bestSqr = float.MaxValue;
                    Vector3 bestPoint = probe;
                    bool found = false;
                    for (int i = 0; i < _cargoColliders.Length; i++)
                    {
                        Collider col = _cargoColliders[i];
                        if (col == null || !col.enabled)
                            continue;
                        Vector3 cp = col.ClosestPoint(probe);
                        float sqr = (cp - probe).sqrMagnitude;
                        if (sqr < bestSqr)
                        {
                            bestSqr = sqr;
                            bestPoint = cp;
                            found = true;
                        }
                    }

                    if (found)
                    {
                        Vector3 outward = probe - bestPoint;
                        outward.y = 0f;
                        normal = outward.sqrMagnitude > 1e-8f ? outward.normalized : radial;
                        point = bestPoint + normal * m_surfaceOffset;
                        return;
                    }
                }
            }

            float r = Mathf.Max(0.2f, startDist * 0.35f);
            point = new Vector3(center.x + radial.x * r, center.y + localY, center.z + radial.z * r);
            normal = radial;
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

        private void UpdateFeedStrip(bool force)
        {
            bool show = m_showFeedStrip && m_wrappingEnabled && m_nozzleTip != null && m_cargo != null;
            if (_feedRenderer != null)
                _feedRenderer.enabled = show;
            if (!show || _feedMesh == null)
                return;

            Vector3 nozzlePos = m_nozzleTip.position;
            Vector3 center = m_cargo.position;
            Vector3 radial = nozzlePos - center;
            radial.y = 0f;
            if (radial.sqrMagnitude < 1e-8f)
                radial = Vector3.right;
            else
                radial.Normalize();

            Vector3 contactMid;
            Vector3 contactN;
            if (_hasContact && !force)
            {
                contactMid = _lastContactMid;
                contactN = _lastContactNormal;
            }
            else
            {
                float halfW = m_filmWidth * 0.5f;
                float localY = nozzlePos.y - center.y;
                SampleSurface(radial, localY + halfW, out Vector3 top, out Vector3 n0);
                SampleSurface(radial, localY - halfW, out Vector3 bot, out Vector3 n1);
                contactMid = (top + bot) * 0.5f;
                contactN = n0 + n1;
                if (contactN.sqrMagnitude < 1e-8f)
                    contactN = radial;
                contactN.Normalize();
                _lastContactMid = contactMid;
                _lastContactNormal = contactN;
                _hasContact = true;
            }

            Vector3 start = m_nozzleTip.TransformPoint(m_feedStartLocalOffset);
            float half = m_filmWidth * 0.5f;
            Vector3 up = Vector3.up * half;

            // 两段折线：喷嘴 → 中点外扩 → 接触，避免每点 ClosestPoint
            Vector3 mid = (start + contactMid) * 0.5f + radial * 0.12f;
            Vector3 feedSide = Vector3.Cross(contactMid - start, Vector3.up);
            if (feedSide.sqrMagnitude < 1e-8f)
                feedSide = contactN;
            else
                feedSide.Normalize();
            if (Vector3.Dot(feedSide, radial) < 0f)
                feedSide = -feedSide;

            _feedVertices.Clear();
            _feedNormals.Clear();
            _feedUvs.Clear();
            _feedTriangles.Clear();

            AddFeedRing(start, feedSide, up, 0f);
            AddFeedRing(mid, Vector3.Normalize(feedSide + contactN), up, 0.5f);
            AddFeedRing(contactMid, contactN, up, 1f);

            _feedTriangles.Add(0);
            _feedTriangles.Add(2);
            _feedTriangles.Add(1);
            _feedTriangles.Add(1);
            _feedTriangles.Add(2);
            _feedTriangles.Add(3);
            _feedTriangles.Add(2);
            _feedTriangles.Add(4);
            _feedTriangles.Add(3);
            _feedTriangles.Add(3);
            _feedTriangles.Add(4);
            _feedTriangles.Add(5);

            _feedMesh.Clear(false);
            _feedMesh.SetVertices(_feedVertices);
            _feedMesh.SetNormals(_feedNormals);
            _feedMesh.SetUVs(0, _feedUvs);
            _feedMesh.SetTriangles(_feedTriangles, 0, false);
            _feedMesh.RecalculateBounds();
        }

        private void AddFeedRing(Vector3 mid, Vector3 normal, Vector3 up, float u)
        {
            _feedVertices.Add(transform.InverseTransformPoint(mid + up));
            _feedVertices.Add(transform.InverseTransformPoint(mid - up));
            Vector3 localN = transform.InverseTransformDirection(normal);
            _feedNormals.Add(localN);
            _feedNormals.Add(localN);
            _feedUvs.Add(new Vector2(u, 1f));
            _feedUvs.Add(new Vector2(u, 0f));
        }

        public void AutoFitFromCargoBounds()
        {
            if (m_cargo == null)
                return;

            var renderers = m_cargo.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
                return;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            m_rayStartDistance = Mathf.Max(b.extents.x, b.extents.z) + 1.2f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_surfaceOffset = Mathf.Max(0.005f, m_surfaceOffset);
            m_minSegmentDistance = Mathf.Max(0.01f, m_minSegmentDistance);
            CacheThresholds();
        }
#endif
    }
}
