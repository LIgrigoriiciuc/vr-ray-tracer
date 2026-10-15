using System;


namespace rt
{
    public class Ellipsoid : Geometry
    {
        private Vector Center { get; }
        private Vector SemiAxesLength { get; }
        private double Radius { get; }
        
        public Quaternion Rotation { get; set;  } = Quaternion.NONE;
        
        public Ellipsoid(Vector center, Vector semiAxesLength, double radius, Material material, Color color) : base(material, color)
        {
            Center = center;
            SemiAxesLength = semiAxesLength;
            Radius = radius;
        }

        public Ellipsoid(Vector center, Vector semiAxesLength, double radius, Color color) : base(color)
        {
            Center = center;
            SemiAxesLength = semiAxesLength;
            Radius = radius;
        }

        public Ellipsoid (Ellipsoid e) : this(new Vector(e.Center), new Vector(e.SemiAxesLength), e.Radius, new Material(e.Material), new Color(e.Color))
        {
        }

        public override Intersection GetIntersection(Line line, double minDist, double maxDist)
        {
            Vector O = line.X0;               // or line.Start
            Vector D = line.Dx; // or (line.End - line.Start).Normalized()

            double denom = Vector.Dot(, D);

            // Ray is parallel to the plane
            if (Math.Abs(denom) < 1e-9)
                return Intersection.NONE;

            double t = Vector.Dot(Point - O, Normal) / denom;

            if (t < minDist || t > maxDist)
                return Intersection.NONE;

            Vector hit = O + D * t;

            // If the ray hits the back side, flip the normal for shading
            Vector n = denom < 0 ? Normal : -Normal;

            return new Intersection
            {
                T = t,
                Position = hit,
                Normal = n,
                // Material = this.Material
            };
        }
    }
}
