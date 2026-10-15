using System;
using System.Reflection.Metadata.Ecma335;

namespace rt
{
    class RayTracer(Geometry[] geometries, Light[] lights)
    {
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
            // TODO: ADD CODE HERE
            return true;
        }

        public void Render(Camera camera, int width, int height, string filename)
        {
            var background = new Color(0.2, 0.2, 0.2, 1.0);

            var image = new Image(width, height);

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var viewCenter = camera.Position + camera.Direction * camera.ViewPlaneDistance;
                    var positionVec = new Vector((i - width/2d), (j - height/2d), 1);
                    positionVec.X *= camera.ViewPlaneWidth/width;
                    positionVec.Y *= camera.ViewPlaneHeight/height;
                    var viewPlane = Vector.Cross(camera.Up, camera.Direction);
                    /*Console.WriteLine($"""
                                       UP {camera.Up}
                                       DIR {camera.Direction}
                                       VP {viewPlane}
                                       """);
                                       */
                    viewPlane = viewPlane*positionVec.X + camera.Up*positionVec.Y;
                    Vector realPos = viewCenter + viewPlane;
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
                        Console.WriteLine($"{!inter.Valid} {inter.T} {inter.Visible}");
                        image.SetPixel(i, j, background);
                    }
                    else
                        image.SetPixel(i, j, inter.Color);
                }
            }

            image.Store(filename);
        }
    }
}