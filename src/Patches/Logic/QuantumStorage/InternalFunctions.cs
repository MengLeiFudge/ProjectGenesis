﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using ProjectGenesis.Utils;

namespace ProjectGenesis.Patches.Logic.QuantumStorage
{
    public static partial class QuantumStoragePatches
    {
        private const int QuantumStorageSize = 90;

        private static StorageComponent _component;

        private static readonly ConcurrentDictionary<int, List<int>> QuantumStorageIds;

        static QuantumStoragePatches()
        {
            QuantumStorageIds = new ConcurrentDictionary<int, List<int>>();
            _component = new StorageComponent(QuantumStorageSize);
            _component.CutNext();
        }

        internal static void SyncNewQuantumStorage(int planetId, int storageid)
        {
            QuantumStorageIds.TryAddOrInsert(planetId, storageid);
            PlanetData planet = GameMain.galaxy.PlanetById(planetId);
            FactoryStorage factoryStorage = GameMain.data.GetOrCreateFactory(planet).factoryStorage;
            factoryStorage.storagePool[storageid] = _component;
        }

        internal static void SyncRemoveQuantumStorage(int planetId, int storageid) => QuantumStorageIds.TryRemove(planetId, storageid);

        internal static void ExportPlanetQuantumStorage(int planetId, BinaryWriter w)
        {
            if (!QuantumStorageIds.ContainsKey(planetId)) QuantumStorageIds[planetId] = new List<int>();

            List<int> datas = QuantumStorageIds[planetId];
            w.Write(datas.Count);
            w.Write(planetId);

            foreach (int id in datas) w.Write(id);
        }

        internal static void ImportPlanetQuantumStorage(BinaryReader r)
        {
            int count = r.ReadInt32();
            int planetId = r.ReadInt32();

            var arr = new int[count];

            for (var j = 0; j < count; j++) arr[j] = r.ReadInt32();

            QuantumStorageIds[planetId] = new List<int>(arr);
        }

        public static bool Import_PatchMethod(FactoryStorage storage, int index)
        {
            bool b = QuantumStorageIds.Contains(storage.planet.id, index);

            if (b) storage.storagePool[index] = _component;

            return b;
        }

        internal static void Export(BinaryWriter w)
        {
            lock (QuantumStorageIds)
            {
                w.Write(QuantumStorageIds.Count);

                foreach (KeyValuePair<int, List<int>> pair in QuantumStorageIds)
                {
                    w.Write(pair.Key);
                    w.Write(pair.Value.Count);

                    foreach (int t in pair.Value) w.Write(t);
                }
            }

            lock (_component) _component.Export(w);
        }

        internal static void Import(BinaryReader r)
        {
            ReInitAll();

            try
            {
                int storagecount = r.ReadInt32();

                for (var j = 0; j < storagecount; j++)
                {
                    int key = r.ReadInt32();
                    int length = r.ReadInt32();
                    var datas = new List<int>();

                    for (var i = 0; i < length; i++) datas.Add(r.ReadInt32());

                    QuantumStorageIds.TryAdd(key, datas);
                }

                _component.Import(r);
            }
            catch (EndOfStreamException)
            {
                // ignored
            }
        }

        internal static void IntoOtherSave() => ReInitAll();

        private static void ReInitAll()
        {
            QuantumStorageIds.Clear();
            _component = new StorageComponent(QuantumStorageSize);
            _component.CutNext();
        }
    }
}
