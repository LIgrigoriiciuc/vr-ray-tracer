# VR Ray Tracer

A ray tracer written in C# (.NET). For every pixel of the image we cast a ray from the camera into the scene, find the first object it hits, and compute the color of that point using Phong lighting and shadows. The program renders 90 frames (800x600 PNG) while the camera orbits the scene, and saves them to `frames/`.

## How it works

1. For each pixel, cast a ray from the camera through the view plane.
2. Find the first intersection with any object in the scene (`FindFirstIntersection`).
3. If something was hit, compute its color with Phong lighting (ambient + diffuse + specular) for each light. A light that is blocked by another object does not contribute (shadow).
4. If nothing was hit, use the background color.
   Scene objects:
- **Ellipsoids / Sphere**: defined by math formulas, rotated using quaternions.
- **Walnut**: real CT scan data (`ctscan/walnut.dat` + `ctscan/walnut.raw`), rendered as a volume of voxels.
## Getting started

```
git clone https://github.com/LIgrigoriiciuc/vr-ray-tracer.git
cd vr-ray-tracer
dotnet run
```

## Work split

### Person 1: base math + ellipsoid
**Files:** `Quaternion.cs`, `Vector.cs`, `Ellipsoid.cs`

- `Quaternion.FromAxisAngle`: builds the rotation quaternion from an angle and an axis.
- In `Quaternion.cs`, also add quaternion multiplication and the conjugate (inverse).
- `Vector.Rotate(Quaternion q)`: rotates the vector using `q * v * q⁻¹`.
- `Ellipsoid.GetIntersection`: moves the ray into the ellipsoid's local space (subtract the center, apply the inverse rotation, divide by the semi-axes), solves the quadratic equation, picks the smallest `t` between `minDist` and `maxDist`, computes the normal and rotates it back. Returns an `Intersection`.
  **Result:** the colored shapes can be hit by rays and they rotate.

### Person 2: rendering + lighting + shadows
**File:** `RayTracer.cs`

- `Render`: loops over pixels, computes the point on the view plane (using `ImageToViewPlane`, `camera.Direction`, `camera.Up` and the "right" vector `Direction ^ Up`), builds a `Line` from the camera position, calls `FindFirstIntersection` and colors the pixel (or sets the background).
- `IsLit`: casts a ray from the point toward the light; if it hits something on the way, the point is in shadow.
- A new function for Phong lighting (ambient + diffuse + specular, for each light), called from `Render`.
  **Result:** the scene looks 3D, with lighting and shadows. Until Person 1's ellipsoid is ready, a temporary test intersection (a simple sphere) can be used.

### Person 3: walnut (CT scan)
**File:** `CtScan.cs`

- `GetIntersection`:
    1. intersect the ray with the box between `_v0` and `_v1`;
    2. march along the ray in small steps, calling `GetColor` at each step;
    3. accumulate color by alpha;
    4. return an `Intersection` with the normal from `GetNormal`.
- `Value`, `GetIndexes`, `GetColor` and `GetNormal` already exist, just use them.
  **Result:** the walnut appears in the scene.

## Difficulty (easiest to hardest)

1. **Person 2**: two pixel loops, one ray per pixel, Phong is just additions and multiplications.
2. **Person 1**: more math, and many pieces that must fit together (inverse rotation, normals).
3. **Person 3**: no closed formula, you design an algorithm (ray-box intersection, ray marching, alpha accumulation), and testing is slow.

## Recommended order

1. **In parallel:** Person 2 builds the pixel loop and the ray through each pixel (with a test sphere), Person 1 does `FromAxisAngle`, `Rotate` and `Ellipsoid.GetIntersection`, Person 3 does the ray-box intersection and ray marching.
2. **Then:** Person 2 pushes a working `Render` to git, the others `git pull` and can see their own objects through it.
3. **Then:** Person 2 adds Phong and shadows, Persons 1 and 3 finish their objects.
4. **Finally, everyone:** run the full scene with `n = 90` and fix what doesn't fit.
   **Most important:** Person 2 should push a basic `Render` quickly (even just colored silhouettes, without lighting) so the others can test with it. This is the only real dependency.

## Rules for the team

- **Don't edit other people's files.** This avoids merge conflicts. `Program.cs`, `Line.cs`, `Camera.cs`, `Color.cs`, `Material.cs`, etc. stay untouched.
- **The contract between us is the `Intersection` class:** every geometry must return `Valid`, `Visible`, `T`, `Normal`, `Material` and `Color`.
- **Walnut note:** its constructor uses `Color.NONE`, so the default material is black. Person 3 must build the material from the voxel color (`Material.FromColor(color)`), otherwise the walnut renders black when lit.
- **Git workflow:** `git pull` before you start, then `git add .`, `git commit -m "..."`, `git pull`, `git push`.
## Documentation: explain your own work

Everyone must write a short theoretical explanation of their part, so the other two can fully understand it conceptually (not only the code). Each person adds one file in the `docs/` folder:

- `docs/person1-math-and-ellipsoid.md`
- `docs/person2-rendering-and-lighting.md`
- `docs/person3-ct-scan.md`
  Each explanation should cover:

1. **The idea in plain words:** what problem your part solves and why it is needed in a ray tracer.
2. **The math/theory:** the formulas used and where they come from (e.g. quaternion rotation, the quadratic equation for ray-ellipsoid intersection, the Phong model, ray marching with alpha accumulation).
3. **How it maps to the code:** which functions implement which step, and what they take as input and return.
4. **A small example or diagram:** e.g. one ray and one object with the numbers worked out, or a sketch.
5. **Problems we ran into:** bugs, wrong signs, performance issues, and how they were fixed.
   Everyone should read the other two explanations before the final integration, so that any of us can explain the whole project to the professor.
