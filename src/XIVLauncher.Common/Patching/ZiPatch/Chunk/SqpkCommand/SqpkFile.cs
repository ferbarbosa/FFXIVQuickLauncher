﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using XIVLauncher.Common.Patching.Util;
using XIVLauncher.Common.Patching.ZiPatch.Util;

namespace XIVLauncher.Common.Patching.ZiPatch.Chunk.SqpkCommand
{
    class SqpkFile : SqpkChunk
    {
        public new static string Command = "F";

        public enum OperationKind : byte
        {
            AddFile = (byte)'A',
            RemoveAll = (byte)'R',
            // I've seen no cases in the wild of these two
            DeleteFile = (byte)'D',
            MakeDirTree = (byte)'M'
        }

        public OperationKind Operation { get; protected set; }
        public long FileOffset { get; protected set; }
        public ulong FileSize { get; protected set; }
        public ushort ExpansionId { get; protected set; }
        public SqexFile TargetFile { get; protected set; }

        public List<long> CompressedDataSourceOffsets { get; protected set; }
        public List<SqpkCompressedBlock> CompressedData { get; protected set; }

        public SqpkFile(ChecksumBinaryReader reader, int offset, int size) : base(reader, offset, size) {}

        protected override void ReadChunk()
        {
            var start = reader.BaseStream.Position;

            Operation = (OperationKind)reader.ReadByte();
            reader.ReadBytes(2); // Alignment

            FileOffset = reader.ReadInt64BE();
            FileSize = reader.ReadUInt64BE();

            var pathLen = reader.ReadUInt32BE();

            ExpansionId = reader.ReadUInt16BE();
            reader.ReadBytes(2);

            TargetFile = new SqexFile(reader.ReadFixedLengthString(pathLen));

            if (Operation == OperationKind.AddFile)
            {
                CompressedDataSourceOffsets = new();
                CompressedData = new List<SqpkCompressedBlock>();

                while (Size - reader.BaseStream.Position + start > 0)
                {
                    CompressedDataSourceOffsets.Add(Offset + reader.BaseStream.Position);
                    CompressedData.Add(new SqpkCompressedBlock(reader));
                    CompressedDataSourceOffsets[CompressedDataSourceOffsets.Count - 1] += CompressedData[CompressedData.Count - 1].HeaderSize;
                }
            }

            reader.ReadBytes(Size - (int)(reader.BaseStream.Position - start));
        }

        private static bool RemoveAllFilter(string filePath) =>
            !new[] { ".var", "00000.bk2", "00001.bk2", "00002.bk2", "00003.bk2" }.Any(filePath.EndsWith);

        public override void ApplyChunk(ZiPatchConfig config)
        {
            switch (Operation)
            {
                // Default behaviour falls through to AddFile, though this shouldn't happen
                case OperationKind.AddFile:
                default:
                    // TODO: Check this. I *think* boot usually creates all the folders like sqpack, movie, etc., so this might be kind of a hack
                    TargetFile.CreateDirectoryTree(config.GamePath);

                    var fileStream = config.Store == null ?
                        TargetFile.OpenStream(config.GamePath, FileMode.OpenOrCreate) :
                        TargetFile.OpenStream(config.Store, config.GamePath, FileMode.OpenOrCreate);

                    if (FileOffset == 0)
                        fileStream.SetLength(0);

                    fileStream.Seek(FileOffset, SeekOrigin.Begin);
                    foreach (var block in CompressedData)
                        block.DecompressInto(fileStream);

                    break;
                case OperationKind.RemoveAll:
                    foreach (var file in SqexFile.GetAllExpansionFiles(config.GamePath, ExpansionId).Where(RemoveAllFilter))
                        File.Delete(file);
                    break;
                case OperationKind.DeleteFile:
                    File.Delete(config.GamePath + "/" + TargetFile.RelativePath);
                    break;
                case OperationKind.MakeDirTree:
                    Directory.CreateDirectory(config.GamePath + "/" + TargetFile.RelativePath);
                    break;
            }
        }

        public override string ToString()
        {
            return $"{Type}:{Command}:{Operation}:{FileOffset}:{FileSize}:{ExpansionId}:{TargetFile}";
        }
    }
}