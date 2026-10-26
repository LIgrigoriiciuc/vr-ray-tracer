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
            var deltaDist = (point - light.Position);
            var i = FindFirstIntersection(new Line(point, deltaDist.Normalize()), 0.2,deltaDist.Length());
            return !i.Valid;
        }

        public void Render(Camera camera, int width, int height, string filename)
        {
            var background = new Color(0.2, 0.2, 0.2, 1.0);

            var image = new Image(width, height);
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
                        image.SetPixel(i, j, background);
                    }
                    else
                    {
                        /*
                        List<Light> found = new();
                        foreach (var light in lights)
                        {
                            if (!IsLit(inter.Position, light))
                                continue;
                            found.Add(light);
                            //if (found.Count >= lightLimit)
                            //    break;
                        }
                        
                        
                        foreach (var l in found)
                        {
                            var lightDir = (inter.Position - l.Position).Normalize();
                            var obsDir = inter.Line.Dx;
                            var diffV = Vector.Dot(inter.Normal,lightDir);
                            var intersectAngle = Math.Acos(Vector.Dot(inter.Normal, inter.Line.Dx) *  inter.Normal.Length() * inter.Line.Dx.Length());
                            var ambient = inter.Material.Ambient;
                            var diffuse = inter.Material.Diffuse * diffV;
                            var specular = inter.Material.Specular * (inter.Normal )
                        }
                        */
                        
                        image.SetPixel(i, j, inter.Color);
                    }
                }
            }

            image.Store(filename);
        }
    }
}