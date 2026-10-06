using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace ModelViewerApp
{
    public static class RuntimeObjLoader
    {
        public static GameObject LoadOBJFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError("OBJ file not found: " + filePath);
                return null;
            }

            string[] lines = File.ReadAllLines(filePath);
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<int> triangles = new List<int>();
            List<Vector3> outVertices = new List<Vector3>();
            List<Vector3> outNormals = new List<Vector3>();
            List<Vector2> outUVs = new List<Vector2>();

            Dictionary<string, int> vertexIndexLookup = new Dictionary<string, int>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                switch (parts[0])
                {
                    case "v":
                        if (parts.Length >= 4)
                        {
                            float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                            vertices.Add(new Vector3(-x, y, z)); // OBJ is right-handed, Unity is left-handed
                        }
                        break;

                    case "vn":
                        if (parts.Length >= 4)
                        {
                            float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                            normals.Add(new Vector3(-x, y, z));
                        }
                        break;

                    case "vt":
                        if (parts.Length >= 3)
                        {
                            float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
                            float v = float.Parse(parts[2], CultureInfo.InvariantCulture);
                            uvs.Add(new Vector2(u, v));
                        }
                        break;

                    case "f":
                        // Face: handle triangles and convex polygons (fan triangulation)
                        if (parts.Length >= 4)
                        {
                            int[] faceIndices = new int[parts.Length - 1];
                            for (int f = 1; f < parts.Length; f++)
                            {
                                string token = parts[f];
                                if (!vertexIndexLookup.TryGetValue(token, out int index))
                                {
                                    string[] comp = token.Split('/');
                                    int vIdx = int.Parse(comp[0]) - 1;
                                    Vector3 pos = vIdx < vertices.Count ? vertices[vIdx] : Vector3.zero;

                                    Vector2 uv = Vector2.zero;
                                    if (comp.Length > 1 && !string.IsNullOrEmpty(comp[1]))
                                    {
                                        int uvIdx = int.Parse(comp[1]) - 1;
                                        if (uvIdx < uvs.Count) uv = uvs[uvIdx];
                                    }

                                    Vector3 norm = Vector3.up;
                                    if (comp.Length > 2 && !string.IsNullOrEmpty(comp[2]))
                                    {
                                        int nIdx = int.Parse(comp[2]) - 1;
                                        if (nIdx < normals.Count) norm = normals[nIdx];
                                    }

                                    index = outVertices.Count;
                                    outVertices.Add(pos);
                                    outUVs.Add(uv);
                                    outNormals.Add(norm);
                                    vertexIndexLookup[token] = index;
                                }
                                faceIndices[f - 1] = index;
                            }

                            // Fan triangulation
                            for (int t = 1; t < faceIndices.Length - 1; t++)
                            {
                                triangles.Add(faceIndices[0]);
                                triangles.Add(faceIndices[t + 1]);
                                triangles.Add(faceIndices[t]);
                            }
                        }
                        break;
                }
            }

            if (outVertices.Count == 0)
            {
                Debug.LogWarning("No vertices found in OBJ file.");
                return null;
            }

            Mesh mesh = new Mesh();
            mesh.name = Path.GetFileNameWithoutExtension(filePath);

            if (outVertices.Count > 65534)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(outVertices);
            mesh.SetTriangles(triangles, 0);

            if (outUVs.Count == outVertices.Count)
            {
                mesh.SetUVs(0, outUVs);
            }

            if (outNormals.Count == outVertices.Count && normals.Count > 0)
            {
                mesh.SetNormals(outNormals);
            }
            else
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();

            GameObject obj = new GameObject(Path.GetFileNameWithoutExtension(filePath));
            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            Shader standardShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(standardShader);
            mat.name = "OBJ_Material";
            mat.color = new Color(0.85f, 0.88f, 0.92f);
            mr.sharedMaterial = mat;

            MeshCollider mc = obj.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            return obj;
        }
    }
}
