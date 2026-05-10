using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI.Models;
using System;
using System.Collections.Generic;

namespace Snap.Nicole.Services.Settings;

internal sealed class AppSettings
{
    public string Language { get; set; } = StringResourceProxy.SupportedCultures[0];

    public IList<ModelProfile> ModelProfiles { get; set; } = [];

    public Guid? SelectedModelProfileId { get; set; }
}
