using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ImageMagick;
using RayTracer;


namespace RayTracer
{
    /// <summary>
    /// Class to represent a ray traced scene, including the objects,
    /// light sources, and associated rendering logic.
    /// </summary>
    public class Scene
    {
        private SceneOptions options;
        private Camera camera;
        private Color ambientLightColor;
        private ISet<SceneEntity> entities;
        private ISet<PointLight> lights;
        private ISet<Animation> animations;

        private readonly Color BackgroundColor = new Color(0, 0, 0);
        private readonly Color GlobalAmbient = new Color(0.1, 0.1, 0.1);
        private int MaxDepth => Math.Max(4, 2 + this.options.Quality);
        private const double EPS = 1e-8;


        /// <summary>
        /// Construct a new scene with provided options.
        /// </summary>
        /// <param name="options">Options data</param>
        public Scene(SceneOptions options = new SceneOptions())
        {
            this.options = options;
            this.camera = new Camera(Transform.Identity);
            this.ambientLightColor = new Color(0, 0, 0);
            this.entities = new HashSet<SceneEntity>();
            this.lights = new HashSet<PointLight>();
            this.animations = new HashSet<Animation>();
        }

        /// <summary>
        /// Set the camera for the scene.
        /// </summary>
        /// <param name="camera">Camera object</param>
        public void SetCamera(Camera camera)
        {
            this.camera = camera;
        }

        /// <summary>
        /// Set the ambient light color for the scene.
        /// </summary>
        /// <param name="color">Color object</param>
        public void SetAmbientLightColor(Color color)
        {
            this.ambientLightColor = color;
        }

        /// <summary>
        /// Add an entity to the scene that should be rendered.
        /// </summary>
        /// <param name="entity">Entity object</param>
        public void AddEntity(SceneEntity entity)
        {
            this.entities.Add(entity);
        }

        /// <summary>
        /// Add a point light to the scene that should be computed.
        /// </summary>
        /// <param name="light">Light structure</param>
        public void AddPointLight(PointLight light)
        {
            this.lights.Add(light);
        }

        /// <summary>
        /// Add an animation to the scene.
        /// </summary>
        /// <param name="animation">Animation object</param>
        public void AddAnimation(Animation animation)
        {
            this.animations.Add(animation);
        }

        /// <summary>
        /// Render the scene to an output image. This is where the bulk
        /// of your ray tracing logic should go... though you may wish to
        /// break it down into multiple functions as it gets more complex!
        /// </summary>
        /// <param name="outputImage">Image to store render output</param>
        /// <param name="time">Time since start in seconds</param>
        public void Render(Image outputImage, double time = 0)
        {
            ApplyAnimations(time);
            
            // Begin writing your code here...
            int width = outputImage.Width;
            int height = outputImage.Height;
            int AA = Math.Max(1, this.options.AAMultiplier);

            Vector3 origin = this.camera.Transform.Position;
            double imgPlaneZ = 1.0f;

            double aspect = (double)width / (double)height;
            //half width of image
            double halfW = computeHalfWidth(width, height, imgPlaneZ);
            //half height of image
            double halfH = halfW / aspect;


            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    Color accum = new Color(0, 0, 0);

                    for (int sy = 0; sy < AA; sy++)
                    {
                        for (int sx = 0; sx < AA; sx++)
                        {
                            double u = (px + (sx + 0.5) / AA) / (double)width;
                            double v = (py + (sy + 0.5) / AA) / (double)height;

                            double x = (2.0 * u - 1.0) * halfW;
                            double y = (1.0 - 2.0 * v) * halfH;
                            Vector3 cameraSpaceDir = new Vector3(x, y, imgPlaneZ).Normalized();
                            
                            Vector3 worldSpaceDir = this.camera.Transform.Rotation.Rotate(cameraSpaceDir);
                            
                            Ray ray = new(origin, worldSpaceDir);

                            accum += Trace(ray, 0);
                        }
                    }

                    if (AA > 1) accum = accum / (AA * AA);
                    outputImage.SetPixel(px, py, Clamp01(accum));
                }
            }
        }

        private double computeHalfWidth(int width, int height, double imgPlaneZ)
        {   double halfW = 0.0f;
            double fovHdeg = 60.0f;
            double fovH = fovHdeg * Math.PI / 180.0f;
            halfW = Math.Tan(0.5f * fovH) * imgPlaneZ;
            return halfW;
        }


        private static Color Clamp01(Color c)
        {
            return new Color(
                c.R < 0 ? 0 : (c.R > 1 ? 1 : c.R),
                c.G < 0 ? 0 : (c.G > 1 ? 1 : c.G),
                c.B < 0 ? 0 : (c.B > 1 ? 1 : c.B)
            );
        }

        private bool FindClosetHit(Ray ray, out SceneEntity entity, out RayHit hit, out double distance)
        {
            entity = null;
            hit = null;
            distance = double.PositiveInfinity;

            foreach (var e in this.entities)
            {
                var h = e.Intersect(ray);
                if (h == null)
                {
                    continue;
                }

                double d = (h.Position - ray.Origin).Length();
                if (d > EPS && d < distance)
                {
                    distance = d;
                    hit = h;
                    entity = e;
                }

            }

            return hit != null;
        }

        private Color ShadeLocalPhong(Material mat, RayHit hit)
        {
            Vector3 P = hit.Position;
            Vector3 N = hit.Normal.Normalized();
            Vector3 V = (-hit.Incident).Normalized();

            Color shaded = new(0, 0, 0);

            if (this.options.AmbientLightingEnabled)
                shaded += (mat.GetDiffuseColor(hit.TextureCoord) * GlobalAmbient);

            foreach (var light in this.lights)
            {
                if (IsShadowed(P, light.Position)) continue;

                Vector3 L = (light.Position - P).Normalized();
                Vector3 R = ReflectDir(-L, N);

                double ndotl = Math.Max(0.0, N.Dot(L));
                double rdotv = Math.Max(0.0, R.Dot(V));

                Color diffuse  = mat.GetDiffuseColor(hit.TextureCoord)  * light.Color * ndotl;
                Color specular = mat.SpecularColor * light.Color *
                                (mat.Shininess <= 0 ? 0.0 : Math.Pow(rdotv, mat.Shininess));

             shaded = shaded + diffuse + specular;
            }

            return shaded;
        }
        private bool IsShadowed(Vector3 point, Vector3 lightPos)
        {
            Vector3 toLight = lightPos - point;

            double maxDist = toLight.Length();
            Vector3 dir = toLight / maxDist;

            Ray shadowRay = new(point + dir * EPS, dir);

            foreach (var e in this.entities)
            {
                var h = e.Intersect(shadowRay);
                if (h == null) continue;
                double d = (h.Position - shadowRay.Origin).Length();
                if (d > EPS && d < maxDist - EPS) return true;
            }
            return false;
        }

        private static Vector3 ReflectDir(Vector3 D, Vector3 N)
        {
            Vector3 d = D.Normalized();
            Vector3 n = N.Normalized();
            return (d - 2.0 * d.Dot(n) * n).Normalized();
        }


        private static bool TryRefractDir(Vector3 D, Vector3 N, double nAir, double nMat, out Vector3 Tdir)
        {
            Vector3 d = D.Normalized();
            Vector3 n = N.Normalized();

            double n1 = nAir, n2 = nMat;
            double cosi = -Math.Max(-1.0, Math.Min(1.0, d.Dot(n)));

            if (cosi < 0) { cosi = -cosi; n = -n; double tmp = n1; n1 = n2; n2 = tmp; }

            double eta = n1 / n2;
            double k = 1.0 - eta * eta * (1.0 - cosi * cosi);

            if (k < 0.0) { Tdir = default; return false; }

            Tdir = (d * eta + n * (eta * cosi - Math.Sqrt(k))).Normalized();
            return true;
        }
        private Color Trace(Ray ray, int depth)
        {
            if (depth > MaxDepth) return BackgroundColor;


            if (!FindClosetHit(ray, out SceneEntity entity, out RayHit hit, out double dist))
                return BackgroundColor;

            Material mat = entity.Material;

            Color result = ShadeLocalPhong(mat, hit);

            if (mat.Reflectivity > 0.0)
            {
                Vector3 Rdir = ReflectDir(ray.Direction, hit.Normal);
                Color rcol = Trace(new Ray(hit.Position + Rdir * EPS, Rdir), depth + 1);
                result += rcol * mat.Reflectivity;
            }

            if (mat.Transmissivity > 0.0)
            {
                if (TryRefractDir(ray.Direction, hit.Normal, 1.0, mat.RefractiveIndex, out Vector3 Tdir))
                {
                    Color tcol = Trace(new Ray(hit.Position + Tdir * EPS, Tdir), depth + 1);
                    result += tcol * mat.Transmissivity;
                }
                else
                {
                    Vector3 Rdir = ReflectDir(ray.Direction, hit.Normal);
                    Color tcol = Trace(new Ray(hit.Position + Rdir * EPS, Rdir), depth + 1);
                    result += tcol * mat.Transmissivity;
                }
            }

            return result;
        }

        private void ApplyAnimations(double time)
        {
            foreach (var animation in this.animations)
            {
                if (animation is SimpleAnimation simpleAnim)
                {
                    ApplySimpleAnimation(simpleAnim, time);
                }
            }
        }

        private void ApplySimpleAnimation(SimpleAnimation animation, double time)
        {
            int frameNumber = (int)(time * this.options.FramesPerSecond);
            
            Vector3 totalTranslation = animation.TranslationPerFrame * frameNumber;
            Quaternion totalRotation = Quaternion.Identity;
            
            for (int i = 0; i < frameNumber; i++)
            {
                totalRotation = totalRotation * animation.RotationPerFrame;
            }
            
            Transform animationTransform = new Transform(totalTranslation, totalRotation, 1.0);
            
            if (animation.Entity is ObjModel objModel)
            {
                Transform composedTransform = ComposeTransforms(objModel.OriginalTransform, animationTransform);
                objModel.UpdateTransform(composedTransform);
            }
        }


        private Transform ComposeTransforms(Transform first, Transform second)
        {
            Vector3 composedPosition = first.Apply(second.Position);
            Quaternion composedRotation = first.Rotation * second.Rotation;
            double composedScale = first.Scale * second.Scale;
            
            return new Transform(composedPosition, composedRotation, composedScale);
        }

        private void UpdateEntityTransform(SceneEntity entity, Vector3 position, Quaternion rotation, double scale = 1.0)
        {
            if (entity is ObjModel objModel)
            {
                Transform newTransform = new Transform(position, rotation, scale);
                objModel.UpdateTransform(newTransform);
            }
        }

    }
}
