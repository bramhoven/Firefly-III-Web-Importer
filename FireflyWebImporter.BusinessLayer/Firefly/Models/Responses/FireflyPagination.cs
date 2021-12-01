﻿using Newtonsoft.Json;

namespace FireflyWebImporter.BusinessLayer.Firefly.Models.Responses
{
    public class FireflyPagination
    {
        #region Properties

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }

        [JsonProperty("per_page")]
        public int PerPage { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("total_pages")]
        public int TotalPages { get; set; }

        #endregion
    }
}