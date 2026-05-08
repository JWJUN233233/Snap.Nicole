namespace Snap.Nicole.Services.Settings;

internal interface IOptionsWriter<in T>
    where T : class
{
    void Update();
}
