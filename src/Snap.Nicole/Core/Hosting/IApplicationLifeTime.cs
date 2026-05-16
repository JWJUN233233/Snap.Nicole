using System.Threading.Tasks;

namespace Snap.Nicole.Core.Hosting;

internal interface IApplicationLifeTime
{
    bool IsExiting { get; }

    Task ShutdownAsync();
}