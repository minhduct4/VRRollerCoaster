using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Tạo mesh tay cầm low-poly (primitive) — nhẹ cho Quest / simulator.
    /// </summary>
    public static class VrHandControllerVisualFactory
    {
        /// <summary>
        /// Tạo root visual dưới anchor; trả về transform gốc để ẩn/hiện sau này (T8.2 súng).
        /// </summary>
        public static Transform CreateControllerVisual(Transform parent, HandSide side, Material sharedMaterial)
        {
            var root = new GameObject(side == HandSide.Left ? "ControllerVisual_L" : "ControllerVisual_R");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.05f, 0.07f, 0.05f);
            ApplyMaterial(body, sharedMaterial, side, 0.85f);

            var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(root.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.02f, 0.02f);
            grip.transform.localScale = new Vector3(0.04f, 0.07f, 0.05f);
            ApplyMaterial(grip, sharedMaterial, side, 1f);

            var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trigger.name = "TriggerHint";
            trigger.transform.SetParent(root.transform, false);
            trigger.transform.localPosition = new Vector3(0f, -0.01f, 0.06f);
            trigger.transform.localScale = new Vector3(0.025f, 0.04f, 0.02f);
            ApplyMaterial(trigger, sharedMaterial, side, 0.65f);

            StripCollider(root.transform);
            return root.transform;
        }

        /// <summary>
        /// URP Lit một màu — trái xanh lam, phải xám nhạt.
        /// </summary>
        static void ApplyMaterial(GameObject go, Material sharedMaterial, HandSide side, float brightness)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            if (sharedMaterial != null)
            {
                renderer.sharedMaterial = sharedMaterial;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                var tint = side == HandSide.Left
                    ? new Color(0.35f, 0.55f, 0.95f, 1f)
                    : new Color(0.82f, 0.84f, 0.88f, 1f);
                block.SetColor("_BaseColor", tint * brightness);
                renderer.SetPropertyBlock(block);
            }
        }

        /// <summary>
        /// Primitive mặc định có Collider — tay cầm chỉ để nhìn.
        /// </summary>
        static void StripCollider(Transform root)
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
