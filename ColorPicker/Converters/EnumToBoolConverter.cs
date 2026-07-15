using System.Globalization;
using System.Windows.Data;

namespace ColorPicker.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if ((bool)value)
            return Enum.Parse(targetType, parameter.ToString()!);
            
        return Binding.DoNothing;
    }
}
