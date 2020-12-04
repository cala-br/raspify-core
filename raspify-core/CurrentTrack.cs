using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static implicit operator CurrentTrack(CurrentlyPlaying currentlyPlaying)
        {
            if (currentlyPlaying.Item is not FullTrack)
                throw new ArgumentException($"{nameof(currentlyPlaying)}: item must be a full track");

            var track = currentlyPlaying.Item as FullTrack;
            
            var progress =
                currentlyPlaying.ProgressMs!.Value;
                
            var artists = track!
                .Artists
                .Select(artist => artist.Name)
                .ToList();

            return new(
                track.Name,
                artists,
                track.Album.Name,
                track.Album.Images,
                TimeSpan.FromMilliseconds(track.DurationMs),
                TimeSpan.FromMilliseconds(progress)
            );
        }

        public override string ToString()
        {
            var albumImages = AlbumImages
                .Select(image => $"\n\t\t\t({image.Width}, {image.Height}) | {image.Url}");

            return $@"
                Name: {Name}
                Artists: {string.Join(", ", Artists)}
                Duration: {Duration}
                Progress: {Progress}
                    
                Album: {AlbumName}
                Covers: {string.Join(' ', albumImages)}
            ";
        }
    }
}
