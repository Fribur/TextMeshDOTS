using TextMeshDOTS.Clipper2AoS;
using Unity.Collections;
using Unity.Mathematics;
using ClipType = TextMeshDOTS.Clipper2AoS.ClipType;
using FillRule = TextMeshDOTS.Clipper2AoS.FillRule;

namespace TextMeshDOTS.HarfBuzz.Rasterizer
{
    internal class PolygonOperation
    {
        /// <summary> Use BURST compatible Clipper2 library to do a union of polygon on itself to remove self-intersections.  </summary>
        public static void RemoveSelfIntersections(ref DrawData subject, ClipType cliptype, FillRule fillrule)
        {
            ClipperL clipper = new ClipperL(Allocator.Temp);
            var nodes = subject.edges;
            var contourIDs = subject.contourIDs;
            var scale = 1000;
            var invScale = 1f / scale;

            var subjectNodes = new NativeList<int2>(nodes.Length, Allocator.Temp);
            for (int i = 0, length = contourIDs.Length - 1; i < length; i++)
            {
                int start = contourIDs[i];
                int end = contourIDs[i + 1];
                for (int k = start; k < end; k++)
                    subjectNodes.Add(new int2((int)(nodes[k].start_pos.x * scale), (int)(nodes[k].start_pos.y * scale)));
            }
            clipper.AddSubject(subjectNodes.AsArray(), contourIDs.AsArray());

            var solutionNodesClosed = new NativeList<int2>(nodes.Length, Allocator.Temp);
            var solutionStartIDsClosed = new NativeList<int>(contourIDs.Length, Allocator.Temp);
            var solutionNodesOpen = new NativeList<int2>(0, Allocator.Temp);
            var solutionStartIDsOpen = new NativeList<int>(0, Allocator.Temp);
            clipper.Execute(cliptype, fillrule, ref solutionNodesClosed, ref solutionStartIDsClosed, ref solutionNodesOpen, ref solutionStartIDsOpen);

            if (solutionStartIDsClosed.Length < 2)
                return;

            nodes.Clear();
            contourIDs.Clear();
            for (int i = 0, length = solutionStartIDsClosed.Length - 1; i < length; i++)
            {
                contourIDs.Add(nodes.Length);
                int start = solutionStartIDsClosed[i];
                int end = solutionStartIDsClosed[i + 1];
                for (int k = start; k < end - 1; k++)
                {
                    var startPos = ((float2)solutionNodesClosed[k]) * invScale;
                    var endPos = ((float2)solutionNodesClosed[k + 1]) * invScale;
                    nodes.Add(new SDFEdge { start_pos = startPos, end_pos = endPos, edge_type = SDFEdgeType.LINE });
                }
            }
            contourIDs.Add(nodes.Length);
            clipper.Dispose();
        }        
    }
}