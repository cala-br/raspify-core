using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Text.Json;

#nullable enable

namespace RaspifyCore
{
    record CurrentTrack (
        string Name,
        List<string> Artists,

        string AlbumName,
        List<Image> AlbumImages,

        TimeSpan Duration,
        TimeSpan Progress
    ) {
        public static CurrentTrack From(CurrentlyPlaying currentlyPlaying)
        {
            return From(
                track: currentlyPlaying.Item as FullTrack,
                progress: currentlyPlaying.ProgressMs
            );
        }


        public static CurrentTrack From(FullTrack? track, int? progress)
        {
            if (!progress.HasValue)
                throw new ArgumentNullException(nameof(progress));

            return From(track) with
            {
                Progress = TimeSpan.FromMilliseconds(progress.Value),
            };
        }


        public static CurrentTrack From(FullTrack? track)
        {
            if (track is null)
                throw new ArgumentNullException(nameof(track));

            return new(
                track.Name,
                track.GetArtistNames(),
                track.Album.Name,
                track.Album.Images,
                TimeSpan.FromMilliseconds(track.DurationMs),
                TimeSpan.FromMilliseconds(0)
            );
        }


        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new()
            {
                WriteIndented = true,
            });
        }
    }
}
