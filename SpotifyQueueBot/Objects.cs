using System.Collections.Generic;

namespace blurr.spotifybot
{
    public class SearchResult
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
    }

    public class SongRequest
    {
        public List<SearchResult> Results { get; set; }
        public ulong UserId { get; set; }
    }

    public class QueueItem
    {
        public string SongName { get; set; }
        public string RequestedBy { get; set; }
        public string SongUri { get; set; }
    }
}
