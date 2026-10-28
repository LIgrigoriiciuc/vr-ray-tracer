using System;
using System.IO;
using System.Text.RegularExpressions;

namespace rt;

public class CtScan: Geometry
{
    private readonly Vector _position;
    private readonly double _scale;
    private readonly ColorMap _colorMap;
    private readonly byte[] _data;

    private readonly int[] _resolution = new int[3];
    private readonly double[] _thickness = new double[3];
    private readonly Vector _v0;
    private readonly Vector _v1;

    public CtScan(string datFile, string rawFile, Vector position, double scale, ColorMap colorMap) : base(Color.NONE)
    {
        _position = position;
        _scale = scale;
        _colorMap = colorMap;

        var lines = File.ReadLines(datFile);
        foreach (var line in lines)
        {
            var kv = Regex.Replace(line, "[:\\t ]+", ":").Split(":");
            if (kv[0] == "Resolution")
            {
                _resolution[0] = Convert.ToInt32(kv[1]);
                _resolution[1] = Convert.ToInt32(kv[2]);
                _resolution[2] = Convert.ToInt32(kv[3]);
            } else if (kv[0] == "SliceThickness")
            {
                _thickness[0] = Convert.ToDouble(kv[1]);
                _thickness[1] = Convert.ToDouble(kv[2]);
                _thickness[2] = Convert.ToDouble(kv[3]);
            }
        }

        _v0 = position;
        _v1 = position + new Vector(_resolution[0]*_thickness[0]*scale, _resolution[1]*_thickness[1]*scale, _resolution[2]*_thickness[2]*scale);

        var len = _resolution[0] * _resolution[1] * _resolution[2];
        _data = new byte[len];
        using FileStream f = new FileStream(rawFile, FileMode.Open, FileAccess.Read);
        if (f.Read(_data, 0, len) != len)
        {
            throw new InvalidDataException($"Failed to read the {len}-byte raw data");
        }
    }
    
    private ushort Value(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= _resolution[0] || y >= _resolution[1] || z >= _resolution[2])
        {
            return 0;
        }

        return _data[z * _resolution[1] * _resolution[0] + y * _resolution[0] + x];
    }

    public override Intersection GetIntersection(Line line, double minDist, double maxDist)
    {
        //functia primeste o raza (line) si trebuie sa afle intre ce valori ale lui t raza e in interiorul cutiei
        //punct=X0+Dx*t (pentru fiecare axa)
        //cutia e definita de doua colturi: _v0 coltul de minim si _v1 coltul de maxim, pe x merge de la _v0.X la _v1.X,la fel pe y si z
        //pe fiecare axa trebuie aflat la ce t intra si la ce t iese
        //punem cele 3 valori intr-un array si facem un for de la 0=x la 2=z ca sa nu scriem aceeasi logica de 3 ori
        double[] o = { line.X0.X, line.X0.Y, line.X0.Z };
        double[] d = { line.Dx.X, line.Dx.Y, line.Dx.Z };
        double[] lo = { _v0.X, _v0.Y, _v0.Z };
        double[] hi = { _v1.X, _v1.Y, _v1.Z };
        double tIn = double.NegativeInfinity;
        double tOut = double.PositiveInfinity;
        for (int i = 0; i < 3; i++)
        {
            if (Math.Abs(d[i]) < 1e-12) //NU comparam double cu 0
            {
                if (o[i]<lo[i] || o[i]>hi[i]) return Intersection.NONE;
                continue;
            }
            double t0 = (lo[i] - o[i]) / d[i]; //cand ajunge la peretele de minim
            double t1 = (hi[i] - o[i]) / d[i]; //de maxim
            if (t0 > t1) //d negativ => se inverseaza
            {
                double tmp = t0;
                t0 = t1;
                t1 = tmp;
            }

            if (t0 > tIn) tIn = t0; //intrarea creste
            if (t1 < tOut) tOut = t1; //iesirea scade
        }
        double tStart = Math.Max(tIn, minDist);
        double tEnd = Math.Min(tOut, maxDist);
        if (tStart > tEnd) return Intersection.NONE;
        double dt = Math.Min(_thickness[0], Math.Min(_thickness[1], _thickness[2]))*_scale;
        Color accColor = Color.NONE; //culoarea adunata by now
        double accAlpha = 0; //opacitatea adunata by now
        double tHit = 0; //primul t unde am dat de material
        bool found = false;
        Vector pHit = null; //punctul primei lovituri
        for (double t = tStart; t <= tEnd; t += dt) //t e ca un slider
        {
            Vector p = line.CoordinateToPosition(t);
            Color c = GetColor(p);
            if (c.Alpha <= 0) continue; //la aer nu adaug nimic
            //opacitatea, coaja are 0.1, iar miezul 0.8, aerul - 0
            if (!found)
            {
                found = true;
                tHit = t; // first visible intersection
                pHit = p;
            }
            double w = c.Alpha * (1 - accAlpha); //cat din ce a ramas neacoperit ia voxelul asta
            accColor = accColor + c * w;
            accAlpha += w;
            if (accAlpha > 0.99) break; //destul de opac
        }

        // Prag de opacitate: razele care trec doar prin coaja subtire nu mai conteaza ca lovituri.
        // 0.0 = dezactivat (comportamentul de pana acum). Pentru test incearca 0.5.
        const double MinOpacity = 0.0;
        if (!found || accAlpha < MinOpacity) return Intersection.NONE; // am trecut prin tot fara sa dau de material

        accColor = accColor / accAlpha; //culoarea medie, nu cea diluata de alpha mic (repara marginile inchise)

        Vector normal = GetNormal(pHit) * -1; //gradientul arata spre interior, noi vrem spre exterior
        if (normal * line.Dx > 0.0) normal = normal * -1; //siguranta: normala sa arate mereu spre camera

        return new Intersection(true, true, this, line, tHit, normal, Material.FromColor(accColor), accColor);
    }
    
    private int[] GetIndexes(Vector v)
    {
        return new []{
            (int)Math.Floor((v.X - _position.X) / _thickness[0] / _scale), 
            (int)Math.Floor((v.Y - _position.Y) / _thickness[1] / _scale),
            (int)Math.Floor((v.Z - _position.Z) / _thickness[2] / _scale)};
    }
    private Color GetColor(Vector v)
    {
        int[] idx = GetIndexes(v);

        ushort value = Value(idx[0], idx[1], idx[2]);
        return _colorMap.GetColor(value);
    }

    private Vector GetNormal(Vector v)
    {
        int[] idx = GetIndexes(v);
        double x0 = Value(idx[0] - 1, idx[1], idx[2]);
        double x1 = Value(idx[0] + 1, idx[1], idx[2]);
        double y0 = Value(idx[0], idx[1] - 1, idx[2]);
        double y1 = Value(idx[0], idx[1] + 1, idx[2]);
        double z0 = Value(idx[0], idx[1], idx[2] - 1);
        double z1 = Value(idx[0], idx[1], idx[2] + 1);

        return new Vector(x1 - x0, y1 - y0, z1 - z0).Normalize();
    }
}