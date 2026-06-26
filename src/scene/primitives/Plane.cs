using System;
using System.Numerics;

namespace RayTracer
{
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Plane : SceneEntity
    {
        private Vector3 center;
        private Vector3 normal;
        private Material material;

        /// <summary>
        /// Construct an infinite plane object.
        /// </summary>
        /// <param name="center">Position of the center of the plane</param>
        /// <param name="normal">Direction that the plane faces</param>
        /// <param name="material">Material assigned to the plane</param>
        public Plane(Vector3 center, Vector3 normal, Material material)
        {
            this.center = center;
            this.normal = normal.Normalized();
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the plane, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            // Write your code here...

            double denom = this.normal.Dot(ray.Direction);
            if (Math.Abs(denom) < 0)
            {
                return null;
            }

            double t = (this.center - ray.Origin).Dot(this.normal) / denom;
            if (t <= 0)
            {
                return null;
            }

            Vector3 pos = ray.Origin + ray.Direction * t;
            Vector3 n = this.normal;
            Vector3 right = new Vector3(1, 0, 0);
            if (Math.Abs(n.Dot(right)) > 0.9)
                right = new Vector3(0, 1, 0);
            Vector3 up = n.Cross(right).Normalized();
            right = up.Cross(n).Normalized();
            
            Vector3 localPos = pos - this.center;
            double u = localPos.Dot(right);
            double v = localPos.Dot(up);
            
            TextureCoord texCoord = new TextureCoord(u, v);
            return new RayHit(pos, n, ray.Direction, this.material, texCoord);
        }

        /// <summary>
        /// The material of the plane.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
