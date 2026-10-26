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
            // Actual semi-axes = SemiAxesLength * Radius
            var semiAxes = SemiAxesLength * Radius;

            // Move the ray into the ellipsoid's local frame: translate to the center and
            // undo the rotation (inverse of a unit quaternion = its conjugate, keep W and minus X,Y,Z).
            var inverseRotation = new Quaternion(Rotation.W, -Rotation.X, -Rotation.Y, -Rotation.Z);
            var rayOriginLocal = line.X0 - Center;
            var rayDirectionLocal = new Vector(line.Dx);
            rayOriginLocal.Rotate(Rotation);
            rayDirectionLocal.Rotate(Rotation);

            // In that frame the ellipsoid is  (x/a)^2 + (y/b)^2 + (z/c)^2 = 1.
            // Scaling the ray by 1/axes turns it into the unit sphere (t stays the same).
            var rayOriginUnitSphere = new Vector(rayOriginLocal); //Make new vectirs since divide mutates and we still need origin and direction 
            var rayDirectionUnitSphere = new Vector(rayDirectionLocal);
            rayOriginUnitSphere.Divide(semiAxes);
            rayDirectionUnitSphere.Divide(semiAxes);


            // Intersect the ray with the unit sphere: |origin + direction*t|² = 1
            // => A t² + B t + C = 0   (quadratic equation in t)
            var quadA = rayDirectionUnitSphere * rayDirectionUnitSphere;
            var quadB = 2.0 * (rayOriginUnitSphere * rayDirectionUnitSphere);
            var quadC = rayOriginUnitSphere * rayOriginUnitSphere - 1.0;

            // Discriminant: < 0 means the ray misses the ellipsoid
            var discriminant = quadB * quadB - 4.0 * quadA * quadC;
            if (quadA == 0.0 || discriminant < 0.0) return Intersection.NONE;

            var sqrtDiscriminant = Math.Sqrt(discriminant);
            var tEnter = (-quadB - sqrtDiscriminant) / (2.0 * quadA);
            var tExit = (-quadB + sqrtDiscriminant) / (2.0 * quadA);

            // Pick the closest intersection that lies inside [minDist, maxDist]
            double hitDistance;
            if (tEnter >= minDist && tEnter <= maxDist) hitDistance = tEnter;
            else if (tExit >= minDist && tExit <= maxDist) hitDistance = tExit;
            else return Intersection.NONE;

            // Normal = gradient of the implicit function: (x/a^2, y/b^2, z/c^2) in the local
            // frame, rotated back to world space.
            var hitPointLocal = rayOriginLocal + rayDirectionLocal * hitDistance;
            var surfaceNormal = new Vector(hitPointLocal);
            surfaceNormal.Divide(semiAxes);   // divide TWICE => /a², /b², /c²
            surfaceNormal.Divide(semiAxes);

            // The normal must go back to world space: rotate it by the forward rotation
            // (no translation needed, since a normal is a direction)
            surfaceNormal.Rotate(inverseRotation);
            surfaceNormal.Normalize();

            return new Intersection(true, true, this, line, hitDistance, surfaceNormal, Material, Color);
        }
    }
}
