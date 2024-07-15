using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClock.Properties;
public sealed class BounceSettings : INotifyPropertyChanged
{

#pragma warning disable CS0067 // The event 'Settings.PropertyChanged' is never used
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067 // The event 'Settings.PropertyChanged' is never used


    public bool Enabled { get; set; } = false;

    public double HorizontalBounce { get; set; } = 100.0;
    public double VerticalBounce { get; set; } = 50.0;


    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(2);


}
