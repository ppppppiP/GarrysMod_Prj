using System.Collections.Generic;
using UnityEngine;

public static class JungleProceduralMeshFactory
{
    public static Mesh CreatePuddlePair(float sideOffset, float radiusX, float radiusZ, int seed)
    {
        const int points = 12;
        var vertices = new List<Vector3>((points + 1) * 2);
        var uvs = new List<Vector2>((points + 1) * 2);
        var triangles = new List<int>(points * 6);
        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            float centerX = sideIndex == 0 ? -sideOffset : sideOffset;
            int center = vertices.Count;
            vertices.Add(new Vector3(centerX, 0f, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < points; i++)
            {
                float angle = i * Mathf.PI * 2f / points;
                float noise = 0.78f + Hash01(seed + sideIndex * 37 + i * 11) * 0.32f;
                vertices.Add(new Vector3(centerX + Mathf.Cos(angle) * radiusX * noise, 0f, Mathf.Sin(angle) * radiusZ * noise));
                uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
            }
            for (int i = 0; i < points; i++)
            {
                triangles.Add(center);
                triangles.Add(center + 1 + (i + 1) % points);
                triangles.Add(center + 1 + i);
            }
        }
        return Build("Irregular Puddle Pair", vertices, uvs, triangles);
    }

    public static Mesh CreateArrow()
    {
        Vector3[] outline =
        {
            new Vector3(1.55f, 0.32f, 0f), new Vector3(-0.35f, 0.32f, 0f),
            new Vector3(-0.35f, 0.78f, 0f), new Vector3(-1.65f, 0f, 0f),
            new Vector3(-0.35f, -0.78f, 0f), new Vector3(-0.35f, -0.32f, 0f),
            new Vector3(1.55f, -0.32f, 0f)
        };
        var vertices = new List<Vector3> { Vector3.zero };
        vertices.AddRange(outline);
        var triangles = new List<int>();
        for (int i = 0; i < outline.Length; i++)
        {
            int a = 1 + i;
            int b = 1 + (i + 1) % outline.Length;
            triangles.Add(0); triangles.Add(b); triangles.Add(a);
            triangles.Add(0); triangles.Add(a); triangles.Add(b);
        }
        return Build("Turn Arrow", vertices, null, triangles);
    }

    public static Mesh CreateBird()
    {
        var vertices = new List<Vector3>
        {
            new Vector3(-0.15f, 0f, -0.45f), new Vector3(0.15f, 0f, -0.45f), new Vector3(0f, 0.08f, 0.55f),
            new Vector3(-0.05f, 0f, 0.15f), new Vector3(-2.1f, 0.15f, -0.2f), new Vector3(-0.7f, -0.12f, 0.45f),
            new Vector3(0.05f, 0f, 0.15f), new Vector3(2.1f, 0.15f, -0.2f), new Vector3(0.7f, -0.12f, 0.45f)
        };
        var triangles = new List<int> { 0, 2, 1, 3, 4, 5, 6, 8, 7, 1, 2, 0, 3, 5, 4, 6, 7, 8 };
        return Build("Low Poly Bird", vertices, null, triangles);
    }

    public static Mesh CreateWaterfall(float width, float height)
    {
        const int rows = 7;
        var vertices = new List<Vector3>(rows * 2);
        var uvs = new List<Vector2>(rows * 2);
        var triangles = new List<int>((rows - 1) * 12);
        for (int row = 0; row < rows; row++)
        {
            float t = row / (rows - 1f);
            float edgeWave = Mathf.Sin(t * 13.7f) * width * 0.08f;
            float zWave = Mathf.Sin(t * 9.2f) * 0.22f;
            vertices.Add(new Vector3(-width * 0.5f - edgeWave, height * (0.5f - t), zWave));
            vertices.Add(new Vector3(width * 0.5f + edgeWave * 0.6f, height * (0.5f - t), -zWave));
            uvs.Add(new Vector2(0f, 1f - t));
            uvs.Add(new Vector2(1f, 1f - t));
            if (row == 0) continue;
            int a = (row - 1) * 2;
            int b = a + 1;
            int c = row * 2;
            int d = c + 1;
            triangles.Add(a); triangles.Add(d); triangles.Add(b);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
            triangles.Add(a); triangles.Add(b); triangles.Add(d);
            triangles.Add(a); triangles.Add(d); triangles.Add(c);
        }
        return Build("Irregular Waterfall", vertices, uvs, triangles);
    }

    public static Mesh CreateRock(float radius, int seed)
    {
        Vector3[] vertices =
        {
            new Vector3(0f, radius * 1.15f, 0f), new Vector3(0f, -radius * 0.75f, 0f),
            new Vector3(radius, 0f, 0f), new Vector3(-radius * 0.9f, 0f, 0f),
            new Vector3(0f, 0f, radius * 0.85f), new Vector3(0f, 0f, -radius),
            new Vector3(radius * 0.55f, radius * 0.2f, radius * 0.5f), new Vector3(-radius * 0.5f, -radius * 0.1f, -radius * 0.45f)
        };
        for (int i = 0; i < vertices.Length; i++) vertices[i] *= 0.86f + Hash01(seed + i * 17) * 0.28f;
        int[] triangles = { 0, 6, 2, 0, 4, 6, 0, 3, 4, 0, 5, 3, 1, 2, 6, 1, 6, 4, 1, 4, 3, 1, 3, 7, 1, 7, 5, 0, 2, 5, 1, 5, 2, 0, 7, 3, 0, 5, 7 };
        return Build("Low Poly Rock", new List<Vector3>(vertices), null, new List<int>(triangles));
    }

    public static Mesh CreateColumn(float radius, float height, int sides)
    {
        sides = Mathf.Clamp(sides, 6, 12);
        var vertices = new List<Vector3>(sides * 4);
        var triangles = new List<int>(sides * 18);
        float[] y = { -height * 0.5f, -height * 0.42f, height * 0.42f, height * 0.5f };
        float[] r = { radius * 1.25f, radius, radius * 0.92f, radius * 1.28f };
        for (int ring = 0; ring < 4; ring++)
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle) * r[ring], y[ring], Mathf.Sin(angle) * r[ring]));
            }
        for (int ring = 0; ring < 3; ring++)
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int a = ring * sides + i, b = ring * sides + next, c = (ring + 1) * sides + i, d = (ring + 1) * sides + next;
                triangles.Add(a); triangles.Add(d); triangles.Add(b);
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
            }
        return Build("Tapered Temple Column", vertices, null, triangles);
    }

    public static Mesh CreateRockBarrier()
    {
        var combines = new CombineInstance[7];
        Mesh[] rocks = new Mesh[combines.Length];
        for (int i = 0; i < combines.Length; i++)
        {
            rocks[i] = CreateRock(0.8f + (i % 3) * 0.12f, 1500 + i * 31);
            float x = -3.1f + i * 1.03f;
            float y = 0.45f + (i % 2) * 0.18f;
            float scaleY = 0.8f + (i % 3) * 0.16f;
            combines[i] = new CombineInstance
            {
                mesh = rocks[i],
                transform = Matrix4x4.TRS(new Vector3(x, y, 0f), Quaternion.Euler(0f, i * 37f, i * 9f), new Vector3(1f, scaleY, 0.85f))
            };
        }
        Mesh result = new Mesh { name = "Low Poly Rock Barrier" };
        result.CombineMeshes(combines, true, true, false);
        result.RecalculateBounds();
        for (int i = 0; i < rocks.Length; i++)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(rocks[i]);
            else UnityEngine.Object.DestroyImmediate(rocks[i]);
        }
        return result;
    }

    private static Mesh Build(string name, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
    {
        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        if (uvs != null && uvs.Count == vertices.Count) mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float Hash01(int value)
    {
        float v = Mathf.Sin(value * 12.9898f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }
}
