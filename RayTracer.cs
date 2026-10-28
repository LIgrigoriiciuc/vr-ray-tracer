using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using SkiaSharp;

namespace rt
{
    class RayTracer(Geometry[] geometries, Light[] lights)
    {
        public const int lightLimit = 1;
        private double ImageToViewPlane(int n, int imgSize, double viewPlaneSize)
        {
            return -n * viewPlaneSize / imgSize + viewPlaneSize / 2;
        }

        private Intersection FindFirstIntersection(Line ray, double minDist, double maxDist)
        {
            var intersection = Intersection.NONE;

            foreach (var geometry in geometries)
            {
                var intr = geometry.GetIntersection(ray, minDist, maxDist);

                if (!intr.Valid || !intr.Visible) continue;

                if (!intersection.Valid || !intersection.Visible)
                {
                    intersection = intr;
                }
                else if (intr.T < intersection.T)
                {
                    intersection = intr;
                }
            }

            return intersection;
        }

        private bool IsLit(Vector point, Light light)
        {
            var deltaDist = (light.Position - point);
            var len = deltaDist.Length();
            var i = FindFirstIntersection(new Line(point, deltaDist.Normalize()), 0.1,len);
            return !i.Valid;
        }

        public void Render(Camera camera, int width, int height, string filename)
        {
            var background = new Color(0.2, 0.2, 0.2, 1.0);

            var image = new Image(width, height);
            var paint = image.GetCanvas();
            var viewCenter = camera.Position + camera.Direction * camera.ViewPlaneDistance;
            var viewPlane = Vector.Cross(camera.Up, camera.Direction);
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var positionVec = new Vector(width/2d - i,height/2d - j, 1);
                    positionVec.X *= camera.ViewPlaneWidth/width;
                    positionVec.Y *= camera.ViewPlaneHeight/height;
                    /*Console.WriteLine($"""
                                       UP {camera.Up}
                                       DIR {camera.Direction}
                                       VP {viewPlane}
                                       """);
                                       */
                    Vector realPos = viewCenter + viewPlane*positionVec.X + camera.Up*positionVec.Y;
                    var r = new Line(camera.Position, realPos);
                    /*
                    Console.WriteLine($"coords : {i}, {j}");
                    Console.WriteLine($"line pos: {r.Dx.ToString()} {r.X0.ToString()}");
                    Console.WriteLine($"cam dir: {camera.Direction.ToString()}");
                    Console.WriteLine($"view plane: {viewPlane.ToString()}");
                    Console.WriteLine($"calculated view pos : {realPos.ToString()}");
                    */
                    var inter = FindFirstIntersection(r, camera.FrontPlaneDistance, camera.BackPlaneDistance);
                    if (!inter.Valid || !inter.Visible)
                    {
                        //Console.WriteLine($"{!inter.Valid} {inter.T} {inter.Visible}");
                        paint.DrawPoint(i,j,background.ToSystemColor());
                        //image.SetPixel(i, j, background);
                    }
                    else
                    {
                        
                        List<Light> found = new();
                        foreach (var light in lights)
                        {
                            if (!IsLit(inter.Position, light))
                                continue;
                            found.Add(light);
                            //if (found.Count >= lightLimit)
                            //    break;
                        }
                        Color diffuses = Color.NONE;
                        Color speculars = Color.NONE;
                        var obsDir = inter.Line.Dx * -1;
                        foreach (var l in found)
                        {
                            var lightDir = (l.Position  - inter.Position).Normalize();
                            var diffV = Vector.Dot(inter.Normal, lightDir);
                            if (diffV <= 0d)
                                continue;
                            var reflectRay = (inter.Normal * Vector.Dot(lightDir, inter.Normal))* 2d - lightDir;
                            diffuses += inter.Material.Diffuse * diffV * l.Diffuse;
                            speculars += inter.Material.Specular * Math.Pow( Math.Max(0d, Vector.Dot(obsDir, reflectRay)),inter.Material.Shininess) * l.Specular;
                        }
                        
                        paint.DrawPoint(i,j,(inter.Material.Ambient+diffuses+speculars ).ToSystemColor());
                        //image.SetPixel(i, j, inter.Material.Ambient+diffuses+speculars);
                    }
                }
            }

            paint.Save();

            image.Store(filename);
        }
    }
}