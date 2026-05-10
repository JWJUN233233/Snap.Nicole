namespace Snap.Nicole.Services.Settings;

internal interface IOptionsWriter<out T>
    where T : class
{
    void Update();
}
