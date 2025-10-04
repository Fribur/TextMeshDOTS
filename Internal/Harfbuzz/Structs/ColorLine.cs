using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine;

namespace TextMeshDOTS.HarfBuzz
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ColorLine
    {
        IntPtr ptr;

        public int GetColorStops(uint start, out NativeArray<ColorStop> colorStops)
        {
            uint count = 16;
            colorStops = new NativeArray<ColorStop>((int)count, Allocator.Temp);
            var len = Harfbuzz.hb_color_line_get_color_stops(ptr, 0, ref count, (IntPtr)colorStops.GetUnsafePtr());
            if (len > count)
            {
                Debug.Log("capacity of 16 was not sufficient, increasing");
                colorStops = new NativeArray<ColorStop>((int)len, Allocator.Temp);
                Harfbuzz.hb_color_line_get_color_stops(ptr, 0, ref len, (IntPtr)colorStops.GetUnsafePtr());
            }
            //for (int i = 0; i < len; i++)
            //{
            //    var colorStop = colorStops[i];
            //    Debug.Log($"{colorStop.offset} {colorStop.color} {colorStop.isForeground}");
            //}
            var colorStopSlice = new NativeSlice<ColorStop>(colorStops, 0, (int)len);
            colorStopSlice.Sort(default(ColorStopComparer));
            return (int)len;
        }

        public PaintExtend GetExtend()
        {
            return Harfbuzz.hb_color_line_get_extend(ptr);
        }
    };
    //struct hb_color_line_t
    //{
    //    void* data;

    //    hb_color_line_get_color_stops_func_t get_color_stops;
    //    void* get_color_stops_user_data;

    //    hb_color_line_get_extend_func_t get_extend;
    //    void* get_extend_user_data;

    //    void* reserved0;
    //    void* reserved1;
    //    void* reserved2;
    //    void* reserved3;
    //    void* reserved5;
    //    void* reserved6;
    //    void* reserved7;
    //    void* reserved8;
    //};
}
