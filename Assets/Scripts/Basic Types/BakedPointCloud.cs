// Pcx - Point cloud importer & renderer for Unity
// https://github.com/keijiro/Pcx

// This is a derivative work of the original that was authored by Keijiro and published UnLicensed.

using UnityEngine;
using System.Collections.Generic;
using g3;
namespace Virgis
{
    /// A container class for texture-baked point clouds.
    public sealed class BakedPointCloud 
    {

        /// Number of points
        public int pointCount { get { return _pointCount; } }

        /// Position map texture
        public Texture2D positionMap { get { return _positionMap; } }

        /// Color map texture
        public Texture2D colorMap { get { return _colorMap; } }


        [SerializeField] int _pointCount;
        [SerializeField] Texture2D _positionMap;
        [SerializeField] Texture2D _colorMap;



        public void Initialize(IEnumerable<Vector3d> positions, IEnumerable<Vector3f> colors, int size)
        {
            _pointCount = size;

            int width = Mathf.CeilToInt(Mathf.Sqrt(_pointCount));

            _positionMap = new Texture2D(width, width, TextureFormat.RGBAHalf, false);
            _positionMap.name = "Position Map";
            _positionMap.filterMode = FilterMode.Point;

            _colorMap = new Texture2D(width, width, TextureFormat.RGBA32, false);
            _colorMap.name = "Color Map";
            _colorMap.filterMode = FilterMode.Point;

            int i1 = 0;
            uint i2 = 0U;

            IEnumerator<Vector3d> position = positions.GetEnumerator();
            IEnumerator<Vector3f> color = colors.GetEnumerator();

            position.MoveNext();
            color.MoveNext();


            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = i1 < _pointCount ? i1 : (int)(i2 % _pointCount);

                    Vector3d p = position.Current;
                    Vector3f c = color.Current;

                    _positionMap.SetPixel(x, y, new Color((float)p.x, (float)p.y, (float)p.z));
                    _colorMap.SetPixel(x, y, new Color(c.x, c.y, c.z));

                    i1 ++;
                    i2 += 132049U; // prime

                    position.MoveNext();
                    color.MoveNext();
                }
            }

            _positionMap.Apply(false, true);
            _colorMap.Apply(false, true);
        }
    }
}
