using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopClock.Tests;

public class ColorTextConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_ShowsTheColorInFull()
    {
        Assert.Equal("#FFFF0000", new ColorTextConverter().Convert(Colors.Red, typeof(string), null, Culture));
    }

    [Theory]
    [InlineData("#FF0")]
    [InlineData("#FF0000")]
    [InlineData("Red")]
    public void Convert_KeepsWhatWasTypedForTheSameColor(string text)
    {
        var converter = new ColorTextConverter();
        var color = converter.ConvertBack(text, typeof(Color), null, Culture);

        Assert.Equal(text, converter.Convert(color, typeof(string), null, Culture));
    }

    [Fact]
    public void Convert_ShowsAColorPickedElsewhereInFull()
    {
        var converter = new ColorTextConverter();
        converter.ConvertBack("#FF0000", typeof(Color), null, Culture);

        Assert.Equal("#FF0000FF", converter.Convert(Colors.Blue, typeof(string), null, Culture));
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("#FF000")]
    [InlineData("#GG0000")]
    [InlineData("Reddish")]
    public void ConvertBack_RejectsUnreadableText(string text)
    {
        Assert.Equal(DependencyProperty.UnsetValue, new ColorTextConverter().ConvertBack(text, typeof(Color), null, Culture));
    }

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#80FF0000")]
    [InlineData("#00f")]
    public void TypingAColorKeyByKey_KeepsWhatWasTyped(string typed)
    {
        var (text, color, invalid) = InColorBox(Colors.Black, box =>
        {
            box.SelectAll();
            foreach (var key in typed)
                box.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, key.ToString())) { RoutedEvent = UIElement.TextInputEvent });
        });

        Assert.Equal(typed, text);
        Assert.Equal((Color)ColorConverter.ConvertFromString(typed), color);
        Assert.False(invalid);
    }

    [Fact]
    public void Backspace_RemovesOneCharacterAtATime()
    {
        var (text, color, _) = InColorBox(Colors.Red, box =>
        {
            box.CaretIndex = box.Text.Length;
            for (var i = 0; i < 4; i++)
                EditingCommands.Backspace.Execute(null, box);
        });

        // #FFFF0000 less four characters is #FFFF, which is white.
        Assert.Equal("#FFFF", text);
        Assert.Equal(Colors.White, color);
    }

    /// <summary>
    /// Runs the edits in a box bound to a color the way Settings binds its color boxes, then returns what the box shows, the color, and whether the box is marked invalid.
    /// </summary>
    private static (string Text, Color Color, bool Invalid) InColorBox(Color start, Action<TextBox> edit)
    {
        (string, Color, bool) result = default;
        Exception error = null;

        var thread = new Thread(() =>
        {
            try
            {
                var model = new ColorModel { Color = start };
                var box = new TextBox();
                box.SetBinding(TextBox.TextProperty, new Binding(nameof(ColorModel.Color))
                {
                    Source = model,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                    Converter = new ColorTextConverter(),
                });

                edit(box);
                result = (box.Text, model.Color, Validation.GetHasError(box));
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error != null)
            throw new Exception("Editing the box failed.", error);

        return result;
    }

    // Like Settings, which only raises PropertyChanged when the value changes.
    private sealed class ColorModel : INotifyPropertyChanged
    {
        private Color _color;

        public event PropertyChangedEventHandler PropertyChanged;

        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value)
                    return;

                _color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            }
        }
    }
}
