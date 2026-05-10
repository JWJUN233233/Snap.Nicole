using Microsoft.Extensions.Options;

namespace Snap.Nicole.Services.Settings;

internal interface IOptionsProvider<out T> : IOptionsMonitor<T>, IOptionsWriter<T>
    where T : class;