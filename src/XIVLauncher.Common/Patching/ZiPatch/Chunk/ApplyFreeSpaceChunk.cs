﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XIVLauncher.Common.Patching.Util;

namespace XIVLauncher.Common.Patching.ZiPatch.Chunk
{
    public class ApplyFreeSpaceChunk : ZiPatchChunk
    {
        // This is a NOP on recent patcher versions, so I don't think we'll be seeing it.
        public new static string Type = "APFS";

        // TODO: No samples of this were found, so these fields are theoretical
        public long UnknownFieldA { get; protected set; }
        public long UnknownFieldB { get; protected set; }

        protected override void ReadChunk()
        {
            var start = reader.BaseStream.Position;

            UnknownFieldA = reader.ReadInt64BE();
            UnknownFieldB = reader.ReadInt64BE();

            reader.ReadBytes(Size - (int)(reader.BaseStream.Position - start));
        }


        public ApplyFreeSpaceChunk(ChecksumBinaryReader reader, int offset, int size) : base(reader, offset, size) {}

        public override string ToString()
        {
            return $"{Type}:{UnknownFieldA}:{UnknownFieldB}";
        }
    }
}