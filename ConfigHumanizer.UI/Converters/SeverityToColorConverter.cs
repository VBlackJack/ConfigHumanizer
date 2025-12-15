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
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Severity severity)
        {
            return severity switch
            {
                Severity.CriticalSecurity => new SolidColorBrush(Colors.LightPink),
                Severity.Warning => new SolidColorBrush(Colors.LightYellow),
                Severity.GoodPractice => new SolidColorBrush(Colors.LightGreen),
                Severity.Info => new SolidColorBrush(Colors.LightBlue),
                _ => new SolidColorBrush(Colors.White)
            };
        }

        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
