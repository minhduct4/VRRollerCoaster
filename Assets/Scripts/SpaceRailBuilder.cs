using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpaceRailBuilder : MonoBehaviour
{
    [Header("Spline used as the center of the track")]
    public SplineContainer track;

    [Header("Appearance")]
    public Material railMaterial;

    [Header("Track dimensions (Unity units)")]
    [Min(0.05f)] public float railHalfSpacing = 0.7f;
    [Min(0.01f)] public float railThickness = 0.12f;
    [Min(0.05f)] public float step = 0.5f;
    [Min(0.1f)] public float tieSpacing = 2f;

    [ContextMenu("Generate Rails")]
    public void GenerateRails()
    {
        if (track == null)
            track = GetComponentInParent<SplineContainer>();

        if (track == null || track.Spline == null || track.Spline.Count < 2)
        {
            Debug.LogError("SpaceRailBuilder: Assign a Track with a spline containing at least 2 knots.", this);
            return;
        }

        if (step <= 0f || tieSpacing <= 0f || railThickness <= 0f)
        {
            Debug.LogError("SpaceRailBuilder: Step, Tie Spacing and Rail Thickness must be positive.", this);
            return;
        }

        float totalLength = track.CalculateLength();
        if (totalLength <= 0f)
        {
            Debug.LogError("SpaceRailBuilder: The spline has zero length.", this);
            return;
        }

        // Build one combined mesh from narrow box segments and cross ties.
        GameObject temporaryCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        temporaryCube.hideFlags = HideFlags.HideAndDontSave;
        Mesh boxMesh = temporaryCube.GetComponent<MeshFilter>().sharedMesh;
        List<CombineInstance> pieces = new List<CombineInstance>();

        int segments = Mathf.Clamp(Mathf.CeilToInt(totalLength / step), 2, 4000);
        float travelled = 0f;
        float nextTieDistance = 0f;

        for (int i = 0; i < segments; i++)
        {
            float t0 = (float)i / segments;
            float t1 = (float)(i + 1) / segments;

            // SplineContainer.Evaluate returns world-space coordinates.
            track.Evaluate(t0, out float3 p0, out float3 tangent0, out float3 up0);
            track.Evaluate(t1, out float3 p1, out float3 tangent1, out float3 up1);

            Vector3 a = new Vector3(p0.x, p0.y, p0.z);
            Vector3 b = new Vector3(p1.x, p1.y, p1.z);
            Vector3 direction = b - a;
            float segmentLength = direction.magnitude;
            if (segmentLength < 0.0001f)
                continue;

            Vector3 forward = direction / segmentLength;
            Vector3 up = new Vector3(up0.x, up0.y, up0.z);
            up = Vector3.ProjectOnPlane(up, forward).normalized;

            if (up.sqrMagnitude < 0.01f)
                up = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;
            if (up.sqrMagnitude < 0.01f)
                up = Vector3.ProjectOnPlane(Vector3.forward, forward).normalized;

            Vector3 right = Vector3.Cross(up, forward).normalized;
            Quaternion orientation = Quaternion.LookRotation(forward, up);
            Vector3 midpoint = (a + b) * 0.5f;

            // Two parallel rails, one on each side of the original spline.
            AddBox(pieces, boxMesh, midpoint - right * railHalfSpacing,
                orientation, new Vector3(railThickness, railThickness, segmentLength + 0.04f));
            AddBox(pieces, boxMesh, midpoint + right * railHalfSpacing,
                orientation, new Vector3(railThickness, railThickness, segmentLength + 0.04f));

            // Cross ties connect the rails at regular intervals.
            if (travelled + segmentLength >= nextTieDistance)
            {
                AddBox(pieces, boxMesh, midpoint - up * (railThickness * 0.65f),
                    orientation,
                    new Vector3(railHalfSpacing * 2f + railThickness * 2f, 0.10f, 0.18f));
                nextTieDistance += tieSpacing;
            }

            travelled += segmentLength;
        }

        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh previous = filter.sharedMesh;

        Mesh generated = new Mesh();
        generated.name = "Generated Space Rails";
        generated.indexFormat = IndexFormat.UInt32;
        generated.CombineMeshes(pieces.ToArray(), true, true);
        generated.RecalculateBounds();
        filter.sharedMesh = generated;

        if (railMaterial != null)
            GetComponent<MeshRenderer>().sharedMaterial = railMaterial;

        if (previous != null && previous.name == "Generated Space Rails")
        {
            if (Application.isPlaying) Destroy(previous);
            else DestroyImmediate(previous);
        }

        if (Application.isPlaying) Destroy(temporaryCube);
        else DestroyImmediate(temporaryCube);

        Debug.Log("SpaceRailBuilder: Rails generated successfully.", this);
    }

    private void AddBox(List<CombineInstance> pieces, Mesh boxMesh,
        Vector3 worldPosition, Quaternion worldRotation, Vector3 size)
    {
        pieces.Add(new CombineInstance
        {
            mesh = boxMesh,
            transform = transform.worldToLocalMatrix *
                Matrix4x4.TRS(worldPosition, worldRotation, size)
        });
    }
}
