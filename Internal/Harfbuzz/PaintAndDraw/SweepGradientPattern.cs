using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace TextMeshDOTS.HarfBuzz
{
    internal struct SweepGradient : IPattern
    {
        //https://github.com/foo123/Gradient/blob/80e362bea2cb7deb3ab4c2125bf6fa49a726e4be/README.md
        NativeArray<ColorStop> m_colorStops;
        int m_colorStopCount;
        PaintExtend paintExtend;
        float x0;
        float y0;
        float startAngle;
        float endAngle;
        float sectorRange;
        float startAngleScaled;
        float endAngleScaled;
        float minStop;
        float maxStop;
        float2x3 inverseTransform;
        [MarshalAs(UnmanagedType.U1)]
        public bool isValid;
        public SweepGradient(float x0, float y0, float startAngle, float endAngle, PaintExtend paintExtend, float2x3 transform)
        {
            if (Hint.Unlikely(startAngle == endAngle && (paintExtend == PaintExtend.REPEAT || paintExtend == PaintExtend.REFLECT)))
                isValid = false; //points idential, gradient ill formed, draw nothing https://learn.microsoft.com/en-us/typography/opentype/spec/colr            

            // the object to which the gradient will be applied needs to be transformed
            // prior to rasterization. Furthermore, additional transformation can be applied just to the gradient (and not to the obejct)
            // so we need to apply the inverse gradient transfrom to the bitmap coordinates when calling GetColor().
            var success = PaintUtils.Inverse(transform, out inverseTransform);
            if(!success)
                Debug.Log($"Failed to create inverse transform");

            this.x0 = x0;
            this.y0 = y0;
            this.startAngle = startAngle;
            this.endAngle = endAngle;
            sectorRange = (endAngle - startAngle);

            startAngleScaled = default;
            endAngleScaled = default;
            m_colorStops = default;
            minStop = default;
            maxStop = default;
            m_colorStopCount = 0;
            this.paintExtend = paintExtend;
            isValid = true;
        }

        public void InitializeColorLine(ColorLine colorLine)
        {
            m_colorStopCount = colorLine.GetColorStops(0, out NativeArray<ColorStop> colorStops);
            m_colorStops = colorStops;

            minStop = colorStops[0].offset;
            maxStop = colorStops[m_colorStopCount - 1].offset;
            sectorRange = (endAngle - startAngle) / (maxStop - minStop);

            startAngleScaled = startAngle + sectorRange * minStop;
            endAngleScaled = startAngle + sectorRange * maxStop;
        }

        /// <summary>
        /// For a given pixel within the rendered glyph, this method calculates 
        /// the UV coordinates that a texture of the color gradient would have. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ColorARGB GetColor(float2 bitmapCoordinates)
        {
            var designSpaceCoordinates = PaintUtils.mul(inverseTransform, bitmapCoordinates);
            var x = designSpaceCoordinates.x;
            var y = designSpaceCoordinates.y;
            var angle = math.atan2(y - y0, x - x0);   //returns angle from 0 to 2PI 
            angle = PaintUtils.WrapAroundLimit(angle, math.PI2);
            var t = (angle / (endAngleScaled - startAngleScaled)) - startAngle / (endAngle - startAngle);
            PaintUtils.ApplySweepWrapMode(ref t, minStop, maxStop, paintExtend);
            return PaintUtils.SampleGradient(m_colorStops, m_colorStopCount, t);
        }
        public float Interpolate(float v1, float v2, float f)
        {
            return v1 + f * (v2 - v1);
        }
    }
}
