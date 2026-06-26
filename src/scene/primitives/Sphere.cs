using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Sphere : SceneEntity
    {
        private Vector3 center;
        private double radius;
        private Material material;

        /// <summary>
        /// Construct a sphere given its center point and a radius.
        /// </summary>
        /// <param name="center">Center of the sphere</param>
        /// <param name="radius">Radius of the spher</param>
        /// <param name="material">Material assigned to the sphere</param>
        public Sphere(Vector3 center, double radius, Material material)
        {
            this.center = center;
            this.radius = radius;
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the sphere, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {
            // Write your code here...
            const double EPS = 1e-8;

            Vector3 oc = ray.Origin - this.center;
            double b = 2.0 * oc.Dot(ray.Direction);
            double c = oc.Dot(oc) - this.radius * this.radius;

            double disc = b * b - 4.0 * c;
            if (disc < EPS)
            {
                return null;
            }

            double s = Math.Sqrt(disc);
            double t0 = (-b - s) * 0.5;
            double t1 = (-b + s) * 0.5;

            double t;
            if (t0 > EPS)
            {
                t = t0;
            }

            else if (t1 > EPS)
            {
                t = t1;
            }

            else
            {
                return null;
            }

            Vector3 pos = ray.Origin + ray.Direction * t;
            Vector3 n = ((pos - this.center) / radius).Normalized();
            
            Vector3 localPos = (pos - this.center) / this.radius;
            double u = 0.5 + Math.Atan2(localPos.Z, localPos.X) / (2.0 * Math.PI);
            double v = 0.5 - Math.Asin(localPos.Y) / Math.PI;
            
            TextureCoord texCoord = new TextureCoord(u, v);
            return new RayHit(pos, n, ray.Direction, this.material, texCoord);
        }

        /// <summary>
        /// The material of the sphere.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
