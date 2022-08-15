﻿using ACCManager.Data.ACC.Tracks;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ACCManager.Data.ACC.Tracks.TrackNames;

namespace ACCManager.Data.ACC.Database.GameData
{
    public class DbTrackData
    {
        public Guid Id { get; set; }

        public string ParseName { get; set; }
    }

    public class TrackDataCollection
    {
        private static ILiteCollection<DbTrackData> _collection;
        private static ILiteCollection<DbTrackData> Collection
        {
            get
            {
                if (_collection == null)
                    _collection = RaceWeekendDatabase.Database.GetCollection<DbTrackData>();

                return _collection;
            }
        }

        public static List<DbTrackData> GetAll()
        {
            var allTracks = Collection.FindAll();
            if (!allTracks.Any()) return new List<DbTrackData>();

            return allTracks.OrderBy(x => x.ParseName).ToList();
        }

        public static DbTrackData GetTrackData(Guid id)
        {
            var collection = RaceWeekendDatabase.Database.GetCollection<DbTrackData>();
            return Collection.FindById(id);
        }

        public static DbTrackData GetTrackData(string trackParseName)
        {
            var result = Collection.FindOne(x => x.ParseName == trackParseName);
            if (result == null)
            {
                TrackNames.Tracks.TryGetValue(trackParseName, out TrackData trackData);
                Insert(new DbTrackData() { ParseName = trackParseName, Id = trackData.Guid });
                result = Collection.FindOne(x => x.ParseName == trackParseName);
            }

            return result;
        }

        public static void Insert(DbTrackData track)
        {
            RaceWeekendDatabase.Database.BeginTrans();
            Collection.EnsureIndex(x => x.Id, true);
            Collection.Insert(track);
            RaceWeekendDatabase.Database.Commit();
            Debug.WriteLine($"Inserted new track data {track.Id} {track.ParseName}");

        }
    }
}
