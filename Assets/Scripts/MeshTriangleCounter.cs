using UnityEngine;

[ExecuteAlways]
public class MeshTriangleCounter : MonoBehaviour
{
    [ContextMenu("Count Triangles")]
    void CountTriangles()
    {
        long totalTris = 0;

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null)
                continue;

            int tris = mf.sharedMesh.triangles.Length / 3;
            totalTris += tris;

            Debug.Log($"{mf.gameObject.name}: {tris:N0} tris");
        }

        Debug.Log($"TOTAL ({gameObject.name}): {totalTris:N0} tris");
    }
}