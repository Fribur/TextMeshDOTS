using System.IO;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.TextCore;

namespace TextMeshDOTS.HarfBuzz.Bitmap
{   
    internal static class SDFCommon
    {
        public readonly static bool USE_SQUARED_DISTANCES = true;
        // SPREAD represents the permitted distance of a given pixel to an edge in bits.
        // So 8 bit means distance can be from -127 (outside) to +128 (inside).
        // We need 8 bit when storing distances in an 8 bit alpha channel. 
        // When glyphs have a lot of "inside" area (often found in BLACK font weigth), and sampling them at larger sampling
        // point sizes (e.g. 128, or 256), this will led to "holes" due to this line of code in ValidateAndSaveDistance():
        // ignore if the distance is greater than spread;
        // if (dist.distance > sp_sq) return false;
        // Could possibly also clamp the distance here, but this would not look much prettier due
        // due to clipping. Better solution is to increase SPREAD to e.g. 16. When converting to 8 bit alpha, we add SPREAD
        // to give distances from 0..2*SPREAD, and multiply by (256/(2*SPREAD ) via this line of code in the final pass:
        // var scaleTo8Bit = 256 / (spread * 2);
        public const int DEFAULT_SPREAD = 8; // SPREAD and Atlas padding are related, but do not set SPREAD too small 
        public const int MIN_SPREAD = 2;
        public const int MAX_SPREAD = 32;
        public const int MAX_NEWTON_STEPS = 4;
        public const int MAX_NEWTON_DIVISIONS = 4;
        public const int FT_TRIG_SAFE_MSB = 29;
        public const int OUTSIDE_SIGN = -1;

        public static void FinalPass(NativeArray<SignedDistance> dists, NativeArray<byte> buffer, int spread, GlyphRect atlastRect, int atlasWidth, int atlasHeight)
        {
            // final pass
            var atlasX = atlastRect.x;
            var atlasY = atlastRect.y;
            var atlasRectWidth = atlastRect.width;
            var atlasRectHeight = atlastRect.height;
            var scaleTo8Bit = 256 / (spread * 2);
            //var scaleTo16Bit = 65536 / (spread * 2);
            for (int row = 0; row < atlasRectHeight; row++)
            {
                /* We assume the starting pixel of each row is outside. */
                int current_sign = OUTSIDE_SIGN;

                for (int column = 0; column < atlasRectWidth; column++)
                {
                    var sourceIndex = atlasRectWidth * row + column;
                    var targetIndex = (atlasWidth * (row + atlasY)) + (column + atlasX);

                    var dist = dists[sourceIndex];
                    if (dist.sign == 0)
                    {
                        // if the pixel is not set, its shortest distance is more than `spread`
                        dist.sign = OUTSIDE_SIGN;
                        dist.distance = spread;
                    }
                    else
                        current_sign = dist.sign;

                    dist.distance = math.select(dist.distance, spread, dist.distance > spread);

                    // determine if distance is inside(+) or outside(-)
                    dist.distance *= current_sign;
                    dists[sourceIndex] = dist;

                    // convert to byte range of alpha8 texture
                    var result = (dist.distance + spread) * scaleTo8Bit;
                    buffer[targetIndex] = (byte)result;
                }
            }
        }
        public static void FinalPassFlipSign(NativeArray<SignedDistance> dists, NativeArray<byte> buffer, int spread, GlyphRect atlastRect, int atlasWidth, int atlasHeight)
        {
            // final pass
            var atlasX = atlastRect.x;
            var atlasY = atlastRect.y;
            var atlasRectWidth = atlastRect.width;
            var atlasRectHeight = atlastRect.height;
            var scaleTo8Bit = 256 / (spread * 2);
            //var scaleTo16Bit = 65536 / (spread * 2);
            for (int row = 0; row < atlasRectHeight; row++)
            {
                /* We assume the starting pixel of each row is outside. */
                int current_sign = OUTSIDE_SIGN;

                for (int column = 0; column < atlasRectWidth; column++)
                {
                    var sourceIndex = atlasRectWidth * row + column;
                    var targetIndex = (atlasWidth * (row + atlasY)) + (column + atlasX);

                    var dist = dists[sourceIndex];
                    if (dist.sign == 0)
                    {
                        // if the pixel is not set, its shortest distance is more than `spread`
                        dist.sign = OUTSIDE_SIGN;
                        dist.distance = -spread;
                    }
                    else
                        current_sign = dist.sign;

                    dist.distance = math.select(dist.distance, spread, dist.distance > spread);

                    // determine if distance is inside(+) or outside(-)
                    dist.distance *= -current_sign;
                    dists[sourceIndex] = dist;

                    // convert to byte range of alpha8 texture
                    var result = (dist.distance + spread) * scaleTo8Bit;
                    buffer[targetIndex] = (byte)result;
                }
            }
        }

        public static void WriteGlyphOutlineToFile(string path, NativeList<SDFEdge> edges)
        {
            if(edges.Length == 0) return;
            StreamWriter writer = new StreamWriter(path, false);
            var edge = edges[0];
            writer.WriteLine($"{edge.start_pos.x} {edge.start_pos.y}");
            for (int i = 0, end = edges.Length; i < end; i++)
            {
                edge = edges[i];
                writer.WriteLine($"{edge.end_pos.x} {edge.end_pos.y}");              
            }
            writer.WriteLine();
            writer.Close();
        }
        public static void WriteGlyphOutlineToFile(string path, NativeList<Edge> edges)
        {
            if (edges.Length == 0) return;
            StreamWriter writer = new StreamWriter(path, false);
            var edge = edges[0];

            for (int i = 0, end = edges.Length; i < end; i++)
            {
                edge = edges[i];
                writer.WriteLine($"{edge.x0} {edge.y0} {edge.invert}");
                writer.WriteLine($"{edge.x1} {edge.y1}");
                writer.WriteLine();
            }
            writer.WriteLine();
            writer.Close();
        }
        public static void WriteMinDistancesToFile(string path, in NativeArray<SDFDebug> sdfDebug)
        {
            if (sdfDebug.Length == 0) return;
            StreamWriter writer = new StreamWriter(path, false);
            for (int i = 0, end = sdfDebug.Length; i < end; i++)
            {
                writer.WriteLine($"{sdfDebug[i]}");
            }
            writer.WriteLine();
            writer.Close();
        }
        public static void WriteMinDistancesToFile(string path, in NativeArray<float> minDistances)
        {
            if (minDistances.Length == 0) return;
            StreamWriter writer = new StreamWriter(path, false);
            for (int i = 0, end = minDistances.Length; i < end; i++)
            {
                writer.WriteLine($"{minDistances[i]}");
            }
            writer.WriteLine();
            writer.Close();
        }
        public static void WriteMinDistancesToFile(string path, in NativeArray<byte> minDistances)
        {
            if (minDistances.Length == 0) return;
            StreamWriter writer = new StreamWriter(path, false);
            for (int i = 0, end = minDistances.Length; i < end; i++)
            {
                writer.WriteLine($"{minDistances[i]}");
            }
            writer.WriteLine();
            writer.Close();
        }
        public static void WriteMinDistancesToFile(string path, NativeArray<SDFDebug> sdfDebug)
        {
            if (sdfDebug.Length == 0) return;
            StreamWriter writer = new StreamWriter(path, false);
            for (int i = 0, end = sdfDebug.Length; i < end; i++)
            {
                var c = sdfDebug[i];
                writer.WriteLine($"{c.row} {c.column} {c.distanceRaw} {c.signRaw} {c.currentSignRaw} {c.distance} {c.sign} {c.currentSign} {c.cross}");
            }
            writer.WriteLine();
            writer.Close();
        }
        public static void WriteGlyphOutlineToFile(string path, ref DrawData drawData, bool fullBezier=false)
        {
            var edges = drawData.edges;
            var contourIDs = drawData.contourIDs;
            if (contourIDs.Length < 2 || edges.Length == 0)
                return;

            StreamWriter writer = new StreamWriter(path, false);
            SDFEdge edge;
            for (int contourID = 0, end = contourIDs.Length - 1; contourID < end; contourID++) //for each contour
            {
                int startID = contourIDs[contourID];
                int nextStartID = contourIDs[contourID + 1];
                for (int edgeID = startID; edgeID < nextStartID; edgeID++) //for each edge
                {
                    edge = edges[edgeID];
                    if(fullBezier)
                        writer.WriteLine($"{edge.start_pos.x} {edge.start_pos.y} {edge.control1.x} {edge.control1.y} {edge.end_pos.x} {edge.end_pos.y} {edge.edge_type}");
                    else
                        writer.WriteLine($"{edge.start_pos.x} {edge.start_pos.y}");
                    
                }
                writer.WriteLine();
            }
            writer.Close();
        }
    }
    public struct SDFDebug
    {
        public int row;
        public int column;
        public float distanceRaw;
        public int signRaw;
        public float distance;
        public int sign;
        public float cross;
        public int currentSignRaw;
        public int currentSign;
        public SDFDebug(int row, int column, float distanceRaw, int signRaw, int currentSignRaw, float cross)
        {
            this.row = row;
            this.column = column;
            this.distanceRaw = distanceRaw;
            this.signRaw = signRaw;
            this.currentSignRaw = currentSignRaw;
            this.distance = float.MinValue;
            this.sign = int.MinValue;
            this.cross = cross;
            this.currentSign = 0;
        }
    }

}
