using Unity.Mathematics;

namespace TextMeshDOTS.HarfBuzz.Bitmap
{
    internal struct SignedDistance
    {
        public float distance;
        public float cross;
        public int sign;
    }
    internal struct SignedDistance4
    {
        public float4 distance;
        public float4 cross;
        public int4 sign;
        public SignedDistance this[int i]
        {
            get
            {
                return new SignedDistance { distance = distance[i], cross = cross[i], sign = sign[i] };
            }
            set
            {
                distance[i]=value.distance;
                cross[i]=value.cross;
                sign[i]=value.sign;
            }
        }

    }
}
