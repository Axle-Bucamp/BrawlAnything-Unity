using System.Collections.Generic;
using UnityEngine;

namespace BrawlAnything.Social
{
    public class LeaderboardManager : MonoBehaviour
    {
        [System.Serializable]
        public class AudioMetaData
        {
            public string Title;
            public string Artist;
            public float Length;
            public string Genre;
            public string TierAssociation; // Optional link to rank tiers

            public AudioMetaData(string title, string artist, float length, string genre, string tier)
            {
                Title = title;
                Artist = artist;
                Length = length;
                Genre = genre;
                TierAssociation = tier;
            }
        }

        // Music database tied to rank tiers or player ranks
        private Dictionary<AudioClip, AudioMetaData> musicDatabase = new Dictionary<AudioClip, AudioMetaData>();

        public void AddTrack(AudioClip clip, string title, string artist, float length, string genre, string tier)
        {
            if (clip == null) return;

            var meta = new AudioMetaData(title, artist, length, genre, tier);
            musicDatabase[clip] = meta;
        }

        public AudioMetaData GetTrackMetadata(AudioClip clip)
        {
            if (clip != null && musicDatabase.TryGetValue(clip, out var meta))
            {
                return meta;
            }

            return null;
        }

        public List<AudioClip> GetTracksByTier(string tierId)
        {
            var tracks = new List<AudioClip>();
            foreach (var kvp in musicDatabase)
            {
                if (kvp.Value.TierAssociation == tierId)
                {
                    tracks.Add(kvp.Key);
                }
            }
            return tracks;
        }
    }
}
