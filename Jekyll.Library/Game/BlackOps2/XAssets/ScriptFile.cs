﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace JekyllLibrary.Library
{
    public partial class BlackOps2
    {
        public class ScriptFile : IXAssetPool
        {
            public override string Name => "Script File";

            public override int Index => (int)XAssetType.ASSET_TYPE_SCRIPTPARSETREE;

            /// <summary>
            /// Structure of a Black Ops II ScriptParseTree.
            /// </summary>
            private struct ScriptParseTree
            {
                public uint Name { get; set; }
                public int Len { get; set; }
                public uint Buffer { get; set; }
            }

            /// <summary>
            /// Load the valid XAssets for the ScriptFile XAsset Pool.
            /// </summary>
            /// <param name="instance"></param>
            /// <returns>List of ScriptFile XAsset objects.</returns>
            public override List<GameXAsset> Load(JekyllInstance instance)
            {
                List<GameXAsset> results = new List<GameXAsset>();

                Entries = instance.Reader.ReadStruct<uint>(instance.Game.DBAssetPools + (Marshal.SizeOf<DBAssetPool>() * Index));
                PoolSize = instance.Reader.ReadStruct<uint>(instance.Game.DBAssetPoolSizes + (Marshal.SizeOf<DBAssetPoolSize>() * Index));

                for (int i = 0; i < PoolSize; i++)
                {
                    ScriptParseTree header = instance.Reader.ReadStruct<ScriptParseTree>(Entries + Marshal.SizeOf<DBAssetPool>() + (i * Marshal.SizeOf<ScriptParseTree>()));

                    if (IsNullXAsset(header.Name))
                    {
                        continue;
                    }
                    else if (header.Len == 0)
                    {
                        continue;
                    }

                    results.Add(new GameXAsset()
                    {
                        Name = instance.Reader.ReadNullTerminatedString(header.Name),
                        Type = Name,
                        Size = ElementSize,
                        XAssetPool = this,
                        HeaderAddress = Entries + Marshal.SizeOf<DBAssetPool>() + (i * Marshal.SizeOf<ScriptParseTree>()),
                    });
                }

                return results;
            }

            /// <summary>
            /// Exports the specified ScriptFile XAsset.
            /// </summary>
            /// <param name="xasset"></param>
            /// <param name="instance"></param>
            /// <returns>Status of the export operation.</returns>
            public override JekyllStatus Export(GameXAsset xasset, JekyllInstance instance)
            {
                ScriptParseTree header = instance.Reader.ReadStruct<ScriptParseTree>(xasset.HeaderAddress);

                if (xasset.Name != instance.Reader.ReadNullTerminatedString(header.Name))
                {
                    return JekyllStatus.MemoryChanged;
                }

                try
                {
                    string path = Path.Combine(instance.ExportPath, xasset.Name);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    byte[] buffer = instance.Reader.ReadBytes(header.Buffer, header.Len);
                    File.WriteAllBytes(path, buffer);
                }
                catch
                {
                    return JekyllStatus.Exception;
                }

                Console.WriteLine($"Exported {xasset.Type} {xasset.Name}");

                return JekyllStatus.Success;
            }
        }
    }
}