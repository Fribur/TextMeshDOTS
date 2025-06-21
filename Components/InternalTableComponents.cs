using System;
using TextMeshDOTS.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace TextMeshDOTS.HarfBuzz
{
    internal partial struct FontTable : ICollectionComponent
    {
        // These are zero-sized and unused currently.
        public NativeList<Face>               faces;
        public NativeArray<UnsafeList<Font> > perThreadFontCaches;

        // These are temporary. Something like fontAssetRefToFaceIndexMap, but it will probably be refined.
        public NativeHashMap<int, Entity>       faceIndexToFontEntityMap;
        public NativeHashMap<FontAssetRef, int> fontAssetRefToFaceIndexMap;

        public Font GetOrCreateFont(int faceIndex, int threadIndex)
        {
            var fonts = perThreadFontCaches[threadIndex];
            var font  = fonts[faceIndex];
            if (font.ptr == IntPtr.Zero)
            {
                font             = new Font(faces[faceIndex].ptr);
                fonts[faceIndex] = font;
            }
            return font;
        }

        public JobHandle TryDispose(JobHandle inputDeps)
        {
            if (faces.IsCreated)
            {
                var jh = new DisposeInnerJob { table = this }.Schedule(inputDeps);
                jh                                   = faceIndexToFontEntityMap.Dispose(jh);  // Temporary
                return JobHandle.CombineDependencies(faces.Dispose(jh), perThreadFontCaches.Dispose(jh), fontAssetRefToFaceIndexMap.Dispose(jh));
            }
            return inputDeps;
        }

        struct DisposeInnerJob : IJob
        {
            public FontTable table;

            public void Execute()
            {
                for (int thread = 0; thread < table.perThreadFontCaches.Length; thread++)
                {
                    var list = table.perThreadFontCaches[thread];
                    foreach (var font in list)
                    {
                        if (font.ptr == IntPtr.Zero)
                            continue;
                        font.Dispose();
                    }
                    list.Dispose();
                }
                foreach (var face in table.faces)
                {
                    if (face.ptr == IntPtr.Zero)
                        continue;
                    face.Dispose();
                }

                // We don't need to dispose blobs, as we already "destroy" them upon creation after initializing the face,
                // thus the ref count decremented to 0 after disposing the face.
            }
        }
    }

    internal enum RenderFormat : byte
    {
        SDF8 = 0,
        SDF16 = 1,
        Bitmap8888 = 2,
    }

    internal partial struct GlyphTable : ICollectionComponent
    {
        public struct Key : IEquatable<Key>, IComparable<Key>
        {
            public ulong packed;

            public ushort glyphIndex
            {
                get => (ushort)Bits.GetBits(packed, 0, 16);
                set => Bits.SetBits(ref packed, 0, 16, value);
            }

            public int faceIndex
            {
                get => (int)Bits.GetBits(packed, 16, 20);
                set => Bits.SetBits(ref packed, 16, 20, (uint)value);
            }

            public RenderFormat format
            {
                get => (RenderFormat)Bits.GetBits(packed, 36, 2);
                set => Bits.SetBits(ref packed, 36, 2, (uint)value);
            }

            public FontTextureSize textureSize
            {
                get => (FontTextureSize)Bits.GetBits(packed, 38, 2);
                set => Bits.SetBits(ref packed, 38, 2, (uint)value);
            }

            public int variableProfileIndex
            {
                get => (int)Bits.GetBits(packed, 40, 24);
                set => Bits.SetBits(ref packed, 40, 24, (uint)value);
            }

            public bool Equals(Key other) => packed.Equals(other.packed);
            public override int GetHashCode() => packed.GetHashCode();
            public int CompareTo(Key other) => packed.CompareTo(other.packed);
        }

        public struct Entry
        {
            public Key   key;
            public int   refCount;
            public short x;
            public short y;
            public short z;
            public short width;
            public short height;
            public short xBearing;
            public short yBearing;
            public short padding;

            public bool isInAtlas => x >= 0;
            // Todo:
        }

        public NativeHashMap<Key, uint> glyphHashToIdMap;
        public NativeList<Entry>        entries;

        public JobHandle TryDispose(JobHandle inputDeps)
        {
            if (glyphHashToIdMap.IsCreated)
            {
                return JobHandle.CombineDependencies(glyphHashToIdMap.Dispose(inputDeps), entries.Dispose(inputDeps));
            }
            return inputDeps;
        }

        public ref Entry GetEntryRW(uint glyphEntryID)
        {
            return ref entries.ElementAt((int)(glyphEntryID & 0x3fffffff));
        }

        public Entry GetEntry(uint glyphEntryID)
        {
            return entries[(int)(glyphEntryID & 0x3fffffff)];
        }
    }

    internal partial struct GlyphGpuTable : ICollectionComponent
    {
        public struct Totals
        {
            public uint resident;
            public uint dynamic;
        }

        public NativeReference<Totals> totals;
        public NativeList<uint2>       residentGaps;
        public NativeList<uint2>       dispatchDynamicGaps;  // Deferred gaps when multiple dispatches need to skip over previous dynamic regions

        public JobHandle TryDispose(JobHandle inputDeps)
        {
            if (totals.IsCreated)
            {
                return JobHandle.CombineDependencies(totals.Dispose(inputDeps), residentGaps.Dispose(inputDeps), dispatchDynamicGaps.Dispose(inputDeps));
            }
            return inputDeps;
        }
    }

    internal partial struct AtlasTable : ICollectionComponent
    {
        public AtlasTable(AllocatorManager.AllocatorHandle allocator, int textureDimension, int shelfAlignment)
        {
            sdf8Shelves         = new NativeList<Shelf>(8, allocator);
            sdf16Shelves        = new NativeList<Shelf>(8, allocator);
            bitmapShelves       = new NativeList<Shelf>(8, allocator);
            this.allocator      = allocator;
            dimension           = (uint)textureDimension;
            this.shelfAlignment = shelfAlignment;
        }

        public JobHandle TryDispose(JobHandle inputDeps)
        {
            var jh = new DisposeJob { atlasTable = this }.Schedule(inputDeps);
            return JobHandle.CombineDependencies(sdf8Shelves.Dispose(jh), sdf16Shelves.Dispose(jh), bitmapShelves.Dispose(jh));
        }

        public void Allocate(uint glyphEntryId, short width, short height, out short x, out short y, out short z)
        {
            var format  = (RenderFormat)Bits.GetBits(glyphEntryId, 30, 2);
            var shelves = sdf8Shelves;
            if (format == RenderFormat.SDF16)
                shelves = sdf16Shelves;
            else if (format == RenderFormat.Bitmap8888)
                shelves = bitmapShelves;

            var alignedHeight = CollectionHelper.Align(height, shelfAlignment);
            for (int i = 0; i < shelves.Length; i++)
            {
                var shelf = shelves[i];
                if (shelf.height == alignedHeight)
                {
                    if (shelf.requiresCoellescing)
                    {
                        shelf.reservedX = GapAllocator.CoellesceGaps(ref shelf.gaps, shelf.reservedX);
                    }
                    var found = GapAllocator.TryAllocate(ref shelf.gaps, (uint)width, ref shelf.reservedX, out var foundX, dimension);
                    if (found)
                        shelf.usedX += width;
                    shelves[i]       = shelf;
                    if (found)
                    {
                        x = (short)foundX;
                        y = shelf.y;
                        z = shelf.z;
                        return;
                    }
                }
            }

            // We did not found a suitable shelf. Create a new one.
            var previousMaxYPlus = 0;
            var previousZ        = -1;

            for (int i = 0; i < shelves.Length; i++)
            {
                var nextShelf = shelves[i];
                if (nextShelf.z != previousZ)
                {
                    if (previousMaxYPlus + alignedHeight <= dimension)
                    {
                        var newShelf = new Shelf
                        {
                            y                   = (short)previousMaxYPlus,
                            z                   = (short)previousZ,
                            height              = (short)alignedHeight,
                            requiresCoellescing = false,
                            reservedX           = (uint)width,
                            usedX               = width,
                            gaps                = new UnsafeList<uint2>(8, allocator)
                        };
                        shelves.InsertRange(i, 1);
                        shelves[i] = newShelf;
                        x          = 0;
                        y          = newShelf.y;
                        z          = newShelf.z;
                        return;
                    }
                    else if (nextShelf.z > previousZ + 1)
                    {
                        // Totally free texture array index
                        var newShelf = new Shelf
                        {
                            y                   = 0,
                            z                   = (short)(previousZ + 1),
                            height              = (short)alignedHeight,
                            requiresCoellescing = false,
                            reservedX           = (uint)width,
                            usedX               = width,
                            gaps                = new UnsafeList<uint2>(8, allocator)
                        };
                        shelves.InsertRange(i, 1);
                        shelves[i] = newShelf;
                        x          = 0;
                        y          = newShelf.y;
                        z          = newShelf.z;
                        return;
                    }
                    else if (nextShelf.y >= alignedHeight)
                    {
                        // Free shelf space on the same array index as the next
                        var newShelf = new Shelf
                        {
                            y                   = 0,
                            z                   = nextShelf.z,
                            height              = (short)alignedHeight,
                            requiresCoellescing = false,
                            reservedX           = (uint)width,
                            usedX               = width,
                            gaps                = new UnsafeList<uint2>(8, allocator)
                        };
                        shelves.InsertRange(i, 1);
                        shelves[i] = newShelf;
                        x          = 0;
                        y          = newShelf.y;
                        z          = newShelf.z;
                        return;
                    }
                }
                else if (nextShelf.y >= previousMaxYPlus + alignedHeight)
                {
                    var newShelf = new Shelf
                    {
                        y                   = (short)previousMaxYPlus,
                        z                   = (short)previousZ,
                        height              = (short)alignedHeight,
                        requiresCoellescing = false,
                        reservedX           = (uint)width,
                        usedX               = width,
                        gaps                = new UnsafeList<uint2>(8, allocator)
                    };
                    shelves.InsertRange(i, 1);
                    shelves[i] = newShelf;
                    x          = 0;
                    y          = newShelf.y;
                    z          = newShelf.z;
                    return;
                }
                previousMaxYPlus = nextShelf.y + nextShelf.height;
                previousZ        = nextShelf.z;
            }

            // We couldn't insert a shelf, so we have to append a new one.
            if (previousMaxYPlus + alignedHeight <= dimension)
            {
                // There's still some space in the last array index
                var newShelf = new Shelf
                {
                    y                   = (short)previousMaxYPlus,
                    z                   = (short)previousZ,
                    height              = (short)alignedHeight,
                    requiresCoellescing = false,
                    reservedX           = (uint)width,
                    usedX               = width,
                    gaps                = new UnsafeList<uint2>(8, allocator)
                };
                shelves.Add(newShelf);
                x = 0;
                y = newShelf.y;
                z = newShelf.z;
                return;
            }
            else
            {
                // We need a new array index
                var newShelf = new Shelf
                {
                    y                   = 0,
                    z                   = (short)(previousZ + 1),
                    height              = (short)alignedHeight,
                    requiresCoellescing = false,
                    reservedX           = (uint)width,
                    usedX               = width,
                    gaps                = new UnsafeList<uint2>(8, allocator)
                };
                shelves.Add(newShelf);
                x = 0;
                y = newShelf.y;
                z = newShelf.z;
                return;
            }
        }

        public void Free(uint glyphEntryId, short width, short height, short x, short y, short z)
        {
            var format  = (RenderFormat)Bits.GetBits(glyphEntryId, 30, 2);
            var shelves = sdf8Shelves;
            if (format == RenderFormat.SDF16)
                shelves = sdf16Shelves;
            else if (format == RenderFormat.Bitmap8888)
                shelves = bitmapShelves;

            for (int i = 0; i < shelves.Length; i++)
            {
                var shelf = shelves[i];
                if (shelf.y == y && shelf.z == z)
                {
                    shelf.gaps.Add(new uint2((uint)x, (uint)width));
                    shelf.usedX -= width;
                    if (shelf.usedX <= 0)
                    {
                        // Shelf is empty now. Destroy it.
                        shelf.gaps.Dispose();
                        shelves.RemoveAt(i);
                    }
                    return;
                }
            }
        }

        struct Shelf
        {
            public short y;
            public short z;
            public short height;
            public bool  requiresCoellescing;
            public uint  reservedX;
            public int   usedX;

            public UnsafeList<uint2> gaps;
        }

        AllocatorManager.AllocatorHandle allocator;
        uint                             dimension;
        int                              shelfAlignment;
        NativeList<Shelf>                sdf8Shelves;
        NativeList<Shelf>                sdf16Shelves;
        NativeList<Shelf>                bitmapShelves;

        [BurstCompile]
        struct DisposeJob : IJob
        {
            public AtlasTable atlasTable;

            public void Execute()
            {
                foreach (var shelf in atlasTable.sdf8Shelves)
                    shelf.gaps.Dispose();
                foreach (var shelf in atlasTable.sdf16Shelves)
                    shelf.gaps.Dispose();
                foreach (var shelf in atlasTable.bitmapShelves)
                    shelf.gaps.Dispose();
            }
        }
    }
}

