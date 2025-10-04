using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.TextCore;

namespace TextMeshDOTS.HarfBuzz.Bitmap
{
    internal static class SDF
    {
        //generates SDF directly from bezier curves provided by Harfbuzz.
        //approach is inspired by FreeType 
        public static SignedDistance maxSDF => new SignedDistance { distance = int.MaxValue, sign = 0, cross = 0 };

        /// <summary>
        /// Converts a glyph into a SDF bitmap. While function accepts all kinds of edges found in font files
        /// (quadratic beziers, cubic beziers, lines), consider to generates lines before using this function for performance reasons
        /// </summary>
        public static bool SDFGenerateSubDivision(SDFOrientation orientation, ref DrawData drawData, NativeArray<byte> buffer, GlyphRect atlasRect, int padding, int atlasWidth, int atlasHeight, int spread = SDFCommon.DEFAULT_SPREAD)
        {
            if (drawData.contourIDs.Length < 2 || drawData.edges.Length == 0)
                return false;

            var edges = drawData.edges;
            var contourIDs = drawData.contourIDs;

            bool flip_y = true;
            bool flip_sign = false;
            var offset = drawData.glyphRect.min - padding;
            float sp_sq;
            var dists = new NativeArray<SignedDistance>(atlasRect.width * atlasRect.height, Allocator.Temp);

            if (spread < SDFCommon.MIN_SPREAD || spread > SDFCommon.MAX_SPREAD)
                return false;

            if (SDFCommon.USE_SQUARED_DISTANCES)
                sp_sq = spread * spread;
            else
                sp_sq = spread;

            var rectX = atlasRect.x;
            var rectY = atlasRect.y;
            var atlasRectWidth = atlasRect.width;
            var atlasRectHeight = atlasRect.height;

            for (int contourID = 0, end = contourIDs.Length - 1; contourID < end; contourID++) //for each contour
            {
                int startID = contourIDs[contourID];
                int nextStartID = contourIDs[contourID + 1];
                for (int edgeID = startID; edgeID < nextStartID; edgeID++) //for each edge
                {
                    var edge = edges[edgeID];
                    edge.start_pos -= offset;
                    edge.control1 -= offset;
                    edge.control2 -= offset;
                    edge.end_pos -= offset;

                    var cbox = GetControlBox(edge);
                    cbox.Expand(spread);
                    float2 gridPoint;
                    SignedDistance dist = maxSDF;

                    /* now loop over the pixels in the control box. */
                    for (int y = math.max((int)cbox.min.y, 0), yEnd = math.min((int)cbox.max.y, atlasRectHeight); y < yEnd; y++)
                    {
                        if (y < 0 || y >= atlasRectHeight)
                            continue;
                        gridPoint.y = y + 0.5f; // use the center of any pixel to be rendered within cbox
                        for (int x = math.max((int)cbox.min.x, 0), xEnd = math.min((int)cbox.max.x, atlasRectWidth); x < xEnd; x++)
                        {
                            if (x < 0 || x >= atlasRectWidth)
                                continue;

                            gridPoint.x = x + 0.5f; // use the center of any pixel to be rendered within cbox 
                            SDFEdgeGetMinDistance(edge, gridPoint, ref dist);

                            if (!ValidateAndSaveDistance(dists, ref dist, x, y, atlasRectWidth, atlasRectHeight, orientation, sp_sq, flip_y))
                                continue;
                        }
                    }
                }
            }
            if (flip_sign)
                SDFCommon.FinalPassFlipSign(dists, buffer, spread, atlasRect, atlasWidth, atlasHeight);
            else
                SDFCommon.FinalPass(dists, buffer, spread, atlasRect, atlasWidth, atlasHeight);

            return true;
        }

        /// <summary>
        /// Converts a glyph into a SDF bitmap. When using this function, ensure all bezier edges have been split into line edges first. Distance to lines is much less 
        /// math compared to distance to bezier curves, so this approach is faster overall.
        /// </summary>
        public static bool SDFGenerateSubDivisionLineEdges(SDFOrientation orientation, ref DrawData drawData, NativeArray<byte> buffer, GlyphRect atlasRect, int padding, int atlasWidth, int atlasHeight, int spread = SDFCommon.DEFAULT_SPREAD)
        {
            if (drawData.contourIDs.Length < 2 || drawData.edges.Length == 0)
                return false;

            var edges = drawData.edges;
            var contourIDs = drawData.contourIDs;
            bool flip_y = true;
            bool flip_sign = false;
            var offset = drawData.glyphRect.min - padding;
            float sp_sq;
            var dists = new NativeArray<SignedDistance>(atlasRect.width * atlasRect.height, Allocator.Temp);

            if (spread < SDFCommon.MIN_SPREAD || spread > SDFCommon.MAX_SPREAD)
                return false;

            if (SDFCommon.USE_SQUARED_DISTANCES)
                sp_sq = spread * spread;
            else
                sp_sq = spread;

            var rectX = atlasRect.x;
            var rectY = atlasRect.y;
            var atlasRectWidth = atlasRect.width;
            var atlasRectHeight = atlasRect.height;

            for (int contourID = 0, end = contourIDs.Length - 1; contourID < end; contourID++) //for each contour
            {
                int startID = contourIDs[contourID];
                int nextStartID = contourIDs[contourID + 1];
                for (int edgeID = startID; edgeID < nextStartID; edgeID++) //for each edge
                {
                    var edge = edges[edgeID];
                    var p0 = edge.start_pos - offset;
                    var p1 = edge.end_pos - offset;
                    var cbox = BezierMath.GetLineBBox(p0, p1);
                    cbox.Expand(spread);
                    float2 gridPoint = default;
                    SignedDistance dist = maxSDF;

                    /* now loop over the pixels in the control box. */
                    for (int y = math.max((int)cbox.min.y, 0), yEnd = math.min((int)cbox.max.y, atlasRectHeight); y < yEnd; y++)
                    {
                        gridPoint.y = y + 0.5f;     // use the center of any pixel to be rendered within cbox
                        for (int x = math.max((int)cbox.min.x, 0), xEnd = math.min((int)cbox.max.x, atlasRectWidth); x < xEnd; x++)
                        {
                            gridPoint.x = x + 0.5f; // use the center of any pixel to be rendered within cbox
                            GetMinDistanceLineToPoint(p0, p1, gridPoint, ref dist);
                            if (!ValidateAndSaveDistance(dists, ref dist, x, y, atlasRectWidth, atlasRectHeight, orientation, sp_sq, flip_y))
                                continue;
                        }
                    }
                }
            }
            if (flip_sign)
                SDFCommon.FinalPassFlipSign(dists, buffer, spread, atlasRect, atlasWidth, atlasHeight);
            else
                SDFCommon.FinalPass(dists, buffer, spread, atlasRect, atlasWidth, atlasHeight);
            return true;
        }

        /// <summary>
        /// Converts a glyph into a SDF bitmap. When using this function, ensure all bezier edges have been split into line edges first. 
        /// This version tries to avoid a lot of multiplications by interpolating edge y using the edge slope. Currently glitchy as not all edge cases work.
        /// </summary>
        static bool SDFGenerateSubDivisionLineEdgesInterpolate(SDFOrientation orientation, ref DrawData drawData, NativeArray<byte> buffer, GlyphRect atlasRect, int padding, int atlasWidth, int atlasHeight, int spread = SDFCommon.DEFAULT_SPREAD)
        {
            if (drawData.contourIDs.Length < 2 || drawData.edges.Length == 0)
                return false;

            var edges = drawData.edges;
            var contourIDs = drawData.contourIDs;

            bool flip_y = true;
            bool flip_sign = false;
            var offset = drawData.glyphRect.min - padding;
            float sp_sq;
            var dists = new NativeArray<SignedDistance>(atlasRect.width * atlasRect.height, Allocator.Temp);

            if (spread < SDFCommon.MIN_SPREAD || spread > SDFCommon.MAX_SPREAD)
                return false;

            if (SDFCommon.USE_SQUARED_DISTANCES)
                sp_sq = spread * spread;
            else
                sp_sq = spread;

            var atlastRectWidth = atlasRect.width;
            var atlastRectHeight = atlasRect.height;
            for (int contourID = 0, end = contourIDs.Length - 1; contourID < end; contourID++) //for each contour
            {
                int startID = contourIDs[contourID];
                int nextStartID = contourIDs[contourID + 1];
                for (int edgeID = startID; edgeID < nextStartID; edgeID++) //for each edge
                {                    
                    var edge = edges[edgeID];
                    var p0 = edge.start_pos - offset;
                    var p1 = edge.end_pos - offset;
                    var cbox = BezierMath.GetLineBBox(p0, p1);
                    cbox.Expand(spread);
                    var ab = p1 - p0;
                    var abLength = math.length(ab);
                    var normal1 = new float2(-ab.y, ab.x)/ abLength;
                    var normal2 = new float2(ab.y, -ab.x)/ abLength;
                    
                    float2 gridPoint = default;
                    float t, tA, tB;
                    float2 aScanIntersect, bScanIntersect, nearestPoint, aEdgeToScan, bEdgeToScan, nEdgeToScan;
                    SignedDistance dist = maxSDF;

                    /* now loop over the pixels in the control box. */
                    for (int y = (int)cbox.min.y, yEnd = (int)cbox.max.y; y < yEnd; y++)
                    {
                        if (y < 0 || y >= atlastRectHeight)
                            continue;

                        gridPoint.y = y + 0.5f;
                        var dy_as = gridPoint.y - p0.y;
                        var dy_bs = gridPoint.y - p1.y;

                        if (ab.x > 0) //edge and scanline have opposite direction
                            (normal1, normal2) = (normal2, normal1);                        

                        tA = dy_as / normal1.y;
                        tB = dy_bs / normal1.y;
                        aEdgeToScan = tA * normal1;
                        bEdgeToScan = tB * normal1;
                        if (ab.x == 0)//vertical
                        {
                            aScanIntersect =  bScanIntersect = new float2(p0.x,0);
                        }
                        else
                        {
                            aScanIntersect = p0 + aEdgeToScan; //works also for horizontal case (ab.y == 0) where normal1 will be 0 (ab.y is nominator of normal)
                            bScanIntersect = p1 + bEdgeToScan;
                        }

                        if (ab.x == 0) //vertical
                        {
                            var maxY = math.max(p0.y, p1.y);
                            var minY = math.min(p0.y, p1.y);
                            for (int x = (int)cbox.min.x, xEnd = (int)cbox.max.x; x < xEnd; x++)
                            {
                                if (x < 0 || x >= atlastRectWidth)
                                    continue;

                                if (y < minY || y > maxY)
                                    continue;

                                gridPoint.x = x + 0.5f;
                                nearestPoint = new float2(p0.x, y);
                                GetDistBetweenPoints(nearestPoint - gridPoint, ab, false, ref dist);
                                if (!ValidateAndSaveDistance(dists, ref dist, x, y, atlastRectWidth, atlastRectHeight, orientation, sp_sq, flip_y))
                                    continue;
                            }
                        }
                        else
                        {
                            if (p0.x > p1.x)
                            {
                                (aScanIntersect, bScanIntersect) = (bScanIntersect, aScanIntersect);
                                (aEdgeToScan, bEdgeToScan) = (bEdgeToScan, aEdgeToScan);
                                (p0, p1) = (p1, p0);
                            }

                            //interpolate distance between edge endpoints projected onto scanLine
                            var interpolateLength = bScanIntersect.x - aScanIntersect.x;
                            for (int x = math.max((int)cbox.min.x, (int)aScanIntersect.x), xEnd = math.min((int)bScanIntersect.x, (int)cbox.max.x); x < xEnd; x++)
                            {
                                if (x < 0 || x >= atlastRectWidth)
                                    continue;

                                gridPoint.x = x + 0.5f;
                                t = (x - aScanIntersect.x) / interpolateLength;
                                if (p0.y == p1.y) //horizontal
                                {
                                    nearestPoint.x = math.lerp(p0.x, p1.x, t);
                                    nearestPoint.y = p0.y;
                                }
                                else
                                {
                                    nEdgeToScan = -math.lerp(aEdgeToScan, bEdgeToScan, t); //negate aEdgeToScan vector to give opposite direction ScanToaEdge vector
                                    nearestPoint = gridPoint + nEdgeToScan;
                                }

                                GetDistBetweenPoints(nearestPoint-gridPoint, ab, false, ref dist);
                                if (!ValidateAndSaveDistance(dists, ref dist, x, y, atlastRectWidth, atlastRectHeight, orientation, sp_sq, flip_y))
                                    continue;
                            }

                            //fill all pixel having an edge endpoint as their closest point
                            //calculate distance of all points to the left of the projected left endPoint 
                            for (int x = (int)cbox.min.x, xEnd = (int)aScanIntersect.x; x < xEnd; x++)
                            {
                                if (x < 0 || x >= atlastRectWidth)
                                    continue;

                                gridPoint.x = x + 0.5f;

                                int index;
                                if (flip_y)
                                    index = y * atlastRectWidth + x;
                                else
                                    index = (atlastRectHeight - y - 1) * atlastRectWidth + x;
                                if (dists[index].sign != 0) // check if the pixel is already set
                                    continue;

                                GetDistBetweenPoints(p0-gridPoint, ab, true, ref dist);
                                if (!ValidateAndSaveDistance(dists, ref dist, x, y, atlastRectWidth, atlastRectHeight, orientation, sp_sq, flip_y))
                                    continue;
                            }

                            //calculate distance of all points to the right of the projected right endPoint 
                            for (int x = math.min((int)bScanIntersect.x, (int)cbox.max.x), xEnd = (int)cbox.max.x; x < xEnd; x++)
                            {
                                if (x < 0 || x >= atlastRectWidth)
                                    continue;

                                gridPoint.x = x + 0.5f;

                                int index;
                                if (flip_y)
                                    index = y * atlastRectWidth + x;
                                else
                                    index = (atlastRectHeight - y - 1) * atlastRectWidth + x;
                                if (dists[index].sign != 0) // check if the pixel is already set
                                    continue;

                                GetDistBetweenPoints(p1-gridPoint, ab, true, ref dist);
                                if (!ValidateAndSaveDistance(dists, ref dist, x, y, atlastRectWidth, atlastRectHeight, orientation, sp_sq, flip_y))
                                    continue;
                            }
                        }                        
                    }
                }
            }
            if (flip_sign)
                SDFCommon.FinalPassFlipSign(dists, buffer, spread, atlasRect, atlasWidth, atlasHeight);
            else
                SDFCommon.FinalPass(dists, buffer, spread, atlasRect, atlasWidth, atlasHeight);            
            return true;
        }

        static BBox GetControlBox(SDFEdge edge)
        {
            BBox cbox = BBox.Empty;
            bool is_set = false;


            switch (edge.edge_type)
            {
                case SDFEdgeType.CUBIC:
                    cbox.min = edge.control2;
                    cbox.max = edge.control2;

                    is_set = true;
                    goto case SDFEdgeType.QUADRATIC;

                case SDFEdgeType.QUADRATIC:
                    if (is_set)
                    {
                        cbox.min.x = edge.control1.x < cbox.min.x ? edge.control1.x : cbox.min.x;
                        cbox.min.y = edge.control1.y < cbox.min.y ? edge.control1.y : cbox.min.y;

                        cbox.max.x = edge.control1.x > cbox.max.x ? edge.control1.x : cbox.max.x;
                        cbox.max.y = edge.control1.y > cbox.max.y ? edge.control1.y : cbox.max.y;
                    }
                    else
                    {
                        cbox.min = edge.control1;
                        cbox.max = edge.control1;

                        is_set = true;
                    }
                    goto case SDFEdgeType.LINE;

                case SDFEdgeType.LINE:
                    if (is_set)
                    {
                        cbox.min.x = edge.start_pos.x < cbox.min.x ? edge.start_pos.x : cbox.min.x;
                        cbox.max.x = edge.start_pos.x > cbox.max.x ? edge.start_pos.x : cbox.max.x;

                        cbox.min.y = edge.start_pos.y < cbox.min.y ? edge.start_pos.y : cbox.min.y;
                        cbox.max.y = edge.start_pos.y > cbox.max.y ? edge.start_pos.y : cbox.max.y;
                    }
                    else
                    {
                        cbox.min = edge.start_pos;
                        cbox.max = edge.start_pos;
                    }

                    cbox.min.x = edge.end_pos.x < cbox.min.x ? edge.end_pos.x : cbox.min.x;
                    cbox.max.x = edge.end_pos.x > cbox.max.x ? edge.end_pos.x : cbox.max.x;

                    cbox.min.y = edge.end_pos.y < cbox.min.y ? edge.end_pos.y : cbox.min.y;
                    cbox.max.y = edge.end_pos.y > cbox.max.y ? edge.end_pos.y : cbox.max.y;

                    break;

                default:
                    break;
            }

            return cbox;
        }
        static BBox GetBBox(SDFEdge edge)
        {
            switch (edge.edge_type)
            {
                case SDFEdgeType.CUBIC:
                    return BezierMath.GetCubicBezierBBox(edge.start_pos, edge.control1, edge.control2, edge.end_pos);
                case SDFEdgeType.QUADRATIC:
                    return BezierMath.GetQuadraticBezierBBox(edge.start_pos, edge.control1, edge.end_pos);
                case SDFEdgeType.LINE:
                    return BezierMath.GetLineBBox(edge.start_pos, edge.end_pos);
                default:
                    break;
            }
            return BBox.Empty;
        }
        public static bool SDFEdgeGetMinDistance(SDFEdge edge, float2 point, ref SignedDistance signedDistance)
        {
            var p0 = edge.start_pos;
            var p1 = edge.control1;
            var p2 = edge.control2;
            var p3 = edge.end_pos;
            bool success = false;
            switch (edge.edge_type)
            {
                case SDFEdgeType.LINE:
                    success = GetMinDistanceLineToPoint(p0, p3, point, ref signedDistance);
                    break;
                case SDFEdgeType.QUADRATIC:
                    success = GetMinDistanceQuadraticNewton(p0,p1,p3, point, ref signedDistance);
                    break;
                case SDFEdgeType.CUBIC:
                    success = GetMinDistanceCubicNewton(point, p1, p2, p3, point, ref signedDistance);
                    break;
                default:
                    break;
            }
            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GetDistBetweenPoints(float2 pn, float2 ab, bool isEndPoint, ref SignedDistance dist)
        {
            var pnLengthSq = math.lengthsq(pn);
            var cross = BezierMath.cross2D(pn, ab);

            dist.sign = cross < 0 ? 1 : -1;
            dist.distance = SDFCommon.USE_SQUARED_DISTANCES ? pnLengthSq : math.sqrt(pnLengthSq);

            //dist.sign = math.select(-1,1, cross < 0);
            //dist.distance = math.select( math.sqrt(pnLengthSq), pnLengthSq, SDFCommon.USE_SQUARED_DISTANCES);

            if (Hint.Unlikely(isEndPoint))
            {
                ab = math.normalize(ab);
                pn = math.normalize(pn);
                dist.cross = BezierMath.cross2D(ab, pn);
            }
            else
                dist.cross = 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool GetMinDistanceLineToPoint(float2 a, float2 b, float2 p, ref SignedDistance dist)
        {
            var ab = b - a;                         // Vector from A to B
            var ap = p - a;                         // Vector from A to P
            var abLengthSq = math.lengthsq(ab);
            var frac = math.dot(ab, ap);
            frac = math.max(frac, 0.0f);            // Check if P projection is over vectorAB 
            frac = math.min(frac, abLengthSq);      // Check if P projection is over vectorAB 

            frac = frac / abLengthSq;               // The normalized "distance" from a to your closest point
            var n = a + ab * frac;                  // nearest point on egde

            var pn = n - p;
            var pnLengthSq = math.lengthsq(pn);
            var cross = BezierMath.cross2D(pn, ab);

            dist.sign = cross < 0 ? 1 : -1;
            dist.distance = SDFCommon.USE_SQUARED_DISTANCES ? pnLengthSq : math.sqrt(pnLengthSq);

            //dist.sign = math.select(-1,1, cross < 0);
            //dist.distance = math.select( math.sqrt(pnLengthSq), pnLengthSq, SDFCommon.USE_SQUARED_DISTANCES);


            bool nIsEndPoint = BezierMath.EqualsForSmallValues(frac, 0) || BezierMath.EqualsForSmallValues(frac, 1);            
            if (Hint.Unlikely(nIsEndPoint))
                dist.cross = BezierMath.cross2D(ab / math.sqrt(abLengthSq), pn / math.sqrt(pnLengthSq));            
            else
                dist.cross = 1;

            return true;
        }
        static bool GetMinDistanceQuadraticNewton(float2 p0, float2 p1, float2 p2, float2 point, ref SignedDistance dist)
        {
            float min = int.MaxValue;           // shortest distance
            float min_factor = 0;               // factor at shortest distance
            float2 nearest_point = default;     // point on curve nearest to `point`

            // compute substitution coefficients
            var aA = p0 - 2 * p1 + p2;
            var bB = 2 * (p1 - p0);
            var cC = p0;

            // do Newton's iterations
            for (int iterations = 0; iterations <= SDFCommon.MAX_NEWTON_DIVISIONS; iterations++)
            {
                float factor = (float)iterations / SDFCommon.MAX_NEWTON_DIVISIONS;

                for (int steps = 0; steps < SDFCommon.MAX_NEWTON_STEPS; steps++)
                {
                    var factor2 = factor * factor;
                    var curvePoint = (aA * factor2) + (bB * factor) + cC; // B(t) = t^2 * A + t * B + p0                    
                    var dist_vector = curvePoint - point;                // P(t) in the comment
                    var length = SDFCommon.USE_SQUARED_DISTANCES ? math.lengthsq(dist_vector) : math.length(dist_vector);
                    if (length < min)
                    {
                        min = length;
                        min_factor = factor;
                        nearest_point = curvePoint;
                    }

                    /* This is Newton's approximation.          */
                    /*   t := P(t) . B'(t) /                    */
                    /*          (B'(t) . B'(t) + P(t) . B''(t)) */
                    var d1 = (2 * factor * aA) + bB;                            // B'(t) = 2tA + B
                    var d2 = 2 * aA;                                            // B''(t) = 2A                   
                    var temp1 = math.dot(dist_vector, d1);                      // temp1 = P(t) . B'(t)
                    var temp2 = math.dot(d1, d1) + math.dot(dist_vector, d2);   // temp2 = B'(t) . B'(t) + P(t) . B''(t)
                    factor -= temp1 / temp2;

                    if (factor < 0 || factor > 1)
                        break;
                }
            }
            var direction = 2 * (aA * min_factor) + bB; // B'(t) = 2t * A + B

            // assign values, determine the sign
            var nearestVector = nearest_point - point;
            var cross = BezierMath.cross2D(nearestVector, direction);
            dist.distance = min;
            dist.sign = cross < 0 ? 1 : -1;

            bool nIsEndPoint = BezierMath.EqualsForSmallValues(min_factor, 0) || BezierMath.EqualsForSmallValues(min_factor, 1);
            if (Hint.Unlikely(nIsEndPoint))
            {
                direction = math.normalize(direction);
                nearestVector = math.normalize(nearestVector);
                dist.cross = BezierMath.cross2D(direction, nearestVector);
            }
            else
                dist.cross = 1; // the two are perpendicular
           
            return true;
        }
        static bool GetMinDistanceCubicNewton(float2 p0, float2 p1, float2 p2, float2 p3, float2 point, ref SignedDistance dist)
        {
            float2 nearest_point = default;  // point on curve nearest to `point`
            float min_factor = 0;            // factor at shortest distance
            float min_factor_sq = 0;         // factor at shortest distance
            float min = int.MaxValue;        // shortest distance

            // compute substitution coefficients
            var aA = -p0 + 3 * (p1 - p2) + p3;
            var bB = 3 * (p0 - 2 * p1 + p2);
            var cC = 3 * (p1 - p0);
            var dD = p0;

            for (int iterations = 0; iterations <= SDFCommon.MAX_NEWTON_DIVISIONS; iterations++)
            {
                float factor = (float)iterations / SDFCommon.MAX_NEWTON_DIVISIONS;
                for (int steps = 0; steps < SDFCommon.MAX_NEWTON_STEPS; steps++)
                {
                    var factor2 = factor * factor;
                    var factor3 = factor2 * factor;
                    var curve_point = aA * factor3 + bB * factor2 + cC * factor + dD; // B(t) = t^3 * A + t^2 * B + t * C + D
                    var dist_vector = curve_point - point;                              // P(t) in the comment
                    var length = SDFCommon.USE_SQUARED_DISTANCES ? math.lengthsq(dist_vector) : math.length(dist_vector);
                    if (length < min)
                    {
                        min = length;
                        min_factor = factor;
                        min_factor_sq = factor2;
                        nearest_point = curve_point;
                    }

                    /* This the Newton's approximation.         */
                    /*   t := P(t) . B'(t) /                    */
                    /*          (B'(t) . B'(t) + P(t) . B''(t)) */
                    var d1 = aA * 3 * factor2 + bB * 2 * factor + cC;           // B'(t) = 3t^2 * A + 2t * B + C
                    var d2 = aA * 6 * factor + 2 * bB;                          // B''(t) = 6t * A + 2B
                    var temp1 = math.dot(dist_vector, d1);                      // temp1 = P(t) . B'(t)                  
                    var temp2 = math.dot(d1, d1) + math.dot(dist_vector, d2);   // temp2 = B'(t) . B'(t) + P(t) . B''(t)

                    factor -= temp1 / temp2;

                    if (factor < 0 || factor > 1)
                        break;
                }
            }
            var direction = aA * 3 * min_factor_sq + bB * 2 * min_factor + cC;  // B'(t) = 3t^2 * A + 2t * B + C

            // assign values, determine the sign
            var nearestVector = nearest_point - point;
            var cross = BezierMath.cross2D(nearestVector, direction);

            dist.distance = min;
            dist.sign = cross < 0 ? 1 : -1;
            bool nIsEndPoint = BezierMath.EqualsForSmallValues(min_factor, 0) || BezierMath.EqualsForSmallValues(min_factor, 1);
            if (Hint.Unlikely(nIsEndPoint))
            {
                //compute `cross` if not perpendicular
                direction = math.normalize(direction);
                nearestVector = math.normalize(nearestVector);
                dist.cross = BezierMath.cross2D(direction, nearestVector);
            }
            else
                dist.cross = 1; // the two are perpendicular
            
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ValidateAndSaveDistance(NativeArray<SignedDistance> dists, ref SignedDistance dist, int x, int y, int rectWidth, int rectHeight, SDFOrientation orientation, float sp_sq, bool flip_y)
        {
            if (orientation == SDFOrientation.FILL_LEFT)
                dist.sign = -dist.sign;

            // ignore if the distance is greater than spread;
            // otherwise it creates artifacts due to the wrong sign
            if (dist.distance > sp_sq)
                return false;

            if (SDFCommon.USE_SQUARED_DISTANCES)
                dist.distance = math.sqrt(dist.distance);

            int index;
            if (flip_y)
                index = y * rectWidth + x;
            else
                index = (rectHeight - y - 1) * rectWidth + x;

            var targetDist = dists[index];
            if (targetDist.sign == 0) // check if the pixel is already set
                targetDist = dist;
            else
            {
                if (BezierMath.EqualsForLargeValues(targetDist.distance, dist.distance))
                    targetDist = ResolveCorner(dists[index], dist);
                else if (targetDist.distance > dist.distance)
                    targetDist = dist;
            }
            dists[index] = targetDist;
            return true;
        }
        public static SignedDistance ResolveCorner(SignedDistance sdf1, SignedDistance sdf2)
        {
            return math.abs(sdf1.cross) > math.abs(sdf2.cross) ? sdf1 : sdf2;
        }   
    }
}