using System.Threading.Tasks;

namespace Snap.Nicole.Core.Hosting;

internal interface IApplicationLifeTime
{
    Task ShowdownAsync();
}