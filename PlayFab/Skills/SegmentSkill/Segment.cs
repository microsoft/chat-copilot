// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace PlayFab.Skills.SegmentSkill;

internal class Segment
{
    [JsonProperty("SegmentId")]
    public string Id { get; set; }

    [JsonProperty("Name")]
    public string Name { get; set; }
}

