// Copyright 2025 Julien Bombled
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ConfigHumanizer.Core.Models;

namespace ConfigHumanizer.UI.Converters;

public class SeverityToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush CriticalBrush = new((Color)ColorConverter.ConvertFromString("#FFEBEE"));
    private static readonly SolidColorBrush WarningBrush = new((Color)ColorConverter.ConvertFromString("#FFF8E1"));
    private static readonly SolidColorBrush GoodBrush = new((Color)ColorConverter.ConvertFromString("#E8F5E9"));
    private static readonly SolidColorBrush InfoBrush = new((Color)ColorConverter.ConvertFromString("#E3F2FD"));
    private static readonly SolidColorBrush DefaultBrush = new(Colors.White);

    static SeverityToColorConverter()
    {
        CriticalBrush.Freeze();
        WarningBrush.Freeze();
        GoodBrush.Freeze();
        InfoBrush.Freeze();
        DefaultBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Severity severity)
        {
            return severity switch
            {
                Severity.CriticalSecurity => CriticalBrush,
                Severity.Warning => WarningBrush,
                Severity.GoodPractice => GoodBrush,
                Severity.Info => InfoBrush,
                _ => DefaultBrush
            };
        }

        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorString && !string.IsNullOrEmpty(colorString))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorString);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns Visible when value is 0 (for empty state), Collapsed otherwise.
/// </summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts Severity to a French text label.
/// </summary>
public class SeverityToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Severity severity)
        {
            return severity switch
            {
                Severity.CriticalSecurity => "CRITIQUE",
                Severity.Warning => "ALERTE",
                Severity.GoodPractice => "BON",
                Severity.Info => "INFO",
                _ => ""
            };
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts Severity to an emoji icon.
/// </summary>
public class SeverityToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Severity severity)
        {
            return severity switch
            {
                Severity.CriticalSecurity => "🔴",
                Severity.Warning => "🟠",
                Severity.GoodPractice => "🟢",
                Severity.Info => "🔵",
                _ => "⚪"
            };
        }
        return "⚪";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts Severity to a strong accent color for borders/indicators.
/// </summary>
public class SeverityToBorderColorConverter : IValueConverter
{
    private static readonly SolidColorBrush CriticalBrush = new((Color)ColorConverter.ConvertFromString("#E74C3C"));
    private static readonly SolidColorBrush WarningBrush = new((Color)ColorConverter.ConvertFromString("#F39C12"));
    private static readonly SolidColorBrush GoodBrush = new((Color)ColorConverter.ConvertFromString("#27AE60"));
    private static readonly SolidColorBrush InfoBrush = new((Color)ColorConverter.ConvertFromString("#3498DB"));
    private static readonly SolidColorBrush DefaultBrush = new(Colors.Gray);

    static SeverityToBorderColorConverter()
    {
        CriticalBrush.Freeze();
        WarningBrush.Freeze();
        GoodBrush.Freeze();
        InfoBrush.Freeze();
        DefaultBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Severity severity)
        {
            return severity switch
            {
                Severity.CriticalSecurity => CriticalBrush,
                Severity.Warning => WarningBrush,
                Severity.GoodPractice => GoodBrush,
                Severity.Info => InfoBrush,
                _ => DefaultBrush
            };
        }
        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts Severity to badge background color.
/// </summary>
public class SeverityToBadgeColorConverter : IValueConverter
{
    private static readonly SolidColorBrush CriticalBrush = new((Color)ColorConverter.ConvertFromString("#FDECEA"));
    private static readonly SolidColorBrush WarningBrush = new((Color)ColorConverter.ConvertFromString("#FEF5E7"));
    private static readonly SolidColorBrush GoodBrush = new((Color)ColorConverter.ConvertFromString("#E8F8F0"));
    private static readonly SolidColorBrush InfoBrush = new((Color)ColorConverter.ConvertFromString("#EBF5FB"));
    private static readonly SolidColorBrush DefaultBrush = new((Color)ColorConverter.ConvertFromString("#F5F5F5"));

    static SeverityToBadgeColorConverter()
    {
        CriticalBrush.Freeze();
        WarningBrush.Freeze();
        GoodBrush.Freeze();
        InfoBrush.Freeze();
        DefaultBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Severity severity)
        {
            return severity switch
            {
                Severity.CriticalSecurity => CriticalBrush,
                Severity.Warning => WarningBrush,
                Severity.GoodPractice => GoodBrush,
                Severity.Info => InfoBrush,
                _ => DefaultBrush
            };
        }
        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
