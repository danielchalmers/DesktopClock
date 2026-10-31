using System.Globalization;
using System.Reflection;
using System.Threading;
using Xunit.Sdk;

namespace DesktopClock.Tests;

/// <summary>
/// Runs a test with the given UI language so text assertions don't depend on the machine's display language.
/// </summary>
public sealed class UseUICultureAttribute(string culture) : BeforeAfterTestAttribute
{
    private CultureInfo _originalCulture;

    public override void Before(MethodInfo methodUnderTest)
    {
        _originalCulture = Thread.CurrentThread.CurrentUICulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
    }

    public override void After(MethodInfo methodUnderTest)
    {
        Thread.CurrentThread.CurrentUICulture = _originalCulture;
    }
}
