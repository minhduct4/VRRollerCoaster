using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Súng primitive fallback khi chưa có prefab trong Inspector (dev / lớp chưa kịp làm prefab).
    /// </summary>
    public static class VrWeaponRuntimeMesh
    {
        /// <summary>
        /// Ba silhouette khác nhau — T8.6 preview; thay bằng prefab khi sinh viên làm xong.
        /// </summary>
        public static GameObject CreateFallbackWeapon(string objectName, WeaponFallbackSilhouette silhouette)
        {
            var root = new GameObject(objectName);
            switch (silhouette)
            {
                case WeaponFallbackSilhouette.CompactPistol:
                    BuildCompactPistol(root.transform);
                    break;
                case WeaponFallbackSilhouette.LongRifle:
                    BuildRifle(root.transform);
                    break;
                default:
                    BuildBlaster(root.transform);
                    break;
            }

            return root;
        }

        /// <summary>
        /// Blaster ngắn — loại 0.
        /// </summary>
        static void BuildBlaster(Transform root)
        {
            AddCube(root, "Body", new Vector3(0f, 0f, 0.06f), new Vector3(0.04f, 0.06f, 0.14f), new Color(0.25f, 0.25f, 0.28f));
            AddCube(root, "Barrel", new Vector3(0f, 0.02f, 0.16f), new Vector3(0.025f, 0.025f, 0.12f), new Color(0.15f, 0.15f, 0.18f));
        }

        /// <summary>
        /// Pistol — loại 1.
        /// </summary>
        static void BuildCompactPistol(Transform root)
        {
            AddCube(root, "Grip", new Vector3(0f, -0.04f, 0.02f), new Vector3(0.035f, 0.08f, 0.05f), new Color(0.35f, 0.22f, 0.12f));
            AddCube(root, "Slide", new Vector3(0f, 0.01f, 0.08f), new Vector3(0.03f, 0.04f, 0.1f), new Color(0.5f, 0.52f, 0.55f));
        }

        /// <summary>
        /// Súng dài — loại 2.
        /// </summary>
        static void BuildRifle(Transform root)
        {
            AddCube(root, "Stock", new Vector3(0f, 0f, -0.08f), new Vector3(0.035f, 0.05f, 0.14f), new Color(0.32f, 0.2f, 0.1f));
            AddCube(root, "Receiver", new Vector3(0f, 0.01f, 0.06f), new Vector3(0.04f, 0.06f, 0.18f), new Color(0.22f, 0.24f, 0.26f));
            AddCube(root, "Barrel", new Vector3(0f, 0.02f, 0.22f), new Vector3(0.02f, 0.02f, 0.22f), new Color(0.12f, 0.12f, 0.14f));
        }

        static void AddCube(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = scale;
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null)
                    renderer.sharedMaterial = new Material(shader) { color = color };
            }

            Object.Destroy(cube.GetComponent<Collider>());
        }

        /// <summary>
        /// Súng chỉ để nhìn — bỏ collider.
        /// </summary>
        public static void StripColliders(Transform root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>())
            {
                if (Application.isPlaying)
                    Object.Destroy(col);
                else
                    Object.DestroyImmediate(col);
            }
        }
    }
}
