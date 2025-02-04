﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XIVLauncher.Common.Patching.ZiPatch.Util;
using XIVLauncher.Common.Patching.ZiPatch.Chunk;

namespace XIVLauncher.Common.Patching.ZiPatch
{
    public class ZiPatchFile : IDisposable
    {
        private static readonly uint[] ZIPATCH_MAGIC = {
            0x50495A91, 0x48435441, 0x0A1A0A0D
        };

        private readonly Stream _stream;


        /// <summary>
        /// Instantiates a ZiPatchFile from a Stream
        /// </summary>
        /// <param name="stream">Stream to a ZiPatch</param>
        public ZiPatchFile(Stream stream)
        {
            this._stream = stream;

            var reader = new BinaryReader(stream);
            if (ZIPATCH_MAGIC.Any(magic => magic != reader.ReadUInt32()))
                throw new ZiPatchException();
        }

        /// <summary>
        /// Instantiates a ZiPatchFile from a file path
        /// </summary>
        /// <param name="filepath">Path to patch file</param>
        public static ZiPatchFile FromFileName(string filepath)
        {
            var stream = SqexFileStream.WaitForStream(filepath, FileMode.Open);
            return new ZiPatchFile(stream);
        }



        public IEnumerable<ZiPatchChunk> GetChunks()
        {
            ZiPatchChunk chunk;
            do
            {
                chunk = ZiPatchChunk.GetChunk(_stream);

                yield return chunk;
            } while (chunk.ChunkType != EndOfFileChunk.Type);
        }

        public void Dispose()
        {
            _stream?.Dispose();
        }
    }
}