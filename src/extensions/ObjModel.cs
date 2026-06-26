using System.IO;
using System.Collections.Generic;
using System;

namespace RayTracer
{
    /// <summary>
    /// Add-on option C. You should implement your solution in this class template.
    /// </summary>
    public class ObjModel : SceneEntity
    {
        private string objFilePath;
        private Transform transform;
        private Transform originalTransform;
        private Material material;
        private List<Triangle> triangles;
        private Vector3 minBounds;
        private Vector3 maxBounds;

        /// <summary>
        /// Construct a new OBJ model.
        /// </summary>
        /// <param name="objFilePath">File path of .obj</param>
        /// <param name="transform">Transform to apply to each vertex</param>
        /// <param name="material">Material applied to the model</param>
        public ObjModel(string objFilePath, Transform transform, Material material)
        {
            this.objFilePath = objFilePath;
            this.transform = transform;
            this.originalTransform = transform;
            this.material = material;
            this.triangles = new List<Triangle>();

            // Here's some code to get you started reading the file...
            string[] lines = File.ReadAllLines(objFilePath);
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                    continue;

                if (parts[0] == "v")
                {
                    if (double.TryParse(parts[1], out double x) &&
                        double.TryParse(parts[2], out double y) &&
                        double.TryParse(parts[3], out double z))
                    {
                        Vector3 vertex = new Vector3(x, y, z);
                        vertices.Add(vertex);
                    }
                }
                else if (parts[0] == "vn")
                {
                    if (double.TryParse(parts[1], out double x) &&
                        double.TryParse(parts[2], out double y) &&
                        double.TryParse(parts[3], out double z))
                    {
                        Vector3 normal = new Vector3(x, y, z);
                        normals.Add(normal);
                    }
                }
                else if (parts[0] == "f")
                {
                    if (parts.Length >= 4)
                    {
                        List<int> vertexIndices = new List<int>();
                        List<int> normalIndices = new List<int>();

                        for (int j = 1; j < parts.Length; j++)
                        {
                            string[] indices = parts[j].Split('/');
                            if (indices.Length >= 1 && int.TryParse(indices[0], out int vIndex))
                            {
                                vertexIndices.Add(vIndex - 1);
                            }
                            if (indices.Length >= 2 && !string.IsNullOrEmpty(indices[1]) && int.TryParse(indices[1], out int nIndex))
                            {
                                normalIndices.Add(nIndex - 1);
                            }
                            else if (indices.Length >= 3 && int.TryParse(indices[2], out int nIndex2))
                            {
                                normalIndices.Add(nIndex2 - 1);
                            }
                        }

                        if (vertexIndices.Count >= 3)
                        {
                            for (int k = 1; k < vertexIndices.Count - 1; k++)
                            {
                                int i0 = vertexIndices[0];
                                int i1 = vertexIndices[k];
                                int i2 = vertexIndices[k + 1];

                                if (i0 >= 0 && i0 < vertices.Count &&
                                    i1 >= 0 && i1 < vertices.Count &&
                                    i2 >= 0 && i2 < vertices.Count)
                                {
                                    Vector3 v0 = vertices[i0];
                                    Vector3 v1 = vertices[i1];
                                    Vector3 v2 = vertices[i2];

                                    Vector3 normal;
                                    if (normalIndices.Count >= 3 && k < normalIndices.Count - 1)
                                    {
                                        int n0 = normalIndices[0];
                                        int n1 = normalIndices[k];
                                        int n2 = normalIndices[k + 1];

                                        if (n0 >= 0 && n0 < normals.Count &&
                                            n1 >= 0 && n1 < normals.Count &&
                                            n2 >= 0 && n2 < normals.Count)
                                        {
                                            normal = (normals[n0] + normals[n1] + normals[n2]).Normalized();
                                        }
                                        else
                                        {
                                            normal = (v1 - v0).Cross(v2 - v0).Normalized();
                                        }
                                    }
                                    else
                                    {
                                        normal = (v1 - v0).Cross(v2 - v0).Normalized();
                                    }

                                    triangles.Add(new Triangle(v0, v1, v2, material));
                                }
                            }
                        }
                    }
                }
            }
            CalculateBounds();
        }

        /// <summary>
        /// Given a ray, determine whether the ray hits the object
        /// and if so, return relevant hit data (otherwise null).
        /// </summary>
        /// <param name="ray">Ray data</param>
        /// <returns>Ray hit data, or null if no hit</returns>
        public RayHit Intersect(Ray ray)
        {
            Vector3 localOrigin = transform.ApplyInverse(ray.Origin);
            Vector3 localDirection = transform.Rotation.Inverse().Rotate(ray.Direction) / transform.Scale;
            Ray localRay = new Ray(localOrigin, localDirection);

            if (!IntersectsBounds(localRay))
                return null;

            RayHit closestHit = null;
            double closestDistance = double.PositiveInfinity;

            foreach (Triangle triangle in triangles)
            {
                RayHit hit = triangle.Intersect(localRay);
                if (hit != null)
                {
                    Vector3 worldHitPosition = transform.Apply(hit.Position);
                    double distance = (worldHitPosition - ray.Origin).Length();
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestHit = hit;
                    }
                }
            }

            if (closestHit != null)
            {
                Vector3 worldPosition = transform.Apply(closestHit.Position);
                Vector3 worldNormal = transform.Rotation.Rotate(closestHit.Normal).Normalized();
                Vector3 worldIncident = ray.Direction;
                
                return new RayHit(worldPosition, worldNormal, worldIncident, material, closestHit.TextureCoord);
            }

            return closestHit;
        }

        /// <summary>
        /// The material attached to this object.
        /// </summary>
        public Material Material { get { return this.material; } }

        public Transform Transform { get { return this.transform; } }

        public Transform OriginalTransform { get { return this.originalTransform; } }

        public void UpdateTransform(Transform newTransform)
        {
            this.transform = newTransform;
        }

        private void CalculateBounds()
        {
            if (triangles.Count == 0)
            {
                minBounds = maxBounds = Vector3.Zero;
                return;
            }

            Vector3 firstVertex = triangles[0].GetVertex(0);
            minBounds = maxBounds = firstVertex;

            foreach (Triangle triangle in triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 vertex = triangle.GetVertex(i);
                    minBounds = new Vector3(
                        Math.Min(minBounds.X, vertex.X),
                        Math.Min(minBounds.Y, vertex.Y),
                        Math.Min(minBounds.Z, vertex.Z)
                    );
                    maxBounds = new Vector3(
                        Math.Max(maxBounds.X, vertex.X),
                        Math.Max(maxBounds.Y, vertex.Y),
                        Math.Max(maxBounds.Z, vertex.Z)
                    );
                }
            }
        }

        private bool IntersectsBounds(Ray localRay)
        {
            const double EPS = 1e-8;
            
            double tMin = double.NegativeInfinity;
            double tMax = double.PositiveInfinity;

            for (int i = 0; i < 3; i++)
            {
                double rayOrigin = i == 0 ? localRay.Origin.X : (i == 1 ? localRay.Origin.Y : localRay.Origin.Z);
                double rayDir = i == 0 ? localRay.Direction.X : (i == 1 ? localRay.Direction.Y : localRay.Direction.Z);
                double minBound = i == 0 ? minBounds.X : (i == 1 ? minBounds.Y : minBounds.Z);
                double maxBound = i == 0 ? maxBounds.X : (i == 1 ? maxBounds.Y : maxBounds.Z);

                if (Math.Abs(rayDir) < EPS)
                {
                    if (rayOrigin < minBound || rayOrigin > maxBound)
                        return false;
                }
                else
                {
                    double t1 = (minBound - rayOrigin) / rayDir;
                    double t2 = (maxBound - rayOrigin) / rayDir;
                    
                    if (t1 > t2)
                    {
                        double temp = t1;
                        t1 = t2;
                        t2 = temp;
                    }
                    
                    tMin = Math.Max(tMin, t1);
                    tMax = Math.Min(tMax, t2);
                    
                    if (tMin > tMax)
                        return false;
                }
            }
            
            return tMax > EPS;
        }
    }
}
